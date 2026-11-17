using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MistIsle.World
{
    /// <summary>Map-local lighting and motion derived from the actual water cells and authored interaction anchors.</summary>
    [DisallowMultipleComponent]
    public sealed class DesignPackAtmosphere : MonoBehaviour
    {
        private const string ChildName = "DesignPackAtmosphere";
        private readonly List<Mote> motes = new List<Mote>();
        private readonly List<Lamp> lamps = new List<Lamp>();
        private readonly List<UnityEngine.Object> ownedAssets = new List<UnityEngine.Object>();
        private readonly List<WaterSurface> surfaces = new List<WaterSurface>();
        private readonly List<TerrainTint> tints = new List<TerrainTint>();
        private readonly List<ShorelineMotion> shorelines = new List<ShorelineMotion>();
        private readonly List<FoamBatch> foamBatches = new List<FoamBatch>();
        private Rect bounds;
        private Transform effects;
        private Material fxMaterial;
        private Sprite softSprite;
        private Sprite pixelSprite;
        private System.Random random;
        private Camera viewCamera;
        private float nextVisibilityCheck;
        private bool inView = true;
        private Light2D beacon;
        private float beaconDirection;
        private bool animatedWater;
        private GameObject conduitFlow;
        private Light2D conduitLight;
        private Transform sourceMapRoot;
        public string MapId { get; private set; }
        public int LocalLightCount => lamps.Count + (beacon != null ? 1 : 0);
        public int DynamicParticleCount => motes.Count;
        public int FoamSegmentCount { get; private set; }
        public bool HasAnimatedWater => animatedWater;
        public bool HasSoftTerrain { get; private set; }
        public bool ConduitPowered { get; private set; }

        public static DesignPackAtmosphere Attach(Transform mapRoot, string mapId, Rect worldBounds)
        {
            if (mapRoot == null) throw new ArgumentNullException(nameof(mapRoot));
            if (worldBounds.width <= 0 || worldBounds.height <= 0) throw new ArgumentOutOfRangeException(nameof(worldBounds));
            Transform existing = mapRoot.Find(ChildName);
            if (existing != null) return existing.GetComponent<DesignPackAtmosphere>();
            var host = new GameObject(ChildName);
            host.transform.SetParent(mapRoot, false);
            var atmosphere = host.AddComponent<DesignPackAtmosphere>();
            atmosphere.Initialize(mapRoot, CanonicalId(mapId), worldBounds);
            return atmosphere;
        }

        private static string CanonicalId(string value)
        {
            string id = (value ?? "").ToUpperInvariant();
            for (int i = 0; i < 5; i++) if (id.StartsWith("M0" + i, StringComparison.Ordinal)) return "M0" + i;
            switch (id) { case "CAMP-0": return "M00"; case "COAST-0": return "M01"; case "GLOWGLADE-0": return "M02"; case "TIDAL-0": return "M03"; case "LIGHTHOUSE-0": return "M04"; }
            throw new ArgumentOutOfRangeException(nameof(value), value, "Expected a design-pack map ID M00..M04.");
        }

        private void Initialize(Transform mapRoot, string id, Rect worldBounds)
        {
            MapId = id; bounds = worldBounds; sourceMapRoot = mapRoot;
            random = new System.Random(3819 + id[2] * 97);
            effects = new GameObject("Local lights and layered motion").transform;
            effects.SetParent(transform, false);
            Shader shader = Resources.Load<Shader>("Art/MapDesignAdditions/AtmosphereFX");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            fxMaterial = new Material(shader) { name = "Mist Isle map-local FX " + id };
            ownedAssets.Add(fxMaterial);
            softSprite = CreateSprite(true); pixelSprite = CreateSprite(false);
            ConfigureTerrain(mapRoot);
            ConfigureWater(mapRoot);
            ApplyRegionalGroundColor(mapRoot);
            AddAuthoredLights(mapRoot);
            if (id == "M01") AddMotes("Sea-breeze pollen", Point(.5f, .5f), new Vector2(bounds.width * .82f, bounds.height * .76f), 26, MoteKind.Pollen);
            if (id == "M02")
            {
                AddMotes("Forest fireflies", Point(.5f, .48f), new Vector2(bounds.width * .86f, bounds.height * .8f), 56, MoteKind.Firefly);
                AddMotes("Firefly meadow", Local(22, 20), new Vector2(20, 13), 24, MoteKind.Firefly);
                AddMotes("Waterfall spray", Local(24, 56), new Vector2(7, 3), 15, MoteKind.Mist);
                AddLamp("Ancient grove glow", Local(47, 42), new Color(.66f, 1f, .52f), 5.2f, .24f, false);
            }
            if (id == "M03")
            {
                AddEnergyFlow(Local(91, 34), Local(91, 54));
                AddLamp("Tide-channel reflection", Local(38, 56), new Color(.28f, .84f, 1f), 5.5f, .25f, false);
                conduitLight = AddLamp("Crystal conduit glow", Local(91, 34), new Color(.68f, .52f, 1f), 4.5f, .34f, false);
                SetConduitPowered(false);
                AddMotes("Canal ground mist", Local(52, 44), new Vector2(24, 6), 15, MoteKind.Mist);
            }
            if (id == "M04")
            {
                Vector2 anchor = Local(40, 61);
                float highest = 0;
                foreach (var renderer in mapRoot.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.name.IndexOf("LIGHTHOUSE", StringComparison.OrdinalIgnoreCase) >= 0 && renderer.bounds.size.y > highest)
                    { highest = renderer.bounds.size.y; anchor = new Vector2(renderer.bounds.center.x, renderer.bounds.max.y - .5f); }
                beacon = AddLamp("Lighthouse sweeping beam", anchor, new Color(1f, .89f, .63f), 24f, .48f, false);
                if (beacon != null) { lamps.RemoveAll(lamp => lamp.Light == beacon); beacon.pointLightInnerAngle = 13f; beacon.pointLightOuterAngle = 25f; beaconDirection = -90f; }
                AddMotes("Lighthouse sea mist", Local(40, 20), new Vector2(54, 16), 18, MoteKind.Mist);
            }
            Debug.Log("MIST_ISLE_MAP_ATMOSPHERE: " + id + ", localLights=" + LocalLightCount + ", water=" + HasAnimatedWater + ", foamSegments=" + FoamSegmentCount + ", motes=" + DynamicParticleCount + "; no GlobalLight created.");
        }

        private Vector2 Local(float x, float y) => new Vector2(bounds.xMin + x, bounds.yMin + y);
        private Vector2 Point(float u, float v) => new Vector2(bounds.xMin + bounds.width * u, bounds.yMin + bounds.height * v);
        private float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        private void ConfigureTerrain(Transform mapRoot)
        {
            Shader shader = Resources.Load<Shader>("Art/MapDesignAdditions/TerrainLit");
            if (shader == null) return;
            foreach (var ground in mapRoot.GetComponentsInChildren<Tilemap>(true))
            {
                if (ground.name != "Ground") continue;
                var renderer = ground.GetComponent<TilemapRenderer>(); if (renderer == null) continue;
                var material = new Material(shader) { name = "Soft continuous terrain — " + MapId };
                material.SetFloat("_DetailContrast", .44f); material.SetFloat("_PaletteBlend", .17f); material.SetFloat("_MacroAmount", .026f);
                ownedAssets.Add(material); surfaces.Add(new WaterSurface { Renderer = renderer, Original = renderer.sharedMaterial });
                renderer.sharedMaterial = material; HasSoftTerrain = true;
            }
        }

        public void SetConduitPowered(bool powered)
        {
            if (MapId != "M03") return;
            ConduitPowered = powered;
            if (conduitFlow != null) conduitFlow.SetActive(powered);
            if (conduitLight != null) conduitLight.enabled = powered;
        }

        /// <summary>Call after a water-level mechanism changes the Water tilemap. Collision remains the mechanism's responsibility.</summary>
        public void RefreshWaterBanks()
        {
            if (sourceMapRoot == null || effects == null) return;
            foreach (var batch in foamBatches)
            {
                if (batch.Host != null) { batch.Host.SetActive(false); Release(batch.Host); }
                if (batch.Mesh != null) { ownedAssets.Remove(batch.Mesh); Release(batch.Mesh); }
            }
            foamBatches.Clear(); shorelines.Clear(); FoamSegmentCount = 0;
            foreach (var water in sourceMapRoot.GetComponentsInChildren<Tilemap>(true))
                if (water.name == "Water")
                { var renderer = water.GetComponent<TilemapRenderer>(); if (renderer != null) BuildFoam(water, renderer.sortingOrder + 5); }
        }

        private void ApplyRegionalGroundColor(Transform mapRoot)
        {
            Color tint = MapId == "M02" ? new Color(.9f, .96f, .91f) : MapId == "M03" ? new Color(.94f, .97f, 1f) :
                MapId == "M04" ? new Color(.97f, .96f, 1f) : MapId == "M01" ? new Color(.98f, 1f, .98f) : new Color(1f, .99f, .96f);
            foreach (var map in mapRoot.GetComponentsInChildren<Tilemap>(true))
                if (map.name == "Ground" || map.name == "TerrainBack" || map.name == "Water")
                {
                    tints.Add(new TerrainTint { Map = map, Original = map.color });
                    map.color *= tint;
                }
        }

        private void AddAuthoredLights(Transform mapRoot)
        {
            DesignPackMap map = DesignPackCatalog.GetMap(MapId);
            foreach (var node in map.Nodes)
                if (node.Kind == "campfire" || node.Type == "save_craft")
                {
                    Vector2 flame = node.Position + Vector2.up * .85f;
                    AddLamp("Campfire warmth — " + node.Id, flame, new Color(1f, .61f, .28f), 5.8f, .63f, true);
                    AddMotes("Rising embers — " + node.Id, flame, new Vector2(1.2f, 2.3f), 18, MoteKind.Ember);
                }
            int lanterns = 0;
            foreach (var renderer in mapRoot.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name.IndexOf("Lantern", StringComparison.OrdinalIgnoreCase) >= 0 && lanterns++ < 7)
                    AddLamp("Lantern pool — " + lanterns, (Vector2)renderer.transform.position + Vector2.up * .7f, new Color(1f, .76f, .43f), 3.3f, .32f, true);
        }

        private Light2D AddLamp(string name, Vector2 position, Color color, float radius, float intensity, bool flicker)
        {
            // Existing authored lights stay owned by their builder. Do not double the same flame's light.
            foreach (var existing in transform.parent.GetComponentsInChildren<Light2D>(true))
                if (existing.lightType != Light2D.LightType.Global && Vector2.Distance(existing.transform.position, position) < .45f) return null;
            var source = new GameObject(name).transform; source.SetParent(effects, false); source.position = position;
            var light = source.gameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color; light.intensity = intensity;
            light.pointLightInnerRadius = radius * .1f; light.pointLightOuterRadius = radius;
            light.pointLightInnerAngle = 360f; light.pointLightOuterAngle = 360f;
            light.falloffIntensity = .65f; light.shadowIntensity = .15f;
            lamps.Add(new Lamp { Light = light, Intensity = intensity, Phase = Next(0, 9), Flicker = flicker });
            return light;
        }

        private void ConfigureWater(Transform mapRoot)
        {
            foreach (var water in mapRoot.GetComponentsInChildren<Tilemap>(true))
            {
                if (water.name != "Water") continue;
                var renderer = water.GetComponent<TilemapRenderer>();
                if (renderer == null) continue;
                Shader shader = Resources.Load<Shader>("Art/MapDesignAdditions/WaterFlow");
                if (shader != null)
                {
                    var material = new Material(shader) { name = "Flowing water — " + MapId };
                    material.SetVector("_Flow", new Vector4(.023f, MapId == "M03" ? -.006f : -.011f, 0, 0));
                    ownedAssets.Add(material);
                    surfaces.Add(new WaterSurface { Renderer = renderer, Original = renderer.sharedMaterial });
                    renderer.sharedMaterial = material;
                    animatedWater = true;
                }
                BuildFoam(water, renderer.sortingOrder + 5);
            }
        }

        private void BuildFoam(Tilemap water, int order)
        {
            var segments = new List<ShoreEdge>();
            Vector3Int[] neighbors = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            foreach (var cell in water.cellBounds.allPositionsWithin)
            {
                if (!water.HasTile(cell)) continue;
                for (int side = 0; side < 4; side++)
                {
                    Vector3Int neighbor = cell + neighbors[side];
                    Vector3 neighborWorld = water.GetCellCenterWorld(neighbor);
                    if (water.HasTile(neighbor) || !bounds.Contains(neighborWorld)) continue;
                    Vector3 center = water.GetCellCenterWorld(cell);
                    Vector2 inward = -(Vector2)(Vector3)neighbors[side];
                    center -= (Vector3)(inward * .45f);
                    segments.Add(new ShoreEdge { Center = center, Inward = inward, Phase = Next(0, 6.28f) });
                }
            }
            if (segments.Count == 0) return;
            var shore = new GameObject("Living shoreline foam"); shore.transform.SetParent(effects, false);
            // World-coordinate mesh remains fixed to the actual authored water-bank cell boundary.
            shore.transform.position = Vector3.zero;
            var filter = shore.AddComponent<MeshFilter>();
            var renderer = shore.AddComponent<MeshRenderer>(); renderer.sharedMaterial = fxMaterial; renderer.sortingOrder = order;
            var mesh = new Mesh { name = "Water-bank foam — " + MapId }; mesh.MarkDynamic(); ownedAssets.Add(mesh); filter.sharedMesh = mesh;
            int count = segments.Count;
            var vertices = new Vector3[count * 4]; var uv = new Vector2[count * 4]; var colors = new Color32[count * 4]; var indices = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int v = i * 4, t = i * 6;
                uv[v] = new Vector2(0, 0); uv[v + 1] = new Vector2(1, 0); uv[v + 2] = new Vector2(1, 1); uv[v + 3] = new Vector2(0, 1);
                indices[t] = v; indices[t + 1] = v + 1; indices[t + 2] = v + 2; indices[t + 3] = v; indices[t + 4] = v + 2; indices[t + 5] = v + 3;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors32 = colors; mesh.triangles = indices;
            var animation = new ShorelineMotion(); animation.Initialize(mesh, segments.ToArray(), vertices, colors); shorelines.Add(animation);
            foamBatches.Add(new FoamBatch { Host = shore, Mesh = mesh });
            FoamSegmentCount += count;
        }

        private Sprite CreateSprite(bool soft)
        {
            int size = soft ? 24 : 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = soft ? "Map mist/ember falloff" : "Map pixel mote", filterMode = soft ? FilterMode.Bilinear : FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var colors = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * size * .5f) / (size * .5f);
                byte alpha = soft ? (byte)Mathf.RoundToInt(Mathf.Pow(Mathf.Clamp01(1f - distance), 2f) * 255) : (byte)255;
                colors[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(colors); texture.Apply(false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, size);
            ownedAssets.Add(sprite); ownedAssets.Add(texture); return sprite;
        }

        private void AddMotes(string name, Vector2 center, Vector2 area, int count, MoteKind kind)
        {
            var group = new GameObject(name).transform; group.SetParent(effects, false);
            for (int i = 0; i < count; i++)
            {
                var item = new GameObject(name + " " + i).transform; item.SetParent(group, false);
                var renderer = item.gameObject.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = fxMaterial;
                renderer.sprite = kind == MoteKind.Mist ? softSprite : pixelSprite;
                renderer.sortingOrder = kind == MoteKind.Mist ? 15000 : Mathf.RoundToInt(-center.y * 100f) + 4;
                motes.Add(new Mote { Sprite = renderer, Center = center, Area = area, Phase = Next(0, 6.28f), Seed = Next(0, 1), Kind = kind });
            }
        }

        private void AddEnergyFlow(Vector2 from, Vector2 to)
        {
            var path = new GameObject("Crystal conduit energy flow").transform; path.SetParent(effects, false);
            conduitFlow = path.gameObject;
            Vector2 bend = new Vector2(to.x - 2f, (from.y + to.y) * .5f);
            var line = path.gameObject.AddComponent<LineRenderer>(); line.sharedMaterial = fxMaterial;
            line.useWorldSpace = true; line.positionCount = 3; line.SetPositions(new[] { (Vector3)from, (Vector3)bend, (Vector3)to });
            line.startWidth = .045f; line.endWidth = .045f; line.startColor = new Color(.43f, .78f, 1f, .22f); line.endColor = new Color(.78f, .53f, 1f, .22f); line.sortingOrder = -31200;
            for (int i = 0; i < 8; i++)
            {
                var mote = new GameObject("Conduit pulse " + i).transform; mote.SetParent(path, false);
                var renderer = mote.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = softSprite; renderer.sharedMaterial = fxMaterial; renderer.sortingOrder = -31190;
                motes.Add(new Mote { Sprite = renderer, Center = from, Bend = bend, Area = to - from, Seed = i / 8f, Phase = i, Kind = MoteKind.Energy });
            }
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextVisibilityCheck)
            {
                nextVisibilityCheck = Time.unscaledTime + .3f;
                if (viewCamera == null) viewCamera = Camera.main;
                if (viewCamera != null && viewCamera.orthographic)
                {
                    float height = viewCamera.orthographicSize * 2f, width = height * viewCamera.aspect;
                    var view = new Rect(viewCamera.transform.position.x - width * .5f, viewCamera.transform.position.y - height * .5f, width, height);
                    inView = bounds.Overlaps(view);
                    if (effects.gameObject.activeSelf != inView) effects.gameObject.SetActive(inView);
                }
            }
            if (!inView) return;
            float time = Time.time;
            foreach (var shoreline in shorelines) shoreline.UpdateAt(time);
            foreach (var lamp in lamps)
                if (lamp.Light != null && lamp.Flicker) lamp.Light.intensity = lamp.Intensity * (1f + .055f * Mathf.Sin(time * 8.3f + lamp.Phase) + .025f * Mathf.Sin(time * 13.7f));
            if (beacon != null) beacon.transform.rotation = Quaternion.Euler(0, 0, beaconDirection + Mathf.Sin(time * .16f) * 115f);
            foreach (var mote in motes)
            {
                if (mote.Kind == MoteKind.Energy && !ConduitPowered) continue;
                float cycle = Mathf.Repeat(time * (mote.Kind == MoteKind.Ember ? .28f : mote.Kind == MoteKind.Energy ? .1f : .035f) + mote.Seed, 1f);
                Vector2 position; Color color; Vector2 size;
                if (mote.Kind == MoteKind.Ember)
                {
                    position = mote.Center + new Vector2(Mathf.Sin(time * 1.4f + mote.Phase) * .26f, cycle * mote.Area.y);
                    color = new Color(1f, .52f + cycle * .22f, .15f, (1f - cycle) * .65f); size = Vector2.one * (.045f + mote.Seed * .045f);
                }
                else if (mote.Kind == MoteKind.Mist)
                {
                    position = mote.Center + new Vector2((cycle - .5f) * mote.Area.x, Mathf.Sin(mote.Phase) * mote.Area.y * .4f + Mathf.Sin(time * .2f + mote.Phase) * .25f);
                    color = new Color(.72f, .86f, .94f, Mathf.Sin(cycle * Mathf.PI) * .065f); size = new Vector2(3.8f + mote.Seed * 3, 1.2f + mote.Seed);
                }
                else if (mote.Kind == MoteKind.Energy)
                {
                    // Follow the visible conduit instead of cutting straight across its bend.
                    position = cycle < .5f ? Vector2.Lerp(mote.Center, mote.Bend, cycle * 2f)
                        : Vector2.Lerp(mote.Bend, mote.Center + mote.Area, (cycle - .5f) * 2f);
                    color = new Color(.63f, .74f, 1f, .68f); size = Vector2.one * .22f;
                }
                else
                {
                    position = mote.Center + new Vector2(Mathf.Sin(mote.Phase * 5f) * mote.Area.x * .5f + Mathf.Sin(time * .43f + mote.Phase) * .7f,
                        Mathf.Cos(mote.Phase * 3.7f) * mote.Area.y * .5f + Mathf.Sin(time * .6f + mote.Phase) * .5f);
                    color = mote.Kind == MoteKind.Firefly ? new Color(.86f, 1f, .48f, .25f + .55f * Mathf.Pow(Mathf.Sin(time + mote.Phase), 2f)) : new Color(1f, .9f, .62f, .25f);
                    size = Vector2.one * (mote.Kind == MoteKind.Firefly ? .075f : .045f);
                }
                mote.Sprite.transform.position = new Vector3(position.x, position.y, 0);
                mote.Sprite.transform.localScale = new Vector3(size.x, size.y, 1);
                mote.Sprite.color = color;
            }
        }

        private void OnDestroy()
        {
            foreach (var surface in surfaces) if (surface.Renderer != null) surface.Renderer.sharedMaterial = surface.Original;
            foreach (var tint in tints) if (tint.Map != null) tint.Map.color = tint.Original;
            foreach (var asset in ownedAssets) if (asset != null) Release(asset);
        }

        private static void Release(UnityEngine.Object asset)
        { if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); }

        private enum MoteKind { Ember, Pollen, Firefly, Mist, Energy }
        private sealed class Mote { public SpriteRenderer Sprite; public Vector2 Center; public Vector2 Bend; public Vector2 Area; public float Phase; public float Seed; public MoteKind Kind; }
        private sealed class Lamp { public Light2D Light; public float Intensity; public float Phase; public bool Flicker; }
        private sealed class WaterSurface { public TilemapRenderer Renderer; public Material Original; }
        private sealed class TerrainTint { public Tilemap Map; public Color Original; }
        private sealed class FoamBatch { public GameObject Host; public Mesh Mesh; }
        public struct ShoreEdge { public Vector3 Center; public Vector2 Inward; public float Phase; }

        /// <summary>One dynamic mesh per shoreline, rather than a renderer per water cell.</summary>
        public sealed class ShorelineMotion
        {
            private Mesh mesh; private ShoreEdge[] edges; private Vector3[] vertices; private Color32[] colors;
            public void Initialize(Mesh target, ShoreEdge[] bank, Vector3[] points, Color32[] tints)
            { mesh = target; edges = bank; vertices = points; colors = tints; UpdateAt(Time.time); }
            public void UpdateAt(float time)
            {
                if (mesh == null || edges == null) return;
                for (int i = 0; i < edges.Length; i++)
                {
                    ShoreEdge edge = edges[i]; float wave = Mathf.Sin(time * 1.1f + edge.Phase);
                    Vector2 along = new Vector2(-edge.Inward.y, edge.Inward.x);
                    Vector3 center = edge.Center + (Vector3)(edge.Inward * (.07f + wave * .08f));
                    Vector3 halfAlong = (Vector3)(along * (.32f + .1f * Mathf.Sin(edge.Phase * 3f)));
                    Vector3 halfAcross = (Vector3)(edge.Inward * (.035f + .018f * wave)); int v = i * 4;
                    vertices[v] = center - halfAlong - halfAcross; vertices[v + 1] = center + halfAlong - halfAcross;
                    vertices[v + 2] = center + halfAlong + halfAcross; vertices[v + 3] = center - halfAlong + halfAcross;
                    var color = new Color32(189, 239, 246, (byte)Mathf.RoundToInt(65 + 45 * (.5f + .5f * wave)));
                    colors[v] = colors[v + 1] = colors[v + 2] = colors[v + 3] = color;
                }
                mesh.vertices = vertices; mesh.colors32 = colors; mesh.RecalculateBounds();
            }
        }
    }
}
