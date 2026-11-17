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
            session.World.Interactions.Single(x => x.Kind == "campfire" && x.Id == "M00_CAMPFIRE").Interact();
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

            MovePlayer(session.World.GetLandmarkPosition("coast-exit") + Vector2.left * 3f);
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
        public IEnumerator CampExitRemainsReachableFromLeftAboveAndBelowAfterGuideArrives()
        {
            yield return ReachFollowGuide();
            Vector2 exit = session.World.GetLandmarkPosition("coast-exit");
            var guide = session.World.GetActor("shiguang");
            MovePlayer(exit + Vector2.left * 3f);
            Vector2 segmentStart = session.World.GetLandmarkPosition("guide-camp-waypoint");
            Vector2 waitingPoint = session.World.GetLandmarkPosition("guide-exit-wait");
            var obstacles = Physics2D.CircleCastAll(segmentStart, .28f, (waitingPoint - segmentStart).normalized,
                    Vector2.Distance(segmentStart, waitingPoint))
                .Where(hit => hit.collider != null && !hit.collider.isTrigger
                    && !hit.collider.transform.IsChildOf(guide)
                    && !hit.collider.transform.IsChildOf(session.PlayerTransform))
                .Select(hit => hit.collider.name).ToArray();
            Assert.That(obstacles, Is.Empty, "The guide's new final path intersects solid scenery: " + string.Join(", ", obstacles));
            float routeLength = Vector2.Distance(guide.position, segmentStart) + Vector2.Distance(segmentStart, waitingPoint);
            float deadline = Time.realtimeSinceStartup + routeLength / 2.05f + 2f;
            while (Time.realtimeSinceStartup < deadline)
            {
                Vector2 position = guide.position;
                yield return new WaitForSecondsRealtime(.2f);
                if (Vector2.Distance(guide.position, position) < .03f &&
                    Vector2.Distance(guide.position, waitingPoint) < .15f) break;
            }
            Assert.That(Vector2.Distance(guide.position, waitingPoint), Is.LessThan(.15f), "The guide never actually led the player to its authored exit waiting pose.");
            Vector2 arrival = guide.position;
            var director = root.GetComponent<OpeningDirector>();
            // Keep the real reached waiting pose while checking each independent approach to the same doorway.
            director.enabled = false;
            var failures = new List<string>();
            try
            {
                Vector2[] offsets = { Vector2.left, Vector2.up, Vector2.down };
                string[] names = { "left", "above", "below" };
                for (int i = 0; i < offsets.Length; i++)
                {
                    if (session.Progress.roomId != "camp-0") Assert.That(session.ChangeRoom("camp-0", false), Is.True);
                    session.World.MoveActorFacing("shiguang", arrival, Vector2.right);
                    MovePlayer(exit + offsets[i]);
                    yield return new WaitForSecondsRealtime(.23f);
                    if (session.Progress.roomId == "coast-0") continue; // Crossing the design-package trigger is also a successful doorway approach.
                    var sensor = session.PlayerTransform.GetComponent<PlayerInteractionSensor>();
                    var selected = sensor.Current as WorldInteraction;
                    string candidate = selected == null ? "none" : selected.Kind + ":" + selected.Id;
                    bool activated = sensor.TryInteractCurrent();
                    if (!activated || session.Progress.roomId != "coast-0")
                        failures.Add(names[i] + ": candidate=" + candidate + ", activated=" + activated + ", room=" + session.Progress.roomId);
                    yield return OpeningTestDriver.FinishDialogue(session);
                }
            }
            finally { director.enabled = true; }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator RestoringPreCollectedIngredientsReplaysTheRequestThenAdvancesToRealCrafting()
        {
            yield return ReachGuide();
            foreach (string item in new[] { "berry", "dew" })
                session.World.Interactions.First(x => x.Kind == "resource" && x.Id.StartsWith(item + "-camp-0-")).Interact();
            Npc("shiguang").Interact();
            yield return OpeningTestDriver.FinishDialogue(session);
            session.World.Interactions.Single(x => x.Kind == "starter" && x.Id == "moss-lumi").Interact();
            yield return OpeningTestDriver.FinishDialogue(session);
            session.ChooseStarter("moss-lumi");
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.GatherIngredients));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue), "The uncompleted gathering request was silently skipped on reload.");
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.GatherIngredients));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.CraftTreat),
                "Already-collected ingredients left the objective asking the player to collect empty nodes.");
            Assert.That(session.World.Interactions.First(x => x.Kind == "resource" && x.Id.StartsWith("berry-camp-0-")).CanInteract, Is.False);
            Assert.That(session.World.Interactions.First(x => x.Kind == "resource" && x.Id.StartsWith("dew-camp-0-")).CanInteract, Is.False);
            session.World.Interactions.Single(x => x.Kind == "campfire" && x.Id == "M00_CAMPFIRE").Interact();
            session.Craft(session.Recipes.Single(x => x.recipeId == "berry-treat"));
            yield return OpeningTestDriver.FinishDialogue(session);
            Assert.That(session.Inventory.GetCount("berry-treat"), Is.EqualTo(1));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.FollowGuide));
        }

        [UnityTest]
        public IEnumerator FirstJourneyStillArrivesAtTheAuthoredDock()
        {
            session.NewJourney();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            Assert.That(Vector2.Distance(session.PlayerTransform.position, session.World.GetLandmarkPosition("dock-arrival")),
                Is.LessThan(.05f), "Initial story arrival must keep the dock spawn.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealExitRoundTripArrivesAtOppositeDoorAndBringsGuideBackToCamp()
        {
            yield return ReachFollowGuide();
            var outward = session.World.Interactions.Single(target => target.Kind == "exit" && target.Id == "coast-0"
                && session.World.GetRoomId(target.transform.position) == "camp-0");
            outward.Interact();
            Assert.That(session.Progress.roomId, Is.EqualTo("coast-0"));
            var reverse = session.World.Interactions.Single(target => target.Kind == "exit" && target.Id == "camp-0"
                && session.World.GetRoomId(target.transform.position) == "coast-0");
            float arrivingDistance = Vector2.Distance(session.PlayerTransform.position, reverse.transform.position);
            Assert.That((Vector2)session.PlayerTransform.position,
                Is.EqualTo(MistIsle.World.DesignPackCatalog.GetExit("camp-0", "coast-0").Spawn), "Arrival must use the design package's exact paired spawn.");
            Physics2D.SyncTransforms();
            Assert.That(Physics2D.OverlapCircleAll((Vector2)session.PlayerTransform.position + new Vector2(0f, .17f), .3f)
                .Any(shape => !shape.isTrigger && !shape.transform.IsChildOf(session.PlayerTransform)), Is.False,
                "The inward arrival position overlaps solid scenery or a character.");

            reverse.Interact();
            Assert.That(session.Progress.roomId, Is.EqualTo("camp-0"));
            float returnDistance = Vector2.Distance(session.PlayerTransform.position, outward.transform.position);
            Assert.That((Vector2)session.PlayerTransform.position,
                Is.EqualTo(MistIsle.World.DesignPackCatalog.GetExit("coast-0", "camp-0").Spawn));
            Assert.That(Vector2.Distance(session.PlayerTransform.position, session.World.GetLandmarkPosition("dock-arrival")),
                Is.GreaterThan(10f), "Returning through the coast exit incorrectly restarted the dock arrival.");
            var guide = session.World.GetActor("shiguang");
            Assert.That(session.World.GetRoomId(guide.position), Is.EqualTo("camp-0"), "The single guide was left behind in the distant coast room.");
            Assert.That(Vector2.Distance(guide.position, session.World.GetLandmarkPosition("guide-exit-wait")), Is.LessThan(.1f));
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Vector2.Distance(guide.position, session.PlayerTransform.position), Is.LessThan(4.2f));
            outward.Interact();
            Assert.That(session.Progress.roomId, Is.EqualTo("coast-0"), "The guide's return-to-camp state prevented leaving again.");
            Assert.That(session.World.GetRoomId(guide.position), Is.EqualTo("coast-0"));
        }

        [UnityTest]
        public IEnumerator CapturedCrabActorStaysHiddenAfterReloadAndReturnsOnlyForNewJourney()
        {
            yield return OpeningTestDriver.ChooseCompanion(session);
            yield return OpeningTestDriver.FinishCaptureLesson(session);
            Assert.That(session.World.GetActor("tutorial-coast-crab").gameObject.activeSelf, Is.False);
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.Progress.captureTutorialCompleted, Is.True);
            Assert.That(session.World.GetActor("tutorial-coast-crab").gameObject.activeSelf, Is.False,
                "Reload recreated a visible duplicate of the rescued crab.");
            Assert.That(session.World.Interactions.Single(target => target.Id == "tutorial-coast-crab").CanInteract, Is.False);
            session.NewJourney();
            Assert.That(session.World.GetActor("tutorial-coast-crab").gameObject.activeSelf, Is.True,
                "Starting a new journey did not restore the new story's wild crab actor.");
            Assert.That(session.World.Interactions.Single(target => target.Id == "tutorial-coast-crab").CanInteract, Is.True);
            Assert.That(session.Progress.captureTutorialCompleted, Is.False);
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
