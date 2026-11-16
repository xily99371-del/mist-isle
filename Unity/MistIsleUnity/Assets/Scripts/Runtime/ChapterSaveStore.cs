using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
                    Validate(candidate, catalog);
                    data = candidate;
                    message = file.EndsWith(".bak", StringComparison.Ordinal) ? "已从备份恢复进度。" : "已继续旅程。";
                    return true;
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is InvalidDataException)
                {
                    message = "存档无法读取，原文件已保留：" + ex.Message;
                }
            }
            return false;
        }

        public static void Validate(WorldSaveData data, IReadOnlyDictionary<string, CreatureSpecies> catalog)
        {
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
            if (string.IsNullOrEmpty(data.roomId)) data.roomId = "camp-0";
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
                if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".bak");
                else File.Move(temporary, Path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error = "保存失败：" + ex.Message;
                return false;
            }
        }

        public void ArchiveBeforeNewJourney()
        {
            if (File.Exists(Path)) File.Copy(Path, Path + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".history", false);
        }
    }
}
