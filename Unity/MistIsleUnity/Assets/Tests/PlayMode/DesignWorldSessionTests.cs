using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Items;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class DesignWorldSessionTests
    {
        private GameObject root;
        private GameSession session;
        private string temporaryDirectory;
        private string temporaryLeaf;
        private readonly Dictionary<Camera, bool> previousCameras = new Dictionary<Camera, bool>();
        private readonly Dictionary<AudioListener, bool> previousListeners = new Dictionary<AudioListener, bool>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryLeaf = "MistIsle-DesignWorldSession-" + Guid.NewGuid().ToString("N");
            temporaryDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), temporaryLeaf));
            previousCameras.Clear(); previousListeners.Clear();
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            { previousCameras.Add(camera, camera.enabled); camera.enabled = false; }
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            { previousListeners.Add(listener, listener.enabled); listener.enabled = false; }
            yield return CreateSession();
        }

        private IEnumerator CreateSession()
        {
            root = new GameObject("IsolatedDesignWorldSession");
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = temporaryDirectory;
            yield return null;
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(temporaryDirectory, "chapter-01.json")),
                "The fixture must never use the player's persistent save directory.");
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Title));
            Assert.That(root.transform.Find("DesignPackWorld"), Is.Not.Null, "GameSession did not build the supplied design maps.");
        }

        private void DestroySession()
        {
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!previousCameras.ContainsKey(camera)) Object.Destroy(camera.gameObject);
        }

        private IEnumerator RestartFromDisk()
        {
            DestroySession();
            yield return null;
            yield return CreateSession();
            session.ContinueJourney();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroySession();
            yield return null;
            foreach (var pair in previousCameras) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in previousListeners) if (pair.Key != null) pair.Key.enabled = pair.Value;
            if (!Directory.Exists(temporaryDirectory)) yield break;
            string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(temporaryDirectory);
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(full), temporaryLeaf, StringComparison.Ordinal)
                || !string.Equals(full, Path.Combine(allowed, temporaryLeaf), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to remove an unverified design-world session directory.");
            Directory.Delete(full, true);
        }

        // Start from a real saved post-tutorial checkpoint; these tests exercise the new world loop.
        private void ResumeExploration(string starterId = "moss-lumi", string roomId = "camp-0", bool finalReady = false)
        {
            var data = new WorldSaveData {
                selectedStarterSpeciesId = starterId, openingStage = (int)OpeningStage.Complete,
                captureTutorialCompleted = true, roomId = roomId, hasPosition = false
            };
            var starter = new CreatureInstance(session.Catalog[starterId], 3, true);
            data.roster.Add(starter); data.deployedIds.Add(starter.instanceId);
            data.inventoryItemIds.Add(ShopCatalog.CurrencyItemId); data.inventoryCounts.Add(24);
            if (finalReady)
            {
                foreach (string id in GameCatalog.GuardianIds) data.seals.RecordGuardianVictory(id);
                data.completedFieldIds.Add("M04_THREE_SEAL_GATE");
            }
            Assert.That(session.Saves.Save(data, out var error), Is.True, error);
            session.ContinueJourney();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(session.OpeningPhase, Is.EqualTo(OpeningStage.Complete));
        }

        private WorldInteraction Target(string id) => session.World.Interactions.Single(value => value.Id == id);

        private void MovePlayer(Vector2 point)
        {
            var body = session.PlayerTransform.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero; body.position = point;
            session.PlayerTransform.position = point;
            Physics2D.SyncTransforms();
        }

        private IEnumerator Interact(WorldInteraction target)
        {
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(target.CanInteract, Is.True, target.Id);
            MovePlayer((Vector2)target.transform.position + Vector2.down * .8f);
            // Honour the real post-dialogue input suppression, then query the real sensor.
            yield return new WaitForSecondsRealtime(.24f);
            var sensor = session.PlayerTransform.GetComponent<PlayerInteractionSensor>();
            Assert.That(sensor.Current, Is.SameAs(target), "The real sensor could not reach " + target.Id);
            Assert.That(sensor.TryInteractCurrent(), Is.True, target.Id);
        }

        private IEnumerator ReadDialogue()
        {
            int pages = 32;
            while (session.Panel == GamePanel.Dialogue && pages-- > 0)
            {
                yield return new WaitForSecondsRealtime(.18f);
                session.AdvanceDialogue();
            }
            Assert.That(pages, Is.GreaterThan(0), "The dialogue did not return control or start its encounter.");
        }

        [UnityTest]
        public IEnumerator NewJourneyBeginsWithModestSupplyMoneyAndASafeDesignPackHub()
        {
            session.NewJourney();
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(24));
            Assert.That(session.Party.Count, Is.Zero);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            Assert.That(session.Progress.mapContentVersion, Is.EqualTo(1));
            Assert.That(session.World.Interactions.Any(value => value.Kind == "encounter"
                && session.World.GetRoomId(value.transform.position) == "camp-0"), Is.False);
            Assert.That(session.Saves.TryLoad(session.Catalog, out var saved, out var reason), Is.True, reason);
            Assert.That(saved.inventoryItemIds.IndexOf(ShopCatalog.CurrencyItemId), Is.GreaterThanOrEqualTo(0));
            Assert.That(saved.inventoryCounts[saved.inventoryItemIds.IndexOf(ShopCatalog.CurrencyItemId)], Is.EqualTo(24));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealShopNodePurchasesSurviveDestructionAndContinuationWithTheSameBalanceAndPosition()
        {
            ResumeExploration();
            yield return Interact(Target("M00_SUPPLY_SHOP"));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Shop));
            session.BuySupply("berry-treat");
            session.BuySupply("resonance-capsule");
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(4));
            Assert.That(session.Inventory.GetCount("berry-treat"), Is.EqualTo(1));
            Assert.That(session.Inventory.GetCount("resonance-capsule"), Is.EqualTo(1));
            Vector2 position = session.PlayerTransform.position;
            session.Save(false);

            yield return RestartFromDisk();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(4));
            Assert.That(session.Inventory.GetCount("berry-treat"), Is.EqualTo(1));
            Assert.That(session.Inventory.GetCount("resonance-capsule"), Is.EqualTo(1));
            Assert.That(Vector2.Distance(session.PlayerTransform.position, position), Is.LessThan(.02f));
            Assert.That(session.Progress.hasPosition, Is.True);
        }

        [UnityTest]
        public IEnumerator FreeRecoveryDoesNotRefreshHarvestedResourcesButTheirSavedDeadlineDoes()
        {
            ResumeExploration();
            var resource = Target("M00_GATHER_SHELL");
            yield return Interact(resource);
            Assert.That(session.Inventory.GetCount("shell"), Is.EqualTo(1));
            Assert.That(resource.CanInteract, Is.False);
            long deadline = session.Progress.nodeRefreshStates.Single(value => value.id == resource.Id).availableAtUtc;
            Assert.That(deadline, Is.GreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            session.Party.Members[0].currentHp = 1;
            session.Recover(false);
            Assert.That(session.Party.Members[0].currentHp, Is.EqualTo(CreatureInstance.MaxHp(session.Catalog["moss-lumi"], 3)));
            Assert.That(resource.CanInteract, Is.False, "Free healing also refreshed a harvested node.");
            Assert.That(session.NodeAvailable(resource.Id), Is.False);
            Assert.That(session.Progress.nodeRefreshStates.Single(value => value.id == resource.Id).availableAtUtc, Is.EqualTo(deadline));

            yield return RestartFromDisk();
            resource = Target("M00_GATHER_SHELL");
            Assert.That(resource.CanInteract, Is.False);
            Assert.That(session.NodeAvailable(resource.Id), Is.False);
            Assert.That(session.Progress.nodeRefreshStates.Single(value => value.id == resource.Id).availableAtUtc, Is.EqualTo(deadline));
            resource.Interact();
            Assert.That(session.Inventory.GetCount("shell"), Is.EqualTo(1));
            // Move only the fixture's saved deadline across the real wall-clock boundary.
            session.Progress.nodeRefreshStates.Single(value => value.id == resource.Id).availableAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(resource.CanInteract, Is.True);
            yield return Interact(resource);
            Assert.That(session.Inventory.GetCount("shell"), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TidewindExitActuallyEntersTheRuinsWithoutRequiringTheForestSeal()
        {
            ResumeExploration();
            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            Assert.That(session.Progress.seals.HasSeal("glowglade-warden"), Is.False);
            var exit = session.World.Interactions.Single(value => value.Kind == "exit" && value.Id == "tidal-0"
                && session.World.GetRoomId(value.transform.position) == "coast-0");
            yield return new WaitForSecondsRealtime(.75f);
            MovePlayer(exit.transform.position);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(session.Progress.roomId, Is.EqualTo("tidal-0"), "The actual boundary trigger imposed the former linear-region gate.");
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(session.Progress.visitedMapIds, Does.Contain("tidal-0"));
            Assert.That(Vector2.Distance(session.PlayerTransform.position, DesignPackCatalog.GetExit("coast-0", "tidal-0").Spawn), Is.LessThan(.05f));
            Assert.That(session.Progress.seals.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HarvestUsesTheConfiguredMultiPartItemIdAndPersistsItIntact()
        {
            ResumeExploration();
            Assert.That(DesignPackCatalog.TryGetInteraction("M00_GATHER_SHELL", out var node), Is.True);
            var itemProperty = typeof(DesignPackNode).GetProperty(nameof(DesignPackNode.ItemId));
            string original = node.ItemId;
            try
            {
                // Represent a future tool pickup without changing source JSON or inventing an item.
                itemProperty.SetValue(node, "resonance-capsule");
                yield return Interact(Target(node.Id));
                Assert.That(session.Inventory.GetCount("resonance-capsule"), Is.EqualTo(node.Amount));
                Assert.That(session.Inventory.GetCount("resonance"), Is.Zero);
                Assert.That(session.Inventory.GetCount(node.Id), Is.Zero);
                Assert.That(session.Inventory.GetCount("shell"), Is.Zero);
                yield return RestartFromDisk();
                Assert.That(session.Inventory.GetCount("resonance-capsule"), Is.EqualTo(node.Amount));
                Assert.That(session.Inventory.GetCount("resonance"), Is.Zero);
            }
            finally { itemProperty.SetValue(node, original); }
        }

        [UnityTest]
        public IEnumerator AnUnavailableAbilityCannotClaimSuccessButARealPartnerOpensAndRestoresTheRoute()
        {
            ResumeExploration("crystal-gecko");
            Assert.That(session.ChangeRoom("glowglade-0", false), Is.True);
            var field = Target("M02_ROOT_BRIDGE");
            yield return Interact(field);
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            Assert.That(session.World.IsFieldOpen(field.Id), Is.False);
            Assert.That(session.Progress.completedFieldIds, Does.Not.Contain(field.Id));
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(24));
            string successPage = DesignPackNarrative.Observe("M02_ROOT_BRIDGE")[0].text;
            Assert.That(session.DialogueText, Is.Not.EqualTo(successPage), "A blocked ability showed successful restoration prose.");
            yield return ReadDialogue();
            var helper = new CreatureInstance(session.Catalog["wild-01"], 2, false);
            Assert.That(session.Party.TryAdd(helper), Is.True);
            yield return Interact(field);
            Assert.That(session.World.IsFieldOpen(field.Id), Is.True);
            Assert.That(session.Progress.completedFieldIds, Does.Contain(field.Id));
            Assert.That(session.DialogueText, Is.EqualTo(successPage));
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(36));
            Assert.That(helper.bond, Is.EqualTo(3));
            Assert.That(field.CanInteract, Is.False);
            yield return ReadDialogue();
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.World.IsFieldOpen("M02_ROOT_BRIDGE"), Is.True);
            Assert.That(session.Progress.completedFieldIds, Does.Contain("M02_ROOT_BRIDGE"));
            Assert.That(Target("M02_ROOT_BRIDGE").CanInteract, Is.False);
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(36));
            Assert.That(session.Party.Members.Single(value => value.instanceId == helper.instanceId).bond, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator UnreadCluesCanBeReadAfterReloadAndAnOpenedChestNeverAwardsTwice()
        {
            ResumeExploration();
            yield return Interact(Target("M00_NOTICE"));
            string firstPage = session.DialogueText;
            Assert.That(session.Progress.storyFlags, Does.Not.Contain("M00_NOTICE"));
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.Progress.storyFlags, Does.Not.Contain("M00_NOTICE"));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue), "An unfinished saved clue must resume automatically.");
            Assert.That(session.DialogueText, Is.EqualTo(firstPage));
            yield return ReadDialogue();
            Assert.That(session.Progress.storyFlags, Does.Contain("M00_NOTICE"));

            Assert.That(session.ChangeRoom("coast-0", false), Is.True);
            yield return Interact(Target("M01_TREASURE"));
            Assert.That(session.Progress.openedChestIds, Does.Contain("M01_TREASURE"));
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(44));
            Assert.That(session.Inventory.GetCount("resonance-capsule"), Is.EqualTo(1));
            int berries = session.Inventory.GetCount("berry");
            yield return ReadDialogue();
            session.Recover(false);
            Assert.That(Target("M01_TREASURE").CanInteract, Is.False);
            yield return RestartFromDisk();
            Assert.That(session.Progress.storyFlags, Does.Contain("M00_NOTICE"));
            var chest = Target("M01_TREASURE");
            Assert.That(chest.CanInteract, Is.False);
            chest.Interact();
            Assert.That(session.Inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(44));
            Assert.That(session.Inventory.GetCount("resonance-capsule"), Is.EqualTo(1));
            Assert.That(session.Inventory.GetCount("berry"), Is.EqualTo(berries));
        }

        [UnityTest]
        public IEnumerator TheFinalEncounterWaitsForEveryIntroPageAndAnInterruptedIntroCanBeReplayed()
        {
            ResumeExploration("moss-lumi", "lighthouse-0", true);
            yield return Interact(Target("final-boss"));
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Dialogue));
            Assert.That(session.Battle, Is.Null);
            Assert.That(session.DialoguePageCount, Is.GreaterThan(1));
            string firstPage = session.DialogueText;
            yield return new WaitForSecondsRealtime(.18f);
            session.AdvanceDialogue();
            Assert.That(session.DialoguePageIndex, Is.EqualTo(1));
            Assert.That(session.Battle, Is.Null, "The encounter started before the core motivation was read.");
            session.Save(false);
            yield return RestartFromDisk();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Explore));
            Assert.That(session.Battle, Is.Null);
            yield return Interact(Target("final-boss"));
            Assert.That(session.DialogueText, Is.EqualTo(firstPage));
            yield return ReadDialogue();
            Assert.That(session.Panel, Is.EqualTo(GamePanel.Battle));
            Assert.That(session.Battle, Is.Not.Null);
            Assert.That(session.Battle.Kind, Is.EqualTo(BattleKind.FinalBoss));
            Assert.That(session.Battle.Wild.speciesId, Is.EqualTo("final-boss"));
            Assert.That(session.Progress.chapterCompleted, Is.False);
        }
    }
}
