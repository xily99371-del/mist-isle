using System;
using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Items
{
    [Serializable]
    public struct RecipeIngredient
    {
        public string itemId;
        [Min(1)] public int amount;
    }

    [CreateAssetMenu(menuName = "Mist Isle/Campfire Recipe", fileName = "CampfireRecipe")]
    public sealed class CampfireRecipe : ScriptableObject
    {
        public string recipeId;
        public string displayName;
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        public string outputItemId;
        [Min(1)] public int outputAmount = 1;
        [TextArea] public string discoveryHint;
    }
}
