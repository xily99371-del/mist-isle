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
    public sealed class SaveTests
    {
        private Dictionary<string, CreatureSpecies> catalog;

        [SetUp]
        public void SetUp() => catalog = GameCatalog.CreateSpecies();

        [TearDown]
        public void TearDown()
        {
            foreach (var species in catalog.Values) UnityEngine.Object.DestroyImmediate(species);
        }

        private CreatureInstance Creature(string species = "moss-lumi", bool starter = true)
            => new CreatureInstance(catalog[species], 3, starter);

        private WorldSaveData Journey()
        {
            var data = new WorldSaveData { selectedStarterSpeciesId = "moss-lumi" };
            data.roster.Add(Creature());
            data.deployedIds.Add(data.roster[0].instanceId);
            return data;
        }

        private static void InTemporaryDirectory(Action<ChapterSaveStore, string> action)
        {
            string tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath())
                .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                + System.IO.Path.DirectorySeparatorChar;
            string leaf = "MistIsle-SaveTests-" + Guid.NewGuid().ToString("N");
            string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(tempRoot, leaf));
            Directory.CreateDirectory(directory);
            try { action(new ChapterSaveStore(directory), directory); }
            finally
            {
                string resolved = System.IO.Path.GetFullPath(directory);
                // Never recursively delete an unverified, caller-supplied, or shared directory.
                if (!resolved.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(System.IO.Path.GetFileName(resolved), leaf, StringComparison.Ordinal)
                    || !string.Equals(resolved, System.IO.Path.Combine(tempRoot, leaf), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing to delete an unverified test directory.");
                if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            }
        }

        [Serializable]
        private sealed class LegacySaveV1
        {
            public int schemaVersion = 1;
            public string lastSceneId = "coastal-camp";
            public string selectedStarterSpeciesId = "moss-lumi";
            public List<CreatureInstance> roster = new List<CreatureInstance>();
            public SealProgression seals = new SealProgression();
            public List<string> inventoryItemIds = new List<string>();
            public List<int> inventoryCounts = new List<int>();
            public bool captureTutorialCompleted;
        }

        [Test]
        public void SchemaOneMigrationInitializesNewFieldsAndPreservesExistingProgressWithoutRewriting()
        {
            InTemporaryDirectory((store, directory) =>
            {
                var legacy = new LegacySaveV1();
                var partner = Creature();
                partner.experience = 9;
                partner.bond = 17;
                legacy.roster.Add(partner);
                legacy.inventoryItemIds.Add("berry");
                legacy.inventoryCounts.Add(5);
                legacy.seals.RecordGuardianVictory("glowglade-warden");
                legacy.seals.RecordGuardianVictory("tidal-warden");
                legacy.seals.RecordGuardianVictory("crystal-warden");
                legacy.captureTutorialCompleted = true;
                string json = JsonUtility.ToJson(legacy);
                Assert.That(json.Contains("storedRoster"), Is.False);
                Assert.That(json.Contains("deployedIds"), Is.False);
                File.WriteAllText(store.Path, json, new UTF8Encoding(false));
                byte[] original = File.ReadAllBytes(store.Path);

                Assert.That(store.TryLoad(catalog, out var loaded, out var message), Is.True, message);
                Assert.That(loaded.schemaVersion, Is.EqualTo(WorldSaveData.CurrentSchemaVersion));
                Assert.That(loaded.roster.Single().instanceId, Is.EqualTo(partner.instanceId));
                Assert.That(loaded.roster.Single().experience, Is.EqualTo(9));
                Assert.That(loaded.roster.Single().bond, Is.EqualTo(17));
                Assert.That(loaded.inventoryCounts.Single(), Is.EqualTo(5));
                Assert.That(loaded.captureTutorialCompleted, Is.True);
                Assert.That(loaded.seals.Count, Is.EqualTo(3));
                Assert.That(loaded.seals.CanEnterLighthouse, Is.False);
                Assert.That(loaded.roomId, Is.EqualTo("camp-0"));
                Assert.That(loaded.storedRoster, Is.Not.Null.And.Empty);
                Assert.That(loaded.deployedIds, Is.Not.Null);
                Assert.That(loaded.collectedNodeIds, Is.Not.Null);
                Assert.That(loaded.completedFieldIds, Is.Not.Null);
                CollectionAssert.AreEqual(original, File.ReadAllBytes(store.Path));
            });
        }

        [Test]
        public void ExcessTravelMembersAreMovedToStorageWithoutLosingIdentityOrGrowth()
        {
            InTemporaryDirectory((store, directory) =>
            {
                var data = Journey();
                for (int i = 0; i < 7; i++)
                {
                    var creature = Creature("wild-01", false);
                    creature.nickname = "伙伴" + i;
                    creature.bond = 10 + i;
                    creature.experience = i;
                    data.roster.Add(creature);
                }
                var existingStored = Creature("wild-03", false);
                data.storedRoster.Add(existingStored);
                string[] allIds = data.roster.Concat(data.storedRoster).Select(x => x.instanceId).ToArray();
                Assert.That(store.Save(data, out var error), Is.True, error);

                Assert.That(store.TryLoad(catalog, out var loaded, out var message), Is.True, message);
                Assert.That(loaded.roster.Count, Is.EqualTo(6));
                Assert.That(loaded.storedRoster.Count, Is.EqualTo(3));
                CollectionAssert.AreEquivalent(allIds,
                    loaded.roster.Concat(loaded.storedRoster).Select(x => x.instanceId).ToArray());
                foreach (var old in data.roster.Concat(data.storedRoster))
                {
                    var restored = loaded.roster.Concat(loaded.storedRoster).Single(x => x.instanceId == old.instanceId);
                    Assert.That(restored.nickname, Is.EqualTo(old.nickname));
                    Assert.That(restored.bond, Is.EqualTo(old.bond));
                    Assert.That(restored.experience, Is.EqualTo(old.experience));
                }
            });
        }

        [Test]
        public void DamagedPrimaryFallsBackToBackupWithoutOverwritingEitherFile()
        {
            InTemporaryDirectory((store, directory) =>
            {
                var data = Journey();
                data.inventoryItemIds.Add("berry");
                data.inventoryCounts.Add(7);
                Assert.That(store.Save(data, out var firstError), Is.True, firstError);
                data.inventoryCounts[0] = 9;
                Assert.That(store.Save(data, out var secondError), Is.True, secondError);
                byte[] backup = File.ReadAllBytes(store.Path + ".bak");
                File.WriteAllText(store.Path, "{this-is-not-valid-json", new UTF8Encoding(false));
                byte[] damaged = File.ReadAllBytes(store.Path);

                Assert.That(store.TryLoad(catalog, out var restored, out var message), Is.True, message);
                Assert.That(message, Does.Contain("备份"));
                Assert.That(restored.inventoryCounts[0], Is.EqualTo(7));
                CollectionAssert.AreEqual(damaged, File.ReadAllBytes(store.Path));
                CollectionAssert.AreEqual(backup, File.ReadAllBytes(store.Path + ".bak"));
            });
        }

        [Test]
        public void FutureSchemaIsRejectedWithoutFallingBackOrRewritingAnyData()
        {
            InTemporaryDirectory((store, directory) =>
            {
                var old = Journey();
                Assert.That(store.Save(old, out var error), Is.True, error);
                File.Copy(store.Path, store.Path + ".bak");
                byte[] backup = File.ReadAllBytes(store.Path + ".bak");
                var future = Journey();
                future.schemaVersion = WorldSaveData.CurrentSchemaVersion + 10;
                File.WriteAllText(store.Path, JsonUtility.ToJson(future), new UTF8Encoding(false));
                byte[] primary = File.ReadAllBytes(store.Path);

                Assert.That(store.TryLoad(catalog, out var loaded, out var message), Is.False);
                Assert.That(loaded, Is.Null);
                Assert.That(message, Does.Contain("更新"));
                CollectionAssert.AreEqual(primary, File.ReadAllBytes(store.Path));
                CollectionAssert.AreEqual(backup, File.ReadAllBytes(store.Path + ".bak"));
            });
        }

        [Test]
        public void RoundTripRetainsStorageDeploymentEvolutionAwakeningAndWorldFields()
        {
            InTemporaryDirectory((store, directory) =>
            {
                var data = Journey();
                data.roomId = "lighthouse-2";
                data.lastSceneId = "chapter-one";
                data.playerX = 123.25f;
                data.playerY = -16.5f;
                data.hasPosition = true;
                data.playSeconds = 3745.75f;
                data.chapterCompleted = true;
                data.captureTutorialCompleted = true;
                data.collectedNodeIds.Add("berry-coast-01");
                data.completedFieldIds.Add("field-glowglade");
                data.discoveredSpeciesIds.Add("moss-lumi-evolved");
                foreach (string id in GameCatalog.GuardianIds) data.seals.RecordGuardianVictory(id);
                var starter = data.roster[0];
                starter.speciesId = "moss-lumi-evolved";
                starter.nickname = "最初的伙伴";
                starter.level = 9;
                starter.experience = 11;
                starter.currentHp = 21;
                starter.bond = 64;
                starter.evolutionStage = 1;
                starter.awakened = true;
                var stored = Creature("wild-03", false);
                stored.currentHp = 8;
                stored.bond = 33;
                stored.nickname = "潮声";
                data.storedRoster.Add(stored);
                data.inventoryItemIds.AddRange(new[] { "berry-treat", "resonance-capsule" });
                data.inventoryCounts.AddRange(new[] { 3, 4 });
                Assert.That(store.Save(data, out var error), Is.True, error);

                Assert.That(store.TryLoad(catalog, out var restored, out var message), Is.True, message);
                Assert.That(restored.roomId, Is.EqualTo(data.roomId));
                Assert.That(restored.lastSceneId, Is.EqualTo(data.lastSceneId));
                Assert.That(restored.playerX, Is.EqualTo(data.playerX));
                Assert.That(restored.playerY, Is.EqualTo(data.playerY));
                Assert.That(restored.hasPosition && restored.chapterCompleted && restored.captureTutorialCompleted, Is.True);
                Assert.That(restored.playSeconds, Is.EqualTo(data.playSeconds));
                Assert.That(restored.seals.CanEnterLighthouse, Is.True);
                Assert.That(restored.seals.Count, Is.EqualTo(4));
                CollectionAssert.AreEqual(data.deployedIds, restored.deployedIds);
                CollectionAssert.AreEqual(data.collectedNodeIds, restored.collectedNodeIds);
                CollectionAssert.AreEqual(data.completedFieldIds, restored.completedFieldIds);
                CollectionAssert.AreEqual(data.discoveredSpeciesIds, restored.discoveredSpeciesIds);
                CollectionAssert.AreEqual(data.inventoryItemIds, restored.inventoryItemIds);
                CollectionAssert.AreEqual(data.inventoryCounts, restored.inventoryCounts);
                Assert.That(JsonUtility.ToJson(restored.roster[0]), Is.EqualTo(JsonUtility.ToJson(starter)));
                Assert.That(JsonUtility.ToJson(restored.storedRoster[0]), Is.EqualTo(JsonUtility.ToJson(stored)));
            });
        }

        [Test]
        public void UnitySaveLoadAndNewJourneyArchiveNeverModifyLegacyCFile()
        {
            InTemporaryDirectory((store, directory) =>
            {
                string legacyPath = System.IO.Path.Combine(directory, "mist_isle_save.dat");
                byte[] sentinel = { 0x4D, 0x49, 0x53, 0x54, 0x00, 0xFF, 0x03, 0x01 };
                File.WriteAllBytes(legacyPath, sentinel);
                DateTime modified = File.GetLastWriteTimeUtc(legacyPath);
                var data = Journey();
                Assert.That(store.Save(data, out var error), Is.True, error);
                Assert.That(store.TryLoad(catalog, out _, out var message), Is.True, message);
                store.ArchiveBeforeNewJourney();
                Assert.That(Directory.GetFiles(directory, "*.history").Length, Is.EqualTo(1));
                CollectionAssert.AreEqual(sentinel, File.ReadAllBytes(legacyPath));
                Assert.That(File.GetLastWriteTimeUtc(legacyPath), Is.EqualTo(modified));
                Assert.That(System.IO.Path.GetFileName(store.Path), Is.EqualTo("chapter-01.json"));
            });
        }

        [Test]
        public void FourSealsRemainIndependentAndLegacyCoastFlagMigratesToLighthouseRequirement()
        {
            var seals = new SealProgression();
            foreach (string id in GameCatalog.GuardianIds.Take(3)) seals.RecordGuardianVictory(id);
            Assert.That(seals.Count, Is.EqualTo(3));
            Assert.That(seals.HasSeal("lighthouse-warden"), Is.False);
            Assert.That(seals.CanEnterLighthouse, Is.False);
            seals.RecordGuardianVictory("lighthouse-warden");
            Assert.That(seals.Count, Is.EqualTo(4));
            Assert.That(seals.CanEnterLighthouse, Is.True);
            var legacy = JsonUtility.FromJson<SealProgression>(
                "{\"glowgladeGuardianDefeated\":true,\"tidalGuardianDefeated\":true,\"crystalGuardianDefeated\":true,\"coastGuardianDefeated\":true}");
            Assert.That(legacy.HasSeal("lighthouse-warden"), Is.True);
            Assert.That(legacy.CanEnterLighthouse, Is.True);
            Assert.That(legacy.Count, Is.EqualTo(4));
        }
    }
}
