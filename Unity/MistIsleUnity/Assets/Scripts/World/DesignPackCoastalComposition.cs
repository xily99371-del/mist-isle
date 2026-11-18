using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MistIsle.World
{
    /// <summary>M00's worn camp clearing and rocky water edge. Adds presentation only; every solid cell remains authored by the world.</summary>
    [DisallowMultipleComponent]
    public sealed class DesignPackCoastalComposition : MonoBehaviour
    {
        private const string HostName = "Coastal camp composition";
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly List<Vector2> decorationAnchors = new List<Vector2>();
        private Tilemap ground, water, collision;
        private DesignPackMap map;
        private Material groundMaterial, rockMaterial, rimMaterial, washMaterial;
        public int CliffSegmentCount { get; private set; }
        public IReadOnlyList<Vector2> DecorationAnchors => decorationAnchors;
        public bool HasBlendedGround { get; private set; }

        public static DesignPackCoastalComposition Attach(Transform mapRoot)
        {
            if (mapRoot == null) throw new ArgumentNullException(nameof(mapRoot));
            var existing = mapRoot.Find(HostName);
            if (existing != null) return existing.GetComponent<DesignPackCoastalComposition>();
            var layers = mapRoot.GetComponentsInChildren<Tilemap>(true);
            var ground = layers.FirstOrDefault(layer => layer.name == "Ground");
            var water = layers.FirstOrDefault(layer => layer.name == "Water");
            var collision = layers.FirstOrDefault(layer => layer.name == "Collision");
            if (ground == null || water == null || collision == null)
                throw new InvalidOperationException("Coastal composition requires the authored Ground, Water and Collision tilemaps.");
            var host = new GameObject(HostName);
            host.transform.SetParent(mapRoot, false);
            var composition = host.AddComponent<DesignPackCoastalComposition>();
            composition.map = DesignPackCatalog.GetMap("M00");
            composition.ground = ground; composition.water = water; composition.collision = collision;
            composition.Build();
            return composition;
        }

        private void Build()
        {
            var shader = Resources.Load<Shader>("Art/MapDesignAdditions/CoastalComposition");
            if (shader == null) throw new InvalidOperationException("Missing coastal composition shader.");
            var data = CreateSurfaceData();
            groundMaterial = Material(shader, data, 0f);
            rockMaterial = Material(shader, data, 1f);
            rimMaterial = Material(shader, data, 2f);
            washMaterial = Material(shader, data, 3f);
            var groundMesh = new MeshBatch();
            groundMesh.Quad(new Vector2(0, 0), new Vector2(map.Width, 0), new Vector2(map.Width, map.Height), new Vector2(0, map.Height), Color.white);
            Render("Worn sand clearing and continuous camp paths", groundMesh, groundMaterial, -31998);
            HasBlendedGround = true;
            BuildCoast();
            AddGardenClumps();
        }

        private Material Material(Shader shader, Texture data, float mode)
        {
            var material = new Material(shader) { name = "M00 coastal composition " + mode };
            material.SetTexture("_SurfaceData", data);
            material.SetVector("_MapSize", new Vector4(map.Width, map.Height, 0, 0));
            material.SetFloat("_SurfaceMode", mode);
            material.SetTexture("_GrassTex", RequiredSprite("DesignPack/Tiles/Coastal/Tile_004").texture);
            material.SetTexture("_SandTex", RequiredSprite("DesignPack/Tiles/Coastal/Tile_016").texture);
            material.SetTexture("_SandDetailTex", RequiredSprite("DesignPack/Tiles/Coastal/Tile_017").texture);
            material.SetTexture("_RockTex", RequiredSprite("Environment/GrassCliff").texture);
            owned.Add(material);
            return material;
        }

        private Texture2D CreateSurfaceData()
        {
            // Data, not painted artwork: R is a low-pass footprint of the actual path tiles; G/B/A are water/deck/ground.
            int width = map.Width, height = map.Height;
            var paths = new bool[width, height];
            var grass = RequiredSprite("DesignPack/Tiles/Coastal/Tile_004");
            var sourcePath = new HashSet<Sprite>(new[] { RequiredSprite("DesignPack/Tiles/Coastal/Tile_012"), RequiredSprite("DesignPack/Tiles/Coastal/Tile_013") });
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var sprite = ground.GetSprite(new Vector3Int(x, y, 0));
                paths[x, y] = sourcePath.Contains(sprite) || y > 12 && sprite != null && sprite != grass
                    && Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32f, 24f)) < 6f;
            }
            const int samples = 4;
            var texture = new Texture2D(width * samples, height * samples, TextureFormat.RGBA32, false, true)
                { name = "M00 actual path and shore data", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var colors = new Color32[texture.width * texture.height];
            for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
            {
                float px = (x + .5f) / samples, py = (y + .5f) / samples;
                int cellX = Mathf.FloorToInt(px), cellY = Mathf.FloorToInt(py);
                float weight = 0, total = 0;
                for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
                {
                    int sx = cellX + dx, sy = cellY + dy;
                    if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
                    float distance = (sx + .5f - px) * (sx + .5f - px) + (sy + .5f - py) * (sy + .5f - py);
                    float amount = Mathf.Exp(-distance / 1.05f);
                    total += amount;
                    if (paths[sx, sy]) weight += amount;
                }
                var cell = new Vector3Int(cellX, cellY, 0);
                bool wet = water.HasTile(cell), solid = collision.HasTile(cell), floor = ground.HasTile(cell);
                var sprite = ground.GetSprite(cell);
                bool decking = sprite != null && sprite.name.IndexOf("Tile_03", StringComparison.Ordinal) >= 0 && cellY < 12;
                colors[y * texture.width + x] = new Color32((byte)Mathf.RoundToInt(255f * weight / Mathf.Max(.001f, total)),
                    wet ? (byte)255 : (byte)0, decking && !wet && !solid ? (byte)255 : (byte)0, floor ? (byte)255 : (byte)0);
            }
            texture.SetPixels32(colors); texture.Apply(false, true); owned.Add(texture);
            return texture;
        }

        private void BuildCoast()
        {
            var faces = new MeshBatch(); var rims = new MeshBatch(); var wash = new MeshBatch();
            var boulderMaterial = new Material(rockMaterial) { name = "Approved slate boulders at waterline" };
            boulderMaterial.SetFloat("_SurfaceMode", 4f);
            boulderMaterial.SetTexture("_RockTex", RequiredSprite("Environment/RockLarge").texture);
            owned.Add(boulderMaterial);
            for (int y = 1; y < 12; y++) for (int x = 1; x < map.Width - 1; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!water.HasTile(cell) || !collision.HasTile(cell) || water.HasTile(cell + Vector3Int.up)
                    || !ground.HasTile(cell + Vector3Int.up) || collision.HasTile(cell + Vector3Int.up)) continue;
                // Do not turn the landing or the shell-gathering approach into a decorative wall.
                var bank = new Vector2(x + .5f, y + 1f);
                if (Vector2.Distance(bank, map.Spawn - map.Origin) < 5f
                    || Vector2.Distance(bank, new Vector2(55f, 7f)) < 3.2f) continue;
                float depth = water.HasTile(cell + Vector3Int.down) && collision.HasTile(cell + Vector3Int.down) ? 1.35f : .72f;
                float leftDepth = depth + Mathf.Sin(x * 2.3f) * .13f, rightDepth = depth + Mathf.Sin((x + 1) * 2.3f) * .13f;
                var a = new Vector2(x, y + 1f); var b = new Vector2(x + 1f, y + 1f);
                var c = new Vector2(x + 1f, y + 1f - rightDepth); var d = new Vector2(x, y + 1f - leftDepth);
                var tint = new Color(1f, 1f, 1f, .94f + .06f * Mathf.Sin(x * 7f));
                faces.Quad(d, c, b, a, tint);
                rims.Quad(a + Vector2.down * .06f, b + Vector2.down * .06f, b + Vector2.up * .3f, a + Vector2.up * .3f, Color.white);
                wash.Quad(d + Vector2.down * .15f, c + Vector2.down * .15f, c + Vector2.up * .12f, d + Vector2.up * .12f, Color.white);
                if (x % 9 == 2 || x % 9 == 3)
                {
                    var rock = new GameObject("Layered slate shoulder", typeof(SpriteRenderer)); rock.transform.SetParent(transform, false);
                    var sprite = RequiredSprite("Environment/RockLarge");
                    float height = x % 9 == 2 ? 1.65f : 1.05f;
                    float scale = height / sprite.bounds.size.y;
                    rock.transform.localScale = Vector3.one * scale;
                    Vector2 foot = bank + Vector2.down * (depth + .38f);
                    rock.transform.localPosition = (Vector3)foot - new Vector3(sprite.bounds.center.x * scale, sprite.bounds.min.y * scale, 0f);
                    var renderer = rock.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
                    renderer.sharedMaterial = boulderMaterial; renderer.sortingOrder = -31887;
                }
                CliffSegmentCount++;
            }
            Render("Gray rock faces at solid water banks", faces, rockMaterial, -31890);
            Render("Low grass and stone bank lip", rims, rimMaterial, -31889);
            Render("Wave wash beneath coastal rock ledges", wash, washMaterial, -31888);
        }

        private void AddGardenClumps()
        {
            var candidates = new[] {
                new Vector2(24f,26.8f),new Vector2(26f,30f),new Vector2(33f,31f),new Vector2(37f,30.2f),
                new Vector2(39.8f,18.1f),new Vector2(27.4f,16.5f),new Vector2(33f,15.7f),new Vector2(20f,23f),
                new Vector2(20f,28.5f),new Vector2(37f,36f),new Vector2(57f,30f),new Vector2(57f,18f),
                new Vector2(16f,17f),new Vector2(47f,16f),new Vector2(49f,20f),new Vector2(23f,35f),
                new Vector2(40f,36.5f),new Vector2(29f,30.5f),new Vector2(24f,19f),new Vector2(43f,18f)
            };
            for (int index = 0; index < candidates.Length; index++)
            {
                var point = candidates[index];
                if (!CanDecorate(point)) continue;
                decorationAnchors.Add(point);
                AddProp("Camp flower border", index % 3 == 0 ? "FlowerBlue" : "FlowerWhite", point, 1.1f);
                AddProp("Small border flowers", index % 3 == 0 ? "FlowerWhite" : "FlowerBlue", point + new Vector2(.68f, .18f), .64f);
                AddProp("Border grass and stones", "RockSmall", point + new Vector2(-.55f, -.14f), .52f);
                if (index % 3 == 1) AddProp("Flowering camp shrub", "BushPink", point + new Vector2(-.3f, .55f), 1.15f);
            }
        }

        private bool CanDecorate(Vector2 point)
        {
            foreach (var node in map.Nodes)
                if (Vector2.Distance(point, node.ApproachPosition - map.Origin) < 2.8f) return false;
            foreach (var route in map.Routes) for (int index = 1; index < route.Length; index++)
                if (SegmentDistance(point, route[index - 1], route[index]) < 3.4f) return false;
            foreach (var rect in map.CollisionRects)
                if (new Rect(rect.x - .9f, rect.y - .9f, rect.width + 1.8f, rect.height + 1.8f).Contains(point)) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                var cell = Vector3Int.FloorToInt(point + new Vector2(dx, dy));
                if (!ground.HasTile(cell) || water.HasTile(cell) || collision.HasTile(cell)) return false;
                var sprite = ground.GetSprite(cell);
                if (sprite != null && (sprite.name.IndexOf("Tile_012", StringComparison.Ordinal) >= 0
                    || sprite.name.IndexOf("Tile_013", StringComparison.Ordinal) >= 0)) return false;
            }
            return true;
        }

        private static float SegmentDistance(Vector2 point, Vector2 from, Vector2 to)
        {
            var delta = to - from;
            return Vector2.Distance(point, from + delta * Mathf.Clamp01(Vector2.Dot(point - from, delta) / Mathf.Max(.0001f, delta.sqrMagnitude)));
        }

        private void AddProp(string name, string asset, Vector2 point, float height)
        {
            var sprite = RequiredSprite("Environment/" + asset);
            var host = new GameObject(name, typeof(SpriteRenderer)); host.transform.SetParent(transform, false);
            float scale = height / sprite.bounds.size.y;
            host.transform.localScale = Vector3.one * scale;
            host.transform.localPosition = (Vector3)point - new Vector3(sprite.bounds.center.x * scale, sprite.bounds.min.y * scale, 0f);
            var renderer = host.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            renderer.sortingOrder = Mathf.RoundToInt(-(transform.position.y + point.y) * 100f);
        }

        private Sprite RequiredSprite(string path) => Resources.Load<Sprite>("Art/" + path)
            ?? throw new InvalidOperationException("Missing approved coastal art: " + path);

        private void Render(string name, MeshBatch batch, Material material, int order)
        {
            if (batch.Vertices.Count == 0) return;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(batch.Vertices); mesh.SetUVs(0, batch.Uv); mesh.SetColors(batch.Colors); mesh.SetTriangles(batch.Triangles, 0); mesh.RecalculateBounds();
            owned.Add(mesh);
            var host = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); host.transform.SetParent(transform, false);
            host.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = host.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = order;
        }

        private void OnDestroy()
        {
            foreach (var asset in owned)
                if (asset != null) { if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); }
            owned.Clear();
        }

        private sealed class MeshBatch
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<int> Triangles = new List<int>();
            public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
            {
                int start = Vertices.Count;
                Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
                Uv.Add(Vector2.zero); Uv.Add(Vector2.right); Uv.Add(Vector2.one); Uv.Add(Vector2.up);
                for (int index = 0; index < 4; index++) Colors.Add(color);
                Triangles.Add(start); Triangles.Add(start + 1); Triangles.Add(start + 2);
                Triangles.Add(start); Triangles.Add(start + 2); Triangles.Add(start + 3);
            }
        }
    }
}
