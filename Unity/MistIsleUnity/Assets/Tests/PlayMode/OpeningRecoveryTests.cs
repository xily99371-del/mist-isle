using System;
using System.Collections;
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
    public sealed class OpeningRecoveryTests
    {
        private GameObject root;
        private GameSession session;
        private Camera[] previousCameras;
        private string temporaryDirectory;
        private string temporaryLeaf;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryLeaf = "MistIsleOpeningRecovery-" + Guid.NewGuid().ToString("N");
            temporaryDirectory = Path.Combine(Path.GetTempPath(), temporaryLeaf);
            previousCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            yield return CreateSession();
        }

        private IEnumerator CreateSession()
        {
            root = new GameObject("IsolatedOpeningRecovery");
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = temporaryDirectory;
            yield return null;
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(temporaryDirectory, "chapter-01.json")));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Title));
        }

        private void DestroySessionAndOwnedCameras()
        {
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!previousCameras.Contains(camera)) Object.Destroy(camera.gameObject);
        }

        private IEnumerator RestartFromDisk()
        {
            DestroySessionAndOwnedCameras();
            yield return null;
            yield return CreateSession();
            session.ContinueJourney();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroySessionAndOwnedCameras();
            yield return null;
            if (!Directory.Exists(temporaryDirectory)) yield break;
            string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(temporaryDirectory);
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(full), temporaryLeaf, StringComparison.Ordinal)
                || !string.Equals(full, Path.Combine(allowed, temporaryLeaf), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to remove an unverified temporary recovery-test directory.");
            Directory.Delete(full, true);
        }

        private WorldInteraction Npc(string id) => session.World.Interactions.Single(x => x.Kind == "npc" && x.Id == id);

        private void MovePlayer(Vector2 point)
        {
            session.PlayerTransform.GetComponent<Rigidbody2D>().position = point;
            session.PlayerTransform.position = point;
            Physics2D.SyncTransforms();
        }

        private IEnumerator ReachWatcher()
        {
            session.NewJourney();
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetWatcher));
        }

        private IEnumerator ReachGuide()
        {
            yield return ReachWatcher();
            Npc("watcher").Interact();
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));
        }

        private IEnumerator ReachFollowGuide()
        {
            yield return OpeningTestDriver.ChooseCompanion(session);
            foreach (string item in new[] { "berry", "dew" })
            {
                session.World.Interactions.First(x => x.Kind == "resource" && x.Id.StartsWith(item + "-camp-0-")).Interact();
                yield return OpeningTestDriver.FinishDialogue(session);
            }
            session.World.Interactions.Single(x => x.Kind == "campfire" && x.Id == "campfire-camp-0").Interact();
            session.Craft(session.Recipes.Single(x => x.recipeId == "berry-treat"));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.FollowGuide));
        }

        [UnityTest]
        public IEnumerator FinalDialogueConfirmCannotAlsoActivateTheWorldTargetButPromptAndNextPressWork()
        {
            yield return ReachWatcher();
            MovePlayer(session.World.GetLandmarkPosition("watcher-approach"));
            yield return new WaitForSecondsRealtime(.23f);
            var watcher = Npc("watcher");
            var sensor = session.PlayerTransform.GetComponent<PlayerInteractionSensor>();
            Assert.That(sensor.Current, Is.SameAs(watcher), "The real sensor cannot reach Watcher at the authored approach point.");
            Assert.That(sensor.TryInteractCurrent(), Is.True);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            while (session.DialoguePageIndex < session.DialoguePageCount - 1)
            {
                yield return new WaitForSecondsRealtime(.18f);
                session.AdvanceDialogue();
            }
            yield return new WaitForSecondsRealtime(.18f);
            session.AdvanceDialogue();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));

            // Reproduce the adverse component order: dialogue finishes before this frame's sensor update.
            sensor.SendMessage("Update", SendMessageOptions.RequireReceiver);
            Assert.That(sensor.Current, Is.SameAs(watcher), "Suppressing activation must keep the candidate prompt available.");
            Assert.That(session.InteractionPrompt, Is.Not.Null.And.Not.Empty);
            Assert.That(sensor.TryInteractCurrent(), Is.False, "The E activation already consumed by dialogue leaked to the world.");
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            yield return null;
            Assert.That(sensor.Current, Is.SameAs(watcher));
            yield return new WaitForSecondsRealtime(.23f);
            Assert.That(sensor.TryInteractCurrent(), Is.True, "A later distinct interaction was incorrectly blocked.");
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Watcher));
        }

        [UnityTest]
        public IEnumerator ReloadingMidwayAndAtTheEndOfGuideRouteKeepsTheGuideNearbyAndAbleToContinue()
        {
            yield return ReachFollowGuide();
            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            MovePlayer(session.World.GetLandmarkPosition("coast-waypoint-2"));
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.FollowGuide));
            var guide = session.World.GetActor("shiguang");
            Assert.That(Vector2.Distance(guide.position, session.PlayerTransform.position), Is.LessThan(4.2f),
                "Loading returned the guide to the distant coast entrance.");
            Vector2 restoredGuide = guide.position;
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(Vector2.Distance(restoredGuide, guide.position), Is.GreaterThan(.15f),
                "The guide did not resume walking along the remaining route.");

            MovePlayer(session.World.GetLandmarkPosition("tutorial-viewpoint"));
            session.Save(false);
            yield return RestartFromDisk();
            guide = session.World.GetActor("shiguang");
            Assert.That(Vector2.Distance(guide.position, session.PlayerTransform.position), Is.LessThan(4.2f));
            session.World.Interactions.Single(x => x.Id == "tutorial-coast-crab").Interact();
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            Assert.That(session.Battle.IsCaptureLesson, Is.True,
                "A route checkpoint could not continue into the actual capture lesson.");
        }

        [UnityTest]
        public IEnumerator GuideWaitsForPlayerBehindButKeepsWalkingWhenPlayerRunsAhead()
        {
            yield return ReachFollowGuide();
            Vector2 routeStart = session.World.GetLandmarkPosition("campfire-approach") + new Vector2(1.6f, 1.1f);
            Vector2 firstWaypoint = session.World.GetLandmarkPosition("guide-camp-waypoint");
            Vector2 waitingPosition = Vector2.Lerp(routeStart, firstWaypoint, .5f);
            session.World.MoveActorFacing("shiguang", waitingPosition, Vector2.right);
            MovePlayer(routeStart);
            var guide = session.World.GetActor("shiguang");
            Assert.That(Vector2.Distance(guide.position, session.PlayerTransform.position), Is.GreaterThan(4.2f));
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(Vector2.Distance(guide.position, waitingPosition), Is.LessThan(.05f),
                "The guide must wait when the player is far behind on the route.");

            MovePlayer(session.World.GetLandmarkPosition("coast-exit"));
            Vector2 beforeCatchUp = guide.position;
            Assert.That(Vector2.Distance(guide.position, session.PlayerTransform.position), Is.GreaterThan(4.2f));
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(Vector2.Distance(guide.position, beforeCatchUp), Is.GreaterThan(.25f),
                "Running ahead left the guide permanently waiting behind the player.");
            Vector2 continuedPosition = guide.position;
            Assert.That(Vector2.Distance(guide.position, session.PlayerTransform.position), Is.GreaterThan(4.2f));
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(Vector2.Distance(guide.position, continuedPosition), Is.GreaterThan(.25f),
                "The guide must continue catching up while the player remains farther ahead.");
        }

        [UnityTest]
        public IEnumerator InterruptedWatcherIntroductionReplaysBeforeAdvancingItsSavedStage()
        {
            yield return ReachWatcher();
            Npc("watcher").Interact();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            Assert.That(session.DialogueSpeakerId, Is.EqualTo("watcher"));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetWatcher));
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetWatcher));
            Npc("watcher").Interact();
            Assert.That(session.DialoguePageIndex, Is.Zero);
            Assert.That(session.DialoguePageCount, Is.GreaterThan(1));
            Assert.That(session.DialogueSpeakerId, Is.EqualTo("watcher"));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetWatcher));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));
            Assert.That(session.Saves.TryLoad(session.Catalog, out var stored, out var message), Is.True, message);
            Assert.That(stored.openingStage, Is.EqualTo((int)OpeningStage.MeetShiguang));
        }

        [UnityTest]
        public IEnumerator InterruptedShiguangIntroductionCannotUnlockPartnerChoiceBeforeItIsRead()
        {
            yield return ReachGuide();
            Npc("shiguang").Interact();
            Assert.That(session.DialogueSpeakerId, Is.EqualTo("shiguang"));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));
            Assert.That(session.Party.Count, Is.Zero);
            Npc("shiguang").Interact();
            Assert.That(session.DialoguePageIndex, Is.Zero);
            Assert.That(session.DialoguePageCount, Is.GreaterThan(1));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.ChooseCompanion));
            Assert.That(session.Saves.TryLoad(session.Catalog, out var stored, out var message), Is.True, message);
            Assert.That(stored.openingStage, Is.EqualTo((int)OpeningStage.ChooseCompanion));
        }

        [UnityTest]
        public IEnumerator InterruptedFinalHandoffRetainsRescuedCompanionAndReplaysBeforeOpeningCompletes()
        {
            yield return ReachFollowGuide();
            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            session.World.Interactions.Single(x => x.Id == "tutorial-coast-crab").Interact();
            yield return OpeningTestDriver.FinishDialogue(session);
            session.Act(BattleAction.Attack);
            session.Act(BattleAction.Feed, "berry-treat");
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.Captured));
            session.LeaveBattle();
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.ReportWatcher));
            Assert.That(session.ChangeRoom("camp-0", false), Is.True);
            string[] companionIds = session.Party.Members.Select(x => x.instanceId).ToArray();
            Npc("watcher").Interact();
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.ReportWatcher));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.ReportWatcher));
            CollectionAssert.AreEqual(companionIds, session.Party.Members.Select(x => x.instanceId).ToArray());
            Npc("watcher").Interact();
            Assert.That(session.DialoguePageIndex, Is.Zero);
            Assert.That(session.DialoguePageCount, Is.GreaterThan(1));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.ReportWatcher));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.Complete));
            Assert.That(session.Saves.TryLoad(session.Catalog, out var stored, out var message), Is.True, message);
            Assert.That(stored.openingStage, Is.EqualTo((int)OpeningStage.Complete));
            Assert.That(stored.captureTutorialCompleted, Is.True);
            CollectionAssert.AreEqual(companionIds, stored.roster.Select(x => x.instanceId).ToArray());
        }
    }
}
