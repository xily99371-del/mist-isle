using System;
using System.Collections.Generic;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Items;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class DomainTests
    {
        private Dictionary<string, CreatureSpecies> catalog;
        private List<CampfireRecipe> recipes;
        private readonly List<UnityEngine.Object> extraAssets = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            catalog = GameCatalog.CreateSpecies();
            recipes = GameCatalog.CreateRecipes();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in catalog.Values) UnityEngine.Object.DestroyImmediate(value);
            foreach (var recipe in recipes) UnityEngine.Object.DestroyImmediate(recipe);
            foreach (var asset in extraAssets) UnityEngine.Object.DestroyImmediate(asset);
            extraAssets.Clear();
        }

        private CreatureInstance Make(string id = "moss-lumi", int level = 1, bool starter = true)
            => new CreatureInstance(catalog[id], level, starter);

        private PartyRoster MakeParty(int count = 1)
        {
            var party = new PartyRoster();
            for (int i = 0; i < count; i++) Assert.That(party.TryAdd(Make(starter: i == 0)), Is.True);
            return party;
        }

        [Test]
        public void CatalogProvidesEighteenReadyFormsAndDistinguishesSixDesignCandidates()
        {
            Assert.That(catalog.Count, Is.EqualTo(23));
            Assert.That(catalog.Values.Count(x => x.capturable), Is.EqualTo(18));
            Assert.That(catalog.Values.Count(x => x.isStarterSpecies), Is.EqualTo(3));
            Assert.That(catalog.Values.Count(x => x.capturable && x.contentReady), Is.EqualTo(18));
            Assert.That(catalog.Values.Count(x => x.capturable && x.designApprovalPending), Is.EqualTo(6));
            Assert.That(catalog["wild-07"].contentReady, Is.True);
            Assert.That(catalog["wild-07"].designApprovalPending, Is.True);
            foreach (string id in GameCatalog.GuardianIds) Assert.That(catalog[id].capturable, Is.False);
            Assert.That(catalog["final-boss"].capturable, Is.False);
            Assert.That(catalog.Values.All(x => !string.IsNullOrEmpty(x.spriteResourcePath)), Is.True);
        }

        [Test]
        public void PartyAcceptsSixUniqueMembersAndDeploysOnlyThree()
        {
            var party = MakeParty(6);
            Assert.That(party.Count, Is.EqualTo(6));
            Assert.That(party.Deployed.Count, Is.EqualTo(3));
            Assert.That(party.TryAdd(Make()), Is.False);
            Assert.That(party.TryAdd(party.Members[0]), Is.False);
            Assert.That(party.TryAdd(null), Is.False);
            Assert.That(party.Remove(party.Members[0].instanceId), Is.True);
            Assert.That(party.Deployed.Count, Is.EqualTo(2));
        }

        [Test]
        public void InvalidDeploymentDoesNotPartiallyReplaceExistingSelection()
        {
            var party = MakeParty(4);
            string[] original = party.DeployedIds.ToArray();
            Assert.That(party.TrySetDeployment(new[] { original[0], "missing" }, out _), Is.False);
            CollectionAssert.AreEqual(original, party.DeployedIds);
            Assert.That(party.TrySetDeployment(new[] { original[0], original[0] }, out _), Is.False);
            Assert.That(party.TrySetDeployment(new[] { party.Members[3].instanceId }, out _), Is.True);
            Assert.That(party.Deployed[0], Is.SameAs(party.Members[3]));
        }

        [Test]
        public void CraftingFailureConsumesNothingAndSuccessUsesExactQuantities()
        {
            var inventory = new Inventory();
            inventory.Add("berry", 3);
            Assert.That(CraftingService.TryCraft(inventory, recipes[0], out _), Is.False);
            Assert.That(inventory.GetCount("berry"), Is.EqualTo(3));
            inventory.Add("dew");
            Assert.That(CraftingService.TryCraft(inventory, recipes[0], out _), Is.True);
            Assert.That(inventory.GetCount("berry"), Is.EqualTo(1));
            Assert.That(inventory.GetCount("dew"), Is.Zero);
            Assert.That(inventory.GetCount("berry-treat"), Is.EqualTo(1));
        }

        [Test]
        public void CraftingAggregatesDuplicateIngredientRowsBeforeCheckingInventory()
        {
            var recipe = ScriptableObject.CreateInstance<CampfireRecipe>();
            extraAssets.Add(recipe);
            recipe.outputItemId = "berry-treat";
            recipe.ingredients.Add(new RecipeIngredient { itemId = "berry", amount = 2 });
            recipe.ingredients.Add(new RecipeIngredient { itemId = "berry", amount = 2 });
            var inventory = new Inventory();
            inventory.Add("berry", 3);
            Assert.That(CraftingService.TryCraft(inventory, recipe, out _), Is.False);
            Assert.That(inventory.GetCount("berry"), Is.EqualTo(3));
        }

        [TestCase(ElementType.Verdant, ElementType.Crystal, 1.5f)]
        [TestCase(ElementType.Crystal, ElementType.Verdant, 0.75f)]
        [TestCase(ElementType.Gale, ElementType.Stone, 1f)]
        [TestCase(ElementType.Stone, ElementType.Gale, 1f)]
        public void ElementTableUsesApprovedRelationsOnly(ElementType attack, ElementType defense, float expected)
        {
            Assert.That(ElementChart.Multiplier(attack, defense), Is.EqualTo(expected));
        }

        [Test]
        public void WeakenedAndPreferredFoodContextsImproveCaptureChanceWithoutGuaranteeingIt()
        {
            float healthy = CaptureCalculator.Calculate(new CaptureContext(0, 0.2f, 0, 0.5f, 0.2f), false);
            float weakened = CaptureCalculator.Calculate(new CaptureContext(0.8f, 0.2f, 0.5f, 0.5f, 0.2f), false);
            Assert.That(weakened, Is.GreaterThan(healthy).And.LessThan(1f));
            Assert.That(CaptureCalculator.Calculate(new CaptureContext(0, 1, 0, 0, 0), true),
                Is.GreaterThan(CaptureCalculator.Calculate(new CaptureContext(0, 0, 0, 0, 0), true)));
        }

        [Test]
        public void GuardianCannotBeCapturedAndRejectedActionConsumesNoItemOrTurn()
        {
            var party = MakeParty();
            var inventory = new Inventory();
            inventory.Add("resonance-capsule", 2);
            var battle = new BattleSession(party, catalog, inventory, Make("glowglade-warden", 2, false),
                () => 0f, BattleKind.Guardian);
            var actor = battle.CurrentActor;
            Assert.That(battle.TryAct(BattleAction.Capture).Success, Is.False);
            Assert.That(inventory.GetCount("resonance-capsule"), Is.EqualTo(2));
            Assert.That(battle.CurrentActor, Is.SameAs(actor));
            Assert.That(battle.TryAct(BattleAction.Flee).Success, Is.False);
        }

        [Test]
        public void FullPartyPreventsCaptureBeforeConsumingTools()
        {
            var party = MakeParty(6);
            var inventory = new Inventory();
            inventory.Add("resonance-capsule");
            var battle = new BattleSession(party, catalog, inventory, Make("wild-01", 1, false), () => 0f);
            Assert.That(battle.TryAct(BattleAction.Capture).Success, Is.False);
            Assert.That(inventory.GetCount("resonance-capsule"), Is.EqualTo(1));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
        }

        [Test]
        public void SuccessfulFeedingAddsWildCreatureAndConsumesFoodExactlyOnce()
        {
            var party = MakeParty();
            var inventory = new Inventory();
            inventory.Add("berry-treat");
            var wild = Make("wild-01", 1, false);
            var battle = new BattleSession(party, catalog, inventory, wild, () => 0f);
            Assert.That(battle.TryAct(BattleAction.Feed).Outcome, Is.EqualTo(BattleOutcome.Captured));
            Assert.That(party.Members.Contains(wild), Is.True);
            Assert.That(inventory.GetCount("berry-treat"), Is.Zero);
            Assert.That(battle.TryAct(BattleAction.Feed).Success, Is.False);
        }

        [Test]
        public void AllThreeDeployedCompanionsActBeforeWildResponse()
        {
            var party = MakeParty(3);
            var wild = Make("glowglade-warden", 4, false);
            var battle = new BattleSession(party, catalog, new Inventory(), wild, () => 0f, BattleKind.Guardian);
            int initialHp = party.Members[0].currentHp;
            battle.TryAct(BattleAction.Defend);
            battle.TryAct(BattleAction.Defend);
            Assert.That(party.Members[0].currentHp, Is.EqualTo(initialHp));
            battle.TryAct(BattleAction.Defend);
            Assert.That(party.Members[0].currentHp, Is.LessThan(initialHp));
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[0]));
        }

        [Test]
        public void VerdantSkillHealsAndRootsWhileCooldownPreventsRepeatedLock()
        {
            var party = MakeParty();
            var actor = party.Members[0];
            actor.currentHp -= 10;
            int hurtHp = actor.currentHp;
            var battle = new BattleSession(party, catalog, new Inventory(), Make("glowglade-warden", 3, false),
                () => 0f, BattleKind.Guardian);
            Assert.That(battle.TryAct(BattleAction.Skill).Success, Is.True);
            Assert.That(actor.currentHp, Is.GreaterThan(hurtHp));
            Assert.That(battle.Log.Any(x => x.Contains("没能行动")), Is.True);
            Assert.That(battle.TryAct(BattleAction.Skill).Success, Is.False);
        }

        [Test]
        public void ReserveSwapConsumesCurrentActionAndUpdatesDeploymentWithoutFourthFighter()
        {
            var party = MakeParty(4);
            var outgoing = party.Members[0];
            var incoming = party.Members[3];
            var battle = new BattleSession(party, catalog, new Inventory(), Make("glowglade-warden", 3, false),
                () => 0f, BattleKind.Guardian);
            Assert.That(battle.TrySwap(outgoing.instanceId, incoming.instanceId).Success, Is.True);
            Assert.That(party.Deployed.Count, Is.EqualTo(3));
            Assert.That(party.Deployed.Contains(outgoing), Is.False);
            Assert.That(party.Deployed.Contains(incoming), Is.True);
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[1]));
            Assert.That(battle.TrySwap(party.Members[2].instanceId, outgoing.instanceId).Success, Is.False);
        }

        [Test]
        public void AllDeployedFaintingPromotesLivingReservesAndOnlyWholeRosterDefeatEndsBattle()
        {
            var party = MakeParty(6);
            foreach (var member in party.Members) member.currentHp = 1;
            catalog["glowglade-warden"].baseAttack = 1000;
            var reserves = party.Members.Skip(3).ToArray();
            var battle = new BattleSession(party, catalog, new Inventory(), Make("glowglade-warden", 3, false),
                () => 0f, BattleKind.Guardian);
            for (int i = 0; i < 6; i++) battle.TryAct(BattleAction.Defend);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            CollectionAssert.AreEqual(reserves, party.Deployed);
            Assert.That(battle.CurrentActor, Is.SameAs(reserves[0]));
            for (int i = 0; i < 6; i++) battle.TryAct(BattleAction.Defend);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(party.Members.All(x => x.currentHp == 0), Is.True);
        }

        [Test]
        public void EncounterStartsWithLivingReservesIfPreviousDeploymentIsFainted()
        {
            var party = MakeParty(4);
            foreach (var member in party.Members.Take(3)) member.currentHp = 0;
            var battle = new BattleSession(party, catalog, new Inventory(), Make("wild-01", 1, false));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[3]));
            Assert.That(party.Deployed.Count, Is.EqualTo(1));
        }

        [Test]
        public void ScriptedBossSilenceDoesNotAutomaticallyPromoteHealthyReserves()
        {
            var party = MakeParty(4);
            catalog["moss-lumi"].baseAttack = 1000;
            var reserve = party.Members[3];
            int reserveHp = reserve.currentHp;
            var battle = new BattleSession(party, catalog, new Inventory(), Make("final-boss", 1, false),
                () => 0f, BattleKind.FinalBoss);
            for (int i = 0; i < 6; i++) battle.TryAct(BattleAction.Attack);
            Assert.That(battle.FinalStage, Is.EqualTo(FinalBossStage.Silence));
            Assert.That(party.Deployed.All(x => x.currentHp == 0), Is.True);
            Assert.That(party.Deployed.Contains(reserve), Is.False);
            Assert.That(reserve.currentHp, Is.EqualTo(reserveHp));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
        }

        [Test]
        public void StorageProtectsStarterAndKeepsAtLeastOneTravelCompanion()
        {
            var storage = new CompanionStorage();
            var party = MakeParty(2);
            Assert.That(storage.TryDeposit(party, party.Members[0].instanceId, out _), Is.False);
            var ordinary = party.Members[1];
            Assert.That(storage.TryDeposit(party, ordinary.instanceId, out _), Is.True);
            Assert.That(storage.Members[0], Is.SameAs(ordinary));
            var single = new PartyRoster();
            single.TryAdd(Make(starter: false));
            Assert.That(storage.TryDeposit(single, single.Members[0].instanceId, out _), Is.False);
            Assert.That(single.Count, Is.EqualTo(1));
        }

        [Test]
        public void StorageWithdrawIsAtomicAtSixMemberLimitAndPreservesIdentityAndGrowth()
        {
            var member = Make("wild-03", 5, false);
            member.bond = 23;
            var storage = new CompanionStorage(new List<CreatureInstance> { member });
            var party = MakeParty(6);
            Assert.That(storage.TryWithdraw(party, member.instanceId, out _), Is.False);
            Assert.That(storage.Members.Count, Is.EqualTo(1));
            Assert.That(party.Count, Is.EqualTo(6));
            party.Remove(party.Members[5].instanceId);
            Assert.That(storage.TryWithdraw(party, member.instanceId, out _), Is.True);
            Assert.That(storage.Members.Count, Is.Zero);
            Assert.That(party.Members.Contains(member), Is.True);
            Assert.That(member.level, Is.EqualTo(5));
            Assert.That(member.bond, Is.EqualTo(23));
        }

        [Test]
        public void ExperienceCanCrossSeveralLevelsWithoutRevivingFaintedMember()
        {
            var member = Make();
            member.currentHp = 0;
            Assert.That(GrowthRules.GrantExperience(member, catalog[member.speciesId], 48), Is.EqualTo(3));
            Assert.That(member.level, Is.EqualTo(4));
            Assert.That(member.currentHp, Is.Zero);
        }

        [Test]
        public void EvolutionRequiresLevelAndBondAndPreservesIndividualIdentity()
        {
            var member = Make();
            string instanceId = member.instanceId;
            member.nickname = "小伙伴";
            Assert.That(GrowthRules.TryEvolve(member, catalog, out _), Is.False);
            member.level = 4;
            member.bond = 14;
            Assert.That(GrowthRules.TryEvolve(member, catalog, out var evolved), Is.True);
            Assert.That(member.speciesId, Is.EqualTo("moss-lumi-evolved"));
            Assert.That(member.instanceId, Is.EqualTo(instanceId));
            Assert.That(member.nickname, Is.EqualTo("小伙伴"));
            Assert.That(member.evolutionStage, Is.EqualTo(1));
            Assert.That(GrowthRules.TryEvolve(member, catalog, out _), Is.False);
            Assert.That(evolved, Is.SameAs(catalog["moss-lumi-evolved"]));
        }

        [Test]
        public void FinalBossTelegraphsThenKnocksOutThenRequiresPlayerToFinishComeback()
        {
            var party = MakeParty();
            catalog["moss-lumi"].baseAttack = 1000;
            var starter = party.Members[0];
            var battle = new BattleSession(party, catalog, new Inventory(), Make("final-boss", 1, false),
                () => 0f, BattleKind.FinalBoss);
            battle.TryAct(BattleAction.Attack);
            Assert.That(battle.FinalStage, Is.EqualTo(FinalBossStage.Charging));
            Assert.That(starter.currentHp, Is.GreaterThan(0));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            battle.TryAct(BattleAction.Defend);
            Assert.That(battle.FinalStage, Is.EqualTo(FinalBossStage.Silence));
            Assert.That(starter.currentHp, Is.Zero);
            Assert.That(battle.TryAct(BattleAction.Attack).Success, Is.False);
            battle.TryAct(BattleAction.Continue);
            Assert.That(starter.awakened, Is.True);
            Assert.That(starter.currentHp, Is.GreaterThan(0));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            Assert.That(battle.TryAct(BattleAction.Attack).Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void FinalResonanceUnlocksOnlyForAwakenedStarterAndStillRequiresManualAction()
        {
            var party = MakeParty(2);
            var battle = new BattleSession(party, catalog, new Inventory(), Make("final-boss", 1, false),
                () => 0f, BattleKind.FinalBoss);
            int initialWildHp = battle.Wild.currentHp;
            Assert.That(battle.TryAct(BattleAction.Resonance).Success, Is.False);
            Assert.That(battle.Wild.currentHp, Is.EqualTo(initialWildHp));
            Assert.That(battle.CurrentActor, Is.SameAs(party.Members[0]));
            catalog["moss-lumi"].baseAttack = 1000;
            battle.TryAct(BattleAction.Attack);
            battle.TryAct(BattleAction.Defend);
            battle.TryAct(BattleAction.Defend);
            battle.TryAct(BattleAction.Defend);
            Assert.That(battle.FinalStage, Is.EqualTo(FinalBossStage.Silence));
            battle.TryAct(BattleAction.Continue);
            Assert.That(battle.FinalStage, Is.EqualTo(FinalBossStage.Awakened));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            catalog["moss-lumi"].baseAttack = 9;
            int beforeSkill = battle.Wild.currentHp;
            var response = battle.TryAct(BattleAction.Resonance);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Does.Contain("归航回响"));
            Assert.That(battle.Wild.currentHp, Is.LessThan(beforeSkill).And.GreaterThan(0));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            Assert.That(battle.CurrentActor.isStarter, Is.False);
            Assert.That(battle.TryAct(BattleAction.Resonance).Success, Is.False);
        }

        [Test]
        public void PersistentAwakeningDoesNotAllowFinalResonanceInOrdinaryEncounters()
        {
            var party = MakeParty();
            party.Members[0].awakened = true;
            var battle = new BattleSession(party, catalog, new Inventory(), Make("wild-01", 1, false));
            Assert.That(battle.TryAct(BattleAction.Resonance).Success, Is.False);
        }

        [Test]
        public void FleeReturnsAnActualTerminalResultWithoutAwardingExperience()
        {
            var party = MakeParty();
            var battle = new BattleSession(party, catalog, new Inventory(), Make("wild-02", 1, false));
            Assert.That(battle.TryAct(BattleAction.Flee).Outcome, Is.EqualTo(BattleOutcome.Escaped));
            Assert.That(party.Members[0].experience, Is.Zero);
            Assert.That(battle.TryAct(BattleAction.Attack).Success, Is.False);
        }

        [Test]
        public void StarterAndTwoCapturedCompanionsCanCompleteGuardianAndBossProgression()
        {
            var party = new PartyRoster();
            party.TryAdd(Make("crystal-gecko", 3));
            var inventory = new Inventory();
            inventory.Add("resonance-capsule", 2);
            foreach (string wildId in new[] { "wild-01", "wild-03" })
            {
                var encounter = new BattleSession(party, catalog, inventory, Make(wildId, 2, false), () => 0f);
                Assert.That(encounter.TryAct(BattleAction.Capture).Outcome, Is.EqualTo(BattleOutcome.Captured));
            }
            int guardianIndex = 0;
            foreach (string id in GameCatalog.GuardianIds.Concat(new[] { "final-boss" }))
            {
                foreach (var member in party.Members)
                    member.currentHp = CreatureInstance.MaxHp(catalog[member.speciesId], member.level);
                bool final = id == "final-boss";
                int level = final ? 12 : 6 + guardianIndex++ * 2;
                var battle = new BattleSession(party, catalog, inventory, Make(id, level, false), () => 0.3f,
                    final ? BattleKind.FinalBoss : BattleKind.Guardian);
                for (int action = 0; action < 200 && battle.Outcome == BattleOutcome.Ongoing; action++)
                {
                    if (battle.FinalStage == FinalBossStage.Silence) battle.TryAct(BattleAction.Continue);
                    else if (!battle.TryAct(BattleAction.Skill).Success) battle.TryAct(BattleAction.Attack);
                }
                Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Victory), id + " could not be completed.");
            }
        }
    }
}
