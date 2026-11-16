using System;
using UnityEngine;

namespace MistIsle.Creatures
{
    [Serializable]
    public sealed class CreatureInstance
    {
        public string instanceId;
        public string speciesId;
        public string nickname;
        public int level;
        public int experience;
        public int currentHp;
        public int bond;
        public bool isStarter;
        public int evolutionStage;
        public bool awakened;

        public CreatureInstance() { }

        public CreatureInstance(CreatureSpecies species, int startingLevel, bool starter)
        {
            if (species == null) throw new ArgumentNullException(nameof(species));
            instanceId = Guid.NewGuid().ToString("N");
            speciesId = species.speciesId;
            nickname = species.displayName;
            level = Mathf.Max(1, startingLevel);
            experience = 0;
            currentHp = MaxHp(species, level);
            bond = starter ? 10 : 0;
            isStarter = starter;
        }

        public static int MaxHp(CreatureSpecies species, int level = 1)
        {
            return species.baseMaxHp + Mathf.Max(0, level - 1) * 3;
        }

        public int Attack(CreatureSpecies species) => Mathf.RoundToInt(
            (species.baseAttack + Mathf.Max(0, level - 1) * 2) * (awakened ? 1.7f : 1f));
        public int Defense(CreatureSpecies species) => Mathf.RoundToInt(
            (species.baseDefense + Mathf.Max(0, level - 1)) * (awakened ? 1.25f : 1f));
    }
}
