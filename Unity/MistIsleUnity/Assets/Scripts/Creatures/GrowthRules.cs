using System;
using System.Collections.Generic;
using UnityEngine;

namespace MistIsle.Creatures
{
    public static class GrowthRules
    {
        public const int LevelCap = 50;
        public static int ExperienceToNextLevel(int level) => Mathf.Max(1, level) * 8;

        public static int GrantExperience(CreatureInstance creature, CreatureSpecies species, int amount)
        {
            if (creature == null || species == null) throw new ArgumentNullException();
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            creature.level = Mathf.Clamp(creature.level, 1, LevelCap);
            if (creature.level == LevelCap) { creature.experience = 0; return 0; }
            long total = (long)Mathf.Max(0, creature.experience) + amount;
            int initialLevel = creature.level;
            int oldMax = CreatureInstance.MaxHp(species, creature.level);
            while (creature.level < LevelCap && total >= ExperienceToNextLevel(creature.level))
            {
                total -= ExperienceToNextLevel(creature.level);
                creature.level++;
            }
            creature.experience = creature.level == LevelCap ? 0 : (int)total;
            // Gaining a level must not revive a fainted companion.
            if (creature.currentHp > 0)
                creature.currentHp = Mathf.Min(CreatureInstance.MaxHp(species, creature.level),
                    creature.currentHp + CreatureInstance.MaxHp(species, creature.level) - oldMax);
            return creature.level - initialLevel;
        }

        public static bool TryEvolve(CreatureInstance creature,
            IReadOnlyDictionary<string, CreatureSpecies> catalog, out CreatureSpecies target)
        {
            target = null;
            if (creature == null || catalog == null
                || !catalog.TryGetValue(creature.speciesId, out var species)
                || species.evolutionTarget == null || creature.level < species.evolutionLevel
                || creature.bond < species.evolutionBond) return false;
            target = species.evolutionTarget;
            if (!catalog.ContainsKey(target.speciesId) || target.speciesId == species.speciesId)
            { target = null; return false; }
            int oldMax = CreatureInstance.MaxHp(species, creature.level);
            creature.speciesId = target.speciesId;
            creature.evolutionStage++;
            if (string.IsNullOrEmpty(creature.nickname) || creature.nickname == species.displayName)
                creature.nickname = target.displayName;
            if (creature.currentHp > 0)
                creature.currentHp = Mathf.Clamp(creature.currentHp
                    + CreatureInstance.MaxHp(target, creature.level) - oldMax,
                    1, CreatureInstance.MaxHp(target, creature.level));
            return true;
        }
    }
}
