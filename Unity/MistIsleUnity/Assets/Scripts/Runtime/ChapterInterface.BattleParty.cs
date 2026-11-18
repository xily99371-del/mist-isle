using System.Linq;
using MistIsle.Creatures;
using UnityEngine;

namespace MistIsle.Runtime
{
    public sealed partial class ChapterInterface
    {
        private bool battlePartyOpen;
        private BattleSession battlePartyBattle;
        private CreatureInstance battlePartyActor;
        private string battleOutgoingId, battleIncomingId;
        private Vector2 battlePartyReserveScroll;

        private void RefreshBattlePartySelection(BattleSession battle)
        {
            if (battlePartyOpen && (battle != battlePartyBattle || battle.CurrentActor != battlePartyActor
                || battle.Outcome != BattleOutcome.Ongoing || battle.FinalStage == FinalBossStage.Silence))
                CancelBattleParty();
        }

        private bool CanChooseBattleOutgoing(CreatureInstance companion) => companion != null
            && Session.Party.DeployedIds.Contains(companion.instanceId)
            && (companion == battlePartyActor || companion.currentHp <= 0);

        private bool CanChooseBattleIncoming(CreatureInstance companion) => companion != null
            && companion.currentHp > 0 && Session.Catalog.ContainsKey(companion.speciesId)
            && !Session.Party.DeployedIds.Contains(companion.instanceId);

        private void OpenBattleParty()
        {
            var battle = Session.Battle;
            if (battle == null || battle.Outcome != BattleOutcome.Ongoing || battle.CurrentActor == null
                || !Session.Party.Members.Any(CanChooseBattleIncoming)) return;
            battlePartyBattle = battle;
            battlePartyActor = battle.CurrentActor;
            battleOutgoingId = battlePartyActor.instanceId;
            battleIncomingId = null;
            battlePartyReserveScroll = Vector2.zero;
            battlePartyOpen = true;
        }

        private void CancelBattleParty()
        {
            battlePartyOpen = false;
            battlePartyBattle = null;
            battlePartyActor = null;
            battleOutgoingId = battleIncomingId = null;
        }

        private void SelectBattleOutgoing(string instanceId)
        {
            if (battlePartyOpen && Session.Party.Members.Any(companion => companion.instanceId == instanceId
                && CanChooseBattleOutgoing(companion))) battleOutgoingId = instanceId;
        }

        private void SelectBattleIncoming(string instanceId)
        {
            if (battlePartyOpen && Session.Party.Members.Any(companion => companion.instanceId == instanceId
                && CanChooseBattleIncoming(companion))) battleIncomingId = instanceId;
        }

        private bool CanConfirmBattleParty() => battlePartyOpen && Session.Battle == battlePartyBattle
            && Session.Battle.CurrentActor == battlePartyActor
            && Session.Party.Members.Any(companion => companion.instanceId == battleOutgoingId && CanChooseBattleOutgoing(companion))
            && Session.Party.Members.Any(companion => companion.instanceId == battleIncomingId && CanChooseBattleIncoming(companion));

        private void ConfirmBattleParty()
        {
            if (!CanConfirmBattleParty()) return;
            Session.SwapBattle(battleOutgoingId, battleIncomingId);
            CancelBattleParty();
        }

        private void DrawBattlePartyButton(BattleSession battle)
        {
            bool previousEnabled = GUI.enabled;
            bool available = battle.CurrentActor != null && Session.Party.Members.Any(CanChooseBattleIncoming);
            GUI.enabled = previousEnabled && available;
            if (GUI.Button(new Rect(110, 598, 420, 37), available ? "更换出战伙伴 · 选择后备" : "暂无可换入的后备伙伴", button))
                OpenBattleParty();
            GUI.enabled = previousEnabled;
        }

        private void DrawBattleParty()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                CancelBattleParty();
                Event.current.Use();
                return;
            }
            GUI.DrawTexture(new Rect(0, 105, Width, 575), veil);
            GUI.Box(new Rect(110, 142, 1060, 510), GUIContent.none, panel);
            GUI.Label(new Rect(145, 163, 980, 38), "旅队接力", heading);
            GUI.Label(new Rect(146, 207, 980, 64), "消耗「" + battlePartyActor.nickname
                + "」本次行动；可换下它自己，或替换一位倒下的队友。\n换入伙伴从下一轮开始行动。选择与取消不消耗行动。", small);
            GUI.Label(new Rect(146, 276, 450, 30), "换下哪位伙伴", body);
            GUI.Label(new Rect(662, 276, 450, 30), "换入哪位后备", body);

            int row = 0;
            foreach (var companion in Session.Party.Deployed)
            {
                bool available = CanChooseBattleOutgoing(companion);
                string state = companion.currentHp <= 0 ? "倒下 · 可替换"
                    : companion == battlePartyActor ? "当前行动" : "等待其行动";
                if (DrawBattlePartyCard(companion, new Rect(146, 310 + row++ * 86, 450, 78),
                    available, companion.instanceId == battleOutgoingId, state)) SelectBattleOutgoing(companion.instanceId);
            }
            row = 0;
            var reserves = Session.Party.Members.Where(companion => !Session.Party.DeployedIds.Contains(companion.instanceId)).ToArray();
            battlePartyReserveScroll = GUI.BeginScrollView(new Rect(662, 306, 465, 260), battlePartyReserveScroll,
                new Rect(0, 0, 445, reserves.Length * 86));
            foreach (var companion in reserves)
            {
                bool available = CanChooseBattleIncoming(companion);
                if (DrawBattlePartyCard(companion, new Rect(0, 4 + row++ * 86, 445, 78),
                    available, companion.instanceId == battleIncomingId, available ? "待命" : "倒下 · 需要休息"))
                    SelectBattleIncoming(companion.instanceId);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(146, 578, 560, 46), string.IsNullOrEmpty(battleIncomingId)
                ? "选择一位后备伙伴，再确认接力。" : "准备好了就确认；本轮其他伙伴仍会依次行动。", small);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && CanConfirmBattleParty();
            if (GUI.Button(new Rect(740, 576, 235, 46), "确认接力 · 消耗一次行动", button)) ConfirmBattleParty();
            GUI.enabled = previousEnabled;
            if (GUI.Button(new Rect(990, 576, 145, 46), "取消", button)) CancelBattleParty();
        }

        private bool DrawBattlePartyCard(CreatureInstance companion, Rect area, bool available, bool chosen, string state)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && available;
            var style = new GUIStyle(button);
            if (chosen) style.normal.background = paper;
            bool clicked = GUI.Button(area, GUIContent.none, style);
            DrawCreature(companion.speciesId, new Rect(area.x + 5, area.y + 4, 74, 69));
            var species = Session.Catalog[companion.speciesId];
            GUI.Label(new Rect(area.x + 89, area.y + 7, 350, 28), companion.nickname + "  Lv." + companion.level
                + (chosen ? "  · 已选" : ""), small);
            GUI.Label(new Rect(area.x + 89, area.y + 37, 350, 34), "HP " + companion.currentHp + "/"
                + CreatureInstance.MaxHp(species, companion.level) + "  ·  " + ElementName(species.primaryElement) + "  ·  " + state, small);
            GUI.enabled = previousEnabled;
            return clicked;
        }
    }
}
