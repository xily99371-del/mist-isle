using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using MistIsle.Creatures;
using MistIsle.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class BattlePartyInterfaceTests
    {
        private GameObject root;
        private GameSession session;
        private ChapterInterface ui;
        private Camera[] previousCameras;
        private string temporaryDirectory;
        private BattleSession battle;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "MistIsleBattleParty-" + Guid.NewGuid().ToString("N"));
            previousCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            root = new GameObject("IsolatedBattlePartyInterface");
            session = root.AddComponent<GameSession>();
            session.SaveDirectoryOverride = temporaryDirectory;
            yield return null;
            Assert.That(session.Saves.Path, Is.EqualTo(Path.Combine(temporaryDirectory, "chapter-01.json")));
            ui = root.GetComponent<ChapterInterface>();
            for (int index = 0; index < 6; index++)
                Assert.That(session.Party.TryAdd(new CreatureInstance(session.Catalog["moss-lumi"], 4, index == 0)
                    { nickname = "同行伙伴 " + (index + 1) }), Is.True);
            InstallBattle();
        }

        private void InstallBattle()
        {
            battle = new BattleSession(session.Party, session.Catalog, session.Inventory,
                new CreatureInstance(session.Catalog["wild-01"], 10, false), () => 0f);
            typeof(GameSession).GetProperty(nameof(GameSession.Battle)).SetValue(session, battle);
            session.SetPanel(GamePanel.Battle);
        }

        // Call the same presentation commands as the buttons, without claiming physical mouse-input coverage.
        private object Command(string name, params object[] args) => typeof(ChapterInterface)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, args);

        private T Selection<T>(string name) => (T)typeof(ChapterInterface)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!previousCameras.Contains(camera)) Object.Destroy(camera.gameObject);
            yield return null;
            if (!Directory.Exists(temporaryDirectory)) yield break;
            string allowed = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(temporaryDirectory);
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(full).StartsWith("MistIsleBattleParty-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to remove a directory outside this test's temporary save folder.");
            Directory.Delete(full, true);
        }

        [UnityTest]
        public IEnumerator EveryReserveCanBeChosenAndCancelLeavesTheActionAndDeploymentUntouched()
        {
            var deployed = session.Party.Deployed.ToArray();
            var actor = battle.CurrentActor;
            int[] hp = session.Party.Members.Select(companion => companion.currentHp).ToArray();
            for (int index = 3; index < 6; index++)
            {
                Command("OpenBattleParty");
                Command("SelectBattleIncoming", session.Party.Members[index].instanceId);
                Assert.That(Selection<string>("battleIncomingId"), Is.EqualTo(session.Party.Members[index].instanceId));
                Assert.That(Command("CanConfirmBattleParty"), Is.True);
                Command("CancelBattleParty");
                Assert.That(Selection<bool>("battlePartyOpen"), Is.False);
                Assert.That(battle.CurrentActor, Is.SameAs(actor));
                CollectionAssert.AreEqual(deployed, session.Party.Deployed);
                CollectionAssert.AreEqual(hp, session.Party.Members.Select(companion => companion.currentHp).ToArray());
            }
            Command("OpenBattleParty");
            Command("SelectBattleIncoming", session.Party.Members[5].instanceId);
            Command("ConfirmBattleParty");
            Assert.That(session.Party.Deployed[0], Is.SameAs(session.Party.Members[5]));
            Assert.That(battle.CurrentActor, Is.SameAs(session.Party.Members[1]));
            Assert.That(Selection<bool>("battlePartyOpen"), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectorOffersAFaintedSlotAndRejectsAnotherLivingActorsSlot()
        {
            session.Party.Members[1].currentHp = 0;
            InstallBattle();
            Command("OpenBattleParty");
            string actorId = battle.CurrentActor.instanceId;
            Command("SelectBattleOutgoing", session.Party.Members[2].instanceId);
            Assert.That(Selection<string>("battleOutgoingId"), Is.EqualTo(actorId));
            Command("SelectBattleOutgoing", session.Party.Members[1].instanceId);
            Assert.That(Selection<string>("battleOutgoingId"), Is.EqualTo(session.Party.Members[1].instanceId));
            Command("SelectBattleIncoming", session.Party.Members[4].instanceId);
            Command("ConfirmBattleParty");
            Assert.That(session.Party.Deployed[1], Is.SameAs(session.Party.Members[4]));
            Assert.That(session.Party.Deployed.Count, Is.EqualTo(3));
            Assert.That(battle.CurrentActor, Is.SameAs(session.Party.Members[2]));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OneDeployedCompanionStillAllowsChoosingTheSixthOfFiveReserves()
        {
            Assert.That(session.Party.TrySetDeployment(new[] { session.Party.Members[0].instanceId }, out _), Is.True);
            InstallBattle();
            Command("OpenBattleParty");
            Command("SelectBattleIncoming", session.Party.Members[5].instanceId);
            Assert.That(Command("CanConfirmBattleParty"), Is.True);
            Command("ConfirmBattleParty");
            CollectionAssert.AreEqual(new[] { session.Party.Members[5] }, session.Party.Deployed);
            Assert.That(battle.CurrentActor, Is.SameAs(session.Party.Members[5]));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StaleSelectionClosesAfterTheActorChangesOrTheBattleEnds()
        {
            Command("OpenBattleParty");
            Command("SelectBattleIncoming", session.Party.Members[5].instanceId);
            battle.TryAct(BattleAction.Defend);
            Command("RefreshBattlePartySelection", battle);
            Assert.That(Selection<bool>("battlePartyOpen"), Is.False);
            Command("OpenBattleParty");
            Assert.That(Selection<bool>("battlePartyOpen"), Is.True);
            battle.TryAct(BattleAction.Flee);
            Command("RefreshBattlePartySelection", battle);
            Assert.That(Selection<bool>("battlePartyOpen"), Is.False);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Escaped));
            yield return null;
        }
    }
}
