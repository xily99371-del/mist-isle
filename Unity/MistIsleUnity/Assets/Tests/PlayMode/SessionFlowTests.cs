using System;
using System.Collections;
using System.IO;
using System.Linq;
using MistIsle.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class SessionFlowTests
    {
        private GameObject root;
        private GameSession session;
        private string temporaryDirectory;
        private Camera[] previousCameras;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "MistIsleSessionFlow-" + Guid.NewGuid().ToString("N"));
            previousCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            root = new GameObject("IsolatedSessionFlowTest");
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = temporaryDirectory;
            yield return null;
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(temporaryDirectory, "chapter-01.json")),
                "Session tests must never access the user's persistent save directory.");
            Assert.That(session.Catalog, Is.Not.Empty);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Title));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (previousCameras != null && !previousCameras.Contains(camera)) Object.Destroy(camera.gameObject);
            yield return null;
            if (!string.IsNullOrEmpty(temporaryDirectory) && Directory.Exists(temporaryDirectory))
            {
                string full = Path.GetFullPath(temporaryDirectory);
                string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(full).StartsWith("MistIsleSessionFlow-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Refusing to remove a directory outside this test's temporary save folder.");
                Directory.Delete(full, true);
            }
        }

        [UnityTest]
        public IEnumerator NewJourneyGatherCraftOpenRouteAndContinueRestoresTheActualSession()
        {
            Directory.CreateDirectory(temporaryDirectory);
            string legacyPath = Path.Combine(temporaryDirectory, "mist_isle_save.dat");
            byte[] legacyBytes = { 0x4d, 0x49, 0x53, 0x54, 0x03, 0x00, 0x01, 0xff };
            File.WriteAllBytes(legacyPath, legacyBytes);

            session.NewJourney();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Starter));
            session.ChooseStarter("moss-lumi");
            Assert.That(session.Party.Count, Is.EqualTo(1));
            Assert.That(session.Progress.selectedStarterSpeciesId, Is.EqualTo("moss-lumi"));
            session.ClosePanel();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));

            var resource = session.World.Interactions.First(item => item.Kind == "resource" && item.Id.StartsWith("berry-camp-0-"));
            int berries = session.Inventory.GetCount("berry");
            resource.Interact();
            Assert.That(session.Inventory.GetCount("berry"), Is.EqualTo(berries + 2));
            Assert.That(resource.CanInteract, Is.False);
            Assert.That(session.Progress.collectedNodeIds, Does.Contain(resource.Id));
            resource.Interact();
            Assert.That(session.Inventory.GetCount("berry"), Is.EqualTo(berries + 2), "A collected pickup was duplicated.");

            var fire = session.World.Interactions.First(item => item.Kind == "campfire" && item.Id == "campfire-camp-0");
            fire.Interact();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Campfire));
            var recipe = session.Recipes.Single(item => item.recipeId == "berry-treat");
            int treats = session.Inventory.GetCount(recipe.outputItemId);
            session.Craft(recipe);
            Assert.That(session.Inventory.GetCount(recipe.outputItemId), Is.EqualTo(treats + recipe.outputAmount));
            session.ClosePanel();

            Assert.That(session.ChangeRoom("glowglade-1", false), Is.True);
            var field = session.World.Interactions.Single(item => item.Kind == "field" && item.Id == "field-glowglade");
            field.Interact();
            Assert.That(session.Progress.completedFieldIds, Does.Contain("field-glowglade"));
            Assert.That(session.World.IsFieldOpen("field-glowglade"), Is.True,
                "Ability dialogue completed without physically opening the route.");
            session.ClosePanel();
            session.Save(false);
            Assert.That(File.Exists(session.Saves.Path), Is.True);
            int savedTreats = session.Inventory.GetCount("berry-treat");
            string starterInstanceId = session.Party.Members[0].instanceId;

            session.Inventory.Clear();
            session.Progress.completedFieldIds.Clear();
            session.Progress.collectedNodeIds.Clear();
            session.World.ResetFields(_ => false);
            session.World.ResetCollected(_ => false);
            session.ContinueJourney();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(session.Progress.roomId, Is.EqualTo("glowglade-1"));
            Assert.That(session.Party.Members[0].instanceId, Is.EqualTo(starterInstanceId));
            Assert.That(session.Inventory.GetCount("berry-treat"), Is.EqualTo(savedTreats));
            Assert.That(session.World.IsFieldOpen("field-glowglade"), Is.True, "Reloading did not restore the opened branch.");
            Assert.That(resource.CanInteract, Is.False, "Reloading respawned a saved collected resource.");
            CollectionAssert.AreEqual(legacyBytes, File.ReadAllBytes(legacyPath), "Unity save flow modified the legacy C save file.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarterSelectionCannotBeAppliedTwiceByRepeatedCallbacks()
        {
            session.NewJourney();
            session.ChooseStarter("moss-lumi");
            session.ChooseStarter("moatail");
            Assert.That(session.Party.Count, Is.EqualTo(1), "Repeated starter input added a second initial partner.");
            Assert.That(session.Progress.selectedStarterSpeciesId, Is.EqualTo("moss-lumi"));
            yield return null;
        }
    }
}
