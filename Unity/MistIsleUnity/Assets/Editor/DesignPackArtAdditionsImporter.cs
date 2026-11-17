#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MistIsle.Editor
{
    /// <summary>Only new standalone map additions. Original handoff files and the historical importer stay untouched.</summary>
    public static class DesignPackArtAdditionsImporter
    {
        private const string Source = "Assets/Art/MapDesignAdditions/Source/";
        private const string Output = "Assets/Resources/Art/MapDesignAdditions/";
        private static string backupFolder;

        [MenuItem("Mist Isle/Design Pack/Import Art Additions")]
        public static void ImportAdditions()
        {
            Import(Source + "SupplyShopV1-source.png", null, "SupplyShopV1");
            // Top-left rectangles were reviewed against the real 774x274 transparent atlas, not its approximate manifest.
            Import(Source + "ApprovedEnvironmentAtlas.png", new RectInt(638, 0, 126, 156), "Lighthouse");
            Import(Source + "ApprovedEnvironmentAtlas.png", new RectInt(388, 111, 122, 109), "StoneArch");
            Import(Source + "ApprovedEnvironmentAtlas.png", new RectInt(123, 171, 154, 96), "Bridge");
            Import(Source + "ApprovedEnvironmentAtlas.png", new RectInt(0, 171, 122, 96), "Dock");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MIST_ISLE_DESIGN_ADDITIONS: independent SupplyShopV1, Lighthouse, StoneArch, Bridge and Dock imported with real alpha, 16PPU and bottom pivots.");
        }

        [MenuItem("Mist Isle/Design Pack/Rebuild Art Additions With Backup")]
        public static void RebuildAdditionsWithBackup()
        {
            backupFolder = Path.Combine("Builds", "ArtBackups", "map-design-additions-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            try { ImportAdditions(); ValidateAdditions(); }
            finally { backupFolder = null; }
        }

        private static void Import(string sourcePath, RectInt? reviewedRect, string name)
        {
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("Map art addition source was not copied into the project.", sourcePath);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(sourcePath), false)) throw new InvalidDataException(sourcePath);
                RectInt rect = reviewedRect ?? new RectInt(0, 0, source.width, source.height);
                if (rect.x < 0 || rect.y < 0 || rect.xMax > source.width || rect.yMax > source.height) throw new InvalidDataException("Reviewed addition rectangle exceeds source: " + name);
                Color32[] all = source.GetPixels32();
                int left = rect.width, right = -1, bottom = rect.height, top = -1;
                for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++)
                {
                    Color32 pixel = all[(source.height - rect.yMax + y) * source.width + rect.x + x];
                    if (pixel.a < 16) continue;
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                }
                if (right < left || top < bottom) throw new InvalidDataException("Empty map addition: " + name);
                // A one-pixel clear margin makes the bottom pivot explicit, without resampling any visible artwork.
                int width = right - left + 3, height = top - bottom + 3;
                var pixels = new Color32[width * height];
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++)
                {
                    Color32 pixel = all[(source.height - rect.yMax + y) * source.width + rect.x + x];
                    if (pixel.a >= 16) pixels[(y - bottom + 1) * width + x - left + 1] = pixel;
                }
                string outputPath = Output + name + ".png";
                Directory.CreateDirectory(Output);
                if (File.Exists(outputPath) && backupFolder != null)
                {
                    Directory.CreateDirectory(backupFolder);
                    File.Copy(outputPath, Path.Combine(backupFolder, name + ".png"), false);
                    if (File.Exists(outputPath + ".meta")) File.Copy(outputPath + ".meta", Path.Combine(backupFolder, name + ".png.meta"), false);
                }
                if (!File.Exists(outputPath) || backupFolder != null)
                {
                    var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    try { texture.SetPixels32(pixels); texture.Apply(false); File.WriteAllBytes(outputPath, texture.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(outputPath);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, 0f);
                settings.spriteGenerateFallbackPhysicsShape = false; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        [MenuItem("Mist Isle/Design Pack/Validate Art Additions")]
        public static void ValidateAdditions()
        {
            foreach (string name in new[] { "SupplyShopV1", "Lighthouse", "StoneArch", "Bridge", "Dock" })
            {
                string path = Output + name + ".png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (sprite == null || importer == null || importer.spritePixelsPerUnit != 16 || importer.filterMode != FilterMode.Point)
                    throw new InvalidDataException("Missing or incompatible map addition sprite: " + name);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) throw new InvalidDataException(path);
                    Color32[] pixels = texture.GetPixels32(); int clear = 0, solid = 0;
                    foreach (var pixel in pixels)
                        if (pixel.a == 0) { clear++; if (pixel.r != 0 || pixel.g != 0 || pixel.b != 0) throw new InvalidDataException("Hidden background RGB: " + name); }
                        else solid++;
                    if (clear == 0 || solid == 0) throw new InvalidDataException("Addition has no usable alpha: " + name);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            Debug.Log("MIST_ISLE_DESIGN_ADDITIONS_VALIDATED: 4 independent sprites with actual clear alpha and point imports. Visual rectangle review is separate from this check.");
        }
    }
}
#endif
