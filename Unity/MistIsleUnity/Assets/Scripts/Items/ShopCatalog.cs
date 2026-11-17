using System;
using System.Collections.Generic;

namespace MistIsle.Items
{
    public sealed class ShopOffer
    {
        public string Id { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public int BuyPrice { get; }
        public int SellPrice { get; }
        public int UnlockSealCount { get; }

        internal ShopOffer(string itemId, string displayName, int buyPrice, int sellPrice, int unlockSealCount = 0)
        {
            Id = ItemId = itemId;
            DisplayName = displayName;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            UnlockSealCount = unlockSealCount;
        }
    }

    public static class ShopCatalog
    {
        public const string CurrencyItemId = "camp-token";
        public const string CurrencyDisplayName = "共鸣贝";

        // A zero buy price means the camp only purchases this material from the traveler.
        // Stable existing item ids also keep shop transactions compatible with inventory saves.
        public static IReadOnlyList<ShopOffer> Offers { get; } = Array.AsReadOnly(new[]
        {
            new ShopOffer("berry", "野莓", 3, 1),
            new ShopOffer("dew", "露水", 2, 1),
            new ShopOffer("berry-treat", "莓果团", 8, 3),
            new ShopOffer("resonance-capsule", "共鸣捕获器", 12, 5),
            new ShopOffer("shell", "贝壳", 0, 2),
            new ShopOffer("pollen", "花粉", 0, 2),
            new ShopOffer("crystal", "晶尘", 0, 3)
        });

        public static bool TryGetOffer(string id, out ShopOffer offer)
        {
            foreach (var candidate in Offers)
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                { offer = candidate; return true; }
            offer = null;
            return false;
        }
    }
}
