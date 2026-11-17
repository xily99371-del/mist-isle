using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MistIsle.Player
{
    /// <summary>Sizes human actors from fixed neutral references. Each direction shares its scale across all walk frames.</summary>
    public static class ActorVisualSizing
    {
        public const float HumanVisibleHeight = 1.62f;
        private const byte VisibleAlpha = 32;
        private static readonly Dictionary<Sprite, Rect> BoundsCache = new Dictionary<Sprite, Rect>();
        private static readonly Dictionary<SpriteRenderer, Sprite> HumanReferences = new Dictionary<SpriteRenderer, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            BoundsCache.Clear();
            HumanReferences.Clear();
        }

        public static void ApplyHuman(SpriteRenderer renderer, Sprite referencePose = null)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            var reference = referencePose != null ? referencePose : renderer.sprite;
            if (reference == null) throw new ArgumentException("A reference pose is required.", nameof(referencePose));
            ApplyReference(renderer, reference);
        }

        public static void RemoveHuman(SpriteRenderer renderer)
        {
            // A destroyed Unity object compares equal to null but can still be a dictionary key.
            if (!ReferenceEquals(renderer, null)) HumanReferences.Remove(renderer);
        }

        internal static void ApplyFacingReference(SpriteRenderer renderer, Sprite reference)
        {
            if (!HumanReferences.TryGetValue(renderer, out var current) || current == reference) return;
            ApplyReference(renderer, reference);
        }

        private static void ApplyReference(SpriteRenderer renderer, Sprite reference)
        {
            Rect visible = GetVisibleLocalBounds(reference);
            if (visible.height <= .0001f) throw new ArgumentException("The reference pose has no visible alpha.", nameof(reference));
            float scale = HumanVisibleHeight / Mathf.Max(.0001f, visible.height);
            renderer.transform.localScale = Vector3.one * scale;
            Vector3 position = renderer.transform.localPosition;
            position.y = -visible.yMin * scale;
            renderer.transform.localPosition = position;
            HumanReferences[renderer] = reference;
        }

        public static float GetVisibleWorldHeight(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return 0f;
            return GetVisibleLocalBounds(renderer.sprite).height * Mathf.Abs(renderer.transform.lossyScale.y);
        }

        public static Rect GetVisibleLocalBounds(Sprite sprite)
        {
            if (sprite == null) return Rect.zero;
            if (BoundsCache.TryGetValue(sprite, out Rect cached)) return cached;
            Texture2D readable = null;
            bool ownsReadable = false;
            try
            {
                if (sprite.texture.isReadable) readable = sprite.texture;
                else if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture temporary = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0,
                        RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                    try
                    {
                        Graphics.Blit(sprite.texture, temporary);
                        RenderTexture.active = temporary;
                        readable = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false, true);
                        ownsReadable = true;
                        readable.ReadPixels(new Rect(0, 0, readable.width, readable.height), 0, 0);
                        readable.Apply(false, false);
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(temporary);
                    }
                }
                Rect result;
                if (readable != null)
                {
                    Rect region = sprite.rect;
                    var pixels = readable.GetPixels32();
                    int minX = Mathf.FloorToInt(region.xMax), minY = Mathf.FloorToInt(region.yMax);
                    int maxX = -1, maxY = -1;
                    for (int y = Mathf.FloorToInt(region.yMin); y < Mathf.CeilToInt(region.yMax); y++)
                        for (int x = Mathf.FloorToInt(region.xMin); x < Mathf.CeilToInt(region.xMax); x++)
                        {
                            if (pixels[y * readable.width + x].a < VisibleAlpha) continue;
                            minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                            maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                        }
                    result = maxX < minX ? Rect.zero : new Rect(
                        (minX - region.x - sprite.pivot.x) / sprite.pixelsPerUnit,
                        (minY - region.y - sprite.pivot.y) / sprite.pixelsPerUnit,
                        (maxX - minX + 1) / sprite.pixelsPerUnit, (maxY - minY + 1) / sprite.pixelsPerUnit);
                }
                else
                {
                    // No graphics device: imported sprite bounds are a deterministic, documented headless fallback.
                    result = new Rect(sprite.bounds.min.x, sprite.bounds.min.y, sprite.bounds.size.x, sprite.bounds.size.y);
                }
                BoundsCache[sprite] = result;
                return result;
            }
            finally
            {
                if (ownsReadable && readable != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(readable);
                    else UnityEngine.Object.DestroyImmediate(readable);
                }
            }
        }
    }
}
