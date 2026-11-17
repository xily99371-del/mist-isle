using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MistIsle.Creatures;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class DesignWorldSaveTests
    {
        private Dictionary<string, CreatureSpecies> catalog;

        [SetUp]
        public void SetUp() => catalog = GameCatalog.CreateSpecies();

        [TearDown]
        public void TearDown()
        {
            foreach (var species in catalog.Values) UnityEngine.Object.DestroyImmediate(species);
        }

        // This is the actual pre-design-world shape: no map-content version or new map-state fields.
        [Serializable]
        private sealed class LegacyJourney
        {
            public int schemaVersion = 3;
            public string lastSceneId = "coastal-camp";
            public string selectedStarterSpeciesId = "moss-lumi";
            public List<CreatureInstance> roster = new List<CreatureInstance>();
            public List<CreatureInstance> storedRoster = new List<CreatureInstance>();
            public SealProgression seals = new SealProgression();
            public List<string> inventoryItemIds = new List<string>();
            public List<int> inventoryCounts = new List<int>();
            public bool captureTutorialCompleted;
            public string roomId;
            public float playerX = 681.75f;
            public float playerY = -4.25f;
            public bool hasPosition = true;
            public List<string> deployedIds = new List<string>();
            public List<string> collectedNodeIds = new List<string>();
            public List<string> discoveredSpeciesIds = new List<string>();
            public List<string> completedFieldIds = new List<string>();
            public bool chapterCompleted;
            public float playSeconds = 1837.5f;
            public int openingStage;
        }

        private LegacyJourney Legacy(string room, OpeningStage stage)
        {
            var result = new LegacyJourney { roomId = room, openingStage = (int)stage, captureTutorialCompleted = stage == OpeningStage.Complete };
            var starter = new CreatureInstance(catalog["moss-lumi-evolved"], 9, true)
            { nickname = "旧航伴", experience = 11, currentHp = 26, bond = 64, evolutionStage = 1, awakened = true };
            var stored = new CreatureInstance(catalog["wild-03"], 6, false)
            { nickname = "潮声", experience = 9, currentHp = 13, bond = 33 };
            result.roster.Add(starter);
            result.storedRoster.Add(stored);
            result.deployedIds.Add(starter.instanceId);
            result.inventoryItemIds.AddRange(new[] { "berry", "dew", "resonance-capsule" });
            result.inventoryCounts.AddRange(new[] { 7, 2, 3 });
            result.collectedNodeIds.Add("berry-crystal-1-0");
            result.discoveredSpeciesIds.AddRange(new[] { "moss-lumi", "moss-lumi-evolved", "wild-03" });
            result.completedFieldIds.Add("field-glowglade");
            if (stage == OpeningStage.Complete)
                foreach (string id in GameCatalog.GuardianIds) result.seals.RecordGuardianVictory(id);
            return result;
        }

        [TestCase("camp-2", "camp-0", OpeningStage.Complete)]
        [TestCase("camp-0", "camp-0", OpeningStage.GatherIngredients)]
        [TestCase("coast-0", "coast-0", OpeningStage.FollowGuide)]
        [TestCase("coast-2", "coast-0", OpeningStage.Complete)]
        [TestCase("glowglade-2", "glowglade-0", OpeningStage.Complete)]
        [TestCase("tidal-2", "tidal-0", OpeningStage.Complete)]
        [TestCase("crystal-0", "tidal-0", OpeningStage.Complete)]
        [TestCase("crystal-2", "tidal-0", OpeningStage.Complete)]
        [TestCase("lighthouse-2", "lighthouse-0", OpeningStage.Complete)]
        public void SchemaThreeRoomsMigrateToSafeMacroArrivalWithoutDiscardingTheJourney(
            string oldRoom, string newRoom, OpeningStage stage)
        {
            InTemporaryDirectory(store =>
            {
                var old = Legacy(oldRoom, stage);
                File.WriteAllText(store.Path, JsonUtility.ToJson(old), new UTF8Encoding(false));
                byte[] originalFile = File.ReadAllBytes(store.Path);

                Assert.That(store.TryLoad(catalog, out var loaded, out var reason), Is.True, reason);
                Assert.That(loaded.roomId, Is.EqualTo(newRoom));
                Assert.That(loaded.hasPosition, Is.False, "Old absolute coordinates must never be used in the redesigned world.");
                Assert.That(loaded.schemaVersion, Is.EqualTo(WorldSaveData.CurrentSchemaVersion));
                Assert.That(loaded.mapContentVersion, Is.EqualTo(1));
                Assert.That(loaded.openingStage, Is.EqualTo((int)stage));
                Assert.That(loaded.selectedStarterSpeciesId, Is.EqualTo(old.selectedStarterSpeciesId));
                Assert.That(loaded.captureTutorialCompleted, Is.EqualTo(old.captureTutorialCompleted));
                Assert.That(loaded.playSeconds, Is.EqualTo(old.playSeconds));
                Assert.That(JsonUtility.ToJson(loaded.roster.Single()), Is.EqualTo(JsonUtility.ToJson(old.roster.Single())));
                Assert.That(JsonUtility.ToJson(loaded.storedRoster.Single()), Is.EqualTo(JsonUtility.ToJson(old.storedRoster.Single())));
                Assert.That(JsonUtility.ToJson(loaded.seals), Is.EqualTo(JsonUtility.ToJson(old.seals)));
                CollectionAssert.AreEqual(old.deployedIds, loaded.deployedIds);
                CollectionAssert.AreEqual(old.inventoryItemIds, loaded.inventoryItemIds);
                CollectionAssert.AreEqual(old.inventoryCounts, loaded.inventoryCounts);
                CollectionAssert.AreEqual(old.discoveredSpeciesIds, loaded.discoveredSpeciesIds);
                CollectionAssert.AreEqual(old.completedFieldIds, loaded.completedFieldIds);
                CollectionAssert.AreEqual(originalFile, File.ReadAllBytes(store.Path), "Reading a migrated save rewrote its source.");
                Assert.That(loaded.storyFlags, Is.Not.Null);
                Assert.That(loaded.nodeRefreshStates, Is.Not.Null);
                Assert.That(loaded.openedChestIds, Is.Not.Null);

                Assert.That(store.Save(loaded, out reason), Is.True, reason);
                Assert.That(store.TryLoad(catalog, out var migratedAgain, out reason), Is.True, reason);
                Assert.That(migratedAgain.roomId, Is.EqualTo(newRoom));
                Assert.That(migratedAgain.hasPosition, Is.False, "Writing the migration must not reinstate obsolete coordinates.");
            });
        }

        [Test]
        public void CurrentSchemaAndMapVersionPreserveCoordinatesTimersStoriesChestsAndInventory()
        {
            InTemporaryDirectory(store =>
            {
                var old = Legacy("tidal-0", OpeningStage.Complete);
                var data = new WorldSaveData {
                    schemaVersion = WorldSaveData.CurrentSchemaVersion, mapContentVersion = 1,
                    roomId = "tidal-0", hasPosition = true, playerX = 859.25f, playerY = 34.5f,
                    selectedStarterSpeciesId = old.selectedStarterSpeciesId, openingStage = old.openingStage,
                    captureTutorialCompleted = true, playSeconds = 2345.5f, seals = old.seals,
                    roster = old.roster, storedRoster = old.storedRoster, deployedIds = old.deployedIds
                };
                data.inventoryItemIds.AddRange(new[] { "camp-token", "berry-treat", "resonance-capsule", "shell" });
                data.inventoryCounts.AddRange(new[] { 37, 2, 4, 3 });
                data.storyFlags.AddRange(new[] { "M03_CRYSTAL_ARCHIVE", "guardian-after:tidal-warden" });
                data.visitedMapIds.AddRange(new[] { "camp-0", "coast-0", "tidal-0" });
                data.openedChestIds.Add("M01_TREASURE");
                data.completedFieldIds.AddRange(new[] { "M03_WATER_CHANNEL", "M03_CONDUIT" });
                data.collectedNodeIds.Add("M03_CRYSTAL_RESOURCE");
                long deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600;
                data.nodeRefreshStates.Add(new NodeRefreshState { id = "M03_CRYSTAL_RESOURCE", availableAtUtc = deadline });
                Assert.That(store.Save(data, out var reason), Is.True, reason);

                Assert.That(store.TryLoad(catalog, out var loaded, out reason), Is.True, reason);
                Assert.That(loaded.hasPosition, Is.True);
                Assert.That(loaded.roomId, Is.EqualTo("tidal-0"));
                Assert.That(loaded.playerX, Is.EqualTo(859.25f));
                Assert.That(loaded.playerY, Is.EqualTo(34.5f));
                Assert.That(loaded.playSeconds, Is.EqualTo(2345.5f));
                Assert.That(loaded.nodeRefreshStates.Single().id, Is.EqualTo("M03_CRYSTAL_RESOURCE"));
                Assert.That(loaded.nodeRefreshStates.Single().availableAtUtc, Is.EqualTo(deadline));
                CollectionAssert.AreEqual(data.inventoryItemIds, loaded.inventoryItemIds);
                CollectionAssert.AreEqual(data.inventoryCounts, loaded.inventoryCounts);
                CollectionAssert.AreEqual(data.storyFlags, loaded.storyFlags);
                CollectionAssert.AreEqual(data.visitedMapIds, loaded.visitedMapIds);
                CollectionAssert.AreEqual(data.openedChestIds, loaded.openedChestIds);
                CollectionAssert.AreEqual(data.completedFieldIds, loaded.completedFieldIds);
                CollectionAssert.AreEqual(data.collectedNodeIds, loaded.collectedNodeIds);
                Assert.That(JsonUtility.ToJson(loaded.roster.Single()), Is.EqualTo(JsonUtility.ToJson(data.roster.Single())));
                Assert.That(JsonUtility.ToJson(loaded.storedRoster.Single()), Is.EqualTo(JsonUtility.ToJson(data.storedRoster.Single())));
            });
        }

        [Test]
        public void ACurrentSchemaWithLegacyMapContentStillDiscardsOnlyItsOldPosition()
        {
            InTemporaryDirectory(store =>
            {
                var data = new WorldSaveData {
                    schemaVersion = WorldSaveData.CurrentSchemaVersion, mapContentVersion = 0,
                    roomId = "crystal-1", hasPosition = true, playerX = 631f, playerY = 2f,
                    selectedStarterSpeciesId = "moss-lumi", openingStage = (int)OpeningStage.Complete
                };
                data.roster.Add(new CreatureInstance(catalog["moss-lumi"], 3, true));
                data.inventoryItemIds.Add("camp-token"); data.inventoryCounts.Add(17);
                data.storyFlags.Add("M00_NOTICE");
                Assert.That(store.Save(data, out var reason), Is.True, reason);
                Assert.That(store.TryLoad(catalog, out var loaded, out reason), Is.True, reason);
                Assert.That(loaded.roomId, Is.EqualTo("tidal-0"));
                Assert.That(loaded.hasPosition, Is.False);
                Assert.That(loaded.mapContentVersion, Is.EqualTo(1));
                Assert.That(loaded.inventoryCounts.Single(), Is.EqualTo(17));
                CollectionAssert.AreEqual(data.storyFlags, loaded.storyFlags);
                Assert.That(loaded.roster.Single().instanceId, Is.EqualTo(data.roster.Single().instanceId));
            });
        }

        [Test]
        public void MigrationKeepsAnExactOriginalArchiveAfterRepeatedAutosavesReplaceTheNormalBackup()
        {
            InTemporaryDirectory(store =>
            {
                byte[] original = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(Legacy("crystal-2", OpeningStage.Complete), true));
                File.WriteAllBytes(store.Path, original);
                Assert.That(store.TryLoad(catalog, out var loaded, out var reason), Is.True, reason);
                Assert.That(store.TryLoad(catalog, out _, out reason), Is.True, reason);
                var archives = Directory.GetFiles(Path.GetDirectoryName(store.Path), "*.before-map-v1.*.history");
                Assert.That(archives.Length, Is.EqualTo(1), "Repeated loads must not duplicate the same original archive.");
                CollectionAssert.AreEqual(original, File.ReadAllBytes(archives[0]));
                Assert.That(store.Save(loaded, out reason), Is.True, reason);
                loaded.playSeconds += 40;
                Assert.That(store.Save(loaded, out reason), Is.True, reason);
                CollectionAssert.AreEqual(original, File.ReadAllBytes(archives[0]), "Normal backup rotation must not discard the pre-migration original.");
                Assert.That(JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(store.Path + ".bak")).schemaVersion,
                    Is.EqualTo(WorldSaveData.CurrentSchemaVersion));
            });
        }

        [Test]
        public void LegacyCollectedNodesReceiveFiniteDeadlinesWithoutExtendingThemOnCurrentFormatReload()
        {
            InTemporaryDirectory(store =>
            {
                var legacy = Legacy("camp-0", OpeningStage.Complete);
                legacy.collectedNodeIds.Add("berry-camp-0-0");
                File.WriteAllText(store.Path, JsonUtility.ToJson(legacy), new UTF8Encoding(false));
                long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Assert.That(store.TryLoad(catalog, out var loaded, out var reason), Is.True, reason);
                var state = loaded.nodeRefreshStates.Single(value => value.id == "berry-camp-0-0");
                Assert.That(state.availableAtUtc, Is.InRange(before + 120, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120));
                Assert.That(loaded.collectedNodeIds, Does.Contain(state.id));
                long originalDeadline = state.availableAtUtc;
                Assert.That(store.Save(loaded, out reason), Is.True, reason);
                Assert.That(store.TryLoad(catalog, out var current, out reason), Is.True, reason);
                Assert.That(current.nodeRefreshStates.Single(value => value.id == state.id).availableAtUtc, Is.EqualTo(originalDeadline));
            });
        }

        private static void InTemporaryDirectory(Action<ChapterSaveStore> action)
        {
            string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string leaf = "MistIsle-DesignWorldSave-" + Guid.NewGuid().ToString("N");
            string directory = Path.GetFullPath(Path.Combine(tempRoot, leaf));
            Directory.CreateDirectory(directory);
            try { action(new ChapterSaveStore(directory)); }
            finally
            {
                string resolved = Path.GetFullPath(directory);
                if (!resolved.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(Path.GetFileName(resolved), leaf, StringComparison.Ordinal)
                    || !string.Equals(resolved, Path.Combine(tempRoot, leaf), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing to delete an unverified design-world-save directory.");
                if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            }
        }
    }
}
