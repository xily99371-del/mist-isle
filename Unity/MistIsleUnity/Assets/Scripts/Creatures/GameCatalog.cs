using System.Collections.Generic;
using MistIsle.Items;
using UnityEngine;

namespace MistIsle.Creatures
{
    // Stable ids are shared with scenes and saves. Runtime assets are owned by the game session.
    public static class GameCatalog
    {
        public static readonly string[] StarterIds = { "moss-lumi", "moatail", "crystal-gecko" };
        public static readonly string[] GuardianIds = { "glowglade-warden", "tidal-warden", "crystal-warden", "lighthouse-warden" };

        public static Dictionary<string, CreatureSpecies> CreateSpecies()
        {
            var result = new Dictionary<string, CreatureSpecies>();
            Add(result, "moss-lumi", "苔藓灵", ElementType.Verdant, 36, 9, 7, "verdant", true);
            Add(result, "moatail", "沫尾兽", ElementType.Tide, 34, 10, 6, "tide", true);
            Add(result, "crystal-gecko", "鸣晶蜥", ElementType.Crystal, 30, 12, 5, "crystal", true);
            Add(result, "moss-lumi-evolved", "苔辉灵", ElementType.Verdant, 48, 14, 10, "verdant");
            Add(result, "moatail-evolved", "潮澜狐", ElementType.Tide, 45, 15, 9, "tide");
            Add(result, "crystal-gecko-evolved", "霆晶龙蜥", ElementType.Crystal, 40, 18, 7, "crystal");
            foreach (string id in StarterIds)
            {
                result[id].evolutionTarget = result[id + "-evolved"];
                result[id].evolutionLevel = 4;
                result[id].evolutionBond = 14;
            }
            // Art is taken from the supplied model sheets. Names can be revised without changing save ids.
            string[] wildNames = {
                "露栗团鼠", "萤羽雀", "潮壳蟹", "雾棘蝠",
                "藤角鹿", "碎光水母", "露芽绵羊", "潮泡海獭",
                "晶角蜗牛", "余烬小狐", "风铃飞蛾", "礁岩石龟" };
            ElementType[] wildElements = {
                ElementType.Verdant, ElementType.Gale, ElementType.Tide, ElementType.Umbral,
                ElementType.Verdant, ElementType.Tide, ElementType.Verdant, ElementType.Tide,
                ElementType.Crystal, ElementType.Ember, ElementType.Gale, ElementType.Stone };
            for (int i = 0; i < wildNames.Length; i++)
            {
                string id = "wild-" + (i + 1).ToString("00");
                Add(result, id, wildNames[i], wildElements[i], 20 + i, 6 + i / 3, 3 + i / 4);
                result[id].temperamentResistance = i < 6 ? 0.12f : 0.3f;
                result[id].contentReady = true;
                result[id].designApprovalPending = i >= 6;
                if (i >= 6) result[id].fieldLore = "原创候选伙伴，正式美术设计待审核。" + result[id].fieldLore;
                result[id].preferredFoodIds.Add(i % 2 == 0 ? "berry-treat" : "berry");
            }
            Add(result, "glowglade-warden", "花冠鹿灵", ElementType.Verdant, 70, 10, 7, capturable: false);
            Add(result, "tidal-warden", "断潮蟹王", ElementType.Tide, 88, 12, 8, capturable: false);
            Add(result, "crystal-warden", "棱晶蛇王", ElementType.Crystal, 98, 14, 9, capturable: false);
            Add(result, "lighthouse-warden", "回响观测者", ElementType.Radiant, 110, 16, 10, capturable: false);
            Add(result, "final-boss", "回响观测者 · 终局", ElementType.Umbral, 170, 18, 10, capturable: false);
            result["final-boss"].spriteResourcePath = "Art/Creatures/lighthouse-warden";
            return result;
        }

        private static void Add(Dictionary<string, CreatureSpecies> catalog, string id, string name,
            ElementType element, int hp, int attack, int defense, string ability = "",
            bool starter = false, bool capturable = true)
        {
            var species = ScriptableObject.CreateInstance<CreatureSpecies>();
            species.name = id;
            species.speciesId = id;
            species.displayName = name;
            species.primaryElement = element;
            species.baseMaxHp = hp;
            species.baseAttack = attack;
            species.baseDefense = defense;
            species.baseSpeed = 6;
            species.fieldAbilityId = ability;
            species.isStarterSpecies = starter;
            species.capturable = capturable;
            species.artKey = id;
            species.spriteResourcePath = "Art/Creatures/" + id;
            species.fieldLore = starter ? "与旅人结伴出发，借由探索、战斗和羁绊逐渐成长。"
                : capturable ? "生活在雾屿的野生伙伴；可以通过削弱或偏好食物建立共鸣。"
                : "守护区域封印的古老存在，无法捕获。";
            catalog.Add(id, species);
        }

        public static List<CampfireRecipe> CreateRecipes()
        {
            return new List<CampfireRecipe>
            {
                Recipe("berry-treat", "莓果团", "berry-treat", ("berry", 2), ("dew", 1)),
                Recipe("resonance-capsule", "共鸣捕获器", "resonance-capsule", ("crystal", 2), ("pollen", 1))
            };
        }

        private static CampfireRecipe Recipe(string id, string name, string output,
            (string id, int count) first, (string id, int count) second)
        {
            var recipe = ScriptableObject.CreateInstance<CampfireRecipe>();
            recipe.name = id;
            recipe.recipeId = id;
            recipe.displayName = name;
            recipe.outputItemId = output;
            recipe.outputAmount = 1;
            recipe.ingredients.Add(new RecipeIngredient { itemId = first.id, amount = first.count });
            recipe.ingredients.Add(new RecipeIngredient { itemId = second.id, amount = second.count });
            recipe.discoveryHint = "在海岸营地的篝火旁制作。";
            return recipe;
        }

        public static string ItemName(string itemId)
        {
            switch (itemId)
            {
                case "berry": return "野莓";
                case "dew": return "露水";
                case "crystal": return "晶尘";
                case "pollen": return "花粉";
                case "shell": return "海岸贝壳";
                case "camp-token": return "共鸣贝";
                case "berry-treat": return "莓果团";
                case "resonance-capsule": return "共鸣捕获器";
                default: return itemId ?? "未知物品";
            }
        }
    }
}
