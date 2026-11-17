namespace MistIsle.Items
{
    public static class ShopService
    {
        public static bool TryBuy(Inventory inventory, string offerId, int amount, int sealCount, out string reason)
        {
            if (!Validate(inventory, offerId, amount, sealCount, out var offer, out reason)) return false;
            if (offer.BuyPrice <= 0) { reason = "营地只收购这份材料。"; return false; }
            long cost = (long)offer.BuyPrice * amount;
            if (cost > int.MaxValue) { reason = "购买数量过大。"; return false; }
            int balance = inventory.GetCount(ShopCatalog.CurrencyItemId);
            int owned = inventory.GetCount(offer.ItemId);
            if (balance < 0 || owned < 0) { reason = "背包数量异常，交易已取消。"; return false; }
            if (balance < cost) { reason = ShopCatalog.CurrencyDisplayName + "不足。"; return false; }
            if ((long)owned + amount > int.MaxValue) { reason = "背包中的物品数量已达上限。"; return false; }

            // Validate both sides before changing either stack. Inventory has no mutation callbacks.
            if (!inventory.TryConsume(ShopCatalog.CurrencyItemId, (int)cost))
            { reason = "余额发生变化，请重新选择。"; return false; }
            inventory.Add(offer.ItemId, amount);
            reason = "购入" + offer.DisplayName + " ×" + amount + "，花费" + ShopCatalog.CurrencyDisplayName + " ×" + cost + "。";
            return true;
        }

        public static bool TrySell(Inventory inventory, string offerId, int amount, int sealCount, out string reason)
        {
            if (!Validate(inventory, offerId, amount, sealCount, out var offer, out reason)) return false;
            if (offer.SellPrice <= 0) { reason = "营地暂不收购这件物品。"; return false; }
            long proceeds = (long)offer.SellPrice * amount;
            if (proceeds > int.MaxValue) { reason = "出售数量过大。"; return false; }
            int balance = inventory.GetCount(ShopCatalog.CurrencyItemId);
            int owned = inventory.GetCount(offer.ItemId);
            if (balance < 0 || owned < 0) { reason = "背包数量异常，交易已取消。"; return false; }
            if (owned < amount) { reason = "没有足够的" + offer.DisplayName + "。"; return false; }
            if ((long)balance + proceeds > int.MaxValue) { reason = ShopCatalog.CurrencyDisplayName + "数量已达上限。"; return false; }

            if (!inventory.TryConsume(offer.ItemId, amount))
            { reason = "物品数量发生变化，请重新选择。"; return false; }
            inventory.Add(ShopCatalog.CurrencyItemId, (int)proceeds);
            reason = "售出" + offer.DisplayName + " ×" + amount + "，获得" + ShopCatalog.CurrencyDisplayName + " ×" + proceeds + "。";
            return true;
        }

        private static bool Validate(Inventory inventory, string offerId, int amount, int sealCount,
            out ShopOffer offer, out string reason)
        {
            offer = null;
            if (inventory == null) { reason = "背包尚未就绪。"; return false; }
            if (amount <= 0) { reason = "请选择正整数数量。"; return false; }
            if (sealCount < 0) { reason = "旅途进度异常，交易已取消。"; return false; }
            if (!ShopCatalog.TryGetOffer(offerId, out offer)) { reason = "没有这件商品。"; return false; }
            if (sealCount < offer.UnlockSealCount) { reason = "完成更多区域封印后，这件商品才会开放。"; return false; }
            reason = null;
            return true;
        }
    }
}
