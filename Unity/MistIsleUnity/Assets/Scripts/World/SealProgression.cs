using System;
using UnityEngine;

namespace MistIsle.World
{
    [Serializable]
    public sealed class SealProgression
    {
        [SerializeField] private bool glowgladeGuardianDefeated;
        [SerializeField] private bool tidalGuardianDefeated;
        [SerializeField] private bool crystalGuardianDefeated;
        [SerializeField] private bool coastGuardianDefeated;
        [SerializeField] private bool lighthouseGuardianDefeated;

        public bool CanEnterLighthouse =>
            HasSeal("lighthouse-warden") && glowgladeGuardianDefeated && tidalGuardianDefeated && crystalGuardianDefeated;

        public int Count => (HasSeal("lighthouse-warden") ? 1 : 0) + (glowgladeGuardianDefeated ? 1 : 0)
            + (tidalGuardianDefeated ? 1 : 0) + (crystalGuardianDefeated ? 1 : 0);

        public bool HasSeal(string id)
        {
            switch (id)
            {
                case "coast-warden": return coastGuardianDefeated;
                case "lighthouse-warden": return lighthouseGuardianDefeated || coastGuardianDefeated;
                case "glowglade-warden": return glowgladeGuardianDefeated;
                case "tidal-warden": return tidalGuardianDefeated;
                case "crystal-warden": return crystalGuardianDefeated;
                default: return false;
            }
        }

        public void RecordGuardianVictory(string guardianId)
        {
            switch (guardianId)
            {
                case "coast-warden": coastGuardianDefeated = true; break;
                case "lighthouse-warden": lighthouseGuardianDefeated = true; break;
                case "glowglade-warden": glowgladeGuardianDefeated = true; break;
                case "tidal-warden": tidalGuardianDefeated = true; break;
                case "crystal-warden": crystalGuardianDefeated = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(guardianId), guardianId,
                    "Unknown seal guardian id.");
            }
        }
    }
}
