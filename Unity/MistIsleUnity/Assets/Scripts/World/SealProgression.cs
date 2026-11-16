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

        public bool CanEnterLighthouse =>
            glowgladeGuardianDefeated && tidalGuardianDefeated && crystalGuardianDefeated;

        public void RecordGuardianVictory(string guardianId)
        {
            switch (guardianId)
            {
                case "glowglade-warden": glowgladeGuardianDefeated = true; break;
                case "tidal-warden": tidalGuardianDefeated = true; break;
                case "crystal-warden": crystalGuardianDefeated = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(guardianId), guardianId,
                    "Unknown seal guardian id.");
            }
        }
    }
}
