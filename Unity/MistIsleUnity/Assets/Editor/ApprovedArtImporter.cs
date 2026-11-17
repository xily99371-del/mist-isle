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
        private static string originalPixelRebuildBackup;
        private static string portraitRebuildBackup;
        private static readonly Vector2Int[] WatcherPaperVoidSeeds = {
            new Vector2Int(310, 330), new Vector2Int(735, 750), new Vector2Int(790, 755)
        };

        [MenuItem("Mist Isle/Rebuild Approved Portraits With Backup")]
        public static void RebuildApprovedPortraitsWithBackup()
        {
            portraitRebuildBackup = Path.Combine("Builds", "ArtBackups", "approved-portraits-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            try
            {
                ImportApprovedPortraits();
                ValidateApprovedPortraits();
                Debug.Log("MIST_ISLE_APPROVED_PORTRAIT_BACKUP: " + portraitRebuildBackup);
            }
            finally { portraitRebuildBackup = null; }
        }

        [MenuItem("Mist Isle/Import Approved Portraits")]
        public static void ImportApprovedPortraits()
        {
            // Fixed canvases preserve stable source coordinates for the dialogue upper-body windows.
            ImportPortrait("PortraitTravelerOriginal.png", new RectInt(30, 0, 740, 1216), new Vector2Int(380, 520), 0, "TravelerApproved");
            ImportPortrait("PortraitShiguangOriginal.png", new RectInt(178, 34, 568, 1100), new Vector2Int(272, 426), 1, "ShiguangApproved");
            // The current art-reference folder has no Watcher painting; retain the previously approved legacy portrait.
            ImportPortrait("PortraitWatcherLegacyApproved.png", new RectInt(0, 0, 1086, 1448), new Vector2Int(560, 810), 2, "WatcherApproved");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MIST_ISLE_APPROVED_PORTRAITS_IMPORTED: original high-resolution Traveler, Shiguang and previously approved Watcher; no pixel actor enlargements, repainting or resampling.");
        }

        private static void ImportPortrait(string source, RectInt crop, Vector2Int subjectSeedTopLeft, int paperProfile, string name)
        {
            Texture2D board = ReadTexture(Source + source);
            try
            {
                Color32[] pixels = ReadTopLeftPixels(board, crop);
                // The Traveler/guide paintings have scenery physically joined to the boots and open
                // pale edges. Keep their original paper cards rather than damage a confirmed drawing.
                // The legacy Watcher has a closed outline against plain white and can be separated.
                if (paperProfile == 2)
                {
                    ClearWatcherPaper(pixels, crop.width, crop.height);
                    RetainPortraitSubject(pixels, crop.width, crop.height, subjectSeedTopLeft);
                }
                string path = Output + "Portraits/" + name + ".png";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (portraitRebuildBackup != null && File.Exists(path))
                {
                    Directory.CreateDirectory(portraitRebuildBackup);
                    string backup = Path.Combine(portraitRebuildBackup, name + ".png");
                    if (!File.Exists(backup)) File.Copy(path, backup, false);
                    if (File.Exists(path + ".meta") && !File.Exists(backup + ".meta")) File.Copy(path + ".meta", backup + ".meta", false);
                }
                if (!File.Exists(path) || portraitRebuildBackup != null)
                {
                    var portrait = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
                    try
                    {
                        portrait.SetPixels32(pixels); portrait.Apply(false);
                        File.WriteAllBytes(path, portrait.EncodeToPNG());
                    }
                    finally { UnityEngine.Object.DestroyImmediate(portrait); }
                }
                ConfigureSprite(path, false);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Bilinear; // These are original HD paintings, not world pixel sprites.
                importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(board); }
        }

        private static void ClearWatcherPaper(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            int read = 0, write = 0;
            bool IsPaper(Color32 p)
            {
                if (p.a == 0) return true;
                int min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                int max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                return min >= 238 && max - min <= 16;
            }
            void Enqueue(int index)
            {
                if (visited[index]) return;
                visited[index] = true;
                if (!IsPaper(pixels[index])) return;
                pixels[index] = new Color32(0, 0, 0, 0);
                queue[write++] = index;
            }
            for (int x = 0; x < width; x++) { Enqueue(x); Enqueue((height - 1) * width + x); }
            for (int y = 1; y < height - 1; y++) { Enqueue(y * width); Enqueue(y * width + width - 1); }
            // Manually reviewed white-paper components enclosed by arms, hair curls and rope loops.
            // These seeds are outside the body/clothing, not highlights on the pale shirt or hair.
            foreach (Vector2Int point in WatcherPaperVoidSeeds)
                if (point.x < width && point.y < height) Enqueue((height - 1 - point.y) * width + point.x);
            while (read < write)
            {
                int index = queue[read++], x = index % width, y = index / width;
                if (x > 0) Enqueue(index - 1);
                if (x + 1 < width) Enqueue(index + 1);
                if (y > 0) Enqueue(index - width);
                if (y + 1 < height) Enqueue(index + width);
            }
        }

        private static void RetainPortraitSubject(Color32[] pixels, int width, int height, Vector2Int seedTopLeft)
        {
            int seed = (height - 1 - seedTopLeft.y) * width + seedTopLeft.x;
            if (seed < 0 || seed >= pixels.Length || pixels[seed].a == 0)
                throw new InvalidDataException("Portrait subject anchor was removed; inspect the original illustration before proceeding.");
            var retained = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            int read = 0, write = 1;
            queue[0] = seed; retained[seed] = true;
            // Eight-neighbour connectivity retains thin diagonal hair strands, straps and fingers.
            while (read < write)
            {
                int index = queue[read++], x = index % width, y = index / width;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int next = ny * width + nx;
                    if (retained[next] || pixels[next].a == 0) continue;
                    retained[next] = true; queue[write++] = next;
                }
            }
            for (int i = 0; i < pixels.Length; i++) if (!retained[i]) pixels[i] = new Color32(0, 0, 0, 0);
        }

        [MenuItem("Mist Isle/Validate Approved Portraits")]
        public static void ValidateApprovedPortraits()
        {
            ValidatePortrait("PortraitTravelerOriginal.png", new RectInt(30, 0, 740, 1216), "TravelerApproved", false);
            ValidatePortrait("PortraitShiguangOriginal.png", new RectInt(178, 34, 568, 1100), "ShiguangApproved", false);
            ValidatePortrait("PortraitWatcherLegacyApproved.png", new RectInt(0, 0, 1086, 1448), "WatcherApproved", true);
            Debug.Log("MIST_ISLE_APPROVED_PORTRAITS_VALIDATED: Traveler and Shiguang use exact original-paper portrait cards; Watcher uses a transparent background. Character RGB is unchanged. Actual visual review remains required.");
        }

        private static void ValidatePortrait(string sourceName, RectInt crop, string name, bool requiresTransparency)
        {
            Texture2D source = ReadTexture(Source + sourceName);
            Texture2D output = ReadTexture(Output + "Portraits/" + name + ".png");
            try
            {
                Color32[] original = ReadTopLeftPixels(source, crop), actual = output.GetPixels32();
                if (output.width != crop.width || output.height != crop.height || original.Length != actual.Length)
                    throw new InvalidDataException("Approved portrait canvas differs from its reviewed source crop: " + name);
                int retained = 0, transparent = 0;
                for (int i = 0; i < actual.Length; i++)
                {
                    Color32 p = actual[i], s = original[i];
                    if (p.a != 0)
                    {
                        retained++;
                        if (p.r != s.r || p.g != s.g || p.b != s.b || p.a != s.a)
                            throw new InvalidDataException("Approved portrait character colors were altered: " + name);
                    }
                    else
                    {
                        transparent++;
                        if (p.r != 0 || p.g != 0 || p.b != 0) throw new InvalidDataException("Approved portrait has hidden paper RGB: " + name);
                    }
                }
                if (retained < actual.Length / 8 || (requiresTransparency && transparent < actual.Length / 20)
                    || (!requiresTransparency && transparent != 0))
                    throw new InvalidDataException("Approved portrait matte appears empty or opaque; inspect it before delivery: " + name);
                if (requiresTransparency)
                {
                    foreach (Vector2Int point in new[] { new Vector2Int(500, 225), new Vector2Int(118, 510), new Vector2Int(930, 590),
                        new Vector2Int(583, 95), new Vector2Int(668, 51), new Vector2Int(335, 1050), new Vector2Int(565, 974), new Vector2Int(794, 1050) })
                        if (actual[(crop.height - 1 - point.y) * crop.width + point.x].a == 0)
                            throw new InvalidDataException("Watcher face, sleeve or accessory landmark was removed: " + point);
                    foreach (Vector2Int point in WatcherPaperVoidSeeds)
                        if (actual[(crop.height - 1 - point.y) * crop.width + point.x].a != 0)
                            throw new InvalidDataException("Watcher arm negative space still contains paper: " + point);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(output); }
        }

        [MenuItem("Mist Isle/Rebuild Original Pixel Actors With Backup")]
        public static void RebuildOriginalPixelActorsWithBackup()
        {
            originalPixelRebuildBackup = Path.Combine("Builds", "ArtBackups", "original-pixels-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            try
            {
                ImportApprovedPixelActors();
                ImportOriginalOpeningEnvironment();
                ValidateOriginalPixelActors();
                ValidateOriginalPixelCreatures();
                Debug.Log("MIST_ISLE_ORIGINAL_PIXEL_BACKUP: " + originalPixelRebuildBackup);
            }
            finally { originalPixelRebuildBackup = null; }
        }

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
            ImportApprovedPixelActors();
            ImportOriginalOpeningEnvironment();
            ImportApprovedPortraits();
            ConfigureTitleTextures();
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

        private static void ConfigureTitleTextures()
        {
            foreach (string file in new[] { "TitleCover.png", "TitleBackdrop-v2.png" })
            {
                string path = Output + "UI/" + file;
                if (!File.Exists(path)) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Uses the original board pixels; no AI repaint, resampling, palette change or invented frames.</summary>
        [MenuItem("Mist Isle/Import Original Pixel Characters")]
        public static void ImportApprovedPixelActors()
        {
            string path = Source + "PixelPeopleApproved.png";
            if (!File.Exists(path)) throw new FileNotFoundException("The approved pixel character board is required.", path);
            Texture2D board = ReadTexture(path);
            try
            {
                if (board.width != 1484 || board.height != 1060)
                    throw new InvalidDataException("The approved character board must be the original 1484x1060 image.");
                // Source rows are North, South, West, East. Runtime rows are South, West, East, North.
                // Rectangles are reviewed top-left pixel coordinates in the 1484 x 1060 original.
                int[] columns = { 1008, 1130, 1254 };
                RectInt[] shiguang = { new RectInt(0, 84, 112, 108), new RectInt(0, 193, 112, 104),
                    new RectInt(0, 298, 112, 104), new RectInt(0, 405, 112, 112) };
                RectInt[] traveler = { new RectInt(0, 558, 112, 112), new RectInt(0, 675, 112, 112),
                    new RectInt(0, 787, 112, 122), new RectInt(0, 910, 112, 124) };
                ImportBoardActor(board, columns, traveler, "ApprovedTraveler");
                ImportBoardActor(board, columns, shiguang, "ShiguangPixel");
            }
            finally { UnityEngine.Object.DestroyImmediate(board); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MIST_ISLE_ORIGINAL_PIXEL_ACTORS: 24 original frames, 128x128 canvas, 64 PPU, South/West/East/North x 3, foot baseline 3px; retained RGB unchanged.");
        }

        [MenuItem("Mist Isle/Validate Original Pixel Characters")]
        public static void ValidateOriginalPixelActors()
        {
            Texture2D board = ReadTexture(Source + "PixelPeopleApproved.png");
            try
            {
                int[] columns = { 1008, 1130, 1254 };
                RectInt[] shiguang = { new RectInt(0, 84, 112, 108), new RectInt(0, 193, 112, 104),
                    new RectInt(0, 298, 112, 104), new RectInt(0, 405, 112, 112) };
                RectInt[] traveler = { new RectInt(0, 558, 112, 112), new RectInt(0, 675, 112, 112),
                    new RectInt(0, 787, 112, 122), new RectInt(0, 910, 112, 124) };
                ValidateBoardActor(board, columns, traveler, "ApprovedTraveler");
                ValidateBoardActor(board, columns, shiguang, "ShiguangPixel");
            }
            finally { UnityEngine.Object.DestroyImmediate(board); }
            Debug.Log("MIST_ISLE_ORIGINAL_PIXEL_ACTORS_VALIDATED: all 24 independent PNGs match the approved board pixels exactly after boundary-only alpha cleanup and canvas placement; direction order, pivot, PPU and point sampling validated.");
        }

        private static void ValidateBoardActor(Texture2D board, int[] columns, RectInt[] sourceRows, string actor)
        {
            int[] order = { 1, 2, 3, 0 };
            for (int direction = 0; direction < 4; direction++) for (int frame = 0; frame < 3; frame++)
            {
                RectInt rect = sourceRows[order[direction]]; rect.x = columns[frame];
                Color32[] source = ReadTopLeftPixels(board, rect);
                ClearConnectedBoardBackground(source, rect.width, rect.height);
                Color32[] expected = AlignOnCanvas(source, rect.width, rect.height, 128, 128, 3);
                string path = Output + "Actors/" + actor + "/" + actor + "_" + (direction * 3 + frame).ToString("00") + ".png";
                Texture2D image = ReadTexture(path);
                try
                {
                    Color32[] actual = image.GetPixels32();
                    if (image.width != 128 || image.height != 128 || actual.Length != expected.Length)
                        throw new InvalidDataException("Original pixel character canvas differs: " + path);
                    for (int pixel = 0; pixel < expected.Length; pixel++)
                        if (!actual[pixel].Equals(expected[pixel]))
                            throw new InvalidDataException("Original pixel colors were changed at pixel " + pixel + ": " + path);
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var settings = AssetImporter.GetAtPath(path) as TextureImporter;
                if (sprite == null || settings == null || sprite.pixelsPerUnit != 64f || settings.filterMode != FilterMode.Point
                    || settings.mipmapEnabled || sprite.pivot != new Vector2(64f, 0f))
                    throw new InvalidDataException("Original pixel character import settings differ: " + path);
            }
        }

        [MenuItem("Mist Isle/Import Original Opening Environment")]
        public static void ImportOriginalOpeningEnvironment()
        {
            // Every rectangle selects pixels from an existing course-edition or approved reference file.
            Crop(Source + "CliffsAndFencesSource.png", new RectInt(185, 190, 80, 62), "Environment/Dock.png", false);
            // Reviewed source components: fence x859..1005/y169..219; the next prop starts at y226.
            // Grass cliff components have real gaps at x128..137 and x235..243, preventing cut seams.
            Crop(Source + "CliffsAndFencesSource.png", new RectInt(852, 160, 162, 64), "Environment/FenceTimber.png", true);
            Crop(Source + "CliffsAndFencesSource.png", new RectInt(8, 17, 124, 125), "Environment/GrassCliff.png", true);
            Crop(Source + "CliffsAndFencesSource.png", new RectInt(131, 17, 109, 125), "Environment/GrassCliffCorner.png", true);
            Crop(Source + "Area96.png", new RectInt(96, 0, 96, 96), "Environment/HarborBoat.png", true);
            Crop(Source + "Area96.png", new RectInt(0, 0, 96, 96), "Environment/DockPlatform.png", true);
            Crop(Source + "Area96.png", new RectInt(192, 0, 96, 96), "Environment/CampSupply.png", true);
            Crop(Source + "Structures128.png", new RectInt(768, 0, 128, 128), "Environment/CampfireRing.png", true);
            Crop(Source + "Structures128.png", new RectInt(896, 0, 128, 128), "Environment/FireSeat.png", true);
            Crop(Source + "Structures128.png", new RectInt(0, 0, 128, 128), "Environment/StoneWall.png", true);
            Crop(Source + "OriginalProps16.png", new RectInt(32, 0, 32, 32), "Environment/Boat.png", true);
            Crop(Source + "CampRuinsLighthouseSource.png", new RectInt(5, 27, 330, 232), "Environment/Tent.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(17, 60, 128, 91), "Environment/BushPink.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(146, 61, 92, 90), "Environment/BushWhite.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(233, 149, 64, 62), "Environment/FlowerBlue.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(27, 148, 64, 64), "Environment/FlowerWhite.png", true);
            Crop(Source + "NaturePropsSource.png", new RectInt(20, 670, 220, 160), "Environment/RockLarge.png", true);
            Crop(Source + "NaturePropsSource.png", new RectInt(260, 710, 160, 110), "Environment/RockSmall.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(907, 211, 92, 92), "Environment/Crate.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(1003, 211, 92, 92), "Environment/Barrel.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(785, 174, 122, 136), "Environment/Lantern.png", true);
            Crop(Source + "FloraAndPropsSource.png", new RectInt(347, 213, 172, 88), "Environment/Log.png", true);
            ImportOriginalBoardSprite("PixelCompanionsApproved.png", new RectInt(442, 121, 60, 62), "Creatures/PixelMossLumi.png");
            ImportOriginalBoardSprite("PixelCompanionsApproved.png", new RectInt(442, 442, 60, 62), "Creatures/PixelMoatail.png");
            ImportOriginalBoardSprite("PixelCompanionsApproved.png", new RectInt(441, 748, 62, 65), "Creatures/PixelCrystalGecko.png");
            // On pixel_other the direction diamonds occupy x460..482; the first crab is x495..545.
            // This is a different column layout from pixel_initial_companition: never reuse its x442 crop.
            ImportOriginalBoardSprite("pixel_other.png", new RectInt(485, 435, 72, 72), "Creatures/PixelWildCrab.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MIST_ISLE_ORIGINAL_OPENING_ART: 22 environment cutouts and 4 original pixel creature cutouts imported without repainting.");
        }

        private static void ImportOriginalBoardSprite(string sourceName, RectInt rect, string relative)
        {
            Texture2D board = ReadTexture(Source + sourceName);
            try
            {
                Color32[] pixels = ReadTopLeftPixels(board, rect);
                ClearConnectedBoardBackground(pixels, rect.width, rect.height);
                // A tight canvas lets world scaling use the actual creature silhouette.
                int left = rect.width, right = -1, bottom = rect.height, top = -1;
                for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++)
                    if (pixels[y * rect.width + x].a != 0)
                    { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (right < left || top < bottom) throw new InvalidDataException("Original pixel creature crop was empty: " + relative);
                int width = right - left + 1, height = top - bottom + 1;
                WriteOriginalPixels(AlignOnCanvas(pixels, rect.width, rect.height, width, height, 0), width, height, relative);
            }
            finally { UnityEngine.Object.DestroyImmediate(board); }
        }

        [MenuItem("Mist Isle/Validate Original Pixel Creatures")]
        public static void ValidateOriginalPixelCreatures()
        {
            ValidateOriginalBoardSprite("PixelCompanionsApproved.png", new RectInt(442, 121, 60, 62), "Creatures/PixelMossLumi.png");
            ValidateOriginalBoardSprite("PixelCompanionsApproved.png", new RectInt(442, 442, 60, 62), "Creatures/PixelMoatail.png");
            ValidateOriginalBoardSprite("PixelCompanionsApproved.png", new RectInt(441, 748, 62, 65), "Creatures/PixelCrystalGecko.png");
            ValidateOriginalBoardSprite("pixel_other.png", new RectInt(485, 435, 72, 72), "Creatures/PixelWildCrab.png");
            Debug.Log("MIST_ISLE_ORIGINAL_PIXEL_CREATURES_VALIDATED: all 4 independent PNGs match original RGB/alpha; every source silhouette has at least 3 transparent pixels on all sides, with no cropped body or directional label.");
        }

        private static void ValidateOriginalBoardSprite(string sourceName, RectInt rect, string relative)
        {
            Texture2D board = ReadTexture(Source + sourceName);
            try
            {
                Color32[] pixels = ReadTopLeftPixels(board, rect);
                ClearConnectedBoardBackground(pixels, rect.width, rect.height);
                int left = rect.width, right = -1, bottom = rect.height, top = -1;
                for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++)
                    if (pixels[y * rect.width + x].a != 0)
                    { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                if (right < left || top < bottom || left < 3 || bottom < 3 || rect.width - 1 - right < 3 || rect.height - 1 - top < 3)
                    throw new InvalidDataException("Pixel creature touches its source crop boundary; inspect the original board before importing: " + relative);
                int width = right - left + 1, height = top - bottom + 1;
                Color32[] expected = AlignOnCanvas(pixels, rect.width, rect.height, width, height, 0);
                Texture2D output = ReadTexture(Output + relative);
                try
                {
                    Color32[] actual = output.GetPixels32();
                    if (output.width != width || output.height != height || actual.Length != expected.Length)
                        throw new InvalidDataException("Pixel creature crop is stale or incomplete; rebuild with backup: " + relative);
                    for (int index = 0; index < expected.Length; index++)
                        if (!actual[index].Equals(expected[index]))
                            throw new InvalidDataException("Pixel creature differs from the reviewed source; rebuild with backup: " + relative);
                }
                finally { UnityEngine.Object.DestroyImmediate(output); }
            }
            finally { UnityEngine.Object.DestroyImmediate(board); }
        }

        private static void ImportBoardActor(Texture2D board, int[] columns, RectInt[] sourceRows, string actor)
        {
            int[] sourceRowOrder = { 1, 2, 3, 0 };
            for (int direction = 0; direction < 4; direction++)
                for (int frame = 0; frame < 3; frame++)
                {
                    RectInt rect = sourceRows[sourceRowOrder[direction]];
                    rect.x = columns[frame];
                    Color32[] pixels = ReadTopLeftPixels(board, rect);
                    ClearConnectedBoardBackground(pixels, rect.width, rect.height);
                    Color32[] canvas = AlignOnCanvas(pixels, rect.width, rect.height, 128, 128, 3);
                    WriteOriginalPixels(canvas, 128, 128, "Actors/" + actor + "/" + actor + "_" + (direction * 3 + frame).ToString("00") + ".png");
                }
        }

        private static Color32[] ReadTopLeftPixels(Texture2D board, RectInt rect)
        {
            if (rect.x < 0 || rect.y < 0 || rect.xMax > board.width || rect.yMax > board.height)
                throw new ArgumentOutOfRangeException(nameof(rect));
            Color32[] all = board.GetPixels32();
            var pixels = new Color32[rect.width * rect.height];
            for (int y = 0; y < rect.height; y++)
                Array.Copy(all, (board.height - rect.yMax + y) * board.width + rect.x, pixels, y * rect.width, rect.width);
            return pixels;
        }

        // Flood only the cream paper / printed drop-shadow colors connected to the crop boundary.
        // Enclosed skin, cream clothes, blond hair, bow colors and all outlines retain their exact RGB.
        private static void ClearConnectedBoardBackground(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            int read = 0, write = 0;
            void Enqueue(int index)
            {
                if (visited[index]) return;
                visited[index] = true;
                Color32 p = pixels[index];
                bool paper = p.a == 0 || (p.r >= 160 && p.g >= 145 && p.b >= 135 && p.r >= p.g && p.g >= p.b
                    && p.r - p.g <= 50 && p.g - p.b <= 30);
                if (!paper) return;
                // Removed background has no retained character color. Clear its hidden RGB as well:
                // image previews and texture alpha-bleed must not display the original paper rectangle.
                pixels[index] = new Color32(0, 0, 0, 0);
                queue[write++] = index;
            }
            for (int x = 0; x < width; x++) { Enqueue(x); Enqueue((height - 1) * width + x); }
            for (int y = 1; y < height - 1; y++) { Enqueue(y * width); Enqueue(y * width + width - 1); }
            while (read < write)
            {
                int index = queue[read++], x = index % width, y = index / width;
                if (x > 0) Enqueue(index - 1);
                if (x + 1 < width) Enqueue(index + 1);
                if (y > 0) Enqueue(index - width);
                if (y + 1 < height) Enqueue(index + width);
            }
        }

        private static Color32[] AlignOnCanvas(Color32[] pixels, int width, int height, int canvasWidth, int canvasHeight, int footMargin)
        {
            int left = width, right = -1, bottom = height, top = -1;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                if (pixels[y * width + x].a != 0)
                { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
            if (right < left || top < bottom) throw new InvalidDataException("Original sprite was empty after board cleanup.");
            int contentWidth = right - left + 1, contentHeight = top - bottom + 1;
            if (contentWidth > canvasWidth || contentHeight + footMargin > canvasHeight)
                throw new InvalidDataException("Original sprite exceeds its common canvas; refusing to resample or clip it.");
            var canvas = new Color32[canvasWidth * canvasHeight];
            int targetX = (canvasWidth - contentWidth) / 2;
            for (int y = 0; y < contentHeight; y++)
                Array.Copy(pixels, (bottom + y) * width + left, canvas, (footMargin + y) * canvasWidth + targetX, contentWidth);
            return canvas;
        }

        private static void WriteOriginalPixels(Color32[] pixels, int width, int height, string relative)
        {
            string target = Output + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            bool rebuilding = originalPixelRebuildBackup != null && (relative.StartsWith("Actors/ApprovedTraveler/", StringComparison.Ordinal)
                || relative.StartsWith("Actors/ShiguangPixel/", StringComparison.Ordinal) || relative.StartsWith("Creatures/Pixel", StringComparison.Ordinal));
            if (rebuilding && File.Exists(target))
            {
                string backup = Path.Combine(originalPixelRebuildBackup, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup)) File.Copy(target, backup, false);
                if (File.Exists(target + ".meta") && !File.Exists(backup + ".meta")) File.Copy(target + ".meta", backup + ".meta", false);
            }
            if (!File.Exists(target) || rebuilding)
            {
                var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                image.SetPixels32(pixels); image.Apply(false);
                try { File.WriteAllBytes(target, image.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            ConfigureSprite(target, false);
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
            ConfigureSprite(target, center);
        }

        private static void ConfigureSprite(string target, bool center)
        {
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
