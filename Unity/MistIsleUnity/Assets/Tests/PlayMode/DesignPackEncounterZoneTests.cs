using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Player;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class DesignPackEncounterZoneTests
    {
        private GameObject root;
        private GameSession session;
        private Rigidbody2D player;
        private string saveDirectory;
        private string temporaryLeaf;
        private readonly Dictionary<Camera, bool> previousCameras = new Dictionary<Camera, bool>();
        private readonly Dictionary<AudioListener, bool> previousListeners = new Dictionary<AudioListener, bool>();
        private DesignPackEncounterZone zone;
        private DesignPackNode node;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryLeaf = "MistIsle-WildGrass-" + Guid.NewGuid().ToString("N");
            saveDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), temporaryLeaf));
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            { previousCameras.Add(camera, camera.enabled); camera.enabled = false; }
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            { previousListeners.Add(listener, listener.enabled); listener.enabled = false; }
            root = new GameObject("ActualWildGrassSession");
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = saveDirectory;
            yield return null;
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(saveDirectory, "chapter-01.json")));
            var starter = new CreatureInstance(session.Catalog["moss-lumi"], 3, true);
            var data = new WorldSaveData {
                selectedStarterSpeciesId = starter.speciesId, roomId = "coast-0",
                openingStage = (int)OpeningStage.Complete, captureTutorialCompleted = true
            };
            data.roster.Add(starter); data.deployedIds.Add(starter.instanceId);
            Assert.That(session.Saves.Save(data, out var reason), Is.True, reason);
            session.ContinueJourney();
            session.GetComponent<OpeningDirector>().enabled = false;
            player = session.PlayerTransform.GetComponent<Rigidbody2D>();
            // Drive the actual dynamic Rigidbody directly, without pretending that keyboard input was sent.
            session.PlayerTransform.GetComponent<TopDownPlayerMotor>().enabled = false;
            DesignPackEncounterZone.Install(session, root.transform);
            Assert.That(DesignPackCatalog.TryGetInteraction("M01_ENCOUNTER_GRASS", out node), Is.True);
            zone = root.GetComponentsInChildren<DesignPackEncounterZone>().Single(value => value.NodeId == node.Id);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (player != null) player.linearVelocity = Vector2.zero;
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!previousCameras.ContainsKey(camera)) Object.Destroy(camera.gameObject);
            yield return null;
            foreach (var pair in previousCameras) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in previousListeners) if (pair.Key != null) pair.Key.enabled = pair.Value;
            previousCameras.Clear(); previousListeners.Clear();
            if (!Directory.Exists(saveDirectory)) yield break;
            string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string resolved = Path.GetFullPath(saveDirectory);
            if (!resolved.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(resolved), temporaryLeaf, StringComparison.Ordinal)
                || !string.Equals(resolved, Path.Combine(allowed, temporaryLeaf), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to delete an unverified wild-grass test directory.");
            Directory.Delete(resolved, true);
        }

        private Vector2 Point(float x, float y) => DesignPackCatalog.GetMap(node.MapId).Origin + new Vector2(x, y);

        private IEnumerator PlaceAt(Vector2 point, bool expectedContact)
        {
            player.linearVelocity = Vector2.zero;
            player.position = point; session.PlayerTransform.position = point;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(zone.GetComponent<BoxCollider2D>().IsTouching(session.PlayerTransform.GetComponent<Collider2D>()),
                Is.EqualTo(expectedContact), "The test did not enter/leave the real authored trigger.");
        }

        private IEnumerator Walk(Vector2 direction, float seconds, bool stopAtBattle = false)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until && (!stopAtBattle || session.Panel != GamePanel.Battle))
            {
                player.linearVelocity = direction.normalized * 4.2f;
                yield return new WaitForFixedUpdate();
            }
            player.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator InstallationUsesOnlyAuthoredRectangularWildZonesAndIsIdempotent()
        {
            var first = DesignPackEncounterZone.Install(session, root.transform);
            var second = DesignPackEncounterZone.Install(session, root.transform);
            CollectionAssert.AreEquivalent(new[] { "M01_ENCOUNTER_GRASS", "M02_FIREFLY_MEADOW" }, first.Select(value => value.NodeId));
            CollectionAssert.AreEquivalent(first, second);
            Assert.That(root.GetComponentsInChildren<DesignPackEncounterZone>().Length, Is.EqualTo(2));
            Assert.That(zone.WorldBounds, Is.EqualTo(new Rect(256f + 24f, 37f, 9f, 7f)));
            Assert.That(zone.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(9f, 7f)));
            Assert.That(zone.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(first.Any(value => value.NodeId == "tutorial-coast-crab"), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealWalkingStartsAWildBattleFromTheAuthoredSpeciesAndLevelPool()
        {
            zone.Configure(session, node, new EncounterZoneSettings(.6f, 1f, 2f, 0f), () => 0f);
            yield return PlaceAt(Point(26f, 41f), true);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(session.Battle, Is.Null, "Standing in grass rolled an encounter.");
            Vector2 start = player.position;
            yield return Walk(Vector2.right, .6f, true);
            Assert.That(Vector2.Distance(start, player.position), Is.GreaterThanOrEqualTo(.55f));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            Assert.That(session.Battle.Kind, Is.EqualTo(BattleKind.Wild));
            Assert.That(node.EncounterSpeciesIds, Does.Contain(session.Battle.Wild.speciesId));
            Assert.That(session.Battle.Wild.level, Is.InRange(node.MinLevel, node.MaxLevel));
            Assert.That(session.Battle.IsCaptureLesson, Is.False);
        }

        [UnityTest]
        public IEnumerator StandingMenusAndUnfinishedTutorialNeverRequestAutomaticEncounters()
        {
            int rolls = 0;
            zone.Configure(session, node, new EncounterZoneSettings(.25f, 1f, 0f, 0f), () => { rolls++; return 0f; });
            yield return PlaceAt(Point(26f, 41f), true);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(rolls, Is.Zero);
            session.SetPanel(GamePanel.Party);
            yield return Walk(Vector2.right, .25f);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Party));
            Assert.That(rolls, Is.Zero, "A menu allowed walking encounters while physics displaced the traveler.");
            session.SetPanel(GamePanel.Explore);
            session.Progress.openingStage = (int)OpeningStage.FollowGuide;
            yield return Walk(Vector2.left, .25f);
            Assert.That(rolls, Is.Zero, "The authored first capture lesson was interrupted by random grass encounters.");
            Assert.That(session.Battle, Is.Null);
        }

        [UnityTest]
        public IEnumerator TeleportAndReentryCannotTurnPartialStepsIntoAnImmediateEncounter()
        {
            int rolls = 0;
            zone.Configure(session, node, new EncounterZoneSettings(2f, 1f, 3f, 0f), () => { rolls++; return 0f; });
            yield return PlaceAt(Point(25f, 41f), true);
            yield return Walk(Vector2.right, .2f);
            yield return PlaceAt(Point(31f, 41f), true);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(rolls, Is.Zero, "Teleport distance was counted as walking.");
            yield return PlaceAt(Point(35f, 41f), false);
            yield return PlaceAt(Point(26f, 41f), true);
            yield return Walk(Vector2.right, .2f);
            Assert.That(rolls, Is.Zero, "Leaving/re-entering carried the previous visit's incomplete steps.");
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
        }

        [UnityTest]
        public IEnumerator ADepletedNodeStaysQuietUntilItsSavedRefreshDeadline()
        {
            zone.Configure(session, node, new EncounterZoneSettings(.3f, 1f, 2f, 0f), () => 0f);
            var state = new NodeRefreshState { id = node.Id, availableAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120 };
            session.Progress.nodeRefreshStates.Add(state);
            yield return PlaceAt(Point(26f, 41f), true);
            yield return Walk(Vector2.right, .3f);
            Assert.That(session.Battle, Is.Null, "Walking bypassed the node's persisted cooldown.");
            state.availableAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
            yield return Walk(Vector2.left, .3f, true);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle), "A refreshed node remained permanently suppressed.");
            Assert.That(node.EncounterSpeciesIds, Does.Contain(session.Battle.Wild.speciesId));
        }

        [UnityTest]
        public IEnumerator FailedRollKeepsItsIntervalAcrossReentryAndWaitingDoesNotRoll()
        {
            int rolls = 0;
            zone.Configure(session, node, new EncounterZoneSettings(.25f, .5f, 2.5f, 0f), () => ++rolls == 1 ? 1f : 0f);
            yield return PlaceAt(Point(26f, 41f), true);
            yield return Walk(Vector2.right, .3f);
            Assert.That(rolls, Is.EqualTo(1));
            Assert.That(session.Battle, Is.Null);
            yield return PlaceAt(Point(35f, 41f), false);
            yield return PlaceAt(Point(26f, 41f), true);
            yield return Walk(Vector2.right, .3f);
            Assert.That(rolls, Is.EqualTo(1), "Re-entry bypassed the failed roll interval.");
            yield return new WaitForSecondsRealtime(2.6f);
            Assert.That(rolls, Is.EqualTo(1), "Waiting after a failed roll caused an idle encounter.");
            yield return Walk(Vector2.left, .3f, true);
            Assert.That(rolls, Is.EqualTo(2));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
        }
    }
}
