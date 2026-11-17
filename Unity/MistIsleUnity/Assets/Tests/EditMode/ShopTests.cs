using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MistIsle.Creatures;
using MistIsle.Items;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;

namespace MistIsle.Tests
{
    public sealed class ShopTests
    {
        private Dictionary<string, CreatureSpecies> species;

        [TearDown]
        public void TearDown()
        {
            if (species != null)
                foreach (var value in species.Values) UnityEngine.Object.DestroyImmediate(value);
            species = null;
        }

        private static string Snapshot(Inventory inventory) => JsonUtility.ToJson(inventory);

        [Test]
        public void SellingGatheredMaterialsPaysForAnActualCaptureToolPurchase()
        {
            var inventory = new Inventory();
            inventory.Add("crystal", 4);
            inventory.Add("berry", 2);
            Assert.That(ShopService.TrySell(inventory, "crystal", 4, 0, out var reason), Is.True, reason);
            Assert.That(inventory.GetCount("crystal"), Is.Zero);
            Assert.That(inventory.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(12));
            Assert.That(ShopService.TryBuy(inventory, "resonance-capsule", 1, 0, out reason), Is.True, reason);
            Assert.That(inventory.GetCount("resonance-capsule"), Is.EqualTo(1));
            Assert.That(inventory.GetCount(ShopCatalog.CurrencyItemId), Is.Zero);
            Assert.That(inventory.GetCount("berry"), Is.EqualTo(2), "An unrelated inventory stack changed.");
        }

        [Test]
        public void BuyingThenSellingAnyAvailableProductCannotIncreaseCurrency()
        {
            foreach (var offer in ShopCatalog.Offers.Where(value => value.BuyPrice > 0))
            {
                var inventory = new Inventory();
                inventory.Add(ShopCatalog.CurrencyItemId, 1000);
                Assert.That(ShopService.TryBuy(inventory, offer.Id, 5, 4, out var reason), Is.True, reason);
                Assert.That(inventory.GetCount(offer.ItemId), Is.EqualTo(5));
                Assert.That(ShopService.TrySell(inventory, offer.Id, 5, 4, out reason), Is.True, reason);
                Assert.That(inventory.GetCount(offer.ItemId), Is.Zero);
                Assert.That(inventory.GetCount(ShopCatalog.CurrencyItemId), Is.LessThanOrEqualTo(1000), offer.Id);
            }
        }

        [TestCase("pollen")]
        [TestCase("crystal")]
        [TestCase("shell")]
        public void PurchaseOfSellOnlyMaterialsFailsWithoutSpendingCurrency(string id)
        {
            var inventory = new Inventory();
            inventory.Add(ShopCatalog.CurrencyItemId, 100);
            inventory.Add(id, 3);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, id, 1, 4, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("missing-item")]
        [TestCase("camp-token")]
        public void UnknownOrCurrencyOffersCannotChangeEitherSideOfInventory(string id)
        {
            var inventory = new Inventory();
            inventory.Add(ShopCatalog.CurrencyItemId, 100);
            inventory.Add("berry", 3);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, id, 1, 4, out _), Is.False);
            Assert.That(ShopService.TrySell(inventory, id, 1, 4, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NonPositiveQuantitiesFailWithoutPartialChanges(int amount)
        {
            var inventory = new Inventory();
            inventory.Add(ShopCatalog.CurrencyItemId, 100);
            inventory.Add("berry", 3);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, "berry", amount, 0, out _), Is.False);
            Assert.That(ShopService.TrySell(inventory, "berry", amount, 0, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [Test]
        public void InsufficientFundsOrStockNeverGrantsItemsOrCurrency()
        {
            var inventory = new Inventory();
            inventory.Add(ShopCatalog.CurrencyItemId, 7);
            inventory.Add("berry", 1);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, "berry-treat", 1, 0, out _), Is.False);
            Assert.That(ShopService.TrySell(inventory, "berry", 2, 0, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [Test]
        public void UnitPriceMultiplicationOverflowIsRejectedBeforeConsumingAnything()
        {
            var inventory = new Inventory();
            inventory.Add(ShopCatalog.CurrencyItemId, int.MaxValue);
            inventory.Add("crystal", int.MaxValue);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, "berry", int.MaxValue, 0, out _), Is.False);
            Assert.That(ShopService.TrySell(inventory, "crystal", int.MaxValue, 0, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [Test]
        public void FullDestinationStacksRejectTransactionsBeforeDebitingTheirSource()
        {
            var inventory = new Inventory();
            inventory.Add("berry", int.MaxValue);
            inventory.Add(ShopCatalog.CurrencyItemId, int.MaxValue);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, "berry", 1, 0, out _), Is.False);
            Assert.That(ShopService.TrySell(inventory, "berry", 1, 0, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [Test]
        public void ExactIntegerBoundaryTransactionsRemainValid()
        {
            var buying = new Inventory();
            buying.Add("dew", int.MaxValue - 1);
            buying.Add(ShopCatalog.CurrencyItemId, 2);
            Assert.That(ShopService.TryBuy(buying, "dew", 1, 0, out var reason), Is.True, reason);
            Assert.That(buying.GetCount("dew"), Is.EqualTo(int.MaxValue));
            Assert.That(buying.GetCount(ShopCatalog.CurrencyItemId), Is.Zero);

            var selling = new Inventory();
            selling.Add("berry", int.MaxValue);
            Assert.That(ShopService.TrySell(selling, "berry", int.MaxValue, 0, out reason), Is.True, reason);
            Assert.That(selling.GetCount("berry"), Is.Zero);
            Assert.That(selling.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void MissingInventoryOrInvalidProgressIsRejectedWithoutMutation()
        {
            Assert.That(ShopService.TryBuy(null, "berry", 1, 0, out _), Is.False);
            Assert.That(ShopService.TrySell(null, "berry", 1, 0, out _), Is.False);
            var inventory = new Inventory();
            inventory.Add(ShopCatalog.CurrencyItemId, 10);
            inventory.Add("berry", 2);
            string before = Snapshot(inventory);
            Assert.That(ShopService.TryBuy(inventory, "berry", 1, -1, out _), Is.False);
            Assert.That(ShopService.TrySell(inventory, "berry", 1, -1, out _), Is.False);
            Assert.That(Snapshot(inventory), Is.EqualTo(before));
        }

        [Serializable]
        private sealed class PreShopSave
        {
            public int schemaVersion = 3;
            public string selectedStarterSpeciesId = "moss-lumi";
            public int openingStage = (int)OpeningStage.Complete;
            public List<CreatureInstance> roster = new List<CreatureInstance>();
            public List<string> inventoryItemIds = new List<string>();
            public List<int> inventoryCounts = new List<int>();
        }

        [Test]
        public void PreShopInventoryLoadsWithZeroCurrencyThenTradesAndPersistsUsingExistingSaveFields()
        {
            species = GameCatalog.CreateSpecies();
            InTemporaryDirectory(store =>
            {
                var old = new PreShopSave();
                var partner = new CreatureInstance(species["moss-lumi"], 3, true);
                old.roster.Add(partner);
                old.inventoryItemIds.Add("berry");
                old.inventoryCounts.Add(4);
                File.WriteAllText(store.Path, JsonUtility.ToJson(old), new UTF8Encoding(false));

                Assert.That(store.TryLoad(species, out var loaded, out var reason), Is.True, reason);
                var inventory = RestoreInventory(loaded);
                Assert.That(inventory.GetCount(ShopCatalog.CurrencyItemId), Is.Zero);
                Assert.That(ShopService.TrySell(inventory, "berry", 3, 0, out reason), Is.True, reason);
                Assert.That(ShopService.TryBuy(inventory, "dew", 1, 0, out reason), Is.True, reason);
                StoreInventory(loaded, inventory);
                Assert.That(store.Save(loaded, out reason), Is.True, reason);

                Assert.That(store.TryLoad(species, out var restored, out reason), Is.True, reason);
                var persisted = RestoreInventory(restored);
                Assert.That(persisted.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(1));
                Assert.That(persisted.GetCount("berry"), Is.EqualTo(1));
                Assert.That(persisted.GetCount("dew"), Is.EqualTo(1));
                Assert.That(restored.roster.Single().instanceId, Is.EqualTo(partner.instanceId));
                Assert.That(restored.selectedStarterSpeciesId, Is.EqualTo("moss-lumi"));
            });
        }

        [Test]
        public void PurchasedFoodToolsAndRemainingCurrencyRoundTripThroughChapterSaveStore()
        {
            species = GameCatalog.CreateSpecies();
            InTemporaryDirectory(store =>
            {
                var inventory = new Inventory();
                inventory.Add(ShopCatalog.CurrencyItemId, 40);
                Assert.That(ShopService.TryBuy(inventory, "resonance-capsule", 2, 0, out var reason), Is.True, reason);
                Assert.That(ShopService.TryBuy(inventory, "berry-treat", 1, 0, out reason), Is.True, reason);
                var data = new WorldSaveData { selectedStarterSpeciesId = "moss-lumi", openingStage = (int)OpeningStage.Complete };
                data.roster.Add(new CreatureInstance(species["moss-lumi"], 3, true));
                StoreInventory(data, inventory);
                Assert.That(store.Save(data, out reason), Is.True, reason);
                Assert.That(store.TryLoad(species, out var loaded, out reason), Is.True, reason);
                var restored = RestoreInventory(loaded);
                Assert.That(restored.GetCount("resonance-capsule"), Is.EqualTo(2));
                Assert.That(restored.GetCount("berry-treat"), Is.EqualTo(1));
                Assert.That(restored.GetCount(ShopCatalog.CurrencyItemId), Is.EqualTo(8));
                Assert.That(ShopService.TryBuy(restored, "berry-treat", 1, 0, out reason), Is.True, reason);
                Assert.That(restored.GetCount(ShopCatalog.CurrencyItemId), Is.Zero);
            });
        }

        private static Inventory RestoreInventory(WorldSaveData data)
        {
            var inventory = new Inventory();
            for (int i = 0; i < data.inventoryItemIds.Count; i++) inventory.Add(data.inventoryItemIds[i], data.inventoryCounts[i]);
            return inventory;
        }

        private static void StoreInventory(WorldSaveData data, Inventory inventory)
        {
            data.inventoryItemIds = inventory.Entries.Select(value => value.itemId).ToList();
            data.inventoryCounts = inventory.Entries.Select(value => value.count).ToList();
        }

        private static void InTemporaryDirectory(Action<ChapterSaveStore> action)
        {
            string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string leaf = "MistIsle-ShopTests-" + Guid.NewGuid().ToString("N");
            string directory = Path.GetFullPath(Path.Combine(root, leaf));
            Directory.CreateDirectory(directory);
            try { action(new ChapterSaveStore(directory)); }
            finally
            {
                string resolved = Path.GetFullPath(directory);
                if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(Path.GetFileName(resolved), leaf, StringComparison.Ordinal)
                    || !string.Equals(resolved, Path.Combine(root, leaf), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing to delete an unverified shop-test directory.");
                if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            }
        }
    }
}
