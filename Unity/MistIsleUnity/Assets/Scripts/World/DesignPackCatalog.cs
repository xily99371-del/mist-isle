using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MistIsle.World
{
    /// <summary>Read-only source data. The supplied JSON is retained byte-for-byte in Resources.</summary>
    public static class DesignPackCatalog
    {
        private static readonly string[] Folders = { "M00_CoastalCamp", "M01_TidewindPath", "M02_Glowglade", "M03_TidalRuins", "M04_LighthouseCourtyard" };
        private static readonly string[] Rooms = { "camp-0", "coast-0", "glowglade-0", "tidal-0", "lighthouse-0" };
        private static readonly Dictionary<string, DesignPackMap> byMap = new Dictionary<string, DesignPackMap>();
        private static readonly Dictionary<string, DesignPackNode> byInteraction = new Dictionary<string, DesignPackNode>();
        private static readonly List<DesignPackMap> maps = new List<DesignPackMap>();
        public static IReadOnlyList<DesignPackMap> Maps { get { EnsureLoaded(); return maps; } }

        public static DesignPackMap GetMap(string roomOrMapId)
        {
            EnsureLoaded();
            if (byMap.TryGetValue(roomOrMapId ?? "", out var map)) return map;
            throw new ArgumentOutOfRangeException(nameof(roomOrMapId), roomOrMapId, "Unknown design-pack map.");
        }
        public static string GetMapId(string roomId) => GetMap(roomId).Id;
        public static bool TryGetInteraction(string id, out DesignPackNode node)
        { EnsureLoaded(); return byInteraction.TryGetValue(id ?? "", out node); }
        public static DesignPackExit GetExit(string sourceRoom, string targetRoom)
            => GetMap(sourceRoom).Exits.FirstOrDefault(exit => exit.TargetRoom == targetRoom);
        public static bool TryGetExitById(string id, out DesignPackExit exit)
        { EnsureLoaded(); exit = maps.SelectMany(map => map.Exits).FirstOrDefault(item => item.Id == id); return exit != null; }

        private static void EnsureLoaded()
        {
            if (maps.Count == 5) return;
            // Populate once only after parsing succeeds, so a missing import never leaves partial data.
            var parsed = new List<DesignPackMap>();
            var world = Read("00_World/WorldConnections");
            var overlay = Read("Overlays/GameplayOverlay");
            var overrides = Object(overlay, "nodes");
            var worldMaps = Array(world, "maps");
            for (int index = 0; index < Folders.Length; index++)
            {
                var layout = Object(Read("01_Maps/" + Folders[index] + "/Layout"), "map");
                var sourceNodes = Array(Read("01_Maps/" + Folders[index] + "/Interactions"), "interactions");
                var map = new DesignPackMap {
                    Id = Text(layout, "id"), RoomId = Rooms[index], Index = index,
                    Origin = new Vector2(index * 256f, 0f),
                    Width = Int((List<object>)layout["size"], 0), Height = Int((List<object>)layout["size"], 1),
                    Name = Text((Dictionary<string, object>)worldMaps[index], "name_zh"), Raw = layout
                };
                map.Routes = Array(layout, "routes").Select(route => ((List<object>)route)
                    .Select(point => Point((List<object>)point)).ToArray()).ToArray();
                map.CollisionRects = Array(layout, "collision_rects").Select(value => Rect((List<object>)value)).ToArray();
                var nodes = sourceNodes.Select(value => MakeNode(map, (Dictionary<string, object>)value, overrides)).ToList();
                foreach (var value in Array(overlay, "extra_nodes"))
                {
                    var extra = (Dictionary<string, object>)value;
                    if (Text(extra, "map_id") == map.Id) nodes.Add(MakeNode(map, extra, overrides));
                }
                map.Nodes = nodes.ToArray();
                map.Spawn = map.Origin + (index == 0 ? new Vector2(9f, 5f) : index == 1 ? new Vector2(3f, 28f) :
                    index == 2 ? new Vector2(47f, 3f) : index == 3 ? new Vector2(3f, 40f) : new Vector2(39f, 3f));
                var exits = new List<DesignPackExit>();
                foreach (var value in Array(world, "connections"))
                {
                    var connection = (Dictionary<string, object>)value;
                    var a = Object(connection, "a"); var b = Object(connection, "b");
                    if (Text(a, "map") != map.Id) continue;
                    int target = int.Parse(Text(b, "map").Substring(1), CultureInfo.InvariantCulture);
                    Rect rect = Rect((List<object>)a["trigger_rect"]); rect.position += map.Origin;
                    exits.Add(new DesignPackExit {
                        Id = Text(connection, "id"), SourceRoom = map.RoomId, SourceMap = map.Id,
                        TargetRoom = Rooms[target], TargetMap = Text(b, "map"),
                        SourceExitId = Text(a, "exit"), TargetEntryId = Text(b, "entry"),
                        TriggerRect = rect, Spawn = new Vector2(target * 256f, 0f) + Point((List<object>)b["spawn"]),
                        Facing = Facing(Text(b, "facing")),
                        Requires = Text(connection, "unlock") == "all_seals" ?
                            new[] { "forest_seal", "tide_seal", "crystal_seal" } : System.Array.Empty<string>()
                    });
                }
                map.Exits = exits.ToArray(); parsed.Add(map);
            }
            byMap.Clear(); byInteraction.Clear(); maps.Clear();
            foreach (var map in parsed)
            {
                byMap.Add(map.Id, map); byMap.Add(map.RoomId, map); maps.Add(map);
                foreach (var node in map.Nodes)
                {
                    if (!byInteraction.TryAdd(node.Id, node)) throw new FormatException("Duplicate interaction " + node.Id);
                    if (node.SourceId != node.Id) byInteraction[node.SourceId] = node;
                }
            }
        }

        private static DesignPackNode MakeNode(DesignPackMap map, Dictionary<string, object> source, Dictionary<string, object> overrides)
        {
            string sourceId = Text(source, "id");
            var extra = overrides.TryGetValue(sourceId, out object item) ? (Dictionary<string, object>)item : source;
            Vector2 local = source.TryGetValue("xy", out object xy) ? Point((List<object>)xy) : Rect((List<object>)source["rect"]).center;
            string type = Text(source, "type");
            string kind = Text(extra, "kind");
            if (kind == "") kind = Kind(type);
            return new DesignPackNode {
                Id = Text(extra, "interaction_id", sourceId), SourceId = sourceId, MapId = map.Id, RoomId = map.RoomId,
                Kind = kind, Type = type, DisplayName = Text(source, "name"),
                Position = map.Origin + local,
                ApproachPosition = map.Origin + (extra.TryGetValue("approach_xy", out var approach) ? Point((List<object>)approach) : local),
                Footprint = source.TryGetValue("footprint", out var footprint) ? Point((List<object>)footprint) : Vector2.one,
                Requires = Strings(source, "requires"), ItemId = Text(extra, "item_id"),
                Amount = Number(extra, "amount", 1), RespawnSeconds = Number(extra, "respawn_seconds", Number(source, "respawn_seconds", kind == "encounter" ? 120 : 0)),
                EncounterSpeciesIds = Strings(extra, "encounter_species_ids"), MinLevel = Number(extra, "min_level", 1),
                MaxLevel = Number(extra, "max_level", 3), FieldAbility = Text(extra, "field_ability"),
                Reward = Text(extra, "reward"), SpriteKey = Text(extra, "sprite_key"), Raw = source
            };
        }

        private static string Kind(string type)
        {
            switch (type) {
                case "save_craft": case "save_craft_heal": return "campfire";
                case "gather": case "gather_zone": return "resource";
                case "encounter_zone": case "encounter_gather": return "encounter";
                case "guardian_arena": return "guardian";
                case "boss": return "boss";
                case "field_ability_gate": case "water_level_puzzle": case "energy_puzzle": return "field";
                case "shop": return "shop";
                case "chest": return "chest";
                case "quest_board": return "sign";
                case "gated_door": case "guardian_gate": case "seal_insert": case "hub_puzzle":
                case "tutorial_crafting": case "story_interior": case "storage": return "story";
                default: return "";
            }
        }
        private static Vector2 Facing(string facing) => facing == "N" ? Vector2.up : facing == "S" ? Vector2.down : facing == "W" ? Vector2.left : Vector2.right;
        internal static Dictionary<string, object> Read(string path)
        {
            var asset = Resources.Load<TextAsset>("Data/MapDesign/" + path);
            if (asset == null) throw new InvalidOperationException("Missing design source: " + path);
            return (Dictionary<string, object>)DesignPackJson.Parse(asset.text);
        }
        internal static Dictionary<string, object> Object(Dictionary<string, object> obj, string key)
            => obj.TryGetValue(key, out var value) ? (Dictionary<string, object>)value : new Dictionary<string, object>();
        internal static List<object> Array(Dictionary<string, object> obj, string key)
            => obj.TryGetValue(key, out var value) ? (List<object>)value : new List<object>();
        internal static string Text(Dictionary<string, object> obj, string key, string fallback = "")
            => obj.TryGetValue(key, out var value) && value is string text ? text : fallback;
        internal static int Number(Dictionary<string, object> obj, string key, int fallback)
            => obj.TryGetValue(key, out var value) ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : fallback;
        private static int Int(List<object> values, int index) => Convert.ToInt32(values[index], CultureInfo.InvariantCulture);
        private static Vector2 Point(List<object> values) => new Vector2(Convert.ToSingle(values[0], CultureInfo.InvariantCulture), Convert.ToSingle(values[1], CultureInfo.InvariantCulture));
        private static Rect Rect(List<object> values) => new Rect(Point(values), new Vector2(Convert.ToSingle(values[2], CultureInfo.InvariantCulture), Convert.ToSingle(values[3], CultureInfo.InvariantCulture)));
        private static string[] Strings(Dictionary<string, object> obj, string key)
            => Array(obj, key).Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)).ToArray();
    }

    public sealed class DesignPackMap
    {
        public string Id { get; internal set; }
        public string RoomId { get; internal set; }
        public string Name { get; internal set; }
        public int Index { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public Vector2 Origin { get; internal set; }
        public Rect Bounds => new Rect(Origin, new Vector2(Width, Height));
        public Vector2 Spawn { get; internal set; }
        public Vector2[][] Routes { get; internal set; }
        public Rect[] CollisionRects { get; internal set; }
        public DesignPackNode[] Nodes { get; internal set; }
        public DesignPackExit[] Exits { get; internal set; }
        public IReadOnlyDictionary<string, object> Raw { get; internal set; }
    }
    public sealed class DesignPackNode
    {
        public string Id { get; internal set; }
        public string SourceId { get; internal set; }
        public string MapId { get; internal set; }
        public string RoomId { get; internal set; }
        public string Kind { get; internal set; }
        public string Type { get; internal set; }
        public string DisplayName { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 ApproachPosition { get; internal set; }
        public Vector2 Footprint { get; internal set; }
        public string[] Requires { get; internal set; }
        public string ItemId { get; internal set; }
        public int Amount { get; internal set; }
        public int RespawnSeconds { get; internal set; }
        public string[] EncounterSpeciesIds { get; internal set; }
        public int MinLevel { get; internal set; }
        public int MaxLevel { get; internal set; }
        public string FieldAbility { get; internal set; }
        public string Reward { get; internal set; }
        public string SpriteKey { get; internal set; }
        public IReadOnlyDictionary<string, object> Raw { get; internal set; }
    }
    public sealed class DesignPackExit
    {
        public string Id { get; internal set; }
        public string SourceRoom { get; internal set; }
        public string SourceMap { get; internal set; }
        public string TargetRoom { get; internal set; }
        public string TargetMap { get; internal set; }
        public string SourceExitId { get; internal set; }
        public string TargetEntryId { get; internal set; }
        public Rect TriggerRect { get; internal set; }
        public Vector2 Spawn { get; internal set; }
        public Vector2 Facing { get; internal set; }
        public string[] Requires { get; internal set; }
    }

    // Deliberately small JSON reader: no external package, mutation, polymorphism or code execution.
    internal sealed class DesignPackJson
    {
        private readonly string text;
        private int index;
        private DesignPackJson(string source) { text = source; }
        public static object Parse(string source)
        {
            var reader = new DesignPackJson(source); object result = reader.Value(); reader.Space();
            if (reader.index != source.Length) throw new FormatException("Trailing design JSON data.");
            return result;
        }
        private void Space() { while (index < text.Length && (char.IsWhiteSpace(text[index]) || text[index] == '\ufeff')) index++; }
        private object Value()
        {
            Space(); if (index >= text.Length) throw new FormatException("Unexpected end of JSON.");
            char c = text[index];
            if (c == '{') return Object(); if (c == '[') return Array(); if (c == '"') return String();
            foreach (var literal in new[] { "true", "false", "null" })
                if (text.Substring(index).StartsWith(literal, StringComparison.Ordinal)) {
                    index += literal.Length; return literal == "null" ? null : (object)(literal == "true"); }
            int start = index;
            while (index < text.Length && "-+0123456789.eE".IndexOf(text[index]) >= 0) index++;
            if (index == start) throw new FormatException("Invalid JSON token at " + index);
            return double.Parse(text.Substring(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        private Dictionary<string, object> Object()
        {
            index++; var result = new Dictionary<string, object>(); Space();
            if (Take('}')) return result;
            do { Space(); string key = String(); Space(); Require(':'); result.Add(key, Value()); Space(); }
            while (Take(',')); Require('}'); return result;
        }
        private List<object> Array()
        {
            index++; var result = new List<object>(); Space(); if (Take(']')) return result;
            do { result.Add(Value()); Space(); } while (Take(',')); Require(']'); return result;
        }
        private string String()
        {
            Require('"'); var result = new StringBuilder();
            while (index < text.Length) {
                char c = text[index++]; if (c == '"') return result.ToString();
                if (c != '\\') { result.Append(c); continue; }
                if (index >= text.Length) break;
                c = text[index++];
                if (c == 'u') { result.Append((char)int.Parse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); index += 4; }
                else if (c == 'n') result.Append('\n'); else if (c == 'r') result.Append('\r');
                else if (c == 't') result.Append('\t'); else if (c == 'b') result.Append('\b');
                else if (c == 'f') result.Append('\f'); else if (c == '"' || c == '\\' || c == '/') result.Append(c);
                else throw new FormatException("Unknown JSON escape.");
            }
            throw new FormatException("Unterminated JSON string.");
        }
        private bool Take(char c) { if (index < text.Length && text[index] == c) { index++; return true; } return false; }
        private void Require(char c) { if (!Take(c)) throw new FormatException("Expected '" + c + "' at " + index); }
    }
}
