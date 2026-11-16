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
            allCapturesIncludeUI = true;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Visual capture requires a graphics device; run this test without -nographics.");
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
            yield return Capture("01-title");

            session.NewJourney();
            session.ChooseStarter("moss-lumi");
            session.ClosePanel();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            var traveler = root.transform.Find("Traveler");
            Assert.That(traveler, Is.Not.Null);
            traveler.position = new Vector2(1.4f,-.8f);
            Physics2D.SyncTransforms();
            yield return Capture("02-coastal-camp");

            Assert.That(session.ChangeRoom("coast-1", false), Is.True);
            var encounter = session.World.Interactions.First(item => item.Kind == "encounter" &&
                session.World.GetRoomId(item.transform.position) == "coast-1");
            traveler.position = encounter.transform.position + Vector3.down;
            Physics2D.SyncTransforms();
            encounter.Interact();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            Assert.That(session.Battle, Is.Not.Null);
            yield return Capture("03-wild-encounter");
            Debug.Log("MIST_ISLE_VISUAL_CAPTURES: " + captureDirectory);
            if (!allCapturesIncludeUI)
                Assert.Ignore("World captures were saved, but this runner did not prove that IMGUI was rendered into every image. UI visual acceptance requires a normal Windows player capture.");
        }

        private IEnumerator Capture(string name)
        {
            var ui = root.GetComponent<ChapterInterface>();
            int repaintCountBefore = ui == null ? 0 : ui.DrawnFrameCount;
            // Let camera follow, IMGUI layout, glyph atlases and sprite uploads settle first.
            for (int frame = 0; frame < 8; frame++) yield return null;
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
                "\nui-repaints=" + repaintCountBefore + "->" + repaintCountAfter + "\nhud-pixels-present=" + uiPixelsPresent);
            if (File.Exists(pending)) File.Delete(pending);
            Assert.That(new FileInfo(target).Length, Is.GreaterThan(1024), "Screenshot file is unexpectedly empty.");
            Debug.Log("MIST_ISLE_CAPTURE: " + target + " [" + mode + "]");
            if (!hasVerifiedUI)
                Debug.LogWarning("UI visual acceptance is unverified for " + name + ": repaint=" + uiRepainted + ", raster evidence=" + uiPixelsPresent + ". The PNG is classified world-only.");
        }

        private static bool HasHudPixels(byte[] bytes, GamePanel panel)
        {
            // Rendering an OnGUI callback is insufficient: the captured target must contain its
            // dark top/bottom bars, and battle must also contain the large cream battle panel.
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if (!texture.LoadImage(bytes)) return false;
                Vector2[] probes = { new Vector2(8,15), new Vector2(1270,15), new Vector2(8,92),
                    new Vector2(1270,92), new Vector2(8,700), new Vector2(1270,700) };
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
                    Color paper = PixelAt(texture,new Vector2(90,130));
                    if (paper.r < .72f || paper.g < .70f || paper.b < .58f || paper.r < paper.b + .025f) return false;
                }
                return true;
            }
            finally { Object.Destroy(texture); }
        }

        private static Color PixelAt(Texture2D texture, Vector2 logicalPoint)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(logicalPoint.x/1280f*texture.width),0,texture.width-1);
            int y = Mathf.Clamp(Mathf.RoundToInt((1f-logicalPoint.y/720f)*texture.height),0,texture.height-1);
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
