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
        private GUIStyle title, heading, body, small, button, card, panel, accent;
        private Texture2D dark, paper, selected;
        private Font font;
        private Vector2 scroll;
        private GamePanel lastPanel;
        private readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        private const float Width = 1280f, Height = 720f;
        private Color ink = new Color(.14f, .22f, .25f);

        private void Init()
        {
            font = Resources.Load<Font>("Art/Fonts/NotoSansSC");
            dark = Solid(new Color(.08f, .15f, .18f, .94f));
            paper = Solid(new Color(.96f, .94f, .86f, .97f));
            selected = Solid(new Color(.82f, .9f, .82f, .98f));
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, wordWrap = true, normal = { textColor = ink } };
            small = new GUIStyle(body) { fontSize = 16 };
            heading = new GUIStyle(body) { fontSize = 26, fontStyle = FontStyle.Bold };
            title = new GUIStyle(body) { fontSize = 62, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.99f, .95f, .81f) } };
            accent = new GUIStyle(small) { normal = { textColor = new Color(.94f, .83f, .56f) } };
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
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / Width, Screen.height / Height, 1f));
            if (lastPanel != Session.Panel) { scroll = Vector2.zero; lastPanel = Session.Panel; }
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
        }

        private void DrawTitle()
        {
            GUI.DrawTexture(new Rect(0, 0, Width, Height), dark);
            GUI.Label(new Rect(100, 100, 850, 92), "雾屿回响", title);
            GUI.Label(new Rect(106, 198, 700, 35), "M I S T   I S L E     /     第一章 · 归航之灯", accent);
            GUI.Label(new Rect(106, 274, 620, 96), "在雾海尽头，遇见会回应你的伙伴。\n探索岛屿，解开封印，让灯塔再次照亮归途。", new GUIStyle(body) { normal = { textColor = Color.white } });
            if (Session.Saves.Exists && GUI.Button(new Rect(108, 418, 260, 58), "继续旅程", button)) Session.ContinueJourney();
            if (GUI.Button(new Rect(108, 486, 260, 58), Session.Saves.Exists ? "新旅程（备份旧进度）" : "开启旅程", button)) Session.NewJourney();
            if (GUI.Button(new Rect(108, 554, 260, 48), "离开", button)) Session.Quit();
            DrawCreature("moss-lumi", new Rect(810, 240, 160, 210));
            DrawCreature("moatail", new Rect(995, 315, 150, 180));
            DrawCreature("crystal-gecko", new Rect(865, 470, 170, 170));
            GUI.Label(new Rect(108, 651, 1050, 30), "WASD / 方向键移动    E 交互    Tab 队伍    J 图鉴    F5 保存    Esc 返回", accent);
        }

        private void DrawStarters()
        {
            GUI.DrawTexture(new Rect(0, 0, Width, Height), dark);
            GUI.Label(new Rect(78, 26, 1100, 110), "选择第一位同行者", title);
            GUI.Label(new Rect(82, 143, 1100, 40), "这份相遇会贯穿旅程。其余伙伴仍可在之后的探索中重逢。", accent);
            string[] ids = { "moss-lumi", "moatail", "crystal-gecko" };
            string[] notes = { "生命与生长\n治疗队友，点亮标记与藤蔓支路。", "潮汐与循环\n恢复体力，平息激流与水路机关。", "晶能与共鸣\n集中爆发，唤醒导体与晶矿机关。" };
            for (int i = 0; i < 3; i++)
            {
                float x = 80 + i * 386;
                GUI.Box(new Rect(x, 188, 350, 448), GUIContent.none, panel);
                DrawCreature(ids[i], new Rect(x + 65, 208, 220, 205));
                GUI.Label(new Rect(x + 24, 420, 300, 42), Session.Catalog[ids[i]].displayName, heading);
                GUI.Label(new Rect(x + 24, 470, 300, 88), notes[i], body);
                if (GUI.Button(new Rect(x + 24, 566, 302, 48), "与它同行", button)) Session.ChooseStarter(ids[i]);
            }
        }

        private void DrawHud()
        {
            GUI.DrawTexture(new Rect(0, 0, Width, 104), dark);
            GUI.Label(new Rect(24, 10, 800, 45), Session.RoomName, new GUIStyle(heading) { normal = { textColor = Color.white } });
            GUI.Label(new Rect(26, 58, 975, 38), Session.Objective, accent);
            GUI.Label(new Rect(1010, 16, 250, 30), "封印 " + Session.Progress.seals.Count + " / 4", accent);
            if (Session.Panel == GamePanel.Explore && GUI.Button(new Rect(1104, 54, 150, 36), "队伍 / Tab", button)) Session.OpenParty();
            GUI.DrawTexture(new Rect(0, 680, Width, 40), dark);
            GUI.Label(new Rect(25, 689, 1210, 28), "WASD 移动   E 交互   Tab 队伍   J 图鉴   F5 保存     ·     旅队 " + Session.Party.Count + "/6   出战 " + Session.Party.DeployedIds.Count + "/3", accent);
        }

        private void BeginWindow(string label)
        {
            GUI.Box(new Rect(125, 120, 1030, 540), GUIContent.none, panel);
            GUI.Label(new Rect(154, 139, 920, 44), label, heading);
        }

        private void DrawDialogue()
        {
            GUI.Box(new Rect(140, 300, 1000, 358), GUIContent.none, panel);
            GUI.Label(new Rect(172, 318, 925, 44), Session.DialogueTitle, heading);
            GUI.Label(new Rect(172, 372, 920, 224), Session.DialogueText, body);
            if (GUI.Button(new Rect(916, 603, 190, 40), "继续", button)) Session.ClosePanel();
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
            DrawCreature(battle.Wild.speciesId, new Rect(830, 190, 245, 210));
            GUI.Label(new Rect(840, 408, 290, 36), "Lv." + battle.Wild.level + "   HP " + battle.Wild.currentHp + "/" + CreatureInstance.MaxHp(battle.WildSpecies, battle.Wild.level), body);
            for (int i = 0; i < Session.Party.DeployedIds.Count; i++)
            {
                string id = Session.Party.DeployedIds[i];
                var c = Session.Party.Members.FirstOrDefault(v => v.instanceId == id);
                if (c == null) continue;
                float x = 110 + i * 217;
                DrawCreature(c.speciesId, new Rect(x, 195, 135, 125));
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
            BattleButton(110, "攻击", BattleAction.Attack);
            BattleButton(287, "专属技能", BattleAction.Skill);
            BattleButton(464, "防御", BattleAction.Defend);
            BattleButton(641, "投喂点心", BattleAction.Feed, "berry-treat");
            BattleButton(818, "共鸣捕获", BattleAction.Capture, "resonance-capsule");
            BattleButton(995, "撤离", BattleAction.Flee);
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
            if (GUI.Button(new Rect(x, 530, 163, 51), text, button)) Session.Act(action, item);
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

        private void DrawCreature(string id, Rect rect)
        {
            if (!portraits.TryGetValue(id, out var sprite))
            {
                if (Session.Catalog.TryGetValue(id, out var species))
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
            if (dark != null) Destroy(dark);
            if (paper != null) Destroy(paper);
            if (selected != null) Destroy(selected);
        }
    }
}
