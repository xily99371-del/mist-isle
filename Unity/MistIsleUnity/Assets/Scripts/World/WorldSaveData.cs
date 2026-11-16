using System;
using System.Collections.Generic;
using MistIsle.Creatures;

namespace MistIsle.World
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public int schemaVersion = CurrentSchemaVersion;
        public string lastSceneId = "coastal-camp";
        public string selectedStarterSpeciesId;
        public List<CreatureInstance> roster = new List<CreatureInstance>();
        public SealProgression seals = new SealProgression();
        public List<string> inventoryItemIds = new List<string>();
        public List<int> inventoryCounts = new List<int>();
        public bool captureTutorialCompleted;
    }
}
