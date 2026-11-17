#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MistIsle.Editor
{
    /// <summary>Imports original 128x128 atlases as 64 exact, top-row-first 16px tiles.</summary>
    public static class DesignPackAssetImporter
    {
        private const string Source = "Assets/Art/DesignPackSource/Tilesets/";
        private const string Output = "Assets/Resources/Art/DesignPack/Tiles/";
        private static readonly string[] Regions = { "Common", "Coastal", "Forest", "TidalRuins", "Lighthouse" };

        [MenuItem("Mist Isle/Import Design Pack Terrain")]
        public static void ImportDesignPack()
        {
            foreach (string region in Regions)
            {
                string sourcePath = Source + "TS_" + region + "_8x8_16px.png";
                if (!File.Exists(sourcePath)) throw new FileNotFoundException("Original design-pack atlas is missing.", sourcePath);
                AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(sourcePath);
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 128; importer.SaveAndReimport();
                var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                if (atlas == null || atlas.width != 128 || atlas.height != 128) throw new InvalidDataException("Expected 128x128 original atlas: " + sourcePath);
                Color32[] pixels = atlas.GetPixels32();
                string directory = Output + region; Directory.CreateDirectory(directory);
                for (int index = 0; index < 64; index++)
                {
                    var tile = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                    try {
                        var cell = new Color32[256]; int x0 = index % 8 * 16, y0 = (7 - index / 8) * 16;
                        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) cell[y * 16 + x] = pixels[(y0 + y) * 128 + x0 + x];
                        tile.SetPixels32(cell); tile.Apply(); string path = directory + "/Tile_" + index.ToString("000") + ".png";
                        byte[] bytes = tile.EncodeToPNG();
                        if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes)) File.WriteAllBytes(path, bytes);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        var sprite = (TextureImporter)AssetImporter.GetAtPath(path);
                        sprite.textureType = TextureImporterType.Sprite; sprite.spriteImportMode = SpriteImportMode.Single;
                        sprite.spritePixelsPerUnit = 16f; sprite.spritePivot = new Vector2(.5f, .5f);
                        sprite.filterMode = FilterMode.Point; sprite.textureCompression = TextureImporterCompression.Uncompressed;
                        sprite.mipmapEnabled = false; sprite.npotScale = TextureImporterNPOTScale.None; sprite.isReadable = true;
                        var settings = new TextureImporterSettings(); sprite.ReadTextureSettings(settings);
                        settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Center;
                        settings.spriteGenerateFallbackPhysicsShape = false; sprite.SetTextureSettings(settings);
                        sprite.SaveAndReimport();
                    }
                    finally { UnityEngine.Object.DestroyImmediate(tile); }
                }
            }
            AssetDatabase.SaveAssets();
            ValidateImportedTiles();
            Debug.Log("MIST_ISLE_DESIGN_PACK_TILES_IMPORTED: 320 original-pixel 16x16 / PPU16 tiles.");
        }

        public static void ValidateImportedTiles()
        {
            foreach (string region in Regions)
            {
                string sourcePath = Source + "TS_" + region + "_8x8_16px.png";
                var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath); var pixels = atlas.GetPixels32();
                for (int index = 0; index < 64; index++)
                {
                    string path = Output + region + "/Tile_" + index.ToString("000") + ".png";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null || sprite.pixelsPerUnit != 16f || sprite.rect.size != new Vector2(16f, 16f))
                        throw new InvalidDataException("Incorrect imported tile size/PPU: " + path);
                    Color32[] cell = sprite.texture.GetPixels32(); int x0 = index % 8 * 16, y0 = (7 - index / 8) * 16;
                    for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                        if (!cell[y * 16 + x].Equals(pixels[(y0 + y) * 128 + x0 + x]))
                            throw new InvalidDataException("Tile pixels differ from original atlas: " + path);
                }
            }
        }
    }
}
#endif
