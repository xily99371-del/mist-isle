using System.Collections.Generic;

namespace MistIsle.Creatures
{
    public enum ElementType { Neutral, Verdant, Tide, Ember, Gale, Stone, Crystal, Umbral, Radiant }

    public static class ElementChart
    {
        private static readonly Dictionary<(ElementType, ElementType), float> Matchups =
            new Dictionary<(ElementType, ElementType), float>
            {
                [(ElementType.Verdant, ElementType.Crystal)] = 1.5f,
                [(ElementType.Crystal, ElementType.Verdant)] = 0.75f,
                [(ElementType.Crystal, ElementType.Tide)] = 1.5f,
                [(ElementType.Tide, ElementType.Crystal)] = 0.75f,
                [(ElementType.Tide, ElementType.Ember)] = 1.5f,
                [(ElementType.Ember, ElementType.Tide)] = 0.75f,
                [(ElementType.Ember, ElementType.Verdant)] = 1.5f,
                [(ElementType.Verdant, ElementType.Ember)] = 0.75f
            };

        // Unapproved relations (including Gale/Stone) remain neutral.
        public static float Multiplier(ElementType attack, ElementType defense) =>
            Matchups.TryGetValue((attack, defense), out float value) ? value : 1f;
    }
}
