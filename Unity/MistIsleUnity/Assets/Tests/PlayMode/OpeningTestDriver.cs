using System.Collections;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    internal static class OpeningTestDriver
    {
        internal static IEnumerator FinishDialogue(GameSession session)
        {
            int remaining = 40;
            while (session.Panel == GamePanel.Dialogue && remaining-- > 0)
            {
                yield return new WaitForSecondsRealtime(.18f);
                session.AdvanceDialogue();
            }
            Assert.That(remaining, Is.GreaterThan(0), "Dialogue chain never returned control to the player.");
        }

        internal static IEnumerator ChooseCompanion(GameSession session, string starter = "moss-lumi")
        {
            session.NewJourney();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue), "A new journey must arrive on the island before opening partner selection.");
            Assert.That(session.Party.Count, Is.Zero);
            session.ChooseStarter(starter);
            Assert.That(session.Party.Count, Is.Zero, "Partner choice bypassed the first meetings.");
            yield return FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetWatcher));
            session.World.Interactions.Single(x => x.Kind == "npc" && x.Id == "watcher").Interact();
            yield return FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.MeetShiguang));
            session.World.Interactions.Single(x => x.Kind == "npc" && x.Id == "shiguang").Interact();
            yield return FinishDialogue(session);
            session.World.Interactions.Single(x => x.Kind == "starter" && x.Id == starter).Interact();
            yield return FinishDialogue(session);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Starter));
            session.ChooseStarter(starter);
            yield return FinishDialogue(session);
            Assert.That(session.Party.Count, Is.EqualTo(1));
            Assert.That(session.World.GetActor(starter).gameObject.activeSelf, Is.False, "Chosen companion remained duplicated in the garden.");
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.GatherIngredients));
        }

        internal static IEnumerator FinishCaptureLesson(GameSession session)
        {
            foreach (string item in new[] { "berry", "dew" })
            {
                session.World.Interactions.First(x => x.Kind == "resource" && x.Id.StartsWith(item + "-camp-0-")).Interact();
                yield return FinishDialogue(session);
            }
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.CraftTreat));
            session.World.Interactions.Single(x => x.Kind == "campfire" && x.Id == "campfire-camp-0").Interact();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Campfire));
            session.Craft(session.Recipes.Single(x => x.recipeId == "berry-treat"));
            yield return FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.FollowGuide));
            Assert.That(session.ChangeRoom("glowglade-0", false), Is.False, "The opening was bypassed before helping the stranded creature.");
            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            session.World.Interactions.Single(x => x.Kind == "encounter" && x.Id == "tutorial-coast-crab").Interact();
            yield return FinishDialogue(session);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            Assert.That(session.Battle.Wild.speciesId, Is.EqualTo("wild-03"));
            Assert.That(session.Battle.IsCaptureLesson, Is.True);
            int treats = session.Inventory.GetCount("berry-treat");
            session.Act(BattleAction.Feed, "berry-treat");
            Assert.That(session.Inventory.GetCount("berry-treat"), Is.EqualTo(treats), "Early use consumed the only lesson food.");
            session.Act(BattleAction.Attack);
            Assert.That(session.Battle.CaptureLessonWeakened, Is.True);
            session.Act(BattleAction.Feed, "berry-treat");
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.Captured));
            session.LeaveBattle();
            Assert.That(session.World.Interactions.Single(x => x.Id == "tutorial-coast-crab").CanInteract, Is.False, "The rescued crab was still offered as a wild encounter.");
            yield return FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.ReportWatcher));
            Assert.That(session.ChangeRoom("camp-0", false), Is.True);
            session.World.Interactions.Single(x => x.Kind == "npc" && x.Id == "watcher").Interact();
            yield return FinishDialogue(session);
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.Complete));
            Assert.That(session.Progress.captureTutorialCompleted, Is.True);
        }
    }
}
