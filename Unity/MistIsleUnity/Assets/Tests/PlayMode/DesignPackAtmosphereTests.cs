using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class DesignPackAtmosphereTests
    {
        private GameObject root;
        private GameObject cameraRoot;
        private readonly Dictionary<Camera, bool> previousCameras = new Dictionary<Camera, bool>();
        private Tilemap ground;
        private Tilemap water;
        private Material original;
        private Tile waterTile;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var map = DesignPackCatalog.GetMap("M03");
            previousCameras.Clear();
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            { previousCameras.Add(camera, camera.enabled); camera.enabled = false; }
            root = new GameObject("AtmosphereRuntimeFixture", typeof(Grid));
            root.transform.position = map.Origin;
            original = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            Assert.That(original, Is.Not.Null);
            ground = Layer("Ground"); water = Layer("Water");
            waterTile = ScriptableObject.CreateInstance<Tile>();
            waterTile.sprite = Resources.Load<Sprite>("Art/DesignPack/Tiles/TidalRuins/Tile_044");
            Assert.That(waterTile.sprite, Is.Not.Null);
            // An interior pool has four banks; map-border water intentionally omits outside-map foam.
            water.SetTile(new Vector3Int(4, 4, 0), waterTile);
            water.SetTile(new Vector3Int(5, 4, 0), waterTile);
            water.SetTile(new Vector3Int(4, 5, 0), waterTile);
            water.SetTile(new Vector3Int(5, 5, 0), waterTile);
            cameraRoot = new GameObject("AtmosphereFixtureCamera", typeof(Camera));
            var fixtureCamera = cameraRoot.GetComponent<Camera>();
            fixtureCamera.tag = "MainCamera"; fixtureCamera.orthographic = true;
            fixtureCamera.orthographicSize = 50f;
            fixtureCamera.transform.position = new Vector3(map.Bounds.center.x, map.Bounds.center.y, -10f);
            yield return null;
        }

        private Tilemap Layer(string name)
        {
            var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(root.transform, false);
            child.GetComponent<TilemapRenderer>().sharedMaterial = original;
            return child.GetComponent<Tilemap>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            if (cameraRoot != null) Object.Destroy(cameraRoot);
            if (waterTile != null) Object.Destroy(waterTile);
            yield return null;
            foreach (var pair in previousCameras) if (pair.Key != null) pair.Key.enabled = pair.Value;
        }

        [UnityTest]
        public IEnumerator WaterBankRefreshTracksRealCellsWithoutAccumulatingOldFoamOrGlobalLights()
        {
            var map = DesignPackCatalog.GetMap("M03");
            var atmosphere = DesignPackAtmosphere.Attach(root.transform, map.Id, map.Bounds);
            Assert.That(atmosphere.FoamSegmentCount, Is.EqualTo(8));
            Assert.That(root.GetComponentsInChildren<Light2D>(true).Any(light => light.lightType == Light2D.LightType.Global), Is.False);
            water.SetTile(new Vector3Int(5, 4, 0), null);
            water.SetTile(new Vector3Int(4, 5, 0), null);
            water.SetTile(new Vector3Int(5, 5, 0), null);
            for (int repeat = 0; repeat < 3; repeat++) atmosphere.RefreshWaterBanks();
            yield return null;
            Assert.That(atmosphere.FoamSegmentCount, Is.EqualTo(4));
            Assert.That(root.GetComponentsInChildren<MeshFilter>(true).Count(filter => filter.name == "Living shoreline foam"), Is.EqualTo(1));
            water.ClearAllTiles(); water.SetTile(Vector3Int.zero, waterTile);
            atmosphere.RefreshWaterBanks();
            yield return null;
            Assert.That(atmosphere.FoamSegmentCount, Is.EqualTo(2), "Outside-map neighbors must not create a visible foam bank.");
            water.ClearAllTiles();
            atmosphere.RefreshWaterBanks();
            yield return null;
            Assert.That(atmosphere.FoamSegmentCount, Is.Zero);
            Assert.That(root.GetComponentsInChildren<MeshFilter>(true).Count(filter => filter.name == "Living shoreline foam"), Is.Zero);
        }

        [UnityTest]
        public IEnumerator ConduitPowerControlsActualEffectsAndPulsesStayOnItsVisibleRoute()
        {
            var map = DesignPackCatalog.GetMap("M03");
            var atmosphere = DesignPackAtmosphere.Attach(root.transform, map.Id, map.Bounds);
            var line = root.GetComponentInChildren<LineRenderer>(true);
            var conduitLamp = root.GetComponentsInChildren<Light2D>(true).Single(light => light.name == "Crystal conduit glow");
            Assert.That(line.gameObject.activeSelf, Is.False);
            Assert.That(conduitLamp.enabled, Is.False);
            atmosphere.SetConduitPowered(true);
            yield return null;
            yield return null;
            Assert.That(line.gameObject.activeInHierarchy, Is.True);
            Assert.That(conduitLamp.enabled, Is.True);
            var pulses = line.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(pulses.Length, Is.EqualTo(8));
            foreach (var pulse in pulses)
            {
                Vector2 point = pulse.transform.position;
                float distance = Mathf.Min(DistanceToSegment(point, line.GetPosition(0), line.GetPosition(1)),
                    DistanceToSegment(point, line.GetPosition(1), line.GetPosition(2)));
                Assert.That(distance, Is.LessThan(.001f), pulse.name + " left the drawn conduit.");
            }
            atmosphere.SetConduitPowered(false);
            Assert.That(line.gameObject.activeSelf, Is.False);
            Assert.That(conduitLamp.enabled, Is.False);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float along = delta.sqrMagnitude < .0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - from, delta) / delta.sqrMagnitude);
            return Vector2.Distance(point, from + delta * along);
        }

        [UnityTest]
        public IEnumerator RemovingAtmosphereRestoresAuthoredGroundAndWaterMaterials()
        {
            var map = DesignPackCatalog.GetMap("M03");
            var atmosphere = DesignPackAtmosphere.Attach(root.transform, map.Id, map.Bounds);
            Assert.That(atmosphere.HasSoftTerrain && atmosphere.HasAnimatedWater, Is.True);
            Assert.That(ground.GetComponent<TilemapRenderer>().sharedMaterial, Is.Not.SameAs(original));
            Assert.That(water.GetComponent<TilemapRenderer>().sharedMaterial, Is.Not.SameAs(original));
            Assert.That(ground.color, Is.Not.EqualTo(Color.white));
            Assert.That(DesignPackAtmosphere.Attach(root.transform, map.Id, map.Bounds), Is.SameAs(atmosphere));
            Object.Destroy(atmosphere.gameObject);
            yield return null;
            Assert.That(ground.GetComponent<TilemapRenderer>().sharedMaterial, Is.SameAs(original));
            Assert.That(water.GetComponent<TilemapRenderer>().sharedMaterial, Is.SameAs(original));
            Assert.That(ground.color, Is.EqualTo(Color.white));
            Assert.That(water.color, Is.EqualTo(Color.white));
        }
    }
}
