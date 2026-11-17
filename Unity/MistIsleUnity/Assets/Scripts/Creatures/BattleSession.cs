using System;
using System.Collections.Generic;
using MistIsle.Items;
using UnityEngine;

namespace MistIsle.Creatures
{
    public enum BattleAction { Attack, Defend, Capture, Feed, Flee, Continue, Skill, Resonance }
    public enum BattleOutcome { Ongoing, Victory, Defeat, Captured, Escaped }
    public enum BattleKind { Wild, Guardian, FinalBoss }
    public enum FinalBossStage { None, Charging, Silence, Awakened }

    public readonly struct BattleActionResult
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly BattleOutcome Outcome;
        public BattleActionResult(bool success, string message, BattleOutcome outcome)
        { Success = success; Message = message; Outcome = outcome; }
    }

    // Owns one encounter. UI chooses actions; only this class changes combat state.
    public sealed class BattleSession
    {
        private readonly PartyRoster party;
        private readonly IReadOnlyDictionary<string, CreatureSpecies> catalog;
        private readonly Inventory inventory;
        private readonly Func<float> rng;
        private readonly List<CreatureInstance> participants = new List<CreatureInstance>();
        private readonly List<CreatureInstance> contributors = new List<CreatureInstance>();
        private readonly List<CreatureInstance> pending = new List<CreatureInstance>();
        private readonly List<string> log = new List<string>();
        private readonly HashSet<string> guarding = new HashSet<string>();
        private readonly Dictionary<string, int> skillReadyRound = new Dictionary<string, int>();
        private int round = 1;
        private int chargeRound;
        private int weakenedRounds;
        private bool rooted;
        private float foodTrust;
        private bool rewardsGranted;

        public CreatureInstance Wild { get; }
        public CreatureSpecies WildSpecies { get; }
        public BattleKind Kind { get; }
        public bool IsCaptureLesson { get; }
        public bool CaptureLessonWeakened => 1f - (float)Wild.currentHp / CreatureInstance.MaxHp(WildSpecies, Wild.level) >= .25f;
        public BattleOutcome Outcome { get; private set; }
        public FinalBossStage FinalStage { get; private set; }
        public IReadOnlyList<string> Log => log;
        public string WildStatus => rooted ? "缠绕" : weakenedRounds > 0 ? "弱化" : foodTrust > 0 ? "平静" : "正常";
        public CreatureInstance CurrentActor => FinalStage == FinalBossStage.Silence || Outcome != BattleOutcome.Ongoing
            ? null : pending.Find(x => x.currentHp > 0);

        public BattleSession(PartyRoster party, IReadOnlyDictionary<string, CreatureSpecies> catalog,
            Inventory inventory, CreatureInstance wild, Func<float> rng = null,
            BattleKind kind = BattleKind.Wild, int battleLevel = 0, bool captureLesson = false)
        {
            this.party = party ?? throw new ArgumentNullException(nameof(party));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Wild = wild ?? throw new ArgumentNullException(nameof(wild));
            if (!catalog.TryGetValue(wild.speciesId, out var species))
                throw new ArgumentException("Wild creature species is not in the catalog.", nameof(wild));
            WildSpecies = species;
            Kind = kind;
            IsCaptureLesson = captureLesson && kind == BattleKind.Wild && species.capturable;
            this.rng = rng ?? (() => UnityEngine.Random.value);
            if (battleLevel > 0)
            { Wild.level = Mathf.Clamp(battleLevel, 1, GrowthRules.LevelCap); Wild.currentHp = CreatureInstance.MaxHp(species, Wild.level); }
            foreach (var member in party.Deployed)
                if (catalog.ContainsKey(member.speciesId)) participants.Add(member);
            contributors.AddRange(participants);
            EnsureLivingDeployment();
            BeginRound();
            if (pending.Count == 0) Outcome = BattleOutcome.Defeat;
            log.Add("遭遇 " + WildSpecies.displayName + "！每只出战伙伴依次行动后，对手回应。");
        }

        public BattleActionResult TryAct(BattleAction action, string itemId = null)
        {
            if (Outcome != BattleOutcome.Ongoing) return Result(false, "这场遭遇已经结束。");
            int firstMessage = log.Count;
            if (FinalStage == FinalBossStage.Silence)
            {
                if (action != BattleAction.Continue) return Result(false, "静默中，初始伙伴正在回应旅人的呼唤。");
                AwakenStarter();
                return NewMessages(firstMessage);
            }
            var actor = CurrentActor;
            if (actor == null) { Outcome = BattleOutcome.Defeat; return Result(false, "出战伙伴已经全部倒下。"); }
            var actorSpecies = catalog[actor.speciesId];
            switch (action)
            {
                case BattleAction.Attack:
                    DamageWild(actor, actorSpecies, 1f, "攻击");
                    break;
                case BattleAction.Skill:
                    if (skillReadyRound.TryGetValue(actor.instanceId, out int ready) && round < ready)
                        return Result(false, "技能正在恢复，下轮可再次使用。");
                    UseSkill(actor, actorSpecies);
                    skillReadyRound[actor.instanceId] = round + 2;
                    break;
                case BattleAction.Defend:
                    guarding.Add(actor.instanceId);
                    log.Add(actor.nickname + "专注防守，本轮受到的伤害降低。");
                    break;
                case BattleAction.Resonance:
                    if (Kind != BattleKind.FinalBoss || FinalStage != FinalBossStage.Awakened || !actor.isStarter)
                        return Result(false, "归航回响只能由终局觉醒后的初始伙伴使用。");
                    DamageWild(actor, actorSpecies, 2.8f, "归航回响");
                    break;
                case BattleAction.Capture:
                case BattleAction.Feed:
                    if (IsCaptureLesson && !CaptureLessonWeakened)
                        return Result(false, "拾光轻声提醒：它还在乱撞。先让伙伴轻轻牵制一下，再慢慢靠近。");
                    if (Kind != BattleKind.Wild || !WildSpecies.capturable)
                        return Result(false, "封印守卫和最终守卫不能被捕获或投喂收服。");
                    if (party.Count >= PartyRoster.Capacity)
                        return Result(false, "随身队伍已满；请返回营地调整后再捕获。");
                    bool feeding = action == BattleAction.Feed;
                    string usedItem = itemId ?? (feeding ? "berry-treat" : "resonance-capsule");
                    if ((!feeding && usedItem != "resonance-capsule")
                        || (feeding && usedItem != "berry" && usedItem != "berry-treat"))
                        return Result(false, "这个物品不能用于当前行动。");
                    if (!inventory.TryConsume(usedItem)) return Result(false, "没有可用的" + GameCatalog.ItemName(usedItem) + "。");
                    float affinity = WildSpecies.preferredFoodIds.Contains(usedItem) ? 1f
                        : WildSpecies.dislikedFoodIds.Contains(usedItem) ? 0f : 0.35f;
                    var context = new CaptureContext(1f - (float)Wild.currentHp / CreatureInstance.MaxHp(WildSpecies, Wild.level),
                        affinity + foodTrust, rooted || weakenedRounds > 0 ? 0.6f : 0f, 0.65f,
                        WildSpecies.temperamentResistance);
                    bool captured = (IsCaptureLesson && CaptureLessonWeakened)
                        || Mathf.Clamp01(rng()) < CaptureCalculator.Calculate(context, feeding);
                    if (captured)
                    {
                        Wild.currentHp = Mathf.Max(1, Wild.currentHp);
                        Wild.bond = feeding ? 8 : 2;
                        if (!party.TryAdd(Wild)) throw new InvalidOperationException("Capture party changed during action.");
                        Outcome = BattleOutcome.Captured;
                        log.Add(WildSpecies.displayName + "回应了共鸣，加入随身队伍！");
                        GrantRewards();
                        return NewMessages(firstMessage);
                    }
                    if (feeding) foodTrust = Mathf.Min(0.4f, foodTrust + 0.12f);
                    log.Add(feeding ? WildSpecies.displayName + "吃下了食物，警惕稍有缓和，但还没有加入。"
                        : "共鸣未能稳定；削弱对手或施加状态会提高下一次机会。");
                    break;
                case BattleAction.Flee:
                    if (Kind != BattleKind.Wild) return Result(false, "封印之战无法逃跑；请谨慎行动。");
                    Outcome = BattleOutcome.Escaped;
                    log.Add("你带着伙伴安全退回了探索路线。");
                    return NewMessages(firstMessage);
                default:
                    return Result(false, "当前没有需要继续的剧情。");
            }
            if (Wild.currentHp <= 0)
            {
                Outcome = BattleOutcome.Victory;
                log.Add(WildSpecies.displayName + "停止了战斗。");
                GrantRewards();
                return NewMessages(firstMessage);
            }
            pending.Remove(actor);
            pending.RemoveAll(x => x.currentHp <= 0);
            if (pending.Count == 0) WildTurn();
            return NewMessages(firstMessage);
        }

        public BattleActionResult TrySwap(string outgoingId, string incomingId)
        {
            if (Outcome != BattleOutcome.Ongoing || FinalStage == FinalBossStage.Silence)
                return Result(false, "当前不能更换出战伙伴。");
            var actor = CurrentActor;
            var outgoing = participants.Find(x => x.instanceId == outgoingId);
            var incoming = new List<CreatureInstance>(party.Members).Find(x => x.instanceId == incomingId);
            if (actor == null || outgoing == null || incoming == null || incoming.currentHp <= 0
                || participants.Contains(incoming) || !catalog.ContainsKey(incoming.speciesId))
                return Result(false, "请选择一只存活的后备伙伴。");
            if (outgoing != actor && outgoing.currentHp > 0)
                return Result(false, "当前行动伙伴可以换下自己，或协助替换一位倒下的队友。");
            int firstMessage = log.Count;
            int slot = participants.IndexOf(outgoing);
            participants[slot] = incoming;
            if (!contributors.Contains(incoming)) contributors.Add(incoming);
            pending.Remove(actor);
            pending.Remove(outgoing);
            guarding.Remove(outgoing.instanceId);
            SyncDeployment();
            log.Add(actor.nickname + "消耗本次行动，让后备伙伴" + incoming.nickname + "替换" + outgoing.nickname + "出战。");
            if (pending.Count == 0) WildTurn();
            return NewMessages(firstMessage);
        }

        private void UseSkill(CreatureInstance actor, CreatureSpecies species)
        {
            if (species.primaryElement == ElementType.Verdant)
            {
                int healing = Mathf.Max(5, actor.Attack(species) / 2);
                foreach (var ally in participants)
                    if (ally.currentHp > 0) ally.currentHp = Mathf.Min(
                        CreatureInstance.MaxHp(catalog[ally.speciesId], ally.level), ally.currentHp + healing);
                rooted = true;
                log.Add(actor.nickname + "施展藤光：回复出战伙伴，并缠绕对手一轮。");
                DamageWild(actor, species, 0.7f, "藤光");
            }
            else if (species.primaryElement == ElementType.Tide)
            {
                actor.currentHp = Mathf.Min(CreatureInstance.MaxHp(species, actor.level),
                    actor.currentHp + Mathf.Max(5, CreatureInstance.MaxHp(species, actor.level) / 4));
                weakenedRounds = 2;
                log.Add(actor.nickname + "施展潮汐：回复生命，对手攻击降低两轮。");
                DamageWild(actor, species, 0.9f, "潮汐");
            }
            else
            {
                DamageWild(actor, species, species.primaryElement == ElementType.Crystal ? 1.75f : 1.4f,
                    species.primaryElement == ElementType.Crystal ? "晶能爆发" : "共鸣技能");
            }
        }

        private void DamageWild(CreatureInstance actor, CreatureSpecies species, float power, string label)
        {
            float matchup = ElementChart.Multiplier(species.primaryElement, WildSpecies.primaryElement);
            int damage = Mathf.Max(1, Mathf.RoundToInt(
                (actor.Attack(species) * power - Wild.Defense(WildSpecies) * 0.35f) * matchup));
            Wild.currentHp = Mathf.Max(0, Wild.currentHp - damage);
            if (IsCaptureLesson) Wild.currentHp = Mathf.Max(1, Wild.currentHp);
            if (Kind == BattleKind.FinalBoss && FinalStage != FinalBossStage.Awakened)
                Wild.currentHp = Mathf.Max(1, Wild.currentHp);
            log.Add(IsCaptureLesson ? actor.nickname + "轻轻牵制它的动作，警戒降低了 " + damage + "。" :
                actor.nickname + "使用" + label + "，造成 " + damage + " 点伤害"
                + (matchup > 1f ? "（克制）" : matchup < 1f ? "（抵抗）" : "") + "。");
            if (Kind == BattleKind.FinalBoss && FinalStage == FinalBossStage.None
                && Wild.currentHp <= CreatureInstance.MaxHp(WildSpecies, Wild.level) * 0.6f)
            {
                FinalStage = FinalBossStage.Charging;
                chargeRound = round;
                log.Add("灯塔守卫开始蓄能！下一次回应将释放全岛共鸣冲击。");
            }
        }

        private void WildTurn()
        {
            if (Kind == BattleKind.FinalBoss && FinalStage == FinalBossStage.Charging && round > chargeRound)
            {
                foreach (var member in participants) member.currentHp = 0;
                FinalStage = FinalBossStage.Silence;
                log.Add("共鸣冲击落下，出战伙伴全部倒下。海雾归于静默……");
                pending.Clear();
                return;
            }
            if (Kind == BattleKind.FinalBoss && FinalStage == FinalBossStage.Charging)
            {
                log.Add("共鸣仍在聚集，伙伴们准备迎接冲击。");
                guarding.Clear();
                round++;
                BeginRound();
                return;
            }
            var living = participants.FindAll(x => x.currentHp > 0);
            if (living.Count == 0)
            {
                if (EnsureLivingDeployment()) { round++; BeginRound(); }
                else Outcome = BattleOutcome.Defeat;
                return;
            }
            if (rooted)
            { log.Add(WildSpecies.displayName + "被藤蔓缠绕，没能行动。"); rooted = false; }
            else
            {
                int targetIndex = Mathf.Min(living.Count - 1, Mathf.FloorToInt(Mathf.Clamp01(rng()) * living.Count));
                var target = living[targetIndex];
                var species = catalog[target.speciesId];
                float multiplier = ElementChart.Multiplier(WildSpecies.primaryElement, species.primaryElement);
                float reduction = (guarding.Contains(target.instanceId) ? 0.4f : 1f)
                    * (weakenedRounds > 0 ? 0.6f : 1f) * (foodTrust > 0 ? 0.9f : 1f);
                int damage = Mathf.Max(1, Mathf.RoundToInt(
                    (Wild.Attack(WildSpecies) - target.Defense(species) * 0.35f) * multiplier * reduction));
                target.currentHp = Mathf.Max(0, target.currentHp - damage);
                log.Add(WildSpecies.displayName + "回应了攻击，" + target.nickname + "受到 " + damage + " 点伤害。");
            }
            if (weakenedRounds > 0) weakenedRounds--;
            guarding.Clear();
            if (!EnsureLivingDeployment())
            { Outcome = BattleOutcome.Defeat; log.Add("所有随身伙伴均已倒下。返回营地休息后再来。"); return; }
            round++;
            BeginRound();
        }

        private void BeginRound()
        {
            pending.Clear();
            foreach (var member in participants)
                if (member.currentHp > 0) pending.Add(member);
        }

        private bool EnsureLivingDeployment()
        {
            if (participants.Exists(x => x.currentHp > 0)) return true;
            var reserves = new List<CreatureInstance>();
            foreach (var member in party.Members)
                if (member.currentHp > 0 && catalog.ContainsKey(member.speciesId) && !participants.Contains(member))
                    reserves.Add(member);
            if (reserves.Count == 0) return false;
            participants.Clear();
            foreach (var member in reserves)
            {
                if (participants.Count == PartyRoster.DeploymentCapacity) break;
                participants.Add(member);
                if (!contributors.Contains(member)) contributors.Add(member);
                log.Add("后备伙伴" + member.nickname + "接替倒下的队友出战。");
            }
            SyncDeployment();
            return true;
        }

        private void SyncDeployment()
        {
            var ids = new List<string>();
            foreach (var member in participants) ids.Add(member.instanceId);
            party.TrySetDeployment(ids, out _);
        }

        private void AwakenStarter()
        {
            var starter = new List<CreatureInstance>(party.Members).Find(x => x.isStarter);
            if (starter == null)
            { Outcome = BattleOutcome.Defeat; log.Add("没有初始伙伴回应共鸣。"); return; }
            starter.awakened = true;
            starter.bond = Mathf.Max(starter.bond, 50);
            if (!participants.Contains(starter))
            {
                if (participants.Count >= PartyRoster.DeploymentCapacity) participants.RemoveAt(participants.Count - 1);
                participants.Insert(0, starter);
                if (!contributors.Contains(starter)) contributors.Add(starter);
            }
            SyncDeployment();
            foreach (var member in participants)
                member.currentHp = Mathf.Max(1, Mathf.CeilToInt(CreatureInstance.MaxHp(catalog[member.speciesId], member.level) * 0.65f));
            FinalStage = FinalBossStage.Awakened;
            Wild.currentHp = Mathf.Max(Wild.currentHp, Mathf.CeilToInt(CreatureInstance.MaxHp(WildSpecies, Wild.level) * 0.45f));
            guarding.Clear();
            weakenedRounds = 0;
            rooted = false;
            round++;
            BeginRound();
            log.Add(starter.nickname + "因羁绊完成超进化，唤醒了倒下的伙伴！最终反击仍需你的指挥。");
        }

        private void GrantRewards()
        {
            if (rewardsGranted) return;
            rewardsGranted = true;
            int experience = IsCaptureLesson ? 8 : Kind == BattleKind.Wild ? 16 + Wild.level * 8 : 45 + Wild.level * 10;
            foreach (var member in contributors)
            {
                var species = catalog[member.speciesId];
                int levels = GrowthRules.GrantExperience(member, species, experience);
                member.bond = Mathf.Clamp(member.bond + (IsCaptureLesson ? 2 : Kind == BattleKind.Wild ? 4 : 8), 0, 100);
                log.Add(member.nickname + "获得 " + experience + " 经验" + (levels > 0 ? "，升至 Lv." + member.level : "") + "。");
                if (GrowthRules.TryEvolve(member, catalog, out var evolved))
                    log.Add(member.nickname + "进化为 " + evolved.displayName + "！");
            }
        }

        private BattleActionResult Result(bool success, string message) => new BattleActionResult(success, message, Outcome);
        private BattleActionResult NewMessages(int index) => Result(true, string.Join("\n", log.GetRange(index, log.Count - index)));
    }
}
