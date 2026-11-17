using System;
using System.Collections.Generic;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Items;
using UnityEngine;

namespace MistIsle.Runtime
{
    // Presentation only: all mutations go through GameSession and the gameplay services.
    public sealed class ChapterInterface : MonoBehaviour
    {
        public GameSession Session;
        public int DrawnFrameCount { get; private set; }
        private GUIStyle title, heading, body, small, button, card, panel, accent, dialogueBody, dialogueName;
        private Texture2D dark, paper, selected, veil;
        private Font font;
        private Vector2 scroll;
        private GamePanel lastPanel;
        private readonly TitlePresentation titlePresentation = new TitlePresentation();
        private int starterSelection;
        private float panelOpenedAt;
        private float viewportScale;
        private Vector2 viewportOffset;
        private bool panelInitialized;
        private readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        private const float Width = 1280f, Height = 720f;
        private Color ink = new Color(.14f, .22f, .25f);

        private void Init()
        {
            font = Resources.Load<Font>("Art/Fonts/NotoSansSC");
            dark = Solid(new Color(.08f, .15f, .18f, .94f));
            paper = Solid(new Color(.96f, .94f, .86f, .97f));
            selected = Solid(new Color(.82f, .9f, .82f, .98f));
            veil = Solid(new Color(.04f, .10f, .15f, .56f));
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, wordWrap = true, normal = { textColor = ink } };
            small = new GUIStyle(body) { fontSize = 16 };
            heading = new GUIStyle(body) { fontSize = 26, fontStyle = FontStyle.Bold };
            title = new GUIStyle(body) { fontSize = 62, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.99f, .95f, .81f) } };
            accent = new GUIStyle(small) { normal = { textColor = new Color(.94f, .83f, .56f) } };
            dialogueBody = new GUIStyle(body) { fontSize = 23, normal = { textColor = new Color(.96f, .95f, .88f) } };
            dialogueName = new GUIStyle(heading) { fontSize = 24, normal = { textColor = new Color(.99f, .88f, .61f) } };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18, wordWrap = true, padding = new RectOffset(10, 10, 3, 3) };
            button.normal.background = selected;
            button.normal.textColor = ink;
            button.hover.background = paper;
            button.hover.textColor = ink;
            button.active.background = selected;
            button.active.textColor = ink;
            card = new GUIStyle(GUI.skin.box) { normal = { background = selected }, padding = new RectOffset(12,12,10,10) };
            panel = new GUIStyle(GUI.skin.box) { normal = { background = paper }, padding = new RectOffset(24,24,20,20) };
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (Session == null || Session.Catalog == null) return;
            if (Event.current.type == EventType.Repaint) DrawnFrameCount++;
            if (body == null) Init();
            Matrix4x4 previousMatrix = GUI.matrix;
            viewportScale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            viewportOffset = new Vector2((Screen.width - Width * viewportScale) * .5f, (Screen.height - Height * viewportScale) * .5f);
            GUI.matrix = Matrix4x4.identity;
            if (viewportOffset.x > 0f)
            {
                GUI.DrawTexture(new Rect(0, 0, viewportOffset.x, Screen.height), Texture2D.blackTexture, ScaleMode.StretchToFill, false);
                GUI.DrawTexture(new Rect(Screen.width - viewportOffset.x, 0, viewportOffset.x, Screen.height), Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            }
            if (viewportOffset.y > 0f)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, viewportOffset.y), Texture2D.blackTexture, ScaleMode.StretchToFill, false);
                GUI.DrawTexture(new Rect(0, Screen.height - viewportOffset.y, Screen.width, viewportOffset.y), Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            }
            GUI.matrix = Matrix4x4.TRS(new Vector3(viewportOffset.x, viewportOffset.y, 0), Quaternion.identity, Vector3.one * viewportScale);
            if (!panelInitialized || lastPanel != Session.Panel)
            {
                panelInitialized = true;
                scroll = Vector2.zero;
                lastPanel = Session.Panel;
                panelOpenedAt = Time.unscaledTime;
                if (Session.Panel == GamePanel.Title) titlePresentation.Open(Session.Saves.Exists);
                if (Session.Panel == GamePanel.Starter)
                    starterSelection = Mathf.Max(0, Array.IndexOf(GameCatalog.StarterIds, Session.HighlightStarterId));
            }
            if (Session.Panel == GamePanel.Title) DrawTitle();
            else if (Session.Panel == GamePanel.Starter) DrawStarters();
            else
            {
                DrawHud();
                switch (Session.Panel)
                {
                    case GamePanel.Dialogue: DrawDialogue(); break;
                    case GamePanel.Campfire: DrawCampfire(); break;
                    case GamePanel.Watcher: DrawWatcher(); break;
                    case GamePanel.Party: DrawParty(); break;
                    case GamePanel.Storage: DrawStorage(); break;
                    case GamePanel.Journal: DrawJournal(); break;
                    case GamePanel.Battle: DrawBattle(); break;
                    case GamePanel.Ending: DrawEnding(); break;
                    default:
                        if (!string.IsNullOrEmpty(Session.InteractionPrompt))
                        {
                            GUI.DrawTexture(new Rect(420, 618, 440, 52), dark);
                            GUI.Label(new Rect(440, 630, 400, 32), Session.InteractionPrompt, accent);
                        }
                        break;
                }
            }
            if (Time.unscaledTime < Session.ToastUntil && !string.IsNullOrEmpty(Session.Toast))
            {
                GUI.DrawTexture(new Rect(280, 8, 720, 56), dark);
                GUI.Label(new Rect(298, 18, 684, 42), Session.Toast, accent);
            }
            GUI.matrix = previousMatrix;
        }

        private void DrawTitle()
        {
            var mouse = new Vector2((Input.mousePosition.x - viewportOffset.x) / viewportScale,
                (Screen.height - Input.mousePosition.y - viewportOffset.y) / viewportScale);
            titlePresentation.Draw(Session, font, paper, veil, mouse);
        }

        private void DrawStarters()
        {
            GUI.DrawTexture(new Rect(0, 0, Width, Height), veil);
            GUI.DrawTexture(new Rect(42, 66, 1196, 616), paper);
            DrawRule(new Rect(68, 87, 1144, 2), new Color(.73f, .58f, .33f));
            GUI.Label(new Rect(76, 108, 235, 35), "海岸营地 · 初次回应", small);
            GUI.Label(new Rect(330, 107, 860, 52), "第一位同行者", new GUIStyle(heading) { fontSize = 37 });
            GUI.Label(new Rect(333, 168, 850, 48), "拾光：它们都在等一个愿意倾听的人。先认识一下，再做决定吧。", body);
            DrawSpeaker("shiguang", new Rect(78, 192, 218, 307));
            GUI.Label(new Rect(84, 514, 230, 105), "“别急着比较强弱。\n看看谁愿意回应你。”", new GUIStyle(body) { fontSize = 19 });
            string[] ids = GameCatalog.StarterIds;
            string[] roles = { "草木 · 治疗与辅助", "潮汐 · 恢复与控制", "晶能 · 爆发与共鸣" };
            string[] notes = {
                "藤光为伙伴恢复生命，并用藤蔓缠绕对手。探索时，它能点亮隐藏标记、恢复藤蔓道路。",
                "潮汐回复体力、削弱对手的攻击。探索时，它能引水、平息火焰，唤醒沉睡的水路。",
                "晶能爆发积聚力量，造成集中的共鸣伤害。探索时，它能激活导体、破坏共鸣矿石。" };
            var input = Event.current;
            if (Time.unscaledTime - panelOpenedAt > .3f && input.type == EventType.KeyDown)
            {
                if (input.keyCode == KeyCode.LeftArrow || input.keyCode == KeyCode.A) { starterSelection = (starterSelection + 2) % 3; input.Use(); }
                else if (input.keyCode == KeyCode.RightArrow || input.keyCode == KeyCode.D) { starterSelection = (starterSelection + 1) % 3; input.Use(); }
                else if (input.keyCode == KeyCode.Return) { Session.ChooseStarter(ids[starterSelection]); input.Use(); }
                else if (input.keyCode == KeyCode.Escape) { Session.ClearStoryFocus(); Session.SetPanel(GamePanel.Explore); input.Use(); }
            }
            for (int i = 0; i < 3; i++)
            {
                float x = 337 + i * 289;
                bool focus = starterSelection == i;
                if (focus) DrawRule(new Rect(x + 24, 446, 218, 3), new Color(.72f, .51f, .24f));
                DrawCreature(ids[i], new Rect(x + 27, 232 + Mathf.Sin(Time.unscaledTime * 1.5f + i) * 3f, 217, 190));
                GUI.Label(new Rect(x + 2, 455, 266, 37), Session.Catalog[ids[i]].displayName,
                    new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter, normal = { textColor = focus ? new Color(.55f, .35f, .16f) : ink } });
                GUI.Label(new Rect(x + 2, 493, 266, 31), roles[i], new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
                if (GUI.Button(new Rect(x, 221, 273, 306), GUIContent.none, GUIStyle.none)) starterSelection = i;
            }
            DrawRule(new Rect(336, 536, 849, 1), new Color(.69f, .63f, .49f, .5f));
            GUI.Label(new Rect(340, 551, 820, 62), notes[starterSelection], small);
            GUI.Label(new Rect(340, 631, 533, 38), "未选择的伙伴，仍可在后续旅程中重逢。", small);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = Time.unscaledTime - panelOpenedAt > .3f;
            if (GUI.Button(new Rect(82, 618, 214, 45), "再看看其他伙伴", button))
            { Session.ClearStoryFocus(); Session.SetPanel(GamePanel.Explore); }
            if (GUI.Button(new Rect(917, 618, 271, 45), "确认 · 与" + Session.Catalog[ids[starterSelection]].displayName + "同行", button))
                Session.ChooseStarter(ids[starterSelection]);
            GUI.enabled = wasEnabled;
        }

        private void DrawHud()
        {
            GUI.DrawTexture(new Rect(18, 15, 730, 83), dark);
            DrawRule(new Rect(18, 15, 3, 83), new Color(.82f, .68f, .4f));
            GUI.Label(new Rect(36, 23, 680, 34), Session.RoomName, new GUIStyle(heading) { fontSize = 23, normal = { textColor = Color.white } });
            GUI.Label(new Rect(37, 61, 686, 32), Session.Objective, accent);
            GUI.DrawTexture(new Rect(1054, 18, 204, 80), dark);
            GUI.Label(new Rect(1074, 25, 165, 28), "区域封印  " + Session.Progress.seals.Count + " / 4", accent);
            if (Session.Panel == GamePanel.Explore && GUI.Button(new Rect(1073, 59, 166, 30), "伙伴旅队 · Tab", button)) Session.OpenParty();
            if (Session.Panel == GamePanel.Explore && !string.IsNullOrEmpty(Session.TutorialHint))
            {
                GUI.DrawTexture(new Rect(20, 112, 601, 65), dark);
                GUI.Label(new Rect(38, 125, 566, 46), Session.TutorialHint, accent);
            }
            if (Session.Panel == GamePanel.Explore)
            {
                GUI.DrawTexture(new Rect(274, 679, 732, 32), dark);
                GUI.Label(new Rect(289, 685, 710, 24), "WASD 移动    E 交互    Tab 伙伴    J 图鉴    F5 保存    ·    同行 " + Session.Party.Count + "/6", new GUIStyle(accent) { fontSize = 14 });
            }
        }

        private void BeginWindow(string label)
        {
            GUI.Box(new Rect(125, 120, 1030, 540), GUIContent.none, panel);
            GUI.Label(new Rect(154, 139, 920, 44), label, heading);
        }

        private void DrawDialogue()
        {
            bool hasSpeaker = DrawSpeaker(Session.DialogueSpeakerId, new Rect(62, 343, 196, 316));
            float left = hasSpeaker ? 245f : 91f;
            GUI.DrawTexture(new Rect(left, 460, 1189 - left, 207), dark);
            DrawRule(new Rect(left + 2, 460, 1185 - left, 2), new Color(.83f, .71f, .45f));
            GUI.Label(new Rect(left + 27, 473, 785, 35), Session.DialogueTitle, dialogueName);
            GUI.Label(new Rect(left + 29, 518, 1129 - left, 108), Session.DialogueText, dialogueBody);
            GUI.Label(new Rect(left + 29, 636, 620, 24), "Space / E · 继续", accent);
            int page = Mathf.Clamp(Session.DialoguePageIndex + 1, 1, Mathf.Max(1, Session.DialoguePageCount));
            GUI.Label(new Rect(971, 638, 70, 24), page + " / " + Mathf.Max(1, Session.DialoguePageCount), accent);
            if (GUI.Button(new Rect(1052, 620, 113, 36), page < Session.DialoguePageCount ? "继续  ›" : "回应  ›", button)) Session.AdvanceDialogue();
        }

        private void DrawCampfire()
        {
            BeginWindow("篝火 · 烹饪与共鸣合成");
            GUI.Label(new Rect(158, 192, 940, 62), InventoryLine(), small);
            for (int i = 0; i < Session.Recipes.Count; i++)
            {
                CampfireRecipe recipe = Session.Recipes[i];
                float y = 265 + i * 96;
                GUI.Box(new Rect(156, y, 966, 82), GUIContent.none, card);
                GUI.Label(new Rect(172, y + 7, 670, 32), recipe.displayName, body);
                GUI.Label(new Rect(172, y + 43, 690, 32), string.Join(" + ", recipe.ingredients.Select(v => GameCatalog.ItemName(v.itemId) + " ×" + v.amount)), small);
                if (GUI.Button(new Rect(902, y + 17, 200, 46), "制作 ×" + recipe.outputAmount, button)) Session.Craft(recipe);
            }
            if (GUI.Button(new Rect(160, 594, 250, 40), "休息并恢复伙伴", button)) Session.Recover();
            CloseButton();
        }

        private string InventoryLine() => string.Join("    ", Session.Inventory.Entries.Select(s => GameCatalog.ItemName(s.itemId) + " ×" + s.count));

        private void DrawWatcher()
        {
            BeginWindow("Watcher · 归航照护站");
            GUI.Label(new Rect(160, 208, 930, 75), "欢迎回来。让伙伴们休息一会儿吧。\n我会照料整个旅队；拾光会帮你了解野生伙伴与捕获方法。", body);
            if (GUI.Button(new Rect(160, 310, 420, 60), "恢复全部伙伴 · 免费", button)) Session.Recover();
            if (GUI.Button(new Rect(612, 310, 480, 60), "整理旅队与出战顺序", button)) Session.OpenParty();
            if (GUI.Button(new Rect(160, 377, 930, 40), "伙伴寄存 · 腾出随身位置，保留培养进度", button)) Session.OpenStorage();
            GUI.Label(new Rect(160, 405, 930, 42), "伙伴重逢 · 完成两枚封印后解锁", heading);
            string[] starters = { "moss-lumi", "moatail", "crystal-gecko" };
            for (int i = 0; i < starters.Length; i++)
            {
                string id = starters[i];
                if (GUI.Button(new Rect(160 + i * 316, 465, 296, 65), "拾光的线索 · " + Session.Catalog[id].displayName, button)) Session.ReuniteStarter(id);
            }
            CloseButton();
        }

        private void DrawParty()
        {
            BeginWindow("旅队 · 最多六位同行，三位出战");
            for (int i = 0; i < Session.Party.Members.Count; i++)
            {
                var c = Session.Party.Members[i];
                var species = Session.Catalog[c.speciesId];
                float x = 158 + (i % 2) * 487, y = 202 + (i / 2) * 124;
                GUI.Box(new Rect(x, y, 466, 114), GUIContent.none, card);
                DrawCreature(c.speciesId, new Rect(x + 7, y + 4, 92, 100));
                GUI.Label(new Rect(x + 105, y + 6, 350, 32), species.displayName + "  Lv." + c.level + (c.isStarter ? " · 初始伙伴" : ""), body);
                GUI.Label(new Rect(x + 105, y + 41, 350, 30), "HP " + c.currentHp + "/" + CreatureInstance.MaxHp(species, c.level) + "   亲密 " + c.bond, small);
                if (GUI.Button(new Rect(x + 105, y + 77, 156, 32), Session.Party.DeployedIds.Contains(c.instanceId) ? "出战中 · 收回" : "加入出战", button)) Session.ToggleDeployment(c);
                if (species.evolutionTarget != null && GUI.Button(new Rect(x + 273, y + 77, 176, 32), "进化条件 / 进化", button)) Session.Evolve(c);
            }
            GUI.Label(new Rect(160, 585, 700, 44), "出战顺序即行动顺序。替换伙伴会消耗战斗中的一次行动。", small);
            if (GUI.Button(new Rect(925, 600, 180, 40), "返回", button)) Session.ReturnFromParty();
        }

        private void DrawJournal()
        {
            BeginWindow("同行图鉴 · 属性、偏好与探索线索");
            var species = Session.Catalog.Values.Where(s => s.capturable).ToArray();
            scroll = GUI.BeginScrollView(new Rect(152, 196, 970, 390), scroll, new Rect(0, 0, 940, Mathf.Ceil(species.Length / 3f) * 146));
            for (int i = 0; i < species.Length; i++)
            {
                var s = species[i];
                float x = (i % 3) * 310, y = (i / 3) * 146;
                GUI.Box(new Rect(x, y, 300, 138), GUIContent.none, card);
                DrawCreature(s.speciesId, new Rect(x + 7, y + 10, 85, 100));
                GUI.Label(new Rect(x + 98, y + 11, 196, 33), s.displayName, body);
                GUI.Label(new Rect(x + 98, y + 49, 196, 80), (Session.Progress.discoveredSpeciesIds.Contains(s.speciesId) ? "已发现" : "尚未遇见") + " · " + ElementName(s.primaryElement) + "\n" + s.fieldLore, small);
            }
            GUI.EndScrollView();
            CloseButton();
        }

        private void DrawStorage()
        {
            BeginWindow("Watcher · 伙伴寄存");
            GUI.Label(new Rect(158, 187, 940, 40), "随身最多六只。初始伙伴始终同行；寄存保留等级、经验与亲密度。", small);
            int count = Session.Party.Count + Session.Storage.Members.Count;
            scroll = GUI.BeginScrollView(new Rect(154, 234, 970, 350), scroll, new Rect(0, 0, 942, count * 65));
            int row = 0;
            foreach (var c in Session.Party.Members.ToArray())
            {
                GUI.Label(new Rect(8, row * 65 + 8, 640, 42), "同行 · " + Session.Catalog[c.speciesId].displayName + "  Lv." + c.level, body);
                if (GUI.Button(new Rect(690, row * 65, 220, 48), c.isStarter ? "初始伙伴 · 同行" : "寄存", button) && !c.isStarter) Session.Deposit(c);
                row++;
            }
            foreach (var c in Session.Storage.Members.ToArray())
            {
                GUI.Label(new Rect(8, row * 65 + 8, 640, 42), "寄存 · " + Session.Catalog[c.speciesId].displayName + "  Lv." + c.level, body);
                if (GUI.Button(new Rect(690, row * 65, 220, 48), "接回旅队", button)) Session.Withdraw(c);
                row++;
            }
            GUI.EndScrollView();
            CloseButton();
        }

        private void DrawBattle()
        {
            var battle = Session.Battle;
            if (battle == null) return;
            GUI.Box(new Rect(80, 118, 1120, 545), GUIContent.none, panel);
            GUI.Label(new Rect(108, 135, 1000, 45), (battle.Kind == BattleKind.Wild ? "野生遭遇 · " : "封印回响 · ") + battle.WildSpecies.displayName, heading);
            if (!string.IsNullOrEmpty(Session.TutorialHint))
                GUI.Label(new Rect(112, 181, 670, 44), Session.TutorialHint, new GUIStyle(small) { normal = { textColor = new Color(.53f, .36f, .15f) } });
            DrawCreature(battle.Wild.speciesId, new Rect(830, 190, 245, 210));
            GUI.Label(new Rect(840, 408, 290, 36), battle.IsCaptureLesson
                ? "警戒 " + battle.Wild.currentHp + "/" + CreatureInstance.MaxHp(battle.WildSpecies, battle.Wild.level)
                : "Lv." + battle.Wild.level + "   HP " + battle.Wild.currentHp + "/" + CreatureInstance.MaxHp(battle.WildSpecies, battle.Wild.level), body);
            for (int i = 0; i < Session.Party.DeployedIds.Count; i++)
            {
                string id = Session.Party.DeployedIds[i];
                var c = Session.Party.Members.FirstOrDefault(v => v.instanceId == id);
                if (c == null) continue;
                float x = 110 + i * 217;
                DrawCreature(c.speciesId, new Rect(x, 229, 135, 91));
                if (battle.CurrentActor == c) DrawRule(new Rect(x, 379, 177, 3), new Color(.77f, .54f, .23f));
                GUI.Label(new Rect(x, 320, 210, 75), Session.Catalog[c.speciesId].displayName + " Lv." + c.level + "\nHP " + c.currentHp + "/" + CreatureInstance.MaxHp(Session.Catalog[c.speciesId], c.level), small);
            }
            GUI.Label(new Rect(110, 398, 650, 80), string.Join("\n", battle.Log.Skip(Mathf.Max(0, battle.Log.Count - 3))), small);
            if (battle.Outcome != BattleOutcome.Ongoing)
            {
                if (GUI.Button(new Rect(430, 544, 420, 65), battle.Outcome == BattleOutcome.Victory ? "胜利 · 继续旅程" : battle.Outcome == BattleOutcome.Captured ? "伙伴回应了你 · 返回" : "返回探索", button)) Session.LeaveBattle();
                return;
            }
            if (battle.FinalStage == FinalBossStage.Silence)
            {
                GUI.Label(new Rect(110, 488, 1020, 36), "一切声音都消失了。唯有最初的羁绊，还在回应你。", heading);
                if (GUI.Button(new Rect(410, 552, 460, 63), "呼唤初始伙伴 · 共鸣觉醒", button)) Session.Act(BattleAction.Continue);
                return;
            }
            string current = battle.CurrentActor == null ? "伙伴行动" : Session.Catalog[battle.CurrentActor.speciesId].displayName + "的行动";
            GUI.Label(new Rect(110, 478, 1000, 32), battle.FinalStage == FinalBossStage.Charging ? "警告：观测者正在蓄能！下一轮共鸣冲击将击倒出战伙伴。" : current + "   ·   " + InventoryLine(), small);
            BattleButton(110, battle.IsCaptureLesson ? "轻柔牵制\n让它安静下来" : "攻击\n削弱对手", BattleAction.Attack);
            BattleButton(287, "专属技能\n发挥伙伴特长", BattleAction.Skill);
            BattleButton(464, "防御\n减少本轮伤害", BattleAction.Defend);
            BattleButton(641, "投喂点心\n建立信任", BattleAction.Feed, "berry-treat");
            BattleButton(818, "共鸣捕获\n邀请它同行", BattleAction.Capture, "resonance-capsule");
            BattleButton(995, "撤离\n退回探索", BattleAction.Flee);
            var reserve = Session.Party.Members.FirstOrDefault(c => c.currentHp > 0 && !Session.Party.DeployedIds.Contains(c.instanceId));
            if (reserve != null && battle.CurrentActor != null && GUI.Button(new Rect(110, 598, 420, 37), "换入后备 · " + Session.Catalog[reserve.speciesId].displayName, button))
                Session.SwapBattle(battle.CurrentActor.instanceId, reserve.instanceId);
            if (battle.FinalStage == FinalBossStage.Awakened && battle.CurrentActor != null && battle.CurrentActor.isStarter)
            {
                if (GUI.Button(new Rect(550, 598, 340, 37), "归航回响 · 羁绊反击", button)) Session.Act(BattleAction.Resonance);
            }
            else if (battle.Kind == BattleKind.Wild && GUI.Button(new Rect(550, 598, 210, 37), "投喂野莓", button)) Session.Act(BattleAction.Feed, "berry");
        }

        private void BattleButton(float x, string text, BattleAction action, string item = null)
        {
            bool enabledBefore = GUI.enabled;
            bool wildOnly = action == BattleAction.Capture || action == BattleAction.Feed || action == BattleAction.Flee;
            GUI.enabled = enabledBefore && (!wildOnly || Session.Battle.Kind == BattleKind.Wild);
            string hint = Session.TutorialHint ?? "";
            bool recommended = action == BattleAction.Attack && (hint.Contains("削弱") || hint.Contains("牵制"))
                || action == BattleAction.Capture && hint.Contains("捕获") && !hint.Contains("削弱")
                || action == BattleAction.Feed && hint.Contains("投喂") && !hint.Contains("削弱")
                || action == BattleAction.Skill && hint.Contains("技能");
            if (recommended && GUI.enabled)
                DrawRule(new Rect(x - 2, 526, 167, 3), new Color(.83f, .57f, .19f));
            if (GUI.Button(new Rect(x, 530, 163, 54), text, new GUIStyle(button) { fontSize = 16 })) Session.Act(action, item);
            GUI.enabled = enabledBefore;
        }

        private void DrawEnding()
        {
            GUI.DrawTexture(new Rect(0, 105, Width, 575), dark);
            GUI.Label(new Rect(200, 170, 1000, 90), "灯火回应了雾海", title);
            GUI.Label(new Rect(205, 280, 880, 170), "最初那位伙伴，最终与你一起照亮了归途。\nWatcher 的营地重新迎来了旅人，拾光又在整理下一次远行的笔记。\n\n第一章 · 归航之灯  完", new GUIStyle(body) { normal = { textColor = Color.white } });
            GUI.Label(new Rect(205, 456, 800, 45), "四枚封印 · " + Session.Progress.discoveredSpeciesIds.Count + " 个已发现形态 · " + Mathf.RoundToInt(Session.Progress.playSeconds / 60f) + " 分钟旅程", accent);
            if (GUI.Button(new Rect(205, 539, 420, 60), "回到营地 · 继续收集", button)) { Session.ChangeRoom("camp-0", false); Session.ClosePanel(); Session.Save(false); }
        }

        private void CloseButton()
        {
            if (GUI.Button(new Rect(925, 600, 180, 40), "返回探索", button)) Session.ClosePanel();
        }

        private static string ElementName(ElementType element)
        {
            string[] names = { "无相", "草木", "潮汐", "焰火", "风息", "岩土", "晶能", "幽影", "辉光" };
            return names[(int)element];
        }

        private static void DrawRule(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private bool DrawSpeaker(string id, Rect rect)
        {
            string key = (id ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key) || key == "narrator" || key == "narration") return false;
            if (Session.Catalog.ContainsKey(key)) { DrawCreature(key, rect); return true; }
            string resource = key == "shiguang" || key == "拾光" ? "Art/Actors/ShiguangPixel/ShiguangPixel_00"
                : key == "traveler" || key == "traveller" || key == "旅人" ? "Art/Actors/ApprovedTraveler/ApprovedTraveler_00"
                : key == "watcher" ? "Art/Actors/Watcher/Watcher_00" : null;
            if (resource == null) return false;
            string cacheKey = "$speaker:" + resource;
            if (!portraits.TryGetValue(cacheKey, out var sprite))
            {
                sprite = Resources.Load<Sprite>(resource);
                portraits[cacheKey] = sprite;
            }
            if (sprite == null) return false;
            Rect source = sprite.textureRect;
            float factor = Mathf.Min(rect.width / source.width, rect.height / source.height);
            Rect draw = new Rect(rect.center.x - source.width * factor * .5f,
                rect.yMax - source.height * factor, source.width * factor, source.height * factor);
            GUI.DrawTextureWithTexCoords(draw, sprite.texture,
                new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
                    source.width / sprite.texture.width, source.height / sprite.texture.height), true);
            return true;
        }

        private void DrawCreature(string id, Rect rect)
        {
            if (!portraits.TryGetValue(id, out var sprite))
            {
                string originalPixel = id == "moss-lumi" ? "PixelMossLumi" : id == "moatail" ? "PixelMoatail"
                    : id == "crystal-gecko" ? "PixelCrystalGecko" : id == "wild-03" ? "PixelWildCrab" : null;
                if (originalPixel != null) sprite = Resources.Load<Sprite>("Art/Creatures/" + originalPixel);
                if (sprite == null && Session.Catalog.TryGetValue(id, out var species))
                    sprite = Resources.Load<Sprite>(species.spriteResourcePath);
                if (sprite == null) sprite = Resources.Load<Sprite>("Art/Creatures/" + id);
                portraits[id] = sprite;
            }
            if (sprite == null)
            {
                GUI.Label(rect, "◇", new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter });
                return;
            }
            Rect r = sprite.textureRect;
            float factor = Mathf.Min(rect.width / r.width, rect.height / r.height);
            Rect draw = new Rect(rect.center.x - r.width * factor / 2f, rect.center.y - r.height * factor / 2f, r.width * factor, r.height * factor);
            GUI.DrawTextureWithTexCoords(draw, sprite.texture, new Rect(r.x / sprite.texture.width, r.y / sprite.texture.height, r.width / sprite.texture.width, r.height / sprite.texture.height), true);
        }

        private void OnDestroy()
        {
            titlePresentation.Dispose();
            if (dark != null) Destroy(dark);
            if (paper != null) Destroy(paper);
            if (selected != null) Destroy(selected);
            if (veil != null) Destroy(veil);
        }
    }
}
