namespace MistIsle.Creatures
{
    public enum ElementType { Neutral, Verdant, Tide, Ember, Gale, Stone, Crystal, Umbral, Radiant }

    public static class ElementChart
    {
        public static float Multiplier(ElementType attack, ElementType defense)
        {
            if ((attack == ElementType.Verdant && defense == ElementType.Crystal) ||
                (attack == ElementType.Crystal && defense == ElementType.Tide) ||
                (attack == ElementType.Tide && defense == ElementType.Ember) ||
                (attack == ElementType.Ember && defense == ElementType.Verdant) ||
                (attack == ElementType.Gale && defense == ElementType.Stone) ||
                (attack == ElementType.Stone && defense == ElementType.Gale)) return 1.5f;

            if ((defense == ElementType.Verdant && attack == ElementType.Crystal) ||
                (defense == ElementType.Crystal && attack == ElementType.Tide) ||
                (defense == ElementType.Tide && attack == ElementType.Ember) ||
                (defense == ElementType.Ember && attack == ElementType.Verdant) ||
                (defense == ElementType.Gale && attack == ElementType.Stone) ||
                (defense == ElementType.Stone && attack == ElementType.Gale)) return 0.75f;

            return 1.0f;
        }
    }
}
