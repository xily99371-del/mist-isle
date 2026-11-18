using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class DesignPackStoryConsequencesTests
    {
        private GameObject root;
        private GameSession session;
        private string saveDirectory;
        private string saveLeaf;
        private readonly Dictionary<Camera, bool> cameras = new Dictionary<Camera, bool>();
        private readonly Dictionary<AudioListener, bool> listeners = new Dictionary<AudioListener, bool>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            saveLeaf = "MistIsle-StoryConsequences-" + Guid.NewGuid().ToString("N");
            saveDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), saveLeaf));
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            { cameras[camera] = camera.enabled; camera.enabled = false; }
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            { listeners[listener] = listener.enabled; listener.enabled = false; }
            yield return CreateSession();
        }

        private IEnumerator CreateSession()
        {
            root = new GameObject("IsolatedStoryConsequences");
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = saveDirectory;
            yield return null;
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(saveDirectory, "chapter-01.json")));
        }

        private void DestroySession()
        {
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!cameras.ContainsKey(camera)) Object.Destroy(camera.gameObject);
        }

        private IEnumerator Restart()
        {
            DestroySession(); yield return null;
            yield return CreateSession();
            session.ContinueJourney(); yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroySession(); yield return null;
            foreach (var entry in cameras) if (entry.Key != null) entry.Key.enabled = entry.Value;
            foreach (var entry in listeners) if (entry.Key != null) entry.Key.enabled = entry.Value;
            cameras.Clear(); listeners.Clear();
            string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(saveDirectory);
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(full), saveLeaf, StringComparison.Ordinal)
                || !string.Equals(full, Path.Combine(allowed, saveLeaf), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing deletion outside the isolated story test save.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        private void Resume(string starter = "crystal-gecko", string room = "camp-0", bool threeSeals = false, bool rootBridge = false)
        {
            var data = new WorldSaveData {
                roomId = room, selectedStarterSpeciesId = starter, captureTutorialCompleted = true,
                openingStage = (int)OpeningStage.Complete, hasPosition = false
            };
            var companion = new CreatureInstance(session.Catalog[starter], 20, true);
            data.roster.Add(companion); data.deployedIds.Add(companion.instanceId);
            if (threeSeals)
                foreach (string id in new[] { "glowglade-warden", "tidal-warden", "crystal-warden" }) data.seals.RecordGuardianVictory(id);
            if (rootBridge) data.completedFieldIds.Add("M02_ROOT_BRIDGE");
            Assert.That(session.Saves.Save(data, out var error), Is.True, error);
            session.ContinueJourney();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
        }

        private WorldInteraction Target(string id) => session.World.Interactions.Single(value => value.Id == id);

        private IEnumerator Interact(string id)
        {
            var target = Target(id);
            Assert.That(target.CanInteract, Is.True, id);
            Vector2 point = (Vector2)target.transform.position + Vector2.down * .8f;
            var body = session.PlayerTransform.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero; body.position = point; session.PlayerTransform.position = point;
            Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(.24f);
            var sensor = session.PlayerTransform.GetComponent<PlayerInteractionSensor>();
            Assert.That(sensor.Current, Is.SameAs(target), "The real sensor cannot read " + id);
            Assert.That(sensor.TryInteractCurrent(), Is.True);
        }

        private IEnumerator ReadDialogue()
        {
            int remaining = 32;
            while (session.Panel == GamePanel.Dialogue && remaining-- > 0)
            { yield return new WaitForSecondsRealtime(.18f); session.AdvanceDialogue(); }
            Assert.That(remaining, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator VisibleArchiveAndTowerRecordsCanBeReadAndAnInterruptedArchiveResumes()
        {
            Resume(threeSeals: true);
            foreach (string id in new[] { "M03_CRYSTAL_ARCHIVE", "M04_ASCENT", "M04_FORECOURT" })
            {
                Assert.That(DesignPackCatalog.TryGetInteraction(id, out var node), Is.True);
                Assert.That(session.ChangeRoom(node.RoomId, false), Is.True);
                var record = Target(id).transform.Find("Readable world record");
                Assert.That(record, Is.Not.Null, "Authored dialogue must have a visible reading point.");
                Assert.That(record.GetComponentsInChildren<SpriteRenderer>().All(renderer => renderer.sprite != null && renderer.enabled), Is.True);
                yield return Interact(id);
                Assert.That(session.DialogueText, Is.EqualTo(DesignPackNarrative.Observe(id)[0].text));
                if (id == "M03_CRYSTAL_ARCHIVE")
                {
                    Assert.That(node.Position - DesignPackCatalog.GetMap("M03").Origin, Is.EqualTo(new Vector2(91f, 33f)), "The source anchor was moved.");
                    session.Save(false);
                    yield return Restart();
                    Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
                    Assert.That(session.DialogueText, Is.EqualTo(DesignPackNarrative.Observe(id)[0].text));
                }
                yield return ReadDialogue();
                Assert.That(session.Progress.storyFlags, Does.Contain(id));
            }
        }

        [UnityTest]
        public IEnumerator GroveExplainsItsRootsBeforeTheAbilityAndStillRequiresALivingPartner()
        {
            Resume(room: "glowglade-0");
            yield return Interact("M02_ANCIENT_GROVE");
            Assert.That(session.DialogueText, Is.EqualTo(DesignPackNarrative.Observe("M02_ANCIENT_GROVE")[0].text));
            Assert.That(session.World.IsFieldOpen("M02_ROOT_BRIDGE"), Is.False);
            yield return ReadDialogue();
            Assert.That(session.World.IsFieldOpen("M02_ROOT_BRIDGE"), Is.False, "Reading a clue granted an unavailable ability.");
            var helper = new CreatureInstance(session.Catalog["wild-01"], 3, false) { currentHp = 0 };
            Assert.That(session.Party.TryAdd(helper), Is.True);
            yield return Interact("M02_ANCIENT_GROVE"); yield return ReadDialogue();
            Assert.That(session.World.IsFieldOpen("M02_ROOT_BRIDGE"), Is.False, "A fainted helper activated the bridge.");
            helper.currentHp = 1;
            yield return Interact("M02_ANCIENT_GROVE"); yield return ReadDialogue();
            Assert.That(session.World.IsFieldOpen("M02_ROOT_BRIDGE"), Is.True);
            Assert.That(session.Progress.completedFieldIds, Does.Contain("M02_ROOT_BRIDGE"));
            yield return Restart();
            Assert.That(session.Progress.storyFlags, Does.Contain("M02_ANCIENT_GROVE"));
            Assert.That(session.World.IsFieldOpen("M02_ROOT_BRIDGE"), Is.True);
        }

        [UnityTest]
        public IEnumerator GuardianVictoryRestoresTheGroveAndHerbsKeepTheirHealingAndCooldownAfterReload()
        {
            Resume(room: "glowglade-0", rootBridge: true);
            var consequence = root.GetComponentInChildren<DesignPackWorldConsequences>(true);
            Assert.That(consequence, Is.Not.Null);
            Assert.That(consequence.ForestRestored, Is.False);
            Assert.That(consequence.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
            Assert.That(Target("M02_RECOVERY_HERBS").CanInteract, Is.False);
            session.HandleWorldAction("resource", "M02_RECOVERY_HERBS");
            Assert.That(session.Inventory.GetCount("pollen"), Is.Zero, "A locked harvest paid out.");
            yield return Interact("glowglade-warden"); yield return ReadDialogue();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            for (int i = 0; i < 80 && session.Battle.Outcome == BattleOutcome.Ongoing; i++) session.Act(BattleAction.Attack);
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            session.LeaveBattle();
            Assert.That(consequence.ForestRestored, Is.True, "The real victory path did not update the world.");
            Assert.That(consequence.GetComponentsInChildren<SpriteRenderer>(), Is.Not.Empty);
            Assert.That(Target("M02_RECOVERY_HERBS").CanInteract, Is.True);
            yield return ReadDialogue();
            var companion = session.Party.Members.First();
            companion.currentHp = CreatureInstance.MaxHp(session.Catalog[companion.speciesId], companion.level) - 10;
            int beforeHp = companion.currentHp;
            yield return Interact("M02_RECOVERY_HERBS");
            Assert.That(session.Inventory.GetCount("pollen"), Is.EqualTo(2));
            Assert.That(companion.currentHp, Is.EqualTo(beforeHp + 4));
            Assert.That(Target("M02_RECOVERY_HERBS").CanInteract, Is.False);
            long deadline = session.Progress.nodeRefreshStates.Single(state => state.id == "M02_RECOVERY_HERBS").availableAtUtc;
            Assert.That(deadline - DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Is.InRange(110, 120));
            yield return Restart();
            consequence = root.GetComponentInChildren<DesignPackWorldConsequences>(true);
            Assert.That(consequence.ForestRestored, Is.True);
            Assert.That(Target("M02_RECOVERY_HERBS").CanInteract, Is.False);
            Assert.That(session.Inventory.GetCount("pollen"), Is.EqualTo(2));
            Assert.That(session.Party.Members.First().currentHp, Is.EqualTo(beforeHp + 4));
            Assert.That(session.Progress.nodeRefreshStates.Single(state => state.id == "M02_RECOVERY_HERBS").availableAtUtc, Is.EqualTo(deadline));
            session.HandleWorldAction("resource", "M02_RECOVERY_HERBS");
            Assert.That(session.Inventory.GetCount("pollen"), Is.EqualTo(2), "A saved cooldown was bypassed.");
        }

        [UnityTest]
        public IEnumerator CampReportsSpecificCompletedRoutesAndRepeatWatcherVisitsOpenCareDirectly()
        {
            Resume();
            session.Progress.seals.RecordGuardianVictory("glowglade-warden");
            yield return Interact("M00_NOTICE");
            Assert.That(session.DialogueText, Does.Contain("森息药草重新开花"));
            yield return ReadDialogue();
            yield return Interact("watcher");
            Assert.That(session.DialogueSpeakerId, Is.EqualTo("watcher"));
            Assert.That(session.DialogueText, Does.Contain("森印"));
            yield return ReadDialogue();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Watcher));
            session.ClosePanel();
            yield return Interact("watcher");
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Watcher), "Unchanged progress blocked repeat care behind the same speech.");
            session.ClosePanel();
            session.Progress.chapterCompleted = true;
            yield return Interact("M00_NOTICE");
            Assert.That(session.DialogueText, Does.Contain("回执"));
            Assert.That(session.DialogueText, Does.Contain("可以接收补给"));
            yield return ReadDialogue();
            yield return Interact("shiguang");
            Assert.That(session.DialogueText, Does.Contain("第一段路"));
            yield return ReadDialogue();
            session.Save(false);
            yield return Restart();
            yield return Interact("watcher");
            Assert.That(session.DialogueText, Does.Contain("回执"));
            yield return ReadDialogue();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Watcher));
        }
    }
}
