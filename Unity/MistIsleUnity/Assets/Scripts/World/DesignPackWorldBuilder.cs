using System;
using System.Collections.Generic;
using System.Linq;
using MistIsle.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MistIsle.World
{
    /// <summary>Five full-size maps from the supplied design data, isolated from the historical room builder.</summary>
    public static class DesignPackWorldBuilder
    {
        public static readonly string[] LayerNames = {
            "Ground", "Water", "TerrainBack", "Collision", "PropsBack", "Objects", "Characters", "PropsFront", "Triggers", "VFXMarkers"
        };
        public static readonly string[] CollisionCorrections = {
            "WorldConnections controls target spawn/facing; Layout.exits.spawn is not consistently a target coordinate.",
            "Cut every explicit exit rectangle and its 1-tile interior apron out of coarse collision and deep water.",
            "Connect every WorldConnections incoming spawn to its nearest supplied route before applying deep-water collision.",
            "Cut a 2-tile half-width corridor along supplied routes where coarse blockers overlap those routes.",
            "POI access spurs join the nearest supplied route; building interaction approaches are separate from exact source anchors.",
            "Keep source building rectangles as reference zones; reviewed M00 ground-contact polygons replace their coarse solid fill.",
            "Water is a blueprint-derived first-pass overlay, with walkable route crossings rendered as bridge/dock decking.",
            "The M02 root bridge's source footprint is a flooded gap until its field gate restores the decking.",
            "The M04 gate has a 14-tile opening at its source anchor, flanked by visible permanent tier-cliff wings.",
            "M01/M02 Blueprint files are interchanged; their JSON map IDs, dimensions and coordinates remain authoritative."
        };

        public static CoastalWorldLayout Build(Transform parent, Action<string, string> onInteract, Func<string, bool> wasCollected = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (parent.Find("DesignPackWorld") != null) throw new InvalidOperationException("The design-pack world is already built.");
            var root = Child(parent, "DesignPackWorld");
            var layout = new CoastalWorldLayout();
            var art = new Art();
            foreach (var map in DesignPackCatalog.Maps)
            {
                var mapRoot = Child(root, map.Id + " — " + map.Name);
                mapRoot.position = map.Origin;
                layout.AddRoom(map.RoomId, map.Name, map.Spawn, map.Bounds);
                var layers = CreateLayers(mapRoot, art.Material);
                var plan = new TerrainPlan(map);
                BuildTiles(map, layers, plan, art);
                BuildFoundations(map, layers["Collision"]);
                BuildLandmarks(map, layers, layout, art, plan);
                BuildInteractions(map, layers, layout, art, onInteract, wasCollected);
                if (map.Id == "M02") DesignPackWorldConsequences.Attach(mapRoot);
                BuildExits(map, layers["Triggers"], layout, art, onInteract);
                SetOpeningLandmarks(map, layout);
                DesignPackAtmosphere.Attach(mapRoot, map.Id, map.Bounds);
                if (map.Id == "M00") DesignPackCoastalComposition.Attach(mapRoot);
            }
            layout.Spawn = layout.GetSpawn("camp-0");
            layout.CameraBounds = layout.GetBounds("camp-0");
            Physics2D.SyncTransforms();
            return layout;
        }

        private static Dictionary<string, Transform> CreateLayers(Transform parent, Material material)
        {
            var grid = parent.gameObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            var result = new Dictionary<string, Transform>();
            for (int i = 0; i < LayerNames.Length; i++)
            {
                var layer = Child(parent, LayerNames[i]);
                var map = layer.gameObject.AddComponent<Tilemap>();
                map.tileAnchor = new Vector3(.5f, .5f, 0f);
                var renderer = layer.gameObject.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = -32000 + i * 100;
                if (material != null) renderer.sharedMaterial = material;
                if (LayerNames[i] == "Collision" || LayerNames[i] == "Triggers" || LayerNames[i] == "VFXMarkers") renderer.enabled = false;
                result.Add(LayerNames[i], layer);
            }
            return result;
        }

        private static void BuildTiles(DesignPackMap map, Dictionary<string, Transform> layers, TerrainPlan plan, Art art)
        {
            int size = map.Width * map.Height;
            var ground = new TileBase[size]; var water = new TileBase[size];
            var cliff = new TileBase[size]; var collision = new TileBase[size];
            var tiles = art.Tiles(map.Index);
            Tile collider = ScriptableObject.CreateInstance<Tile>(); collider.name = "DesignPackSolidCell"; collider.colliderType = Tile.ColliderType.Grid;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    int index = y * map.Width + x;
                    Vector2 point = new Vector2(x + .5f, y + .5f);
                    bool route = plan.OnRoute(point, 1.8f);
                    bool wet = plan.Water(point);
                    bool rootGap = plan.RootBridgeGap(point);
                    bool dockRamp = map.Index == 0 && point.y < 11f && point.x > 5f && point.x < 13f;
                    bool coastalDeepWater = map.Index == 0 && wet && !dockRamp;
                    // Access spurs lead to shore pickups; they must not create new piers
                    // or punch holes in deep-water collision beside the one authored dock.
                    if (coastalDeepWater) route = false;
                    int variant = (x * 3 + y * 5) % 4;
                    int row = 0;
                    if (map.Index == 0 && point.y < 12f) row = 2;
                    if (plan.Plaza(point)) row = map.Index <= 1 ? 2 : 3;
                    if (route) row = wet || dockRamp ? 4 : map.Index >= 3 ? 3 : 1;
                    // Row 1's first four cells are grass/dirt transitions, not full path
                    // surfaces. Water row 5 also mixes shallow and deep hues. Curate
                    // continuous surfaces instead of alternating incompatible cells.
                    int cell = row == 1 ? 12 + (variant % 2) : row == 0 ? 4 : row * 8 + variant;
                    ground[index] = tiles[cell];
                    if (wet && (!route || rootGap)) water[index] = tiles[44 + variant % 2];
                    if (coastalDeepWater || plan.Blocked(point))
                    {
                        collision[index] = collider;
                        if (!wet) cliff[index] = tiles[6 * 8 + variant];
                    }
                }
            var bounds = new BoundsInt(0, 0, 0, map.Width, map.Height, 1);
            layers["Ground"].GetComponent<Tilemap>().SetTilesBlock(bounds, ground);
            layers["Water"].GetComponent<Tilemap>().SetTilesBlock(bounds, water);
            layers["TerrainBack"].GetComponent<Tilemap>().SetTilesBlock(bounds, cliff);
            var solid = layers["Collision"].GetComponent<Tilemap>(); solid.SetTilesBlock(bounds, collision);
            var body = solid.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            var composite = solid.gameObject.AddComponent<CompositeCollider2D>(); composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var cells = solid.gameObject.AddComponent<TilemapCollider2D>(); cells.compositeOperation = Collider2D.CompositeOperation.Merge;
            cells.ProcessTilemapChanges(); composite.GenerateGeometry();
        }

        private static void BuildFoundations(DesignPackMap map, Transform collision)
        {
            if (map.Id != "M00") return;
            // The source rectangles are coarse zoning references. These reviewed ground
            // outlines keep awnings, roofs, front steps and surrounding transparent canvas walkable.
            Foundation(collision, "M00_SUPPLY_SHOP", new[] {
                new Vector2(13.05f,27.05f), new Vector2(19.95f,27.05f), new Vector2(19.95f,28.65f),
                new Vector2(19.60f,29.10f), new Vector2(13.40f,29.10f), new Vector2(13.05f,28.65f) });
            Foundation(collision, "M00_TRAVELER_HUT", new[] {
                new Vector2(24.55f,37.05f), new Vector2(32.85f,37.05f), new Vector2(33.55f,38.05f),
                new Vector2(33.15f,39.35f), new Vector2(25.35f,39.45f), new Vector2(24.55f,38.55f) });
            // Reviewed against the independent wooden shed: the roof is visual only;
            // the stepped front and both side approaches remain outside the ground hull.
            Foundation(collision, "M00_STORAGE", new[] {
                new Vector2(47.15f,35.50f), new Vector2(49.05f,35.50f), new Vector2(49.25f,35.10f),
                new Vector2(52.20f,35.10f), new Vector2(54.65f,35.60f), new Vector2(54.65f,37.30f),
                new Vector2(53.95f,37.85f), new Vector2(48.25f,37.90f), new Vector2(47.15f,36.70f) });
        }

        private static void Foundation(Transform parent, string sourceId, Vector2[] points)
        {
            var foundation = Child(parent, "Foundation-" + sourceId);
            foundation.gameObject.AddComponent<PolygonCollider2D>().points = points;
        }

        private static bool IsBuildingFoundation(Rect rect) => rect.xMin == 12f || rect.xMin == 24f || rect.xMin == 47f;

        private static void BuildLandmarks(DesignPackMap map, Dictionary<string, Transform> layers, CoastalWorldLayout layout, Art art, TerrainPlan plan)
        {
            foreach (var node in map.Nodes)
            {
                var anchor = Child(layers["Objects"], node.SourceId + " — " + node.DisplayName);
                anchor.position = node.Position;
                layout.AddLandmark(node.SourceId, node.Position);
                Sprite sprite = null; float height = 1f; Vector2 placement = node.Position - map.Origin;
                switch (node.SourceId)
                {
                    case "M00_SPAWN_DOCK": sprite = art.Get("MapDesignAdditions/Dock") ?? art.Get("Environment/DockPlatform"); height = 5f; placement = new Vector2(9f, 3f); break;
                    case "M00_SUPPLY_SHOP": sprite = art.Get("MapDesignAdditions/SupplyShopV1") ?? art.Get("Environment/CampSupply"); height = 6f; placement = new Vector2(16.5f, 27f); break;
                    case "M00_TRAVELER_HUT": sprite = art.Get("Environment/House"); height = 9f; placement = new Vector2(29f, 35f); break;
                    case "M00_STORAGE": sprite = art.Get("MapDesignAdditions/StorageShedV1"); height = 6.5f; placement = new Vector2(51f, 35f); break;
                    case "M00_SHIGUANG_STATION": sprite = art.Get("MapDesignAdditions/GuideStationV1"); height = 4.6f; placement = new Vector2(45f, 31.5f); break;
                    case "M00_NOTICE": sprite = art.Get("Environment/Signpost"); height = 1.7f; break;
                    case "M01_BRIDGE": sprite = art.Get("MapDesignAdditions/Bridge") ?? art.Get("Environment/DockPlatform"); height = 3.5f; placement = new Vector2(38f, 28.5f); break;
                    case "M01_LOOKOUT": sprite = art.Get("Environment/DockPlatform"); height = 5f; placement = new Vector2(61f, 41f); break;
                    case "M02_ANCIENT_GROVE": sprite = art.Get("Environment/Tree"); height = 10f; placement = new Vector2(47f, 45f); break;
                    case "M02_WATERFALL_LOOKOUT": sprite = art.Get("Environment/DockPlatform"); height = 4f; break;
                    case "M03_TIDE_TEMPLE": sprite = art.Get("Environment/StoneWall"); height = 1f; placement = new Vector2(29f, 61f); break;
                    case "M03_CRYSTAL_ARCHIVE": sprite = art.Get("Environment/StoneWall"); height = 1f; placement = new Vector2(91f, 40f); break;
                    case "M03_FLOATING_PLATFORMS": sprite = art.Get("Environment/DockPlatform"); height = 3f; break;
                }
                if (sprite != null)
                {
                    var visual = Prop(layers["PropsBack"], node.SourceId + "_Art", sprite, placement, height, art.Material);
                    if (node.SourceId == "M00_SPAWN_DOCK" || node.SourceId == "M01_BRIDGE" || node.SourceId == "M01_LOOKOUT" || node.SourceId == "M02_WATERFALL_LOOKOUT")
                        visual.GetComponent<SpriteRenderer>().sortingOrder = -29900;
                }
                if (node.SourceId == "M00_SHIGUANG_STATION")
                {
                    Foundation(layers["Collision"], "M00_SHIGUANG_STATION", new[] {
                        new Vector2(43.35f,31.58f), new Vector2(46.15f,31.58f), new Vector2(46.8f,32f),
                        new Vector2(46.8f,32.85f), new Vector2(43.35f,32.85f) });
                    Box(layers["Collision"], "GuideStationLeftPost", new Vector2(42.85f,32.1f), new Vector2(.3f,.5f));
                    Box(layers["Collision"], "GuideStationRightPost", new Vector2(47.15f,32.15f), new Vector2(.35f,.5f));
                }
                if (node.SourceId == "M02_ANCIENT_GROVE")
                    Box(layers["Collision"], "AncientTreeTrunk", placement + Vector2.up * .45f, new Vector2(1.8f, .9f));
                if (node.SourceId == "M00_SPAWN_DOCK")
                    Prop(layers["PropsBack"], "ArrivalBoat", art.Get("Environment/HarborBoat"), new Vector2(13f, 3.2f), 3.8f, art.Material);
            }
            // Give the supplied blocked ledges an actual cliff face. These sprites stay
            // on existing solid cells; they never invent a new impassable boundary.
            foreach (Rect rect in map.CollisionRects)
            {
                if (map.Id == "M00" && IsBuildingFoundation(rect)) continue;
                var ledges = new List<Vector2>();
                if (rect.width > 20f && rect.height <= 3f)
                    for (float x = rect.xMin + 2f; x < rect.xMax - 1f; x += 3.8f) ledges.Add(new Vector2(x, rect.yMin + .2f));
                else if (rect.height > 20f && rect.width <= 5f)
                    for (float y = rect.yMin + 2f; y < rect.yMax - 3f; y += 3.8f) ledges.Add(new Vector2(rect.center.x, y));
                else ledges.Add(new Vector2(rect.center.x, rect.yMin + .2f));
                foreach (var point in ledges)
                    if (plan.Blocked(point) && !plan.Water(point) && !plan.OnRoute(point, 3.5f) && !plan.NearNode(point, 4f))
                        Prop(layers["TerrainBack"], "AuthoredCliffFace", art.Get("Environment/GrassCliff"), point, 3.4f, art.Material);
            }
            if (map.Id == "M00")
            {
                foreach (var point in new[] { new Vector2(23f, 27f), new Vector2(37f, 27f), new Vector2(39f, 31f), new Vector2(48f, 31f) })
                    Prop(layers["PropsBack"], "CourtyardLantern", art.Get("Environment/Lantern"), point, 1.8f, art.Material);
                foreach (var point in new[] { new Vector2(9f, 14f), new Vector2(12f, 15f), new Vector2(51f, 36f), new Vector2(54f, 36f) })
                    Prop(layers["PropsBack"], "UsefulSupplyCrates", art.Get("Environment/Crate"), point, 1.2f, art.Material);
                foreach (var point in new[] { new Vector2(38f, 32f), new Vector2(41f, 33f), new Vector2(44f, 34f) })
                {
                    Prop(layers["PropsBack"], "CompanionGardenFence", art.Get("Environment/FenceTimber"), point, 1.5f, art.Material);
                    Box(layers["Collision"], "CompanionGardenFenceFootprint", point + Vector2.up * .12f, new Vector2(3.2f, .25f));
                }
                foreach (var point in new[] { new Vector2(29f, 22f), new Vector2(35f, 23f) })
                    Prop(layers["PropsBack"], "CampfireSeat", art.Get("Environment/FireSeat"), point, 1.2f, art.Material);
            }
            // Deliberate groves follow the map's perimeter and its blocked ledges, leaving
            // every supplied route and interaction approach clear. Trunk footprints are
            // authored independently from the transparent canopy rectangles.
            for (int band = 0; band < 3; band++)
            {
                int count = map.Index == 2 ? 28 : map.Index == 0 ? 14 : 19;
                for (int i = 0; i < count; i++)
                {
                    float t = (i + .5f) / count;
                    Vector2 point = band == 0 ? new Vector2(4f + t * (map.Width - 8f), map.Height - 5f - (i % 3)) :
                        band == 1 ? new Vector2(5f + (i % 3) * 1.3f, 11f + t * (map.Height - 19f)) :
                        new Vector2(map.Width - 6f - (i % 3) * 1.2f, 10f + t * (map.Height - 19f));
                    if (plan.OnRoute(point, 4.5f) || plan.NearNode(point, 4f) || plan.Water(point)) continue;
                    Prop(layers["PropsFront"], "PerimeterGrove", art.Get("Environment/Tree"), point, map.Index == 2 ? 5.3f : 4.4f, art.Material);
                    Box(layers["Collision"], "TreeTrunkFootprint", point + Vector2.up * .3f, new Vector2(.9f, .6f));
                    Prop(layers["PropsBack"], "GroveUndergrowth", art.Get(i % 2 == 0 ? "Environment/BushWhite" : "Environment/BushPink"),
                        point + new Vector2(1.5f, -.5f), 1.3f, art.Material);
                }
            }
            foreach (var route in map.Routes)
                for (int i = 1; i < route.Length; i++)
                {
                    Vector2 delta = (route[i] - route[i - 1]).normalized;
                    Vector2 point = route[i] + new Vector2(-delta.y, delta.x) * 3.4f;
                    if (plan.NearNode(point, 3f) || plan.Water(point)) continue;
                    Prop(layers["PropsBack"], "RouteFlowerClump", art.Get(map.Index >= 2 ? "Environment/FlowerBlue" : "Environment/FlowerWhite"), point, 1.15f, art.Material);
                    Prop(layers["PropsBack"], "RouteStone", art.Get("Environment/RockSmall"), point + new Vector2(.85f, .4f), .65f, art.Material);
                }
        }

        private static void BuildInteractions(DesignPackMap map, Dictionary<string, Transform> layers, CoastalWorldLayout layout,
            Art art, Action<string, string> onInteract, Func<string, bool> wasCollected)
        {
            foreach (var node in map.Nodes)
            {
                if (node.Kind == "") continue;
                Vector2 local = node.ApproachPosition - map.Origin;
                bool actor = node.Kind == "npc" || node.Kind == "starter" || node.Kind == "guardian" || node.Kind == "boss" ||
                    (node.Kind == "encounter" && node.EncounterSpeciesIds.Length == 1);
                if (actor)
                {
                    Sprite[] frames = node.Kind == "npc" ? art.Frames(node.SpriteKey) : new[] { art.Get(node.SpriteKey != "" ? node.SpriteKey : DefaultCreature(map.Id)) };
                    CreateActor(layers["Characters"], layout, node, frames, local, art.Material, onInteract);
                    if (wasCollected != null && wasCollected(node.Id)) layout.SetActorVisible(node.Id, false);
                    continue;
                }
                if (node.Kind == "field" || node.Type == "gated_door")
                {
                    BuildGate(map, layers, layout, node, local, art, onInteract);
                    continue;
                }
                GameObject visual = null;
                if (node.Kind == "resource")
                    visual = Prop(layers["Objects"], node.Id + "_Pickup", node.SpriteKey != "" ? art.Get(node.SpriteKey) : art.Resource(node.ItemId),
                        local, node.ItemId == "shell" ? .55f : node.ItemId == "crystal" ? 1f : .8f, art.Material);
                else if (node.Kind == "encounter")
                    visual = Prop(layers["Objects"], node.Id + "_EncounterGrass", art.Get("Environment/BushWhite"), local, 1.8f, art.Material);
                else if (node.Kind == "chest")
                    visual = Prop(layers["Objects"], node.Id + "_Chest", art.Get("Environment/Crate"), local, 1.2f, art.Material);
                else if (node.Kind == "campfire")
                    visual = Prop(layers["Objects"], node.Id + "_Campfire", art.Get("Environment/CampfireRing") ?? art.Get("Environment/Campfire"), local, 2.3f, art.Material);
                var interaction = Interaction(layers["Triggers"], layout, node.Kind, node.Id, node.DisplayName, local, .6f, onInteract, visual);
                DesignPackWorldConsequences.DecorateRecord(interaction.transform, node);
                if (node.Kind == "campfire")
                {
                    var foot = Box(layers["Collision"], node.Id + "_Foot", local + Vector2.up * .22f, new Vector2(.7f, .45f));
                    interaction.RegisterInteractionBody(foot);
                }
                if ((node.Kind == "resource" || node.Kind == "chest") && wasCollected != null && wasCollected(node.Id)) interaction.Collect();
            }
        }

        private static string DefaultCreature(string map) => map == "M01" ? "Creatures/WildBird" : map == "M02" ? "Creatures/wild-04" : "Creatures/wild-07";

        private static void CreateActor(Transform parent, CoastalWorldLayout layout, DesignPackNode node, Sprite[] frames, Vector2 point,
            Material material, Action<string, string> onInteract)
        {
            if (frames.Length == 0 || frames[0] == null) throw new InvalidOperationException("Missing approved world sprite for " + node.Id + ": " + node.SpriteKey);
            var actor = Child(parent, node.Id); actor.localPosition = point;
            var sprite = Prop(actor, "Sprite", frames[0], Vector2.zero,
                node.Kind == "npc" ? 1.62f : node.Kind == "guardian" || node.Kind == "boss" ? (node.MapId == "M04" ? 2.7f : 3.3f) : 1f, material);
            var renderer = sprite.GetComponent<SpriteRenderer>();
            if (node.Kind == "npc") ActorVisualSizing.ApplyHuman(renderer, frames[0]);
            var foot = actor.gameObject.AddComponent<CapsuleCollider2D>(); foot.direction = CapsuleDirection2D.Horizontal;
            foot.size = node.Kind == "npc" ? new Vector2(.45f, .32f) : new Vector2(.4f, .26f); foot.offset = Vector2.up * foot.size.y * .5f;
            actor.gameObject.AddComponent<WorldActorPresentation>().Configure(renderer, frames);
            var interaction = Interaction(actor, layout, node.Kind, node.Id, node.DisplayName, Vector2.up * .15f, .6f, onInteract);
            interaction.RegisterInteractionBody(foot); layout.RegisterActor(node.Id, actor);
        }

        private static void BuildGate(DesignPackMap map, Dictionary<string, Transform> layers, CoastalWorldLayout layout, DesignPackNode node, Vector2 point, Art art, Action<string, string> onInteract)
        {
            Transform parent = layers["Objects"];
            var barrier = Child(parent, "FieldBarrier-" + node.Id); barrier.localPosition = point;
            Vector2 size = node.Id == "M04_THREE_SEAL_GATE" ? new Vector2(14f, .8f) :
                node.FieldAbility == "verdant" ? new Vector2(7f, .7f) : new Vector2(5f, .7f);
            var body = barrier.gameObject.AddComponent<BoxCollider2D>(); body.size = size;
            if (node.Id == "M04_THREE_SEAL_GATE") BuildLighthouseGateWings(map, layers, point, art);
            float gateHeight = node.FieldAbility == "verdant" || node.Id == "M04_THREE_SEAL_GATE" ? 2.1f : .8f;
            var closed = Prop(barrier, "ClosedGate", art.Get(node.FieldAbility == "verdant" ? "Environment/Log" : "Environment/StoneWall"), Vector2.zero, gateHeight, art.Material);
            if (closed != null && node.Id == "M04_THREE_SEAL_GATE")
            {
                float visualWidth = closed.GetComponent<SpriteRenderer>().bounds.size.x;
                var scale = closed.transform.localScale; scale.x *= size.x / Mathf.Max(.01f, visualWidth); closed.transform.localScale = scale;
            }
            if (node.Id == "M04_THREE_SEAL_GATE")
                Prop(barrier, "ThreeSealGateStoneFrame", art.Get("MapDesignAdditions/StoneArch"), Vector2.down * .3f, 6f, art.Material);
            var open = Prop(parent, "OpenGate-" + node.Id, art.Get("Environment/FlowerBlue"), point, .8f, art.Material);
            if (open != null) open.SetActive(false);
            float bankOffset = node.Id == "M02_ROOT_BRIDGE" ? node.Footprint.y * .5f + 1.1f : 1.1f;
            var interaction = Interaction(parent, layout, node.Kind, node.Id, node.DisplayName, point + Vector2.down * bankOffset, .65f, onInteract);
            interaction.RegisterInteractionBody(body);
            layout.RegisterField(node.Id, barrier.gameObject, interaction, open);
            var mechanism = barrier.gameObject.AddComponent<DesignPackFieldMechanism>();
            mechanism.Configure(node, parent.parent, art.Tiles(map.Index)[node.Id == "M02_ROOT_BRIDGE" ? 34 : 25]);
        }

        private static void BuildLighthouseGateWings(DesignPackMap map, Dictionary<string, Transform> layers, Vector2 point, Art art)
        {
            const float halfOpening = 7f;
            foreach (var span in new[] { new Vector2(1f, point.x - halfOpening), new Vector2(point.x + halfOpening, map.Width - 1f) })
            {
                var centre = new Vector2((span.x + span.y) * .5f, point.y);
                Box(layers["Collision"], "LighthouseTierCliffFootprint", centre, new Vector2(span.y - span.x, 1.2f));
                for (float x = span.x + 1.6f; x < span.y; x += 3.2f)
                    Prop(layers["TerrainBack"], "LighthouseTierCliffFace", art.Get("Environment/GrassCliff"), new Vector2(x, point.y - .35f), 3.3f, art.Material);
            }
        }

        private static void BuildExits(DesignPackMap map, Transform parent, CoastalWorldLayout layout, Art art, Action<string, string> onInteract)
        {
            foreach (var exit in map.Exits)
            {
                var trigger = Child(parent, exit.Id + " — " + exit.TargetMap);
                SetInteractionLayer(trigger.gameObject);
                trigger.position = exit.TriggerRect.center;
                var collider = trigger.gameObject.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = exit.TriggerRect.size;
                var interaction = trigger.gameObject.AddComponent<WorldInteraction>();
                interaction.Initialize("exit", exit.TargetRoom, "前往" + DesignPackCatalog.GetMap(exit.TargetMap).Name, onInteract, null);
                layout.Register(interaction);
                trigger.gameObject.AddComponent<DesignPackExitTrigger>().Configure(onInteract, exit.TargetRoom);
                Vector2 point = exit.TriggerRect.center - map.Origin;
                Vector2 inward = point.x < 3f ? Vector2.right : point.x > map.Width - 3f ? Vector2.left : point.y < 3f ? Vector2.up : Vector2.down;
                Vector2 edge = point;
                if (inward.x > 0f) edge.x = .2f; else if (inward.x < 0f) edge.x = map.Width - .2f;
                else if (inward.y > 0f) edge.y = .2f; else edge.y = map.Height - .2f;
                Vector2 size = inward.x == 0f ? new Vector2(exit.TriggerRect.width, .4f) : new Vector2(.4f, exit.TriggerRect.height);
                var barrier = Box(parent, "ExitBarrier-" + exit.Id, edge, size);
                interaction.RegisterInteractionBody(barrier);
                layout.RegisterBarrierOnly(exit.Id, barrier.gameObject);
                Prop(parent, exit.Id + "_Signpost", art.Get("Environment/Signpost"), point + inward * 2.2f + new Vector2(inward.y, -inward.x) * 2f, 1.6f, art.Material);
                layout.AddLandmark(exit.Id, exit.TriggerRect.center);
            }
        }

        private static void SetOpeningLandmarks(DesignPackMap map, CoastalWorldLayout layout)
        {
            var points = new Dictionary<string, Vector2>();
            if (map.Id == "M00") {
                points.Add("arrival-dock", new Vector2(9f, 5f)); points.Add("dock-arrival", new Vector2(9f, 5f));
                points.Add("watcher-approach", new Vector2(27f, 31.5f));
                points.Add("shiguang-home", new Vector2(43f, 30f)); points.Add("shiguang-approach", new Vector2(43f, 28.5f));
                points.Add("starter-meeting", new Vector2(43f, 27f)); points.Add("campfire-approach", new Vector2(32f, 22.3f));
                points.Add("guide-camp-waypoint", new Vector2(50f, 25f)); points.Add("guide-exit-wait", new Vector2(59f, 25.8f));
                points.Add("coast-exit", new Vector2(62f, 24f));
            }
            if (map.Id == "M01") {
                points.Add("coast-arrival", new Vector2(3f, 28f)); points.Add("coast-waypoint-1", new Vector2(12f, 28f));
                points.Add("coast-waypoint-2", new Vector2(19f, 32f)); points.Add("coast-waypoint-3", new Vector2(28f, 34f));
                points.Add("tutorial-viewpoint", new Vector2(31f, 35f)); points.Add("tutorial-coast-crab", new Vector2(28f, 39f));
            }
            foreach (var point in points) layout.AddLandmark(point.Key, map.Origin + point.Value);
        }

        private static WorldInteraction Interaction(Transform parent, CoastalWorldLayout layout, string kind, string id, string prompt,
            Vector2 point, float radius, Action<string, string> callback, GameObject visual = null)
        {
            var target = Child(parent, kind + "-" + id); target.localPosition = point;
            SetInteractionLayer(target.gameObject);
            var circle = target.gameObject.AddComponent<CircleCollider2D>(); circle.isTrigger = true; circle.radius = radius;
            var interaction = target.gameObject.AddComponent<WorldInteraction>(); interaction.Initialize(kind, id, prompt, callback, visual);
            layout.Register(interaction); return interaction;
        }
        private static Transform Child(Transform parent, string name)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
        private static void SetInteractionLayer(GameObject target)
        { int layer = LayerMask.NameToLayer("Interactable"); if (layer >= 0) target.layer = layer; }
        private static BoxCollider2D Box(Transform parent, string name, Vector2 point, Vector2 size)
        { var child = Child(parent, name); child.localPosition = point; var body = child.gameObject.AddComponent<BoxCollider2D>(); body.size = size; return body; }
        private static GameObject Prop(Transform parent, string name, Sprite sprite, Vector2 point, float height, Material material)
        {
            if (sprite == null) return null;
            var child = Child(parent, name); child.localPosition = point;
            var renderer = child.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            if (material != null) renderer.sharedMaterial = material;
            float scale = height / Mathf.Max(.01f, sprite.bounds.size.y);
            child.localScale = Vector3.one * scale;
            child.localPosition -= new Vector3(sprite.bounds.center.x * scale, sprite.bounds.min.y * scale, 0f);
            renderer.sortingOrder = Mathf.RoundToInt(-(parent.position.y + point.y) * 100f);
            return child.gameObject;
        }

        private sealed class TerrainPlan
        {
            private readonly DesignPackMap map;
            private readonly List<Vector2[]> corridors = new List<Vector2[]>();
            public TerrainPlan(DesignPackMap data)
            {
                map = data; corridors.AddRange(map.Routes);
                foreach (var node in map.Nodes)
                {
                    Vector2 target = node.ApproachPosition - map.Origin;
                    if (node.Type == "spawn" || node.Type == "spawn_transition" || node.Type == "puzzle_zone" || node.Type == "main_route") continue;
                    Vector2 nearest = target; float distance = float.MaxValue;
                    foreach (var route in map.Routes)
                        for (int i = 1; i < route.Length; i++) {
                            Vector2 point = Closest(target, route[i - 1], route[i]); float d = Vector2.SqrMagnitude(point - target);
                            if (d < distance) { distance = d; nearest = point; }
                        }
                    corridors.Add(new[] { nearest, target });
                }
                foreach (var exit in map.Exits) {
                    Vector2 target = exit.TriggerRect.center - map.Origin;
                    Vector2 nearest = map.Routes.SelectMany(route => route).OrderBy(point => Vector2.SqrMagnitude(point - target)).First();
                    corridors.Add(new[] { nearest, target });
                }
                foreach (var arrival in DesignPackCatalog.Maps.SelectMany(item => item.Exits).Where(exit => exit.TargetMap == map.Id))
                {
                    Vector2 target = arrival.Spawn - map.Origin;
                    Vector2 nearest = target; float distance = float.MaxValue;
                    foreach (var route in map.Routes)
                        for (int i = 1; i < route.Length; i++) {
                            Vector2 point = Closest(target, route[i - 1], route[i]); float d = Vector2.SqrMagnitude(point - target);
                            if (d < distance) { distance = d; nearest = point; }
                        }
                    corridors.Add(new[] { nearest, target });
                }
                if (map.Id == "M00") corridors.Add(new[] { new Vector2(32f, 22.3f), new Vector2(50f, 25f), new Vector2(59f, 25.8f) });
                if (map.Id == "M01") corridors.Add(new[] { new Vector2(3f, 29f), new Vector2(12f, 28f), new Vector2(19f, 32f), new Vector2(28f, 34f), new Vector2(31f, 35f) });
            }
            public bool NearNode(Vector2 point, float radius) => map.Nodes.Any(node => Vector2.Distance(point, node.ApproachPosition - map.Origin) < radius);
            public bool OnRoute(Vector2 point, float radius)
            {
                foreach (var route in corridors)
                    for (int i = 1; i < route.Length; i++) if (Vector2.Distance(point, Closest(point, route[i - 1], route[i])) <= radius) return true;
                return false;
            }
            public bool Plaza(Vector2 point)
            {
                foreach (var node in map.Nodes)
                {
                    if (node.Type != "save_craft" && node.Type != "guardian_arena" && node.Type != "boss_arena" && node.Type != "hub_puzzle" && node.Type != "seal_insert") continue;
                    Vector2 delta = point - (node.Position - map.Origin);
                    if (delta.sqrMagnitude < (node.Type == "save_craft" ? 25f : 49f)) return true;
                }
                return false;
            }
            public bool Water(Vector2 point)
            {
                if (RootBridgeGap(point)) return true;
                switch (map.Id) {
                    case "M00":
                        float shore = 8f + Mathf.Sin(point.x * .12f) * 1.6f;
                        // The supplied shell bank is a beach, not an unplanned second dock.
                        // Keep its original gathering anchor on land beside the low eastern shore.
                        if (point.x > 48f) shore = Mathf.Lerp(shore, 5.75f, Mathf.Clamp01((point.x - 48f) / 6f));
                        return point.y < shore;
                    // Authored bends keep the stream centred on the supplied bridge,
                    // then turn east of the northern footpath instead of erasing it.
                    case "M01":
                        float streamX = point.y <= 16f ? Mathf.Lerp(32f, 33f, point.y / 16f)
                            : point.y <= 30f ? Mathf.Lerp(33f, 38f, (point.y - 16f) / 14f)
                            : point.y <= 38f ? Mathf.Lerp(38f, 43f, (point.y - 30f) / 8f)
                            : Mathf.Lerp(43f, 49f, (point.y - 38f) / 18f);
                        return Mathf.Abs(point.x - streamX) < 2f;
                    case "M02": return new Rect(18f, 54f, 9f, 14f).Contains(point) ||
                        (point.y > 26f && point.y < 59f && Mathf.Abs(point.x - (25f + Mathf.Sin(point.y * .2f) * 3f)) < 1.6f);
                    case "M03": return new Rect(51f, 9f, 9f, 66f).Contains(point) || new Rect(35f, 49f, 64f, 4f).Contains(point) ||
                        new Rect(12f, 46f, 7f, 25f).Contains(point) || new Rect(77f, 18f, 28f, 4f).Contains(point);
                    default: return point.x < 5f || point.x > map.Width - 5f;
                }
            }
            public bool Blocked(Vector2 point)
            {
                if (RootBridgeGap(point)) return true;
                foreach (var exit in map.Exits) {
                    Rect apron = exit.TriggerRect; apron.position -= map.Origin + Vector2.one; apron.size += Vector2.one * 2f;
                    if (apron.Contains(point)) return false;
                }
                if (OnRoute(point, 2f) || NearNode(point, 1.8f)) return false;
                if (point.x < 1f || point.y < 1f || point.x > map.Width - 1f || point.y > map.Height - 1f) return true;
                foreach (var rect in map.CollisionRects) {
                    if (map.Id == "M00" && IsBuildingFoundation(rect)) continue;
                    if (rect.Contains(point)) return true;
                }
                return Water(point);
            }
            public bool RootBridgeGap(Vector2 point)
            {
                if (map.Id != "M02") return false;
                var node = map.Nodes.First(value => value.SourceId == "M02_ROOT_BRIDGE");
                return new Rect(node.Position - map.Origin - node.Footprint * .5f, node.Footprint).Contains(point);
            }
            private static Vector2 Closest(Vector2 point, Vector2 a, Vector2 b)
            { Vector2 delta = b - a; float length = delta.sqrMagnitude; return length < .0001f ? a : a + delta * Mathf.Clamp01(Vector2.Dot(point - a, delta) / length); }
        }

        private sealed class Art
        {
            public readonly Material Material = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            private readonly Dictionary<int, Tile[]> tiles = new Dictionary<int, Tile[]>();
            public Sprite Get(string key) => key == "" ? null : Resources.Load<Sprite>("Art/" + key);
            public Sprite[] Frames(string key) { var frames = Resources.LoadAll<Sprite>("Art/" + key); System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name)); return frames; }
            public Tile[] Tiles(int map)
            {
                if (tiles.TryGetValue(map, out var existing)) return existing;
                string region = new[] { "Coastal", "Coastal", "Forest", "TidalRuins", "Lighthouse" }[map];
                var sprites = Frames("DesignPack/Tiles/" + region);
                if (sprites.Length != 64) throw new InvalidOperationException("Import Design Pack Terrain first; 64 original 16px tiles required for " + region);
                var result = new Tile[64];
                for (int i = 0; i < result.Length; i++) {
                    Sprite sprite = sprites[i];
                    var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = region + "_Tile_" + i.ToString("000"); tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    Vector3 scale = new Vector3(1f / sprite.bounds.size.x, 1f / sprite.bounds.size.y, 1f);
                    tile.transform = Matrix4x4.TRS(-Vector3.Scale(sprite.bounds.center, scale), Quaternion.identity, scale);
                    result[i] = tile;
                }
                tiles.Add(map, result); return result;
            }
            public Sprite Resource(string item)
            {
                string key = item == "berry" ? "Environment/BushPink" : item == "dew" ? "Environment/FlowerBlue"
                    : item == "pollen" ? "Environment/FlowerWhite" : item == "shell" ? "MapDesignAdditions/ShellBankV1"
                    : item == "crystal" ? "MapDesignAdditions/CrystalClusterV1"
                    : item == "resonance-capsule" || item == "berry-treat" || item == "camp-token" ? "Environment/Crate" : null;
                if (key == null) throw new InvalidOperationException("No gathering prop configured for " + item);
                return Get(key) ?? throw new InvalidOperationException("Import the independent gathering prop: " + key);
            }
        }
    }

    /// <summary>Automatic boundary transitions use the same session callback as E interaction.</summary>
    public sealed class DesignPackExitTrigger : MonoBehaviour
    {
        private Action<string, string> callback;
        private string target;
        private static float lastTransition = float.NegativeInfinity;
        public void Configure(Action<string, string> action, string targetRoom) { callback = action; target = targetRoom; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerInteractionSensor>() == null || Time.unscaledTime - lastTransition < .7f) return;
            lastTransition = Time.unscaledTime; callback?.Invoke("exit", target);
        }
    }
}
