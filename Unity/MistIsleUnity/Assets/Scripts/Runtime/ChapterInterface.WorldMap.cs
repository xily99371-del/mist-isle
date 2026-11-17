using System.Collections.Generic;
using System.Linq;
using MistIsle.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MistIsle.Runtime
{
    public sealed partial class ChapterInterface
    {
        private int selectedMap = -1;
        private int mapCacheRevision = -1;
        private readonly Dictionary<string, Texture2D> mapTextures = new Dictionary<string, Texture2D>();

        private void DrawWorldMap()
        {
            BeginWindow("雾屿航图 · M / Esc 返回");
            if (selectedMap < 0) selectedMap = DesignPackCatalog.GetMap(Session.Progress.roomId).Index;
            var maps = DesignPackCatalog.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                var map = maps[i];
                bool visited = Session.Progress.visitedMapIds.Contains(map.RoomId);
                if (GUI.Button(new Rect(156 + i * 194, 196, 184, 49), map.Name + (visited ? " · 已抵达" : " · 未抵达"), button)) selectedMap = i;
            }
            var selectedRegion = maps[selectedMap];
            bool revealed = Session.Progress.visitedMapIds.Contains(selectedRegion.RoomId);
            GUI.Label(new Rect(163, 263, 388, 75), "营地 ↔ 潮风小径\n小径向北接林地，向东接遗迹", body);
            GUI.Label(new Rect(163, 351, 388, 106), Session.HasThreeSeals
                ? "三处守护已经回应，遗迹北口通往灯塔。"
                : "林地与遗迹可以自由选择先后。\n森印、潮印与晶印齐全后，灯塔通路开启。", body);
            GUI.Label(new Rect(163, 478, 388, 91), "金色：你的位置\n蓝色：出入口　绿色：营地服务\n地图会随抵达区域展开。", small);
            var area = new Rect(588, 266, 508, 317);
            GUI.Box(area, GUIContent.none, card);
            if (!revealed) GUI.Label(new Rect(618, 383, 450, 80), "尚未抵达这里\n沿路牌探索后，航图将记录地形。", body);
            else
            {
                float scale = Mathf.Min((area.width - 16) / selectedRegion.Width, (area.height - 16) / selectedRegion.Height);
                var mapRect = new Rect(area.center.x - selectedRegion.Width * scale * .5f, area.center.y - selectedRegion.Height * scale * .5f,
                    selectedRegion.Width * scale, selectedRegion.Height * scale);
                GUI.DrawTexture(mapRect, GetTerrainMap(selectedRegion), ScaleMode.StretchToFill, false);
                foreach (var exit in selectedRegion.Exits) MapDot(mapRect, selectedRegion, exit.TriggerRect.center, new Color(.4f, .83f, 1f), 7);
                foreach (var node in selectedRegion.Nodes.Where(n => n.Kind == "shop" || n.Kind == "campfire" || n.Id == "watcher" || n.Id == "shiguang"))
                    MapDot(mapRect, selectedRegion, node.Position, new Color(.54f, 1f, .63f), 6);
                if (Session.Progress.roomId == selectedRegion.RoomId) MapDot(mapRect, selectedRegion, Session.PlayerTransform.position, new Color(1f, .83f, .26f), 8);
            }
            CloseButton();
        }

        private Texture2D GetTerrainMap(DesignPackMap region)
        {
            int revision = Session.Progress.completedFieldIds.Count;
            if (mapCacheRevision != revision)
            {
                foreach (var old in mapTextures.Values) if (old != null) Destroy(old);
                mapTextures.Clear(); mapCacheRevision = revision;
            }
            if (mapTextures.TryGetValue(region.Id, out var cached)) return cached;
            var world = Session.transform.Find("DesignPackWorld");
            var mapRoot = world.Cast<Transform>().First(child => child.name.StartsWith(region.Id + " ", System.StringComparison.Ordinal));
            var water = mapRoot.Find("Water").GetComponent<Tilemap>();
            var ground = mapRoot.Find("Ground").GetComponent<Tilemap>();
            var collision = mapRoot.Find("Collision").GetComponent<Tilemap>();
            var texture = new Texture2D(region.Width, region.Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[region.Width * region.Height];
            for (int y = 0; y < region.Height; y++) for (int x = 0; x < region.Width; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                Color color = water.HasTile(cell) ? new Color(.16f, .42f, .61f) : collision.HasTile(cell) ? new Color(.39f, .40f, .38f) : new Color(.51f, .64f, .39f);
                var tile = ground.GetTile<Tile>(cell);
                if (!water.HasTile(cell) && !collision.HasTile(cell) && tile != null && tile.sprite != null
                    && int.TryParse(tile.sprite.name.Substring(tile.sprite.name.LastIndexOf('_') + 1), out int index) && index >= 8 && index < 40)
                    color = new Color(.77f, .67f, .46f);
                pixels[y * region.Width + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply(false, true); mapTextures.Add(region.Id, texture); return texture;
        }
        private static void MapDot(Rect rect, DesignPackMap map, Vector2 point, Color color, float size)
        {
            Vector2 local = point - map.Origin;
            float x = rect.x + local.x / map.Width * rect.width, y = rect.yMax - local.y / map.Height * rect.height;
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(x - size * .5f, y - size * .5f, size, size), Texture2D.whiteTexture); GUI.color = previous;
        }
    }
}
