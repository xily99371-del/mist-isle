using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Creatures
{
    [CreateAssetMenu(menuName = "Mist Isle/Creature Species", fileName = "CreatureSpecies")]
    public sealed class CreatureSpecies : ScriptableObject
    {
        [Header("Identity")]
        public string speciesId;
        public string displayName;
        [TextArea] public string fieldLore;
        public ElementType primaryElement;

        [Header("Growth")]
        [Min(1)] public int baseMaxHp = 30;
        [Min(1)] public int baseAttack = 8;
        [Min(1)] public int baseDefense = 6;
        [Min(1)] public int baseSpeed = 6;

        [Header("Capture personality")]
        [Range(0f, 1f)] public float temperamentResistance = 0.2f;
        public List<string> preferredFoodIds = new List<string>();
        public List<string> dislikedFoodIds = new List<string>();

        [Header("Evolution and exploration")]
        public CreatureSpecies evolutionTarget;
        public string fieldAbilityId;
        [Min(1)] public int evolutionLevel = 16;
        [Min(0)] public int evolutionBond = 30;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(speciesId))
                speciesId = name.Trim().ToLowerInvariant().Replace(" ", "-");
        }
    }
}
