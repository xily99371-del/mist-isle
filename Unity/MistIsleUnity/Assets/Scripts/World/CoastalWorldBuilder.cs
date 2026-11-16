using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MistIsle.World
{
    // Layout is authored here, while inventory, dialogue and progression live in the session.
    public static class CoastalWorldBuilder
    {
        public const float RoomWidth = 32f;
        public const float RoomHeight = 20f;
        private static readonly string[] Regions = { "camp", "coast", "glowglade", "tidal", "crystal", "lighthouse" };
        private static readonly Dictionary<string, Sprite> MarkerSprites = new Dictionary<string, Sprite>();
        private static readonly string[,] Names = {
            { "海岸营地 · 归航之火", "营地后坡 · 旧果园", "观潮栈桥 · 远海来信" },
            { "潮风海岸 · 漂流驿站", "贝壳浅湾 · 潮线小径", "风蚀石台 · 林地前哨" },
            { "辉光林地 · 萤火驿站", "古木回廊 · 藤蔓支路", "根系圣所 · 林地封印" },
            { "潮汐遗迹 · 避雨石庭", "沉水回廊 · 双桥水道", "潮心祭坛 · 潮汐封印" },
            { "鸣晶高地 · 勘探营地", "共鸣矿道 · 晶簇岔路", "星晶祭台 · 晶能封印" },
            { "灯塔山麓 · 最后的篝火", "静默阶庭 · 四印之门", "灯芯圣殿 · 雾海回响" }
        };

        public static CoastalWorldLayout Build(Transform parent, Action<string, string> onInteract,
            Func<string, bool> wasCollected = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            var previous = parent.Find("ChapterOneWorld");
            if (previous != null) throw new InvalidOperationException("World has already been built for this session.");
            var root = new GameObject("ChapterOneWorld").transform;
            root.SetParent(parent, false);
            var art = new WorldArt();
            var layout = new CoastalWorldLayout();
            for (int region = 0; region < Regions.Length; region++)
                for (int room = 0; room < 3; room++)
                {
                    string id = Regions[region] + "-" + room;
                    Vector2 center = new Vector2((region * 3 + room) * 48f, 0f);
                    var roomRoot = new GameObject(id + " — " + Names[region, room]).transform;
                    roomRoot.SetParent(root, false);
                    roomRoot.position = center;
                    layout.AddRoom(id, Names[region, room], center + new Vector2(0f, -5.5f),
                        new Rect(center.x - 16f, -10f, RoomWidth, RoomHeight));
                    BuildGround(roomRoot, art, region, room);
                    BuildBoundaries(roomRoot);
                    BuildLandmarks(roomRoot, art, region, room);
                    Populate(roomRoot, art, layout, region, room, onInteract, wasCollected);
                }
            ConnectRooms(root, art, layout, onInteract);
            layout.Spawn = layout.GetSpawn("camp-0");
            layout.CameraBounds = layout.GetBounds("camp-0");
            return layout;
        }

        private static void BuildGround(Transform parent, WorldArt art, int region, int room)
        {
            var gridObject = new GameObject("GroundGrid", typeof(Grid));
            gridObject.transform.SetParent(parent, false);
            var groundObject = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundObject.transform.SetParent(gridObject.transform, false);
            var map = groundObject.GetComponent<Tilemap>();
            var groundRenderer = groundObject.GetComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = -30000;
            if (art.LitMaterial != null) groundRenderer.sharedMaterial = art.LitMaterial;
            map.tileAnchor = new Vector3(.5f, 0f, 0f);
            Color[] tints = { new Color(.76f,.88f,.67f), new Color(.97f,.89f,.68f), new Color(.54f,.77f,.62f),
                new Color(.61f,.78f,.80f), new Color(.73f,.69f,.84f), new Color(.61f,.68f,.78f) };
            var floor = MakeTile(art.Floor(region), tints[region]);
            var path = MakeTile(art.Floor(region == 0 || region == 2 ? 1 : 4), Color.white);
            var shore = MakeTile(art.Ground(36), new Color(.70f,.86f,.95f));
            for (int x = -16; x < 16; x++)
                for (int y = -10; y < 10; y++)
                {
                    bool border = x == -16 || x == 15 || y == -10 || y == 9;
                    // Every room has a readable central route, with a different side branch.
                    int branchY = room == 0 ? -3 : room == 1 ? 3 : 1;
                    bool trail = Math.Abs(y) <= 1 || (Math.Abs(x) <= 1 && y >= -6 && y <= 5) ||
                        (y == branchY && x >= -10 && x <= 10);
                    map.SetTile(new Vector3Int(x, y, 0), border ? shore : trail ? path : floor);
                }
            if (region == 3 && room == 1)
            {
                // Two water channels with two real crossings; bank collision leaves bridges open.
                for (int x = -12; x <= 12; x++)
                    if ((x < -6 || x > -3) && (x < 8 || x > 10))
                        for (int y = 3; y <= 4; y++) map.SetTile(new Vector3Int(x, y, 0), shore);
                AddBox(parent, "NorthCanalWest", new Vector2(-10f, 4f), new Vector2(5f, 1.6f));
                AddBox(parent, "NorthCanalMiddle", new Vector2(2.5f, 4f), new Vector2(10f, 1.6f));
                AddBox(parent, "NorthCanalEast", new Vector2(11.75f, 4f), new Vector2(1.5f, 1.6f));
            }
        }

        private static Tile MakeTile(Sprite sprite, Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            if (sprite != null)
                tile.transform = Matrix4x4.Scale(new Vector3(1f / sprite.bounds.size.x, 1f / sprite.bounds.size.y, 1f));
            return tile;
        }

        private static void BuildBoundaries(Transform parent)
        {
            AddBox(parent, "WestBoundary", new Vector2(-15.8f, 0f), new Vector2(.7f, 20f));
            AddBox(parent, "EastBoundary", new Vector2(15.8f, 0f), new Vector2(.7f, 20f));
            AddBox(parent, "NorthBoundary", new Vector2(0f, 9.6f), new Vector2(32f, .8f));
            AddBox(parent, "SouthBoundary", new Vector2(0f, -9.6f), new Vector2(32f, .8f));
        }

        private static void BuildLandmarks(Transform parent, WorldArt art, int region, int room)
        {
            if (room == 0)
            {
                var house = Prop(parent, "ShelterFootprint", art.House, new Vector2(-7f, 3f), 6.8f);
                if (house != null)
                {
                    Vector2 size = art.House.bounds.size;
                    var polygon = house.AddComponent<PolygonCollider2D>();
                    polygon.points = new[] { new Vector2(-size.x*.40f,size.y*.05f), new Vector2(size.x*.40f,size.y*.05f),
                        new Vector2(size.x*.44f,size.y*.21f), new Vector2(size.x*.31f,size.y*.36f),
                        new Vector2(-size.x*.32f,size.y*.36f), new Vector2(-size.x*.44f,size.y*.22f) };
                }
                for (int i = 0; i < 3; i++) Fence(parent, art, new Vector2(4.5f + i * 2f, 5f));
            }
            else if (room == 1)
            {
                // Two offset groves form optional loops, leaving the main east/west path open.
                for (int i = 0; i < 4; i++)
                {
                    Tree(parent, art, new Vector2(-9f + i * 2.4f, 5.5f + (i % 2) * .5f));
                    Tree(parent, art, new Vector2(3f + i * 2.4f, -6.5f + (i % 2) * .6f));
                }
                if (region == 0 || region == 5)
                {
                    Fence(parent, art, new Vector2(7f, 3f));
                    Fence(parent, art, new Vector2(9f, 3f));
                }
            }
            else
            {
                // The guardian court has approach lanes around a ring of trees and masonry.
                Tree(parent, art, new Vector2(-9f, 6f));
                Tree(parent, art, new Vector2(9f, 6f));
                Fence(parent, art, new Vector2(-5f, 4f));
                Fence(parent, art, new Vector2(5f, 4f));
                Fence(parent, art, new Vector2(-5f, -4f));
                Fence(parent, art, new Vector2(5f, -4f));
            }
            Tree(parent, art, new Vector2(-12f, -5.5f));
            Tree(parent, art, new Vector2(11.5f, 6.8f));
            if (region == 2)
            {
                Tree(parent, art, new Vector2(-5f, 7.4f));
                Tree(parent, art, new Vector2(4f, 7f));
            }
            if (region == 1 && room == 1)
            {
                Fence(parent, art, new Vector2(-6f, -6f));
                Fence(parent, art, new Vector2(-4f, -6f));
            }
            if (region == 4 && room == 1)
            {
                for (int i = 0; i < 3; i++)
                    Fence(parent, art, new Vector2(-5f + i * 2f, 3.4f));
                for (int i = 0; i < 3; i++)
                    Fence(parent, art, new Vector2(1f + i * 2f, -3.4f));
            }
            if (region == 5 && room == 1)
                for (int i = -4; i <= 4; i++)
                    if (Math.Abs(i) > 1) Fence(parent, art, new Vector2(i * 2f, 4.7f));
        }

        private static void Populate(Transform parent, WorldArt art, CoastalWorldLayout layout, int region,
            int room, Action<string, string> onInteract, Func<string, bool> wasCollected)
        {
            string regionId = Regions[region];
            string roomId = regionId + "-" + room;
            if (room == 0)
            {
                var fire = Prop(parent, "Campfire", art.Campfire, new Vector2(0f, 2f), 1.5f);
                AddBox(parent, "CampfireFootprint", new Vector2(0f, 2.1f), new Vector2(.8f,.45f));
                AddInteraction(parent, layout, "campfire", "campfire-" + roomId, "篝火 · 烹饪、合成与休息",
                    new Vector2(0f, 1.8f), 1f, onInteract, fire);
                var lightObject = new GameObject("CampfireLight", typeof(Light2D));
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                var light = lightObject.GetComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1f, .64f, .28f);
                light.intensity = 1.35f;
                light.pointLightOuterRadius = 4.5f;
                light.pointLightInnerRadius = .5f;
            }
            if (region == 0 && room == 0)
            {
                var guide = Prop(parent, "拾光 · 捕获引导员", art.Shiguang, new Vector2(4f, .5f), 1.25f);
                AddBox(parent, "ShiguangFootprint", new Vector2(4f,.65f), new Vector2(.45f,.35f));
                AddInteraction(parent, layout, "npc", "shiguang", "与拾光交谈 · 捕获指南", new Vector2(4f, .5f), .7f, onInteract, guide);
                var watcher = Prop(parent, "守望者 · 营地补给", art.Watcher, new Vector2(-3.2f, -1.6f), 1.2f);
                AddBox(parent, "WatcherFootprint", new Vector2(-3.2f,-1.45f), new Vector2(.45f,.35f));
                AddInteraction(parent, layout, "npc", "watcher", "与守望者交谈 · 恢复队伍", new Vector2(-3.2f, -1.6f), .7f, onInteract, watcher);
            }
            string[] materialIds = region == 4 ? new[] { "crystal", "dew", "crystal" } :
                region == 2 ? new[] { "pollen", "berry", "dew" } : new[] { "berry", "dew", "pollen" };
            Vector2[] resourcePositions = { new Vector2(-8f,-3.5f), new Vector2(7.5f,2f), new Vector2(5f,-4.5f) };
            for (int i = 0; i < resourcePositions.Length; i++)
            {
                string id = materialIds[i] + "-" + roomId + "-" + i;
                var resource = Prop(parent, materialIds[i], art.Resource(materialIds[i]), resourcePositions[i], .7f);
                var interaction = AddInteraction(parent, layout, "resource", id, "采集" + MaterialName(materialIds[i]),
                    resourcePositions[i], .5f, onInteract, resource);
                if (wasCollected != null && wasCollected(id)) interaction.Collect();
            }
            if (region > 0 && room != 2)
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = i == 0 ? new Vector2(-5.5f, -5f) : new Vector2(8.5f, -2.5f);
                    string id = "wild-" + roomId + "-" + i;
                    var wild = Prop(parent, "WildCreature-" + i, art.Wild(region, i), position, 1.2f);
                    AddInteraction(parent, layout, "encounter", id, "观察野生伙伴 · 战斗 / 投喂", position, .8f, onInteract, wild);
                }
            if ((region >= 2 && region <= 4 && room == 2) || (region == 5 && room == 1))
            {
                string guardianId = regionId + "-warden";
                var guardian = Prop(parent, guardianId, art.Guardian(region), new Vector2(0f, 3f), 3f);
                AddInteraction(parent, layout, "guardian", guardianId, "挑战" + RegionName(region) + "封印守卫",
                    new Vector2(0f, 3f), 1.8f, onInteract, guardian);
            }
            if (region == 5 && room == 2)
            {
                var boss = Prop(parent, "LighthouseFinalBoss", art.FinalBoss, new Vector2(0f, 3f), 4f);
                AddInteraction(parent, layout, "boss", "final-boss", "灯塔核心 · 最终共鸣", new Vector2(0f, 3f), 2f, onInteract, boss);
            }
            if (room == 1 && region > 0 && region < 5)
            {
                BuildFieldRoute(parent, art, layout, region, roomId, onInteract, wasCollected);
            }
            string hint = room == 0 ? "篝火可以恢复队伍、制作食物与共鸣道具。沿石径探索下一片区域。" :
                room == 1 ? "支路藏着资源与野生伙伴。伙伴能力解锁额外奖励，主路始终可以通行。" :
                region == 5 ? "回响观测者守护最后一枚封印。试炼结束后，与初始伙伴一同面对灯塔核心的最终共鸣。" :
                region == 1 ? "穿过海岸前哨就是辉光林地。四枚封印分别位于林地、潮汐、晶能和灯塔。" :
                "先做好准备，再挑战守卫。解除区域封印后可以向更深处前进。";
            var sign = Prop(parent, "路牌", art.Signpost != null ? art.Signpost : Marker("sign"), new Vector2(-1.8f, -3f), .7f);
            AddInteraction(parent, layout, "sign", hint, "阅读路牌", new Vector2(-1.8f, -3f), .55f, onInteract, sign);
        }

        private static void BuildFieldRoute(Transform parent, WorldArt art, CoastalWorldLayout layout, int region,
            string roomId, Action<string, string> onInteract, Func<string, bool> wasCollected)
        {
            string fieldId = "field-" + Regions[region];
            // A fully enclosed optional courtyard has one solid, ability-operated entrance.
            // Its cache is farther than interaction range from every outside wall.
            FieldWall(parent, art, "FieldWest", new Vector2(6f,6.5f), new Vector2(.35f,5f));
            FieldWall(parent, art, "FieldEast", new Vector2(13f,6.5f), new Vector2(.35f,5f));
            FieldWall(parent, art, "FieldNorth", new Vector2(9.5f,9f), new Vector2(7.3f,.35f));
            FieldWall(parent, art, "FieldSouthLeft", new Vector2(7.2f,4f), new Vector2(2.4f,.4f));
            FieldWall(parent, art, "FieldSouthRight", new Vector2(11.8f,4f), new Vector2(2.4f,.4f));
            var barrier = new GameObject("FieldBarrier-" + fieldId, typeof(BoxCollider2D));
            barrier.transform.SetParent(parent, false);
            barrier.transform.localPosition = new Vector2(9.5f,4f);
            barrier.GetComponent<BoxCollider2D>().size = new Vector2(2.2f,.45f);
            var gate = Prop(barrier.transform, "ClosedAbilityGate", region == 2 ? art.Tree : art.Fence, Vector2.zero, 2.2f);
            if (gate != null) gate.GetComponent<SpriteRenderer>().color = region == 2 ? new Color(.62f,.86f,.49f) :
                region == 4 ? new Color(.78f,.58f,1f) : new Color(.50f,.78f,1f);
            var fieldMarker = Prop(parent, "伙伴能力支路", Marker("pollen"), new Vector2(9.5f,2.6f), .7f);
            string ability = region == 2 ? "藤蔓生长" : region == 4 ? "晶体共鸣" : "引水";
            var interaction = AddInteraction(parent, layout, "field", fieldId, "支路机关 · " + ability,
                new Vector2(9.5f,2.6f), .75f, onInteract, fieldMarker);
            var openMarker = Prop(parent, "OpenedField-" + fieldId, Marker("opened"), new Vector2(9.5f,3.7f), .7f);
            if (openMarker != null) openMarker.SetActive(false);
            layout.RegisterField(fieldId, barrier, interaction, openMarker);

            string material = region == 2 ? "pollen" : region == 4 ? "crystal" : "dew";
            string cacheId = material + "-" + roomId + "-field-cache";
            var cache = Prop(parent, "支路遗留物资", art.Resource(material), new Vector2(9.5f,6.5f), 1f);
            var pickup = AddInteraction(parent, layout, "resource", cacheId, "拾取支路宝藏 · " + MaterialName(material),
                new Vector2(9.5f,6.5f), .45f, onInteract, cache);
            if (wasCollected != null && wasCollected(cacheId)) pickup.Collect();
        }

        private static void FieldWall(Transform parent, WorldArt art, string name, Vector2 center, Vector2 size)
        {
            AddBox(parent, name, center, size);
            if (size.x > size.y)
            {
                int count = Mathf.CeilToInt(size.x / 2f);
                for (int i = 0; i < count; i++)
                    Prop(parent, name + "Fence", art.Fence,
                        center + new Vector2(-size.x*.5f + (i+.5f)*size.x/count,0f), size.x/count+.1f);
            }
            else
                for (int i = 0; i < 3; i++)
                    Prop(parent, name + "Fence", art.Fence, center + new Vector2(0f,-1.7f+i*1.7f), .8f);
        }

        private static void ConnectRooms(Transform root, WorldArt art, CoastalWorldLayout layout, Action<string, string> onInteract)
        {
            Connect(root, layout, "camp-0", "camp-1", new Vector2(-12f, -1f), new Vector2(12f, 0f), onInteract);
            Connect(root, layout, "camp-0", "camp-2", new Vector2(1f, -7.5f), new Vector2(-12f, 0f), onInteract);
            Connect(root, layout, "camp-0", "coast-0", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract);
            for (int region = 1; region < Regions.Length; region++)
            {
                Connect(root, layout, Regions[region] + "-0", Regions[region] + "-1", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract);
                Connect(root, layout, Regions[region] + "-1", Regions[region] + "-2", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract);
                if (region + 1 < Regions.Length)
                    Connect(root, layout, Regions[region] + "-2", Regions[region + 1] + "-0", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract);
            }
        }

        private static void Connect(Transform root, CoastalWorldLayout layout, string a, string b,
            Vector2 pointA, Vector2 pointB, Action<string, string> onInteract)
        {
            Transform roomA = FindRoom(root, a);
            Transform roomB = FindRoom(root, b);
            var arrowA = Prop(roomA, "出口 · " + b, Marker("exit"), pointA, 1f);
            var arrowB = Prop(roomB, "出口 · " + a, Marker("exit"), pointB, 1f);
            AddInteraction(roomA, layout, "exit", b, "前往 " + layout.GetRoomName(b), pointA, 1.15f, onInteract, arrowA);
            AddInteraction(roomB, layout, "exit", a, "返回 " + layout.GetRoomName(a), pointB, 1.15f, onInteract, arrowB);
        }

        private static Transform FindRoom(Transform parent, string id)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name.StartsWith(id + " ", StringComparison.Ordinal)) return parent.GetChild(i);
            throw new InvalidOperationException("Missing room " + id);
        }

        private static WorldInteraction AddInteraction(Transform parent, CoastalWorldLayout layout, string kind,
            string id, string prompt, Vector2 position, float radius, Action<string, string> onInteract, GameObject visual = null)
        {
            var obj = new GameObject(kind + ":" + id, typeof(CircleCollider2D), typeof(WorldInteraction));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) obj.layer = layer;
            var collider = obj.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = radius;
            var target = obj.GetComponent<WorldInteraction>();
            target.Initialize(kind, id, prompt, onInteract, visual);
            layout.Register(target);
            return target;
        }

        private static GameObject Prop(Transform parent, string name, Sprite sprite, Vector2 position, float width)
        {
            if (sprite == null) return null;
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            float scale = width / Mathf.Max(.001f, sprite.bounds.size.x);
            obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            var material = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            if (material != null) renderer.sharedMaterial = material;
            renderer.sortingOrder = Mathf.RoundToInt(-obj.transform.position.y * 100f);
            return obj;
        }

        private static void Tree(Transform parent, WorldArt art, Vector2 position)
        {
            var tree = Prop(parent, "TreeTrunkCollider", art.Tree, position, 2.6f);
            if (tree == null) return;
            Vector2 size = art.Tree.bounds.size;
            var collider = tree.AddComponent<PolygonCollider2D>();
            collider.points = new[] { new Vector2(-size.x*.10f,0f), new Vector2(size.x*.10f,0f),
                new Vector2(size.x*.13f,size.y*.13f), new Vector2(0f,size.y*.19f), new Vector2(-size.x*.13f,size.y*.13f) };
        }

        private static void Fence(Transform parent, WorldArt art, Vector2 position)
        {
            Prop(parent, "Fence", art.Fence, position, 2.1f);
            AddBox(parent, "FenceFootprint", position + new Vector2(0f,.15f), new Vector2(2.1f,.35f));
        }

        private static void AddBox(Transform parent, string name, Vector2 point, Vector2 size)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = point;
            obj.GetComponent<BoxCollider2D>().size = size;
        }

        private static string RegionName(int region) => new[] { "营地", "海岸", "林地", "潮汐", "晶能", "灯塔" }[region];
        private static string MaterialName(string id) => id == "berry" ? "野果" : id == "dew" ? "露水" : id == "crystal" ? "晶尘" : "花粉";

        // Small map symbols are code-authored UI glyphs, never character or creature substitutes.
        private static Sprite Marker(string kind)
        {
            if (MarkerSprites.TryGetValue(kind, out Sprite cached) && cached != null) return cached;
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = "MapGlyph-" + kind, filterMode = FilterMode.Point };
            var pixels = new Color[16 * 16];
            Color color = kind == "opened" ? new Color(.42f,.96f,.62f) : kind == "berry" ? new Color(.91f,.24f,.26f) : kind == "dew" ? new Color(.40f,.82f,1f) :
                kind == "crystal" ? new Color(.79f,.51f,1f) : kind == "sign" ? new Color(.65f,.42f,.22f) : new Color(1f,.83f,.32f);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    bool filled = kind == "sign" ? ((x >= 2 && x <= 13 && y >= 7 && y <= 13) || (x >= 7 && x <= 9 && y <= 8)) :
                        kind == "exit" ? (y >= 3 && y <= 12 && Math.Abs(x - 7) <= Math.Max(1, (y - 3) / 2)) :
                        Math.Abs(x - 7) + Math.Abs(y - 7) <= 5;
                    if (filled) pixels[y * 16 + x] = color;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0,0,16,16), new Vector2(.5f,0f), 16f);
            MarkerSprites[kind] = sprite;
            return sprite;
        }

        private sealed class WorldArt
        {
            public readonly Sprite House = Load("Environment/House");
            public readonly Sprite Tree = Load("Environment/Tree");
            public readonly Sprite Fence = Load("Environment/Fence");
            public readonly Sprite Campfire = Load("Environment/Campfire");
            public readonly Sprite Signpost = Load("Environment/Signpost");
            public readonly Sprite Shiguang = Load("Actors/Shiguang");
            public readonly Sprite Watcher = First("Actors/Watcher");
            public readonly Sprite FinalBoss = Load("Creatures/GuardianLighthouse");
            public readonly Material LitMaterial = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            private readonly Sprite[] ground = Resources.LoadAll<Sprite>("Art/Tiles/Ground");
            public WorldArt()
            {
                Array.Sort(ground, (a,b) => SpriteIndex(a).CompareTo(SpriteIndex(b)));
                if (ground.Length < 48) throw new InvalidOperationException("Approved Ground sprite tiles have not been imported (48 required).");
            }
            public Sprite Ground(int index) => ground[Mathf.Clamp(index,0,ground.Length-1)];
            public Sprite Floor(int region) => Ground(region == 1 ? 12 : region == 0 || region == 2 ? 0 : 24);
            public Sprite Resource(string id) => Load("Items/" + (id == "berry" ? "Berry" : id == "dew" ? "Dew" : id == "crystal" ? "Crystal" : "Pollen")) ?? Marker(id);
            public Sprite Wild(int region, int index) => Load("Creatures/" + (region == 1 ? (index == 0 ? "WildCrab" : "WildBird") :
                region == 2 ? "MossLumi" : region == 3 ? "Moatail" : "CrystalGecko"));
            public Sprite Guardian(int region) => Load("Creatures/Guardian" + (region == 5 ? "Lighthouse" : region == 2 ? "Grove" : region == 3 ? "Tide" : "Crystal"));
            private static int SpriteIndex(Sprite sprite)
            {
                string name = sprite.name;
                return int.TryParse(name.Substring(name.LastIndexOf('_')+1),out int index) ? index : 0;
            }
            private static Sprite Load(string path) => Resources.Load<Sprite>("Art/" + path);
            private static Sprite First(string path)
            {
                var sprites = Resources.LoadAll<Sprite>("Art/" + path);
                return sprites.Length > 0 ? sprites[0] : null;
            }
        }
    }

    public sealed class CoastalWorldLayout
    {
        private readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        private readonly List<WorldInteraction> interactions = new List<WorldInteraction>();
        private readonly Dictionary<string, FieldRoute> fields = new Dictionary<string, FieldRoute>();
        public Vector2 Spawn { get; internal set; }
        public Rect CameraBounds { get; internal set; }
        public IReadOnlyList<WorldInteraction> Interactions => interactions;
        public IEnumerable<string> RoomIds => rooms.Keys;
        internal void AddRoom(string id, string name, Vector2 spawn, Rect bounds) => rooms.Add(id, new Room { Name = name, Spawn = spawn, Bounds = bounds });
        internal void Register(WorldInteraction interaction) => interactions.Add(interaction);
        internal void RegisterField(string id, GameObject barrier, WorldInteraction interaction, GameObject openMarker)
            => fields.Add(id, new FieldRoute { Barrier = barrier, Interaction = interaction, OpenMarker = openMarker });
        public Vector2 GetSpawn(string id) => rooms.TryGetValue(id ?? "", out var room) ? room.Spawn : rooms["camp-0"].Spawn;
        public Rect GetBounds(string id) => rooms.TryGetValue(id ?? "", out var room) ? room.Bounds : rooms["camp-0"].Bounds;
        public string GetRoomName(string id) => rooms.TryGetValue(id ?? "", out var room) ? room.Name : "海岸营地";
        public string GetRoomId(Vector2 point)
        {
            foreach (var pair in rooms) if (pair.Value.Bounds.Contains(point)) return pair.Key;
            return "camp-0";
        }
        public void SetCollected(string id)
        {
            foreach (var target in interactions) if (target.Id == id) target.Collect();
        }
        public void ResetCollected(Func<string, bool> wasCollected)
        {
            foreach (var target in interactions)
                if (target.Kind == "resource") target.SetAvailable(wasCollected == null || !wasCollected(target.Id));
        }
        public bool OpenField(string id)
        {
            if (!fields.TryGetValue(id ?? "", out var route)) return false;
            SetFieldState(route, true);
            return true;
        }
        public bool IsFieldOpen(string id) => fields.TryGetValue(id ?? "", out var route) && route.Open;
        public void ResetFields(Func<string, bool> isOpen)
        {
            foreach (var pair in fields) SetFieldState(pair.Value, isOpen != null && isOpen(pair.Key));
        }
        private static void SetFieldState(FieldRoute route, bool open)
        {
            route.Open = open;
            route.Barrier.SetActive(!open);
            route.Interaction.SetAvailable(!open);
            if (route.OpenMarker != null) route.OpenMarker.SetActive(open);
        }
        private sealed class FieldRoute { public GameObject Barrier; public WorldInteraction Interaction; public GameObject OpenMarker; public bool Open; }
        private sealed class Room { public string Name; public Vector2 Spawn; public Rect Bounds; }
    }

    public sealed class WorldInteraction : MonoBehaviour, IWorldInteractable
    {
        private Action<string, string> callback;
        private GameObject visual;
        public string Kind { get; private set; }
        public string Id { get; private set; }
        public string Prompt { get; private set; }
        public string InteractionPrompt => Prompt;
        public bool CanInteract { get; private set; } = true;
        internal void Initialize(string kind, string id, string prompt, Action<string, string> onInteract, GameObject representation)
        { Kind = kind; Id = id; Prompt = prompt; callback = onInteract; visual = representation; }
        public void Interact() { if (CanInteract) callback?.Invoke(Kind, Id); }
        public void Collect() => SetAvailable(false);
        public void SetAvailable(bool available)
        {
            CanInteract = available;
            var shape = GetComponent<Collider2D>();
            if (shape != null) shape.enabled = available;
            if (visual != null) visual.SetActive(available);
        }
    }
}
