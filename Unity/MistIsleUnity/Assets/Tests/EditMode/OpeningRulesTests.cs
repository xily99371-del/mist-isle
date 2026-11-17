using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MistIsle.Creatures;
using MistIsle.Items;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class OpeningRulesTests
    {
        private Dictionary<string, CreatureSpecies> catalog;

        [SetUp]
        public void SetUp() => catalog = GameCatalog.CreateSpecies();

        [TearDown]
        public void TearDown()
        {
            foreach (var species in catalog.Values) UnityEngine.Object.DestroyImmediate(species);
        }

        private PartyRoster PartyWithStarter(string speciesId)
        {
            var party = new PartyRoster();
            Assert.That(party.TryAdd(new CreatureInstance(catalog[speciesId], 3, true)), Is.True);
            return party;
        }

        [TestCase("moss-lumi", BattleAction.Feed, "berry-treat")]
        [TestCase("moatail", BattleAction.Feed, "berry-treat")]
        [TestCase("crystal-gecko", BattleAction.Feed, "berry-treat")]
        [TestCase("moss-lumi", BattleAction.Capture, "resonance-capsule")]
        [TestCase("moatail", BattleAction.Capture, "resonance-capsule")]
        [TestCase("crystal-gecko", BattleAction.Capture, "resonance-capsule")]
        public void EachStarterLearnsWeakeningBeforeGuaranteedCaptureWithModestGrowth(
            string starterId, BattleAction captureAction, string item)
        {
            var party = PartyWithStarter(starterId);
            var starter = party.Members[0];
            var inventory = new Inventory();
            inventory.Add("berry-treat", 2);
            inventory.Add("resonance-capsule", 2);
            var wild = new CreatureInstance(catalog["wild-03"], 1, false);
            // The highest roll would fail a normal encounter; only the guided lesson guarantees this result.
            var lesson = new BattleSession(party, catalog, inventory, wild, () => 1f, captureLesson: true);
            Assert.That(lesson.IsCaptureLesson, Is.True);
            Assert.That(lesson.CaptureLessonWeakened, Is.False);
            int initialStarterHp = starter.currentHp;
            int initialWildHp = wild.currentHp;

            Assert.That(lesson.TryAct(captureAction, item).Success, Is.False);
            Assert.That(inventory.GetCount(item), Is.EqualTo(2));
            Assert.That(starter.currentHp, Is.EqualTo(initialStarterHp));
            Assert.That(wild.currentHp, Is.EqualTo(initialWildHp));
            Assert.That(lesson.CurrentActor, Is.SameAs(starter));
            Assert.That(party.Count, Is.EqualTo(1));

            Assert.That(lesson.TryAct(BattleAction.Attack).Success, Is.True);
            Assert.That(lesson.CaptureLessonWeakened, Is.True);
            Assert.That(wild.currentHp, Is.GreaterThan(0).And.LessThan(initialWildHp));
            Assert.That(lesson.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            var result = lesson.TryAct(captureAction, item);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Captured));
            Assert.That(party.Count, Is.EqualTo(2));
            Assert.That(party.Members.Contains(wild), Is.True);
            Assert.That(inventory.GetCount(item), Is.EqualTo(1));
            Assert.That(party.Deployed.Count, Is.LessThanOrEqualTo(PartyRoster.DeploymentCapacity));
            Assert.That(starter.speciesId, Is.EqualTo(starterId));
            Assert.That(starter.level, Is.EqualTo(3));
            Assert.That(starter.experience, Is.EqualTo(8));
            Assert.That(starter.bond, Is.EqualTo(12));
            Assert.That(starter.evolutionStage, Is.Zero);

            Assert.That(lesson.TryAct(captureAction, item).Success, Is.False);
            Assert.That(inventory.GetCount(item), Is.EqualTo(1));
            Assert.That(starter.experience, Is.EqualTo(8));
            Assert.That(starter.bond, Is.EqualTo(12));
        }

        [Test]
        public void EvenOverpoweredLessonAttacksCannotKillTheWildPartner()
        {
            var party = PartyWithStarter("crystal-gecko");
            catalog["crystal-gecko"].baseAttack = 10000;
            var wild = new CreatureInstance(catalog["wild-03"], 1, false);
            var lesson = new BattleSession(party, catalog, new Inventory(), wild, () => 0f, captureLesson: true);
            Assert.That(lesson.TryAct(BattleAction.Attack).Success, Is.True);
            Assert.That(wild.currentHp, Is.EqualTo(1));
            Assert.That(lesson.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            Assert.That(lesson.TryAct(BattleAction.Skill).Success, Is.True);
            Assert.That(wild.currentHp, Is.EqualTo(1));
            Assert.That(lesson.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
        }

        [Test]
        public void OrdinaryWildCaptureKeepsProbabilityAndDoesNotRequireLessonWeakening()
        {
            var party = PartyWithStarter("moss-lumi");
            var inventory = new Inventory();
            inventory.Add("resonance-capsule", 2);
            var failed = new BattleSession(party, catalog, inventory,
                new CreatureInstance(catalog["wild-03"], 1, false), () => 1f);
            Assert.That(failed.IsCaptureLesson, Is.False);
            Assert.That(failed.CaptureLessonWeakened, Is.False);
            Assert.That(failed.TryAct(BattleAction.Capture).Success, Is.True);
            Assert.That(failed.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            Assert.That(party.Count, Is.EqualTo(1));
            Assert.That(inventory.GetCount("resonance-capsule"), Is.EqualTo(1));

            var succeeded = new BattleSession(party, catalog, inventory,
                new CreatureInstance(catalog["wild-03"], 1, false), () => 0f);
            Assert.That(succeeded.TryAct(BattleAction.Capture).Outcome, Is.EqualTo(BattleOutcome.Captured));
            Assert.That(party.Count, Is.EqualTo(2));
            Assert.That(inventory.GetCount("resonance-capsule"), Is.Zero);
            Assert.That(party.Members[0].experience, Is.Not.EqualTo(8));
        }

        [Test]
        public void LessonFlagCannotMakeGuardiansCapturableOrOrdinaryWildImmortal()
        {
            var party = PartyWithStarter("crystal-gecko");
            var inventory = new Inventory();
            inventory.Add("resonance-capsule");
            var guardian = new BattleSession(party, catalog, inventory,
                new CreatureInstance(catalog["glowglade-warden"], 1, false), () => 0f,
                BattleKind.Guardian, captureLesson: true);
            Assert.That(guardian.IsCaptureLesson, Is.False);
            Assert.That(guardian.TryAct(BattleAction.Capture).Success, Is.False);
            Assert.That(inventory.GetCount("resonance-capsule"), Is.EqualTo(1));

            catalog["crystal-gecko"].baseAttack = 10000;
            var normal = new BattleSession(party, catalog, inventory,
                new CreatureInstance(catalog["wild-03"], 1, false), () => 0f);
            Assert.That(normal.TryAct(BattleAction.Attack).Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(normal.Wild.currentHp, Is.Zero);
        }

        [Serializable]
        private sealed class LegacyV2
        {
            public int schemaVersion = 2;
            public string selectedStarterSpeciesId = "moatail";
            public List<CreatureInstance> roster = new List<CreatureInstance>();
            public List<CreatureInstance> storedRoster = new List<CreatureInstance>();
            public List<string> deployedIds = new List<string>();
            public List<string> inventoryItemIds = new List<string>();
            public List<int> inventoryCounts = new List<int>();
        }

        [Test]
        public void SchemaTwoExistingJourneyMigratesToCompletedOpeningWithoutChangingCompanions()
        {
            InTemporaryDirectory(store =>
            {
                var legacy = new LegacyV2();
                var starter = new CreatureInstance(catalog["moatail-evolved"], 7, true)
                    { bond = 36, experience = 13, currentHp = 21, evolutionStage = 1 };
                var reserve = new CreatureInstance(catalog["wild-02"], 4, false) { nickname = "海风" };
                var stored = new CreatureInstance(catalog["wild-01"], 5, false) { bond = 18 };
                legacy.roster.AddRange(new[] { starter, reserve });
                legacy.storedRoster.Add(stored);
                legacy.deployedIds.AddRange(new[] { starter.instanceId, reserve.instanceId });
                legacy.inventoryItemIds.Add("berry-treat");
                legacy.inventoryCounts.Add(3);
                string json = JsonUtility.ToJson(legacy);
                Assert.That(json.Contains("openingStage"), Is.False);
                File.WriteAllText(store.Path, json, new UTF8Encoding(false));
                byte[] original = File.ReadAllBytes(store.Path);

                Assert.That(store.TryLoad(catalog, out var loaded, out var message), Is.True, message);
                Assert.That(loaded.schemaVersion, Is.EqualTo(WorldSaveData.CurrentSchemaVersion));
                Assert.That(loaded.openingStage, Is.EqualTo((int)OpeningStage.Complete));
                Assert.That(loaded.selectedStarterSpeciesId, Is.EqualTo("moatail"));
                CollectionAssert.AreEqual(legacy.roster.Select(x => JsonUtility.ToJson(x)).ToArray(),
                    loaded.roster.Select(x => JsonUtility.ToJson(x)).ToArray());
                Assert.That(JsonUtility.ToJson(loaded.storedRoster.Single()), Is.EqualTo(JsonUtility.ToJson(stored)));
                CollectionAssert.AreEqual(legacy.deployedIds, loaded.deployedIds);
                Assert.That(loaded.inventoryCounts.Single(), Is.EqualTo(3));
                CollectionAssert.AreEqual(original, File.ReadAllBytes(store.Path));
            });
        }

        [TestCase(OpeningStage.Arrival)]
        [TestCase(OpeningStage.MeetWatcher)]
        [TestCase(OpeningStage.MeetShiguang)]
        [TestCase(OpeningStage.ChooseCompanion)]
        public void SchemaThreeBeforeChoiceCheckpointKeepsItsStageWithoutInventingStarter(OpeningStage stage)
        {
            InTemporaryDirectory(store =>
            {
                var checkpoint = new WorldSaveData { openingStage = (int)stage };
                Assert.That(checkpoint.roster, Is.Empty);
                Assert.That(store.Save(checkpoint, out var error), Is.True, error);
                Assert.That(store.TryLoad(catalog, out var loaded, out var message), Is.True, message);
                Assert.That(loaded.schemaVersion, Is.EqualTo(WorldSaveData.CurrentSchemaVersion));
                Assert.That(loaded.openingStage, Is.EqualTo((int)stage));
                Assert.That(loaded.selectedStarterSpeciesId, Is.Null.Or.Empty);
                Assert.That(loaded.roster, Is.Empty);
                Assert.That(loaded.storedRoster, Is.Empty);
                Assert.That(loaded.deployedIds, Is.Empty);
                Assert.That(loaded.captureTutorialCompleted, Is.False);
            });
        }

        private static void InTemporaryDirectory(Action<ChapterSaveStore> action)
        {
            string tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath())
                .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                + System.IO.Path.DirectorySeparatorChar;
            string leaf = "MistIsle-OpeningRulesTests-" + Guid.NewGuid().ToString("N");
            string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(tempRoot, leaf));
            Directory.CreateDirectory(directory);
            try { action(new ChapterSaveStore(directory)); }
            finally
            {
                string resolved = System.IO.Path.GetFullPath(directory);
                if (!resolved.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(System.IO.Path.GetFileName(resolved), leaf, StringComparison.Ordinal)
                    || !string.Equals(resolved, System.IO.Path.Combine(tempRoot, leaf), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing to delete an unverified test directory.");
                if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            }
        }
    }
}
