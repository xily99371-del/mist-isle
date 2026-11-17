using System;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Items;
using MistIsle.World;
using UnityEngine;

namespace MistIsle.Runtime
{
    public sealed partial class GameSession
    {
        private float nextNodeRefresh;
        public bool HasThreeSeals => Progress.seals.HasSeal("glowglade-warden") && Progress.seals.HasSeal("tidal-warden") && Progress.seals.HasSeal("crystal-warden");

        public bool NodeAvailable(string id)
        {
            if (id == "tutorial-coast-crab") return !Progress.captureTutorialCompleted;
            if (Progress.openedChestIds.Contains(id)) return false;
            var state = Progress.nodeRefreshStates.FirstOrDefault(value => value.id == id);
            return state != null ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= state.availableAtUtc : !Progress.collectedNodeIds.Contains(id);
        }

        private void DepleteNode(string id, int seconds)
        {
            var state = Progress.nodeRefreshStates.FirstOrDefault(value => value.id == id);
            if (state == null) { state = new NodeRefreshState { id = id }; Progress.nodeRefreshStates.Add(state); }
            state.availableAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Mathf.Max(10, seconds);
            if (!Progress.collectedNodeIds.Contains(id)) Progress.collectedNodeIds.Add(id);
            World.SetCollected(id);
        }

        private void RefreshDesignNodes()
        {
            if (World == null || Time.unscaledTime < nextNodeRefresh) return;
            nextNodeRefresh = Time.unscaledTime + 1f;
            SyncDesignExitGates();
            foreach (var target in World.Interactions)
            {
                if (target.Kind != "resource" && target.Kind != "encounter") continue;
                bool ready = NodeAvailable(target.Id);
                if (target.CanInteract != ready) target.SetAvailable(ready);
                if (ready) Progress.collectedNodeIds.Remove(target.Id);
            }
        }

        private void RestoreDesignObjects()
        {
            foreach (var target in World.Interactions.Where(value => value.Kind == "chest"))
                target.SetAvailable(!Progress.openedChestIds.Contains(target.Id));
            World.ResetFields(id => Progress.completedFieldIds.Contains(id));
            SyncDesignExitGates();
            foreach (var target in World.Interactions.Where(value => value.Kind == "encounter"))
                target.SetAvailable(NodeAvailable(target.Id));
        }

        private void SyncDesignExitGates()
        {
            foreach (var exit in DesignPackCatalog.Maps.SelectMany(map => map.Exits))
            {
                bool allowedByStory = OpeningPhase == OpeningStage.None || OpeningPhase == OpeningStage.Complete || exit.TargetRoom == "camp-0"
                    || exit.TargetRoom == "coast-0" && (OpeningPhase == OpeningStage.FollowGuide || OpeningPhase == OpeningStage.FirstEncounter || OpeningPhase == OpeningStage.ReportWatcher);
                World.SetFieldOpen(exit.Id, allowedByStory && (exit.Requires.Length == 0 || HasThreeSeals));
            }
        }

        private bool HandleDesignAction(string kind, string id)
        {
            if (kind == "exit")
            {
                if (DesignPackCatalog.GetExit(Progress.roomId, id) == null) return true;
                ChangeRoom(id, true, Progress.roomId);
                return true;
            }
            if (kind == "guardian" || kind == "boss")
            {
                string prerequisite = id == "glowglade-warden" ? "M02_ROOT_BRIDGE" : id == "tidal-warden" ? "M03_WATER_CHANNEL"
                    : id == "crystal-warden" ? "M03_CONDUIT" : "M04_THREE_SEAL_GATE";
                if (!Progress.completedFieldIds.Contains(prerequisite))
                { Say("守护之路", "先照看这里尚未恢复的机关，再向守卫请求认可。\n\n" + Objective); return true; }
                if (kind == "guardian" && Progress.seals.HasSeal(id))
                { SayPages(DesignPackNarrative.AfterGuardian(id)); return true; }
                if (kind == "boss" && !Progress.seals.CanEnterLighthouse)
                { Say("灯塔前庭", "观测者仍在等待第四场守护试炼的回应。先到前庭与它见面。 "); return true; }
                if (Progress.chapterCompleted && kind == "boss") { Say("雾散之后", "航路已经重新开放。你与伙伴仍可以回到各处继续旅行。 "); return true; }
                SayPages(DesignPackNarrative.BeforeGuardian(id), () => StartEncounter(id, kind == "boss" ? BattleKind.FinalBoss : BattleKind.Guardian));
                return true;
            }
            if (!DesignPackCatalog.TryGetInteraction(id, out var node)) return false;
            switch (kind)
            {
                case "shop": SetPanel(GamePanel.Shop); return true;
                case "resource":
                    if (!NodeAvailable(id)) { Notify("这里的资源还在恢复。 "); return true; }
                    if (string.IsNullOrEmpty(node.ItemId)) { Notify("这处资源尚未配置。 "); return true; }
                    Inventory.Add(node.ItemId, node.Amount);
                    DepleteNode(id, node.RespawnSeconds);
                    Notify("采集 " + GameCatalog.ItemName(node.ItemId) + " ×" + node.Amount);
                    opening.CheckIngredients(); Save(false); return true;
                case "encounter":
                    if (!NodeAvailable(id)) { Notify("草丛暂时安静了，稍后再回来看看。 "); return true; }
                    StartEncounter(id, BattleKind.Wild); return true;
                case "chest":
                    if (Progress.openedChestIds.Contains(id)) { Notify("已经取走了这里的物资。 "); return true; }
                    if (!string.IsNullOrEmpty(node.ItemId)) Inventory.Add(node.ItemId, node.Amount);
                    Inventory.Add(ShopCatalog.CurrencyItemId, 20);
                    Inventory.Add("resonance-capsule", 1);
                    Progress.openedChestIds.Add(id); World.SetCollected(id);
                    ObserveDesignStory(node.SourceId);
                    Notify("找到补给：共鸣贝 ×20、共鸣捕获器 ×1" + (node.ItemId == "" ? "" : "、" + GameCatalog.ItemName(node.ItemId) + " ×" + node.Amount));
                    Save(false); return true;
                case "field": return OpenDesignField(node);
                case "sign": case "story":
                    if (node.SourceId == "M00_STORAGE") { OpenStorage(); return true; }
                    if (node.SourceId == "M02_ANCIENT_GROVE")
                    {
                        DesignPackCatalog.TryGetInteraction("M02_ROOT_BRIDGE", out var rootBridge);
                        return OpenDesignField(rootBridge);
                    }
                    if (node.SourceId == "M04_RESONANCE_PLAZA" || node.SourceId == "M04_THREE_SEAL_GATE")
                    {
                        if (!HasThreeSeals) { Say("三印之门", "森印、潮印、晶印代表三处守护的认可，还不能缺少任何一个。 "); return true; }
                        if (!Progress.completedFieldIds.Contains("M04_THREE_SEAL_GATE"))
                        { Progress.completedFieldIds.Add("M04_THREE_SEAL_GATE"); World.OpenField("M04_THREE_SEAL_GATE"); }
                    }
                    if (node.SourceId == "M00_SHIGUANG_STATION")
                    { SayPages(DesignPackNarrative.Observe(node.SourceId), () => SetPanel(GamePanel.Campfire)); return true; }
                    ObserveDesignStory(node.SourceId); Save(false); return true;
                default: return false;
            }
        }

        private bool OpenDesignField(DesignPackNode node)
        {
            if (node == null) return true;
            if (Progress.completedFieldIds.Contains(node.Id)) { SayPages(DesignPackNarrative.Observe(node.SourceId)); return true; }
            string required = node.FieldAbility;
            bool available = Party.Members.Any(creature => creature.currentHp > 0 && (
                (Catalog[creature.speciesId].fieldAbilityId ?? "").Contains(required) && !string.IsNullOrEmpty(required)
                || required == "verdant" && Catalog[creature.speciesId].primaryElement == ElementType.Verdant
                || required == "tide" && Catalog[creature.speciesId].primaryElement == ElementType.Tide
                || required == "crystal" && Catalog[creature.speciesId].primaryElement == ElementType.Crystal));
            if (!available)
            {
                Say("与伙伴合作", "这里需要一位还能行动的" + (required == "verdant" ? "苔藓系伙伴恢复根桥" : required == "tide" ? "潮汐系伙伴接回水路" : "晶能系伙伴疏通导能")
                    + "。潮风小径与遗迹浅湾都有可以结识的新伙伴；可以先探索另一条路。 ");
                return true;
            }
            Progress.completedFieldIds.Add(node.Id); World.OpenField(node.Id);
            foreach (var creature in Party.Members) creature.bond = Mathf.Min(100, creature.bond + 3);
            Inventory.Add(ShopCatalog.CurrencyItemId, 12);
            ObserveDesignStory(node.SourceId);
            Notify("机关已恢复，伙伴亲密度提升，获得共鸣贝 ×12。 ");
            Save(false); return true;
        }

        private void ObserveDesignStory(string id)
        {
            var pages = id.StartsWith("after:", StringComparison.Ordinal) ? DesignPackNarrative.AfterGuardian(id.Substring(6)) : DesignPackNarrative.Observe(id);
            // Mark after the final page: quitting during a clue must not permanently swallow that clue.
            Progress.pendingStoryId = id;
            SayPages(pages, () => {
                if (!Progress.storyFlags.Contains(id)) Progress.storyFlags.Add(id);
                Progress.pendingStoryId = null;
                if (id == "after:final-boss") SetPanel(GamePanel.Ending);
                Save(false);
            });
        }

        public void BuySupply(string id)
        {
            if (Panel != GamePanel.Shop) return;
            bool changed = ShopService.TryBuy(Inventory, id, 1, Progress.seals.Count, out var reason);
            Notify(reason);
            if (changed) Save(false); // A persistent save error must remain visible after the transaction message.
        }
        public void SellSupply(string id)
        {
            if (Panel != GamePanel.Shop) return;
            bool changed = ShopService.TrySell(Inventory, id, 1, Progress.seals.Count, out var reason);
            Notify(reason);
            if (changed) Save(false);
        }
    }
}
