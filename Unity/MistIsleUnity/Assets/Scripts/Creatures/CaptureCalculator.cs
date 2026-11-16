using UnityEngine;

namespace MistIsle.Creatures
{
    public readonly struct CaptureContext
    {
        public readonly float hpMissingRatio;
        public readonly float foodAffinity;
        public readonly float statusBonus;
        public readonly float toolQuality;
        public readonly float temperamentResistance;

        public CaptureContext(float hpMissingRatio, float foodAffinity, float statusBonus,
            float toolQuality, float temperamentResistance)
        {
            this.hpMissingRatio = Mathf.Clamp01(hpMissingRatio);
            this.foodAffinity = Mathf.Clamp01(foodAffinity);
            this.statusBonus = Mathf.Clamp01(statusBonus);
            this.toolQuality = Mathf.Clamp01(toolQuality);
            this.temperamentResistance = Mathf.Clamp01(temperamentResistance);
        }
    }

    public static class CaptureCalculator
    {
        public static float Calculate(CaptureContext context, bool feeding)
        {
            float chance = feeding
                ? 0.16f + context.foodAffinity * 0.48f + context.hpMissingRatio * 0.12f
                : 0.18f + context.hpMissingRatio * 0.48f + context.statusBonus * 0.12f
                    + context.toolQuality * 0.30f;
            chance -= context.temperamentResistance * 0.28f;
            return Mathf.Clamp(chance, 0.05f, 0.92f);
        }

        public static bool Roll(CaptureContext context, bool feeding)
        {
            return Random.value < Calculate(context, feeding);
        }
    }
}
