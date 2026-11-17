using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MistIsle.Creatures;
using MistIsle.World;
using UnityEngine;

namespace MistIsle.Runtime
{
    public sealed class ChapterSaveStore
    {
        public string Path { get; }
        public bool Exists => File.Exists(Path) || File.Exists(Path + ".bak");
        public ChapterSaveStore(string directory = null)
        {
            Path = System.IO.Path.Combine(directory ?? System.IO.Path.Combine(Application.persistentDataPath, "UnityChapter"), "chapter-01.json");
        }

        public bool TryLoad(IReadOnlyDictionary<string, CreatureSpecies> catalog, out WorldSaveData data, out string message)
        {
            data = null;
            message = "没有可读取的存档。";
            foreach (var file in new[] { Path, Path + ".bak" })
            {
                if (!File.Exists(file)) continue;
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    if (!json.TrimStart().StartsWith("{")) throw new InvalidDataException("存档格式不正确");
                    var candidate = JsonUtility.FromJson<WorldSaveData>(json);
                    if (candidate == null || candidate.schemaVersion < 1) throw new InvalidDataException("存档版本不正确");
                    if (candidate.schemaVersion > WorldSaveData.CurrentSchemaVersion)
                    {
                        message = "该存档来自更新的版本，已保留，请使用对应版本打开。";
                        return false;
                    }
                    if (candidate.schemaVersion < 4 || candidate.mapContentVersion < 1)
                        ArchiveBeforeMapMigration(file, candidate.schemaVersion);
                    Validate(candidate, catalog);
                    data = candidate;
                    message = file.EndsWith(".bak", StringComparison.Ordinal) ? "已从备份恢复进度。" : "已继续旅程。";
                    return true;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidDataException)
                {
                    message = "存档无法读取，原文件已保留：" + ex.Message;
                }
            }
            return false;
        }

        private void ArchiveBeforeMapMigration(string source, int schema)
        {
            byte[] original = File.ReadAllBytes(source);
            string fingerprint;
            using (var hash = SHA256.Create()) fingerprint = BitConverter.ToString(hash.ComputeHash(original)).Replace("-", "").Substring(0, 24);
            // Content-addressed history survives later autosaves replacing the normal .bak file.
            string archive = Path + ".before-map-v1.schema" + schema + "-" + fingerprint + ".history";
            if (!File.Exists(archive)) File.WriteAllBytes(archive, original);
        }

        public static void Validate(WorldSaveData data, IReadOnlyDictionary<string, CreatureSpecies> catalog)
        {
            bool legacyMap = data.schemaVersion < 4 || data.mapContentVersion < 1;
            if (data.schemaVersion < 3 && !string.IsNullOrEmpty(data.selectedStarterSpeciesId))
                data.openingStage = (int)OpeningStage.Complete;
            data.openingStage = Mathf.Clamp(data.openingStage, 0, (int)OpeningStage.Complete);
            data.roster = data.roster ?? new List<CreatureInstance>();
            var ids = new HashSet<string>();
            data.roster.RemoveAll(c => c == null || string.IsNullOrEmpty(c.instanceId)
                || string.IsNullOrEmpty(c.speciesId) || !catalog.ContainsKey(c.speciesId) || !catalog[c.speciesId].capturable || !ids.Add(c.instanceId));
            data.storedRoster = data.storedRoster ?? new List<CreatureInstance>();
            if (data.roster.Count > PartyRoster.Capacity)
            {
                data.storedRoster.AddRange(data.roster.GetRange(PartyRoster.Capacity, data.roster.Count - PartyRoster.Capacity));
                data.roster.RemoveRange(PartyRoster.Capacity, data.roster.Count - PartyRoster.Capacity);
            }
            foreach (var c in data.roster)
            {
                c.level = Mathf.Clamp(c.level, 1, GrowthRules.LevelCap);
                c.currentHp = Mathf.Clamp(c.currentHp, 0, CreatureInstance.MaxHp(catalog[c.speciesId], c.level));
                c.experience = Mathf.Max(0, c.experience);
                c.bond = Mathf.Clamp(c.bond, 0, 100);
            }
            ids = new HashSet<string>(data.roster.ConvertAll(c => c.instanceId));
            data.storedRoster.RemoveAll(c => c == null || string.IsNullOrEmpty(c.instanceId) || c.isStarter
                || string.IsNullOrEmpty(c.speciesId) || !catalog.ContainsKey(c.speciesId) || !catalog[c.speciesId].capturable || !ids.Add(c.instanceId));
            foreach (var c in data.storedRoster)
            {
                c.level = Mathf.Clamp(c.level, 1, GrowthRules.LevelCap);
                c.currentHp = Mathf.Clamp(c.currentHp, 0, CreatureInstance.MaxHp(catalog[c.speciesId], c.level));
                c.experience = Mathf.Max(0, c.experience);
                c.bond = Mathf.Clamp(c.bond, 0, 100);
            }
            data.seals = data.seals ?? new SealProgression();
            data.deployedIds = data.deployedIds ?? new List<string>();
            var partyIds = new HashSet<string>(data.roster.ConvertAll(c => c.instanceId));
            data.deployedIds.RemoveAll(id => !partyIds.Contains(id));
            data.collectedNodeIds = data.collectedNodeIds ?? new List<string>();
            data.discoveredSpeciesIds = data.discoveredSpeciesIds ?? new List<string>();
            data.completedFieldIds = data.completedFieldIds ?? new List<string>();
            data.inventoryItemIds = data.inventoryItemIds ?? new List<string>();
            data.inventoryCounts = data.inventoryCounts ?? new List<int>();
            data.storyFlags = data.storyFlags ?? new List<string>();
            data.visitedMapIds = data.visitedMapIds ?? new List<string>();
            data.openedChestIds = data.openedChestIds ?? new List<string>();
            data.nodeRefreshStates = data.nodeRefreshStates ?? new List<NodeRefreshState>();
            data.nodeRefreshStates.RemoveAll(state => state == null || string.IsNullOrEmpty(state.id));
            if (string.IsNullOrEmpty(data.roomId)) data.roomId = "camp-0";
            if (legacyMap)
            {
                // Keep party, inventory, seals and story checkpoints; old absolute coordinates belong to the 18-room world.
                string prefix = data.roomId.Split('-')[0];
                data.roomId = prefix == "crystal" || prefix == "tidal" ? "tidal-0"
                    : prefix == "glowglade" ? "glowglade-0" : prefix == "lighthouse" ? "lighthouse-0"
                    : prefix == "coast" ? "coast-0" : "camp-0";
                data.hasPosition = false;
                // The old world refreshed on rest and had no deadlines. Preserve its collected flags,
                // but give them a finite recovery time under the new real-time refresh rules.
                long refreshAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120;
                foreach (string id in data.collectedNodeIds)
                    if (!string.IsNullOrEmpty(id) && !data.nodeRefreshStates.Exists(state => state.id == id))
                        data.nodeRefreshStates.Add(new NodeRefreshState { id = id, availableAtUtc = refreshAt });
                data.mapContentVersion = 1;
            }
            data.schemaVersion = WorldSaveData.CurrentSchemaVersion;
        }

        public bool Save(WorldSaveData data, out string error)
        {
            error = null;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                var temporary = Path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
                if (File.Exists(Path)) ReplaceWithBriefRetry(temporary);
                else File.Move(temporary, Path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error = "保存失败：" + ex.Message;
                return false;
            }
        }

        private void ReplaceWithBriefRetry(string temporary)
        {
            // A reader without FileShare.Delete can briefly prevent rotating either save file.
            // Keep atomic replacement; use six 25ms retry delays and leave both files
            // intact if sharing, a byte-range lock, or removal of the old backup stays blocked.
            for (int retry = 0; ; retry++)
            {
                try
                {
                    File.Replace(temporary, Path, Path + ".bak");
                    return;
                }
                catch (IOException ex) when (retry < 6 && IsReplacementBlocked(ex))
                {
                    Thread.Sleep(25);
                }
            }
        }

        private static bool IsReplacementBlocked(IOException exception)
        {
            int errorCode = exception.HResult & 0xFFFF;
            // ERROR_SHARING_VIOLATION, ERROR_LOCK_VIOLATION, ERROR_UNABLE_TO_REMOVE_REPLACED.
            return errorCode == 32 || errorCode == 33 || errorCode == 1175;
        }

        public void ArchiveBeforeNewJourney()
        {
            if (File.Exists(Path)) File.Copy(Path, Path + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".history", false);
        }
    }
}
