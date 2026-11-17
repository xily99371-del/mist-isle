using System;
using System.Collections.Generic;
using MistIsle.Creatures;

namespace MistIsle.World
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public const int CurrentSchemaVersion = 3;
        public int schemaVersion = CurrentSchemaVersion;
        public string lastSceneId = "coastal-camp";
        public string selectedStarterSpeciesId;
        public List<CreatureInstance> roster = new List<CreatureInstance>();
        public List<CreatureInstance> storedRoster = new List<CreatureInstance>();
        public SealProgression seals = new SealProgression();
        public List<string> inventoryItemIds = new List<string>();
        public List<int> inventoryCounts = new List<int>();
        public bool captureTutorialCompleted;
        public string roomId = "camp-0";
        public float playerX;
        public float playerY;
        public bool hasPosition;
        public List<string> deployedIds = new List<string>();
        public List<string> collectedNodeIds = new List<string>();
        public List<string> discoveredSpeciesIds = new List<string>();
        public List<string> completedFieldIds = new List<string>();
        public bool chapterCompleted;
        public float playSeconds;
        public int openingStage;
    }
}
