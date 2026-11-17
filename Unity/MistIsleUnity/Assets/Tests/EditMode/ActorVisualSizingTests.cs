using System.Collections.Generic;
using MistIsle.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace MistIsle.Tests
{
    public sealed class ActorVisualSizingTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var value in owned) if (value != null) Object.DestroyImmediate(value);
            owned.Clear();
        }

        private Sprite CreatePose(int canvasWidth, int canvasHeight, RectInt visible)
        {
            var texture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);
            owned.Add(texture);
            var pixels = new Color32[canvasWidth * canvasHeight];
            for (int y = visible.yMin; y < visible.yMax; y++)
                for (int x = visible.xMin; x < visible.xMax; x++) pixels[y * canvasWidth + x] = new Color32(200, 150, 100, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, canvasWidth, canvasHeight), new Vector2(.5f, 0), 64);
            owned.Add(sprite);
            return sprite;
        }

        private SpriteRenderer Renderer(Sprite sprite)
        {
            var actor = new GameObject("ActorRoot");
            owned.Add(actor);
            var visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(actor.transform, false);
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        [TestCase(128, 128, 29, 3, 69, 96)]
        [TestCase(128, 128, 33, 3, 62, 87)]
        [TestCase(64, 80, 10, 1, 44, 76)]
        public void TransparentCanvasDoesNotChangeVisibleHumanHeightOrFootBaseline(
            int width, int height, int x, int y, int visibleWidth, int visibleHeight)
        {
            var sprite = CreatePose(width, height, new RectInt(x, y, visibleWidth, visibleHeight));
            var renderer = Renderer(sprite);
            ActorVisualSizing.ApplyHuman(renderer, sprite);
            Assert.That(ActorVisualSizing.GetVisibleWorldHeight(renderer), Is.EqualTo(1.62f).Within(.001f));
            Rect local = ActorVisualSizing.GetVisibleLocalBounds(sprite);
            Vector3 foot = renderer.transform.TransformPoint(new Vector3(0f, local.yMin, 0f));
            Assert.That(foot.y, Is.EqualTo(0f).Within(.001f));
            Vector3 size = renderer.transform.localScale;
            Vector3 offset = renderer.transform.localPosition;
            ActorVisualSizing.ApplyHuman(renderer, sprite);
            Assert.That(renderer.transform.localScale, Is.EqualTo(size));
            Assert.That(renderer.transform.localPosition, Is.EqualTo(offset));
        }

        [Test]
        public void ChangingWalkFramesKeepsReferenceScaleAndFootOffsetConstant()
        {
            var neutral = CreatePose(128, 128, new RectInt(29, 3, 69, 96));
            var step = CreatePose(128, 128, new RectInt(29, 3, 69, 94));
            var renderer = Renderer(neutral);
            ActorVisualSizing.ApplyHuman(renderer, neutral);
            Vector3 scale = renderer.transform.localScale;
            Vector3 offset = renderer.transform.localPosition;
            renderer.sprite = step;
            Assert.That(ActorVisualSizing.GetVisibleWorldHeight(renderer), Is.LessThan(ActorVisualSizing.HumanVisibleHeight));
            Assert.That(renderer.transform.localScale, Is.EqualTo(scale), "An authored head bob must not trigger runtime rescaling.");
            Assert.That(renderer.transform.localPosition, Is.EqualTo(offset));
            Rect visible = ActorVisualSizing.GetVisibleLocalBounds(step);
            Assert.That(renderer.transform.TransformPoint(new Vector3(0, visible.yMin)).y, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void ActualApprovedReferencesHaveEqualVisibleHeightDespiteDifferentCanvasPadding()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Actual unreadable sprite alpha needs a graphics device.");
            string[] paths = {
                "Art/Actors/ApprovedTraveler/ApprovedTraveler_00",
                "Art/Actors/ShiguangPixel/ShiguangPixel_00",
                "Art/Actors/Watcher/Watcher_00" };
            int[] expectedVisiblePixels = { 96, 87, 76 };
            for (int i = 0; i < paths.Length; i++)
            {
                var reference = Resources.Load<Sprite>(paths[i]);
                Assert.That(reference, Is.Not.Null, paths[i]);
                var renderer = Renderer(reference);
                Rect bounds = ActorVisualSizing.GetVisibleLocalBounds(reference);
                Assert.That(bounds.height * reference.pixelsPerUnit, Is.EqualTo(expectedVisiblePixels[i]).Within(.01f), paths[i]);
                ActorVisualSizing.ApplyHuman(renderer, reference);
                Assert.That(ActorVisualSizing.GetVisibleWorldHeight(renderer), Is.EqualTo(1.62f).Within(.002f), paths[i]);
                Assert.That(renderer.transform.TransformPoint(new Vector3(0, bounds.yMin)).y, Is.EqualTo(0f).Within(.002f), paths[i]);
            }
        }

        [Test]
        public void ApprovedWalkingSheetsKeepTheirFootLineAndOnlySmallAuthoredHeadBobs()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Actual unreadable sprite alpha needs a graphics device.");
            foreach (string path in new[] { "Art/Actors/ApprovedTraveler", "Art/Actors/ShiguangPixel" })
            {
                var frames = Resources.LoadAll<Sprite>(path);
                System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
                Assert.That(frames.Length, Is.EqualTo(12));
                for (int direction = 0; direction < 4; direction++)
                {
                    float minHeight = float.MaxValue, maxHeight = 0f;
                    for (int step = 0; step < 3; step++)
                    {
                        var frame = frames[direction * 3 + step];
                        Rect bounds = ActorVisualSizing.GetVisibleLocalBounds(frame);
                        float footPixel = bounds.yMin * frame.pixelsPerUnit + frame.pivot.y;
                        float height = bounds.height * frame.pixelsPerUnit;
                        Assert.That(footPixel, Is.EqualTo(3f).Within(.01f), frame.name);
                        minHeight = Mathf.Min(minHeight, height); maxHeight = Mathf.Max(maxHeight, height);
                    }
                    Assert.That(maxHeight - minHeight, Is.LessThanOrEqualTo(3.01f), path + " direction " + direction);
                }
            }
        }

        [Test]
        public void UnreadableTextureMeasurementRestoresPreviousRenderTarget()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("GPU readback is unavailable with Null graphics.");
            var sprite = CreatePose(128, 128, new RectInt(29, 3, 69, 96));
            sprite.texture.Apply(false, true);
            var sentinel = new RenderTexture(16, 16, 0);
            owned.Add(sentinel);
            sentinel.Create();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = sentinel;
                var renderer = Renderer(sprite);
                ActorVisualSizing.ApplyHuman(renderer, sprite);
                Assert.That(RenderTexture.active, Is.SameAs(sentinel));
                Assert.That(ActorVisualSizing.GetVisibleWorldHeight(renderer), Is.EqualTo(1.62f).Within(.002f));
                Assert.That(ActorVisualSizing.GetVisibleLocalBounds(sprite).height * 64f, Is.EqualTo(96f).Within(.01f));
            }
            finally { RenderTexture.active = previous; sentinel.Release(); }
        }
    }
}
