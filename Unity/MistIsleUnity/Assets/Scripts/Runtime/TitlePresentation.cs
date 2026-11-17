using UnityEngine;

namespace MistIsle.Runtime
{
    // Only the cleaned menu patch is generated. Approved characters, logo and scenery use original pixels.
    internal sealed class TitlePresentation
    {
        private enum Overlay { None, Settings, NewJourney, Quit }
        private static readonly Rect[] MenuBounds = {
            new Rect(533, 291, 290, 54), new Rect(558, 355, 249, 52),
            new Rect(558, 414, 249, 49), new Rect(558, 471, 249, 50)
        };
        private Texture2D cover;
        private Texture2D backdrop, activePlate, restingPlate;
        private GUIStyle heading, body, note, button, menuText;
        private Overlay overlay;
        private int selected;
        private Vector2 previousMouse;
        private float lastVolume = 1f;

        public void Open(bool hasSave)
        {
            overlay = Overlay.None;
            selected = hasSave ? 1 : 0;
            previousMouse = new Vector2(-1000, -1000);
        }

        public void Draw(GameSession session, Font font, Texture2D paper, Texture2D shade, Vector2 mouse)
        {
            if (body == null) Init(font);
            if (cover != null) GUI.DrawTexture(new Rect(0, 0, 1280, 720), cover, ScaleMode.ScaleToFit, false);
            else
            {
                GUI.DrawTexture(new Rect(0, 0, 1280, 720), shade);
                GUI.Label(new Rect(100, 90, 900, 80), "雾屿回响 · Mist Isle", heading);
                GUI.Label(new Rect(520, 245, 500, 40), "封面资源暂未载入", body);
            }
            bool liveMenu = cover != null && backdrop != null;
            if (liveMenu) DrawCleanMenuPatch();

            if (overlay == Overlay.None)
            {
                if ((mouse - previousMouse).sqrMagnitude > 1f)
                {
                    for (int i = 0; i < MenuBounds.Length; i++)
                        if (MenuBounds[i].Contains(mouse)) selected = i;
                    previousMouse = mouse;
                }
                HandleMenuKeys(session);
                for (int i = 0; i < MenuBounds.Length; i++)
                {
                    if (liveMenu) DrawMenuButton(MenuBounds[i], i, mouse);
                    if (GUI.Button(MenuBounds[i], GUIContent.none, GUIStyle.none)) Activate(session, i);
                    if (cover == null)
                        GUI.Label(MenuBounds[i], new[] { "开始冒险", "继续旅程", "设置", "退出" }[i], body);
                }
                DrawFocus(MenuBounds[selected]);
                ShadowLabel(new Rect(491, 534, 470, 28),
                    selected == 1 && !session.Saves.Exists ? "还没有旅程存档，从「开始冒险」启程。" :
                    selected == 0 && session.Saves.Exists ? "开启新的故事前，会先备份当前旅程。" : "第一章 · 归航之灯");
                ShadowLabel(new Rect(867, 676, 395, 26), "W / S · 选择     Enter · 确认");
                return;
            }

            GUI.DrawTexture(new Rect(0, 0, 1280, 720), shade);
            Rect box = new Rect(414, 217, 462, overlay == Overlay.Settings ? 346 : 285);
            GUI.DrawTexture(box, paper);
            Line(new Rect(box.x + 22, box.y + 20, box.width - 44, 2), new Color(.65f, .49f, .25f));
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            { overlay = Overlay.None; e.Use(); }
            if (overlay == Overlay.Settings) DrawSettings(box);
            else
            {
                bool newJourney = overlay == Overlay.NewJourney;
                GUI.Label(new Rect(box.x + 30, box.y + 36, 410, 45),
                    newJourney ? "重新启程？" : "暂别雾屿？", heading);
                GUI.Label(new Rect(box.x + 32, box.y + 100, 397, 72),
                    newJourney ? "当前旅程会先另存备份。\n随后，你将从与拾光的初遇开始新的故事。" :
                    "灯火会留在这里，等待你的下一次归来。", body);
                if (GUI.Button(new Rect(box.x + 31, box.y + 207, 190, 45), "留在这里", button))
                    overlay = Overlay.None;
                if (GUI.Button(new Rect(box.x + 241, box.y + 207, 190, 45), newJourney ? "开始新的旅程" : "离开", button))
                {
                    overlay = Overlay.None;
                    if (newJourney) session.NewJourney(); else session.Quit();
                }
            }
        }

        private void Init(Font font)
        {
            cover = Resources.Load<Texture2D>("Art/UI/TitleCover");
            backdrop = Resources.Load<Texture2D>("Art/UI/TitleBackdrop-v2");
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 19, wordWrap = true,
                normal = { textColor = new Color(.17f, .25f, .27f) } };
            heading = new GUIStyle(body) { fontSize = 29, fontStyle = FontStyle.Bold };
            note = new GUIStyle(body) { fontSize = 15, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.99f, .93f, .76f) } };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18,
                padding = new RectOffset(12, 12, 8, 8) };
            menuText = new GUIStyle(GUI.skin.label) { font = font, fontSize = 24, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.98f, .95f, .84f) } };
            activePlate = CreateMenuPlate(true);
            restingPlate = CreateMenuPlate(false);
        }

        private void DrawCleanMenuPatch()
        {
            // Top-left pixel coordinates in the 1672x941 approved original. Protected art lies outside this patch.
            const float x = 680f, y = 366f, width = 420f, height = 336f;
            var destination = new Rect(x / 1672f * 1280f, y / 941f * 720f,
                width / 1672f * 1280f, height / 941f * 720f);
            var uv = new Rect(x / 1672f, 1f - (y + height) / 941f, width / 1672f, height / 941f);
            GUI.DrawTextureWithTexCoords(destination, backdrop, uv, false);
        }

        private void DrawMenuButton(Rect rect, int index, Vector2 mouse)
        {
            bool focused = selected == index;
            bool pressed = Input.GetMouseButton(0) && rect.Contains(mouse);
            Color old = GUI.color;
            if (focused)
            {
                GUI.color = new Color(1f, .83f, .41f, .19f);
                GUI.DrawTexture(new Rect(rect.x - 5, rect.y - 4, rect.width + 10, rect.height + 8), activePlate);
            }
            GUI.color = pressed ? new Color(.88f, .86f, .82f) : Color.white;
            GUI.DrawTexture(rect, focused ? activePlate : restingPlate);
            GUI.color = old;
            menuText.normal.textColor = focused ? new Color(.22f, .15f, .09f) : new Color(.98f, .95f, .84f);
            string label = new[] { "开始冒险", "继续旅程", "设    置", "退    出" }[index];
            GUI.Label(new Rect(rect.x + 18, rect.y - 1, rect.width - 36, rect.height), label, menuText);
        }

        private static Texture2D CreateMenuPlate(bool focused)
        {
            const int width = 512, height = 96;
            var pixels = new Color[width * height];
            Color fill = focused ? new Color(1f, .91f, .69f, .98f) : new Color(.12f, .19f, .33f, .95f);
            Color border = focused ? new Color(1f, .76f, .33f) : new Color(.86f, .74f, .48f);
            for (int y = 0; y < height; y++)
            {
                int inset = 5 + Mathf.RoundToInt(Mathf.Abs(y - (height - 1) * .5f) * .43f);
                for (int x = inset; x < width - inset; x++)
                {
                    bool edge = x < inset + 3 || x >= width - inset - 3 || y < 3 || y >= height - 3;
                    bool inner = (y == 8 || y == height - 9) && x > 35 && x < width - 36;
                    pixels[y * width + x] = edge || inner ? border : fill;
                }
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = focused ? "TitleMenuSelected" : "TitleMenuResting", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Dispose()
        {
            if (activePlate != null) Object.Destroy(activePlate);
            if (restingPlate != null) Object.Destroy(restingPlate);
        }

        private void HandleMenuKeys(GameSession session)
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.UpArrow) selected = (selected + 3) % 4;
            else if (e.keyCode == KeyCode.S || e.keyCode == KeyCode.DownArrow) selected = (selected + 1) % 4;
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space)
                Activate(session, selected);
            else if (e.keyCode == KeyCode.Escape) overlay = Overlay.Quit;
            else return;
            e.Use();
        }

        private void Activate(GameSession session, int index)
        {
            selected = index;
            switch (index)
            {
                case 0:
                    if (session.Saves.Exists) overlay = Overlay.NewJourney;
                    else session.NewJourney();
                    break;
                case 1:
                    if (session.Saves.Exists) session.ContinueJourney();
                    else session.Notify("还没有旅程存档。选择「开始冒险」，与拾光相遇吧。");
                    break;
                case 2: overlay = Overlay.Settings; break;
                case 3: overlay = Overlay.Quit; break;
            }
        }

        private void DrawSettings(Rect box)
        {
            GUI.Label(new Rect(box.x + 30, box.y + 35, 390, 42), "声音与画面", heading);
            GUI.Label(new Rect(box.x + 32, box.y + 101, 390, 32), "总音量", body);
            AudioListener.volume = GUI.HorizontalSlider(new Rect(box.x + 32, box.y + 147, 285, 24), AudioListener.volume, 0f, 1f);
            GUI.Label(new Rect(box.x + 333, box.y + 132, 95, 35), Mathf.RoundToInt(AudioListener.volume * 100f) + "%", body);
            if (GUI.Button(new Rect(box.x + 32, box.y + 191, 185, 43), AudioListener.volume <= .001f ? "恢复声音" : "静音", button))
            {
                if (AudioListener.volume <= .001f) AudioListener.volume = lastVolume;
                else { lastVolume = AudioListener.volume; AudioListener.volume = 0f; }
            }
            if (GUI.Button(new Rect(box.x + 239, box.y + 191, 191, 43), Screen.fullScreen ? "切换为窗口" : "切换为全屏", button))
                Screen.fullScreen = !Screen.fullScreen;
            if (GUI.Button(new Rect(box.x + 32, box.y + 273, 398, 43), "返回", button))
                overlay = Overlay.None;
        }

        private void DrawFocus(Rect rect)
        {
            float glow = .5f + Mathf.Sin(Time.unscaledTime * 3f) * .18f;
            Color gold = new Color(1f, .88f, .48f, glow);
            Line(new Rect(rect.x + 18, rect.y + 2, rect.width - 36, 2), gold);
            Line(new Rect(rect.x + 18, rect.yMax - 3, rect.width - 36, 2), gold);
            ShadowLabel(new Rect(rect.x - 38, rect.y + 9, 30, 34), "◆");
        }

        private void ShadowLabel(Rect rect, string text)
        {
            Color old = GUI.color;
            GUI.color = new Color(.05f, .08f, .15f, .9f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), text, note);
            GUI.color = old;
            GUI.Label(rect, text, note);
        }

        private static void Line(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
