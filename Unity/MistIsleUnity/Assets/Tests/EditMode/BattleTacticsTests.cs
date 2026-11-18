using System.Collections.Generic;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Items;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class BattleTacticsTests
    {
        private Dictionary<string, CreatureSpecies> catalog;

        [SetUp]
        public void SetUp() => catalog = GameCatalog.CreateSpecies();

        [TearDown]
        public void TearDown()
        {
            foreach (var species in catalog.Values) Object.DestroyImmediate(species);
        }

        private PartyRoster CreateParty(int count)
        {
            var party = new PartyRoster();
            for (int index = 0; index < count; index++)
                Assert.That(party.TryAdd(new CreatureInstance(catalog["moss-lumi"], 1, index == 0)), Is.True);
            return party;
        }

        private BattleSession CreateLongTrial(PartyRoster party)
        {
            // Survive repeated low-level skills so the test observes opponent turns instead of winning early.
            var species = catalog["glowglade-warden"];
            species.baseMaxHp = 5000;
            species.baseDefense = 100;
            species.baseAttack = 5;
            return new BattleSession(party, catalog, new Inventory(), new CreatureInstance(species, 1, false),
                () => 0f, BattleKind.Guardian);
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void AnyLivingReserveCanReplaceTheActorAndWaitsUntilNextRound(int reserveIndex)
        {
            var party = CreateParty(6);
            var outgoing = party.Members[0];
            var incoming = party.Members[reserveIndex];
            var battle = CreateLongTrial(party);
            Assert.That(battle.TrySwap(outgoing.instanceId, incoming.instanceId).Success, Is.True);
            Assert.That(party.Deployed.Contains(incoming), Is.True);
            Assert.That(party.Deployed.Contains(outgoing), Is.False);
            Assert.That(party.Deployed.Count, Is.EqualTo(3));
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[1]));
            Assert.That(battle.TryAct(BattleAction.Defend).Success, Is.True);
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[2]));
            Assert.That(battle.TryAct(BattleAction.Defend).Success, Is.True);
            Assert.That(battle.CurrentActor, Is.SameAs(incoming));
        }

        [Test]
        public void ReplacingAFaintedAllySpendsTheLivingActorsActionAndRetainsItsSlot()
        {
            var party = CreateParty(6);
            var actor = party.Members[0];
            var outgoing = party.Members[2];
            var incoming = party.Members[5];
            outgoing.currentHp = 0;
            var battle = CreateLongTrial(party);
            Assert.That(battle.TrySwap(outgoing.instanceId, incoming.instanceId).Success, Is.True);
            CollectionAssert.AreEqual(new[] { actor, party.Members[1], incoming }, party.Deployed);
            Assert.That(outgoing.currentHp, Is.Zero);
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[1]));
            battle.TryAct(BattleAction.Defend);
            Assert.That(battle.Log.Count(line => line.Contains("回应了攻击")), Is.EqualTo(1));
            Assert.That(battle.CurrentActor, Is.SameAs(actor));
            battle.TryAct(BattleAction.Defend);
            battle.TryAct(BattleAction.Defend);
            Assert.That(battle.CurrentActor, Is.SameAs(incoming));
        }

        [Test]
        public void RejectedSwapCannotDisplaceAnotherLivingActorOrConsumeAnAction()
        {
            var party = CreateParty(6);
            var battle = CreateLongTrial(party);
            var deployedBefore = party.Deployed.ToArray();
            Assert.That(battle.TrySwap(party.Members[1].instanceId, party.Members[5].instanceId).Success, Is.False);
            CollectionAssert.AreEqual(deployedBefore, party.Deployed);
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[0]));
            Assert.That(battle.Log.Count(line => line.Contains("回应了攻击")), Is.Zero);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void RotatingVerdantSkillsStillAllowsAnOpponentResponseBetweenRoots(int deployedCount)
        {
            var party = CreateParty(deployedCount);
            var battle = CreateLongTrial(party);
            for (int round = 0; round < 6; round++)
            {
                for (int actorIndex = 0; actorIndex < deployedCount; actorIndex++)
                {
                    var action = actorIndex == round % 2 ? BattleAction.Skill : BattleAction.Defend;
                    Assert.That(battle.TryAct(action).Success, Is.True, "Scheduled skill did not respect its own cooldown.");
                }
            }
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            Assert.That(battle.Log.Count(line => line.Contains("没能行动")), Is.EqualTo(3));
            Assert.That(battle.Log.Count(line => line.Contains("回应了攻击")), Is.EqualTo(3),
                "Multiple Verdant companions permanently suppressed the guardian.");
        }

        [Test]
        public void VerdantStillHealsAndDamagesDuringRecoveryAndCanRootAgainAfterIt()
        {
            var party = CreateParty(2);
            var battle = CreateLongTrial(party);
            battle.TryAct(BattleAction.Skill);
            battle.TryAct(BattleAction.Defend);
            Assert.That(battle.WildStatus, Does.Contain("挣脱"));
            battle.TryAct(BattleAction.Defend);
            var second = party.Members[1];
            second.currentHp -= 10;
            int hurtHp = second.currentHp, opponentHp = battle.Wild.currentHp;
            Assert.That(battle.TryAct(BattleAction.Skill).Success, Is.True);
            Assert.That(second.currentHp, Is.GreaterThan(hurtHp));
            Assert.That(battle.Wild.currentHp, Is.LessThan(opponentHp));
            Assert.That(battle.Log.Count(line => line.Contains("回应了攻击")), Is.EqualTo(1));
            Assert.That(battle.TryAct(BattleAction.Skill).Success, Is.True);
            Assert.That(battle.WildStatus, Is.EqualTo("缠绕"));
            battle.TryAct(BattleAction.Defend);
            Assert.That(battle.Log.Count(line => line.Contains("没能行动")), Is.EqualTo(2));
        }
    }
}
