using System;
using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Items
{
    [Serializable]
    public sealed class ItemStack
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public sealed class Inventory
    {
        [SerializeField] private List<ItemStack> entries = new List<ItemStack>();
        public IReadOnlyList<ItemStack> Entries => entries;
        public int GetCount(string itemId) => entries.Find(x => x.itemId == itemId)?.count ?? 0;

        public void Add(string itemId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item id is required.", nameof(itemId));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var entry = entries.Find(x => x.itemId == itemId);
            if (entry == null) entries.Add(new ItemStack { itemId = itemId, count = amount });
            else entry.count = checked(entry.count + amount);
        }

        public bool TryConsume(string itemId, int amount = 1)
        {
            if (amount <= 0) return false;
            var entry = entries.Find(x => x.itemId == itemId);
            if (entry == null || entry.count < amount) return false;
            entry.count -= amount;
            if (entry.count == 0) entries.Remove(entry);
            return true;
        }

        public void Clear() => entries.Clear();
    }

    public static class CraftingService
    {
        public static bool TryCraft(Inventory inventory, CampfireRecipe recipe, out string reason)
        {
            if (inventory == null || recipe == null || string.IsNullOrWhiteSpace(recipe.outputItemId)
                || recipe.outputAmount < 1 || recipe.ingredients == null || recipe.ingredients.Count == 0)
            { reason = "配方不可用。"; return false; }
            var required = new Dictionary<string, int>();
            foreach (var ingredient in recipe.ingredients)
            {
                if (string.IsNullOrWhiteSpace(ingredient.itemId) || ingredient.amount <= 0)
                { reason = "配方材料无效。"; return false; }
                int total = required.TryGetValue(ingredient.itemId, out int amount) ? amount : 0;
                if ((long)total + ingredient.amount > int.MaxValue)
                { reason = "配方材料数量过大。"; return false; }
                required[ingredient.itemId] = total + ingredient.amount;
            }
            foreach (var pair in required)
                if (inventory.GetCount(pair.Key) < pair.Value)
                { reason = "材料不足。"; return false; }
            int consumedOutput = required.TryGetValue(recipe.outputItemId, out int count) ? count : 0;
            if ((long)inventory.GetCount(recipe.outputItemId) - consumedOutput + recipe.outputAmount > int.MaxValue)
            { reason = "物品数量已达上限。"; return false; }
            foreach (var pair in required) inventory.TryConsume(pair.Key, pair.Value);
            inventory.Add(recipe.outputItemId, recipe.outputAmount);
            reason = "制作完成：" + recipe.displayName;
            return true;
        }
    }
}
