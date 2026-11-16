#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MistIsle.Editor
{
    /// <summary>Deterministic, non-destructive extraction of reviewed artwork into independent sprites.</summary>
    public static class ApprovedArtImporter
    {
        private const string Source = "Assets/Art/SourceReference/";
        private const string Prepared = "Assets/Art/Prepared/";
        private const string Output = "Assets/Resources/Art/";
        private static string preparedRebuildBackup;

        [MenuItem("Mist Isle/Rebuild Prepared Cutouts With Backup")]
        public static void RebuildPreparedCutouts()
        {
            preparedRebuildBackup = Path.Combine("Builds", "ArtBackups", DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            try { ImportApprovedArt(); }
            finally { preparedRebuildBackup = null; }
        }

        [MenuItem("Mist Isle/Import Approved Art")]
        public static void ImportApprovedArt()
        {
            ImportFrames("TravelerLegacy.png", "Actors/Traveler", "Traveler", 64, 80, 4, 4);
            ImportFrames("Watcher.png", "Actors/Watcher", "Watcher", 64, 80, 4, 4);
            ImportFrames("Ground.png", "Tiles/Ground", "Ground", 56, 56, 4, 12, true);
            CopySprite(Source + "House.png", "Environment/House.png", false);
            Crop(Source + "Environment96.png", new RectInt(0, 0, 96, 96), "Environment/Tree.png", true);
            Crop(Source + "Environment96.png", new RectInt(1056, 0, 96, 96), "Environment/Fence.png", true);
            Crop(Source + "Environment96.png", new RectInt(480, 0, 96, 96), "Environment/Signpost.png", true);
            Crop(Source + "PixelProps64.png", new RectInt(512, 0, 64, 64), "Environment/Campfire.png", true);

            // These inputs are transparent imagegen extraction candidates, never the original boards.
            ImportPrepared("Shiguang", "Actors/Shiguang.png");
            ImportPrepared("Shiguang", "Portraits/Shiguang.png");
            string[] species = { "moss-lumi", "moatail", "crystal-gecko", "moss-lumi-evolved", "moatail-evolved", "crystal-gecko-evolved",
                "wild-01", "wild-02", "wild-03", "wild-04", "wild-05", "wild-06", "wild-07", "wild-08", "wild-09", "wild-10", "wild-11", "wild-12" };
            foreach (string id in species) ImportPrepared(id, "Creatures/" + id + ".png");
            ImportPrepared("guardian-grove", "Creatures/GuardianGrove.png");
            ImportPrepared("guardian-tide", "Creatures/GuardianTide.png");
            ImportPrepared("guardian-crystal", "Creatures/GuardianCrystal.png");
            ImportPrepared("guardian-lighthouse", "Creatures/GuardianLighthouse.png");
            ImportPrepared("guardian-lighthouse", "Creatures/FinalBoss.png");
            ImportPrepared("guardian-grove", "Creatures/glowglade-warden.png");
            ImportPrepared("guardian-tide", "Creatures/tidal-warden.png");
            ImportPrepared("guardian-crystal", "Creatures/crystal-warden.png");
            ImportPrepared("guardian-lighthouse", "Creatures/lighthouse-warden.png");
            ImportPrepared("guardian-lighthouse", "Creatures/final-boss.png");
            // Compatible discoverable aliases for the world renderer, not additional species.
            Alias("Creatures/moss-lumi.png", "Creatures/MossLumi.png");
            Alias("Creatures/moatail.png", "Creatures/Moatail.png");
            Alias("Creatures/crystal-gecko.png", "Creatures/CrystalGecko.png");
            Alias("Creatures/wild-03.png", "Creatures/WildCrab.png");
            Alias("Creatures/wild-02.png", "Creatures/WildBird.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MIST_ISLE_ART_IMPORTED: independent actor frames, terrain, environment and available curated cutouts.");
        }

        private static void ImportPrepared(string name, string destination)
        {
            string path = Prepared + name + ".png";
            if (File.Exists(path)) CopySprite(path, destination, true);
            else Debug.LogWarning("Curated art is not available yet: " + path);
        }

        [MenuItem("Mist Isle/Validate Imported Art")]
        public static void ValidateImportedArt()
        {
            string[] species = { "moss-lumi", "moatail", "crystal-gecko", "moss-lumi-evolved", "moatail-evolved", "crystal-gecko-evolved",
                "wild-01", "wild-02", "wild-03", "wild-04", "wild-05", "wild-06", "wild-07", "wild-08", "wild-09", "wild-10", "wild-11", "wild-12" };
            foreach (string id in species)
                if (AssetDatabase.LoadAssetAtPath<Sprite>(Output + "Creatures/" + id + ".png") == null)
                    throw new InvalidDataException("Missing independent creature sprite: " + id);
            foreach (string role in new[] { "Shiguang", "../Creatures/GuardianGrove", "../Creatures/GuardianTide", "../Creatures/GuardianCrystal", "../Creatures/GuardianLighthouse" })
            {
                string path = role.StartsWith("../", StringComparison.Ordinal) ? Output + role.Substring(3) + ".png" : Output + "Actors/" + role + ".png";
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null) throw new InvalidDataException("Missing independent character sprite: " + path);
            }
            int traveler = AssetDatabase.FindAssets("t:Sprite", new[] { Output + "Actors/Traveler" }).Length;
            int watcher = AssetDatabase.FindAssets("t:Sprite", new[] { Output + "Actors/Watcher" }).Length;
            int ground = AssetDatabase.FindAssets("t:Sprite", new[] { Output + "Tiles/Ground" }).Length;
            if (traveler != 16 || watcher != 16 || ground != 48)
                throw new InvalidDataException("Unexpected frame count: traveler=" + traveler + ", watcher=" + watcher + ", ground=" + ground);
            Debug.Log("MIST_ISLE_ART_VALIDATED: 18 individual creature designs, 4 guardian designs, Shiguang, 32 actor frames and 48 terrain tiles. Static candidate creature animation is not production-complete.");
        }

        private static void Alias(string existing, string alias)
        {
            if (string.Equals(existing, alias, StringComparison.OrdinalIgnoreCase)) return;
            if (File.Exists(Output + existing)) CopySprite(Output + existing, alias, false);
        }

        private static void ImportFrames(string source, string folder, string name, int width, int height, int columns, int rows, bool center = false)
        {
            string path = Source + source;
            if (!File.Exists(path)) throw new FileNotFoundException("Missing approved source image", path);
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    Crop(path, new RectInt(column * width, row * height, width, height), folder + "/" + name + "_" + index.ToString("00") + ".png", false, center);
                }
        }

        private static void CopySprite(string source, string relativeDestination, bool trim)
        {
            Texture2D image = ReadTexture(source);
            try { WritePixels(image, new RectInt(0, 0, image.width, image.height), relativeDestination, trim, false); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        // Rectangle origin is explicitly top-left, matching the human-reviewed source coordinates.
        private static void Crop(string source, RectInt topLeftRect, string destination, bool trim, bool center = false)
        {
            Texture2D image = ReadTexture(source);
            try { WritePixels(image, topLeftRect, destination, trim, center); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        private static Texture2D ReadTexture(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(image);
                throw new InvalidDataException("Image could not be decoded: " + path);
            }
            return image;
        }

        private static void WritePixels(Texture2D image, RectInt topLeft, string relative, bool trim, bool center)
        {
            if (topLeft.x < 0 || topLeft.y < 0 || topLeft.xMax > image.width || topLeft.yMax > image.height)
                throw new ArgumentOutOfRangeException(nameof(topLeft), "Reviewed sprite rectangle is outside its source image.");
            Color[] pixels = image.GetPixels(topLeft.x, image.height - topLeft.yMax, topLeft.width, topLeft.height);
            int left = 0, bottom = 0, width = topLeft.width, height = topLeft.height;
            if (trim)
            {
                int right = -1, top = -1;
                left = width; bottom = height;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    // Bounds ignore sub-visible alpha noise; retained source pixels are copied unchanged.
                    if (pixels[y * width + x].a >= 16f / 255f)
                    { left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y); right = Mathf.Max(right, x); top = Mathf.Max(top, y); }
                if (right < left || top < bottom) throw new InvalidDataException("Sprite crop is empty: " + relative);
                int trimmedWidth = right - left + 1, trimmedHeight = top - bottom + 1;
                var trimmed = new Color[trimmedWidth * trimmedHeight];
                for (int y = 0; y < trimmedHeight; y++)
                    Array.Copy(pixels, (bottom + y) * width + left, trimmed, y * trimmedWidth, trimmedWidth);
                pixels = trimmed; width = trimmedWidth; height = trimmedHeight;
            }
            string target = Output + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            // A repeated import preserves independently corrected PNGs already present in the project.
            bool rebuilding = preparedRebuildBackup != null && (relative.StartsWith("Creatures/", StringComparison.Ordinal)
                || relative == "Actors/Shiguang.png" || relative == "Portraits/Shiguang.png");
            if (rebuilding && File.Exists(target))
            {
                string backup = Path.Combine(preparedRebuildBackup, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup)) File.Copy(target, backup, false);
            }
            if (!File.Exists(target) || rebuilding)
            {
                var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.SetPixels(pixels); result.Apply(false);
                File.WriteAllBytes(target, result.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(result);
            }
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.spritePivot = center ? new Vector2(.5f, .5f) : new Vector2(.5f, 0f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = importer.spritePivot;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
    }
}
#endif
