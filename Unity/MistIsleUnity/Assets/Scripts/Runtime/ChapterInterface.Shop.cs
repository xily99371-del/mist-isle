using MistIsle.Items;
using UnityEngine;

namespace MistIsle.Runtime
{
    public sealed partial class ChapterInterface
    {
        private void DrawShop()
        {
            BeginWindow("营地补给商店");
            GUI.Label(new Rect(160, 190, 930, 42), "共鸣贝 " + Session.Inventory.GetCount(ShopCatalog.CurrencyItemId)
                + "  ·  采集贝壳、花粉和晶尘，可在这里换取补给。", body);
            int row = 0;
            foreach (var offer in ShopCatalog.Offers)
            {
                float y = 240 + row++ * 45;
                GUI.Box(new Rect(157, y, 964, 42), GUIContent.none, card);
                GUI.Label(new Rect(173, y + 5, 360, 34), offer.DisplayName + "  ×" + Session.Inventory.GetCount(offer.ItemId), body);
                if (offer.BuyPrice > 0 && GUI.Button(new Rect(578, y + 4, 235, 33), "购买  " + offer.BuyPrice + " 共鸣贝", button)) Session.BuySupply(offer.Id);
                if (offer.SellPrice > 0 && GUI.Button(new Rect(831, y + 4, 268, 33), "出售  " + offer.SellPrice + " 共鸣贝", button)) Session.SellSupply(offer.Id);
            }
            GUI.Label(new Rect(162, 566, 860, 42), "每次交易一份。Watcher 的照护免费，采集点按时间恢复。", small);
            CloseButton();
        }
    }
}
