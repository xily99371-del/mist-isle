using System;
using System.Collections;
using System.IO;
using System.Linq;
using MistIsle.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class VisualSmokeTests
    {
        private GameObject root;
        private GameSession session;
        private Camera[] previousCameras;
        private string temporarySaveDirectory;
        private string captureDirectory;
        private bool allCapturesIncludeUI;

        [UnityTest]
        public IEnumerator CaptureTitleCoastalCampAndWildEncounter()
        {
            if (Application.isBatchMode)
                Assert.Ignore("IMGUI screenshots require normal Unity Play Mode or a normal Windows player. Batchmode is not visual acceptance.");
            allCapturesIncludeUI = true;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Visual capture requires a graphics device; run this test without -nographics.");
#if UNITY_EDITOR
            // This is the application's own frame capture, not a desktop/lock-screen capture.
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
#endif
            Application.runInBackground = true;
            previousCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            temporarySaveDirectory = Path.Combine(Path.GetTempPath(), "MistIsleVisualSmoke-" + Guid.NewGuid().ToString("N"));
            captureDirectory = Environment.GetEnvironmentVariable("MIST_ISLE_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
                captureDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Verification", "Captures"));
            Directory.CreateDirectory(captureDirectory);
            root = new GameObject("VisualSmokeSession");
            if (!Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None).Any(item => item.lightType == Light2D.LightType.Global))
            {
                var ambient = new GameObject("VisualSmokeDaylight", typeof(Light2D));
                ambient.transform.SetParent(root.transform, false);
                var light = ambient.GetComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.color = new Color(.87f,.94f,1f);
                light.intensity = .92f;
            }
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = temporarySaveDirectory;
            yield return null;
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Title));
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(temporarySaveDirectory, "chapter-01.json")));
            yield return Capture("01-title", GamePanel.Title);

            session.NewJourney();
            yield return Capture("02-dock-arrival", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            PlaceTraveler("watcher-approach");
            session.World.Interactions.Single(x => x.Id == "watcher").Interact();
            yield return Capture("03-watcher-first-meeting", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            PlaceTraveler("shiguang-approach");
            session.World.Interactions.Single(x => x.Id == "shiguang").Interact();
            yield return Capture("04-shiguang-first-meeting", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            session.World.Interactions.Single(x => x.Kind == "starter" && x.Id == "moss-lumi").Interact();
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Starter));
            yield return Capture("05-partner-choice", GamePanel.Starter);
            session.ChooseStarter("moss-lumi");
            yield return Capture("06-partner-bond", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            foreach (string item in new[] { "berry", "dew" })
            {
                var resource = session.World.Interactions.First(x => x.Kind == "resource" && x.Id.StartsWith(item + "-camp-0-"));
                PlaceTraveler(resource.transform.position + Vector3.down);
                resource.Interact();
                yield return OpeningTestDriver.FinishDialogue(session);
            }
            PlaceTraveler("campfire-approach");
            session.World.Interactions.Single(x => x.Id == "M00_CAMPFIRE").Interact();
            yield return Capture("07-campfire-crafting", GamePanel.Campfire);
            session.Craft(session.Recipes.Single(x => x.recipeId == "berry-treat"));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            // Walk behind the actual guide rather than teleporting past her for a misleading scene.
            yield return AccompanyGuideAlongCoast();
            PlaceTraveler(session.World.GetLandmarkPosition("tutorial-viewpoint") + Vector2.left * 1.2f);
            session.World.Interactions.Single(x => x.Id == "tutorial-coast-crab").Interact();
            yield return Capture("08-crab-story", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            yield return Capture("09-capture-lesson", GamePanel.Battle);
            session.Act(MistIsle.Creatures.BattleAction.Attack);
            session.Act(MistIsle.Creatures.BattleAction.Feed, "berry-treat");
            session.LeaveBattle();
            yield return Capture("10-crab-trust", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.ChangeRoom("camp-0", false), Is.True);
            PlaceTraveler("watcher-approach");
            session.World.Interactions.Single(x => x.Id == "watcher").Interact();
            yield return Capture("11-return-to-watcher", GamePanel.Dialogue);
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.Complete));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            var traveler = root.transform.Find("Traveler");
            Assert.That(traveler, Is.Not.Null);
            PlaceTraveler("campfire-approach");
            yield return Capture("12-coastal-camp", GamePanel.Explore);

            var shop = session.World.Interactions.Single(item => item.Kind == "shop");
            PlaceTraveler((Vector2)shop.transform.position + Vector2.down);
            shop.Interact();
            yield return Capture("13-supply-shop", GamePanel.Shop);
            session.ClosePanel();
            session.SetPanel(GamePanel.WorldMap);
            yield return Capture("14-camp-world-map", GamePanel.WorldMap);
            session.ClosePanel();
            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            var encounter = session.World.Interactions.First(item => item.Kind == "encounter" &&
                item.Id != "tutorial-coast-crab" && session.World.GetRoomId(item.transform.position) == "coast-0");
            traveler.position = encounter.transform.position + Vector3.down;
            Physics2D.SyncTransforms();
            encounter.Interact();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            Assert.That(session.Battle, Is.Not.Null);
            yield return Capture("15-wild-encounter", GamePanel.Battle);
            File.WriteAllText(Path.Combine(captureDirectory, "opening-replay-kind.txt"),
                "Normal Editor Play Mode; application ScreenCapture includes IMGUI when raster checks pass.\n" +
                "Story progression uses session commands and explicit traveler placement. This is not physical keyboard/mouse playthrough acceptance.\n");
            Debug.Log("MIST_ISLE_VISUAL_CAPTURES: " + captureDirectory);
            if (!allCapturesIncludeUI)
                Assert.Ignore("World captures were saved, but this runner did not prove that IMGUI was rendered into every image. UI visual acceptance requires a normal Windows player capture.");
        }

        private void PlaceTraveler(string landmark) => PlaceTraveler(session.World.GetLandmarkPosition(landmark));

        private IEnumerator AccompanyGuideAlongCoast()
        {
            var guide = session.World.GetActor("shiguang");
            Vector2 destination = session.World.GetLandmarkPosition("tutorial-viewpoint");
            float deadline = Time.realtimeSinceStartup + 30f;
            while (Vector2.Distance(guide.position, destination) > .2f && Time.realtimeSinceStartup < deadline)
            {
                Vector2 following = (Vector2)guide.position + Vector2.left * 1.15f;
                PlaceTraveler(Vector2.MoveTowards(session.PlayerTransform.position, following, 3f * Time.unscaledDeltaTime));
                yield return null;
            }
            Assert.That(Vector2.Distance(guide.position, destination), Is.LessThan(.2f), "The actual guide never reached the lesson viewpoint.");
        }

        private void PlaceTraveler(Vector2 position)
        {
            var player = session.PlayerTransform;
            player.GetComponent<Rigidbody2D>().position = position;
            player.position = position;
            Physics2D.SyncTransforms();
        }

        private IEnumerator Capture(string name, GamePanel expectedPanel)
        {
            Assert.That(session.Panel, Is.EqualTo(expectedPanel), "The story skipped the intended presentation: " + name);
            // This replay places the traveler explicitly; settle the real follow camera at that authored pose.
            var follow = Object.FindObjectsByType<MistIsle.Player.CoastalCamera>(FindObjectsSortMode.None)
                .FirstOrDefault(camera => camera.Target == session.PlayerTransform);
            follow?.Snap();
            var ui = root.GetComponent<ChapterInterface>();
            int repaintCountBefore = ui == null ? 0 : ui.DrawnFrameCount;
            // Let camera follow, IMGUI layout, glyph atlases and sprite uploads settle first.
            for (int frame = 0; frame < 8; frame++) yield return null;
            Assert.That(session.Panel, Is.EqualTo(expectedPanel), "The intended presentation closed before capture: " + name);
            string pending = Path.Combine(captureDirectory, name + "-" + Guid.NewGuid().ToString("N") + ".png");
            string target = Path.Combine(captureDirectory, name + ".png");
            ScreenCapture.CaptureScreenshot(pending);
            byte[] bytes = null;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                yield return null;
                if (File.Exists(pending))
                {
                    try
                    {
                        var candidate = File.ReadAllBytes(pending);
                        if (IsCompletePng(candidate)) { bytes = candidate; break; }
                    }
                    catch (IOException) { /* The graphics thread is still finishing this file. */ }
                }
                yield return new WaitForSecondsRealtime(.05f);
            }
            bool cameraFallback = false;
            if (bytes == null)
            {
                cameraFallback = true;
                Debug.LogWarning("ScreenCapture did not produce " + name + "; using a camera-only fallback that excludes IMGUI.");
                bytes = CaptureCamera();
            }
            Assert.That(IsCompletePng(bytes), Is.True, "Screenshot was missing or incomplete: " + name);
            int repaintCountAfter = ui == null ? 0 : ui.DrawnFrameCount;
            bool uiRepainted = repaintCountAfter > repaintCountBefore;
            bool uiPixelsPresent = !cameraFallback && HasHudPixels(bytes, session.Panel);
            bool hasVerifiedUI = !cameraFallback && uiRepainted && uiPixelsPresent;
            string mode = hasVerifiedUI ? "screen-with-hud" : cameraFallback ? "camera-world-only" : "screen-world-only";
            allCapturesIncludeUI &= hasVerifiedUI;
            File.WriteAllBytes(target, bytes);
            File.WriteAllText(Path.Combine(captureDirectory, name + ".capture-mode.txt"), mode + "\n" + Screen.width + "x" + Screen.height +
                "\nui-repaints=" + repaintCountBefore + "->" + repaintCountAfter + "\nhud-pixels-present=" + uiPixelsPresent +
                "\npanel=" + expectedPanel + "\nspeaker=" + session.DialogueSpeakerId);
            if (File.Exists(pending)) File.Delete(pending);
            Assert.That(new FileInfo(target).Length, Is.GreaterThan(1024), "Screenshot file is unexpectedly empty.");
            Debug.Log("MIST_ISLE_CAPTURE: " + target + " [" + mode + "]");
            if (!hasVerifiedUI)
                Debug.LogWarning("UI visual acceptance is unverified for " + name + ": repaint=" + uiRepainted + ", raster evidence=" + uiPixelsPresent + ". The PNG is classified world-only.");
        }

        private static bool HasHudPixels(byte[] bytes, GamePanel panel)
        {
            // Rendering an OnGUI callback is insufficient: the captured target must contain its
            // dark HUD cards, and battle must also contain the large cream battle panel.
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if (!texture.LoadImage(bytes)) return false;
                if (panel == GamePanel.Title) return HasApprovedTitlePixels(texture);
                if (panel == GamePanel.Starter)
                {
                    Color paper = PixelAt(texture, new Vector2(100, 110));
                    Color edge = PixelAt(texture, new Vector2(20, 110));
                    return paper.r > .7f && paper.g > .65f && paper.r > edge.r + .18f;
                }
                // Insets avoid labels, buttons, the exploration-only footer and the temporary toast.
                Vector2[] probes = { new Vector2(26,20), new Vector2(730,90),
                    new Vector2(1060,90), new Vector2(1250,22) };
                Color first = PixelAt(texture,probes[0]);
                foreach (Vector2 point in probes)
                {
                    Color pixel = PixelAt(texture,point);
                    float brightness = .2126f*pixel.r + .7152f*pixel.g + .0722f*pixel.b;
                    if (brightness > .30f || pixel.b > .42f ||
                        Mathf.Abs(pixel.r-first.r) > .14f || Mathf.Abs(pixel.g-first.g) > .14f || Mathf.Abs(pixel.b-first.b) > .14f)
                        return false;
                }
                if (panel == GamePanel.Battle)
                {
                    Color paper = PixelAt(texture,new Vector2(138,132));
                    if (paper.r < .72f || paper.g < .70f || paper.b < .58f || paper.r < paper.b + .025f) return false;
                }
                return true;
            }
            finally { Object.Destroy(texture); }
        }

        private static bool HasApprovedTitlePixels(Texture2D frame)
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                original.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources/Art/UI/TitleCover.png")));
                // Protected illustration and logo regions, outside the live menu patch.
                Vector2[] probes = { new Vector2(180, 400), new Vector2(310, 620), new Vector2(1120, 75),
                    new Vector2(760, 120), new Vector2(1070, 290), new Vector2(1000, 620) };
                foreach (var point in probes)
                {
                    Color expected = original.GetPixel(Mathf.RoundToInt(point.x / 1280f * original.width),
                        Mathf.RoundToInt((1f - point.y / 720f) * original.height));
                    Color actual = PixelAt(frame, point);
                    if (Mathf.Abs(expected.r - actual.r) + Mathf.Abs(expected.g - actual.g) + Mathf.Abs(expected.b - actual.b) > .28f)
                        return false;
                }
                return true;
            }
            finally { Object.Destroy(original); }
        }

        private static Color PixelAt(Texture2D texture, Vector2 logicalPoint)
        {
            float scale = Mathf.Min(texture.width / 1280f, texture.height / 720f);
            float offsetX = (texture.width - 1280f * scale) * .5f;
            float offsetY = (texture.height - 720f * scale) * .5f;
            int x = Mathf.Clamp(Mathf.RoundToInt(offsetX + logicalPoint.x * scale), 0, texture.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(texture.height - offsetY - logicalPoint.y * scale), 0, texture.height - 1);
            return texture.GetPixel(x,y);
        }

        private static bool IsCompletePng(byte[] bytes)
        {
            if (bytes == null || bytes.Length <= 1024) return false;
            int length = bytes.Length;
            return bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 &&
                bytes[length-8] == 73 && bytes[length-7] == 69 && bytes[length-6] == 78 && bytes[length-5] == 68;
        }

        private static byte[] CaptureCamera()
        {
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var target = RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(texture);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (previousCameras != null && !previousCameras.Contains(camera)) Object.Destroy(camera.gameObject);
            yield return null;
            if (!string.IsNullOrEmpty(temporarySaveDirectory) && Directory.Exists(temporarySaveDirectory))
            {
                string full = Path.GetFullPath(temporarySaveDirectory);
                string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(full).StartsWith("MistIsleVisualSmoke-",StringComparison.Ordinal))
                    throw new InvalidOperationException("Refusing to remove a directory outside this visual test's save folder.");
                Directory.Delete(full,true);
            }
        }
    }
}
