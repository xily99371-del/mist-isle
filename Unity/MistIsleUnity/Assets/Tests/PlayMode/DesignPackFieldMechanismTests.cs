using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace MistIsle.Tests
{
    public sealed class DesignPackFieldMechanismTests
    {
        private GameObject root;
        private CoastalWorldLayout world;
        private readonly List<Object> owned = new List<Object>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("DesignPackFieldMechanismTests");
            world = DesignPackWorldBuilder.Build(root.transform, (_, __) => { });
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root); yield return null;
            foreach (var asset in owned) if (asset != null) Object.DestroyImmediate(asset);
            owned.Clear();
        }

        [UnityTest]
        public IEnumerator RestoringTheRootBridgeChangesTheActualWaterFloorAndPhysicsAndCanBeUndone()
        {
            var map = Map("M02");
            var water = Layer(map, "Water"); var deck = Layer(map, "Objects"); var solid = Layer(map, "Collision");
            var mechanism = Mechanism("M02_ROOT_BRIDGE");
            var gap = new Vector3Int(60, 36, 0);
            Assert.That(mechanism.ControlledWaterCells, Is.EqualTo(21), "The source 7×3 bridge has no real river gap.");
            Assert.That(water.HasTile(gap), Is.True); Assert.That(solid.HasTile(gap), Is.True);
            Assert.That(Blocking(map, water.GetCellCenterWorld(gap)), Is.True);
            Assert.That(deck.HasTile(gap), Is.False);
            var closedWater = water.GetTile(gap); var closedSolid = solid.GetTile(gap);

            Assert.That(world.OpenField("M02_ROOT_BRIDGE"), Is.True);
            Assert.That(mechanism.IsOpen, Is.True);
            Assert.That(water.HasTile(gap), Is.False); Assert.That(solid.HasTile(gap), Is.False);
            Assert.That(deck.HasTile(gap), Is.True, "Unlocking showed a dialogue but did not build a traversable bridge floor.");
            Assert.That(Blocking(map, water.GetCellCenterWorld(gap)), Is.False);
            var bridgeFloor = deck.GetTile(gap);
            world.OpenField("M02_ROOT_BRIDGE");
            Assert.That(deck.GetTile(gap), Is.SameAs(bridgeFloor), "Opening twice accumulated or replaced the floor.");

            world.SetFieldOpen("M02_ROOT_BRIDGE", false);
            Assert.That(mechanism.IsOpen, Is.False);
            Assert.That(water.GetTile(gap), Is.SameAs(closedWater)); Assert.That(solid.GetTile(gap), Is.SameAs(closedSolid));
            Assert.That(deck.HasTile(gap), Is.False); Assert.That(Blocking(map, water.GetCellCenterWorld(gap)), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheWaterControlDrainsItsExistingBasinAndRestoresTheOriginalTilesAndShoreline()
        {
            var map = Map("M03"); var water = Layer(map, "Water"); var solid = Layer(map, "Collision"); var ground = Layer(map, "Ground");
            var mechanism = Mechanism("M03_WATER_CHANNEL"); var atmosphere = map.GetComponentInChildren<DesignPackAtmosphere>();
            Assert.That(mechanism.LocalFootprint.size, Is.EqualTo(new Vector2(12f, 8f)));
            Assert.That(mechanism.ControlledWaterCells, Is.GreaterThan(0), "The water mechanism has no actual water to change.");
            var source = new Dictionary<Vector3Int, TileBase[]>();
            foreach (var cell in water.cellBounds.allPositionsWithin)
                if (mechanism.LocalFootprint.Contains(new Vector2(cell.x + .5f, cell.y + .5f)) && water.HasTile(cell))
                    source[cell] = new[] { water.GetTile(cell), solid.GetTile(cell), ground.GetTile(cell) };
            Assert.That(source.Count, Is.EqualTo(mechanism.ControlledWaterCells));
            var outside = new Vector3Int(54, 20, 0); var outsideWater = water.GetTile(outside); var outsideCollision = solid.GetTile(outside);
            Assert.That(outsideWater, Is.Not.Null);
            int foamBefore = atmosphere.FoamSegmentCount;

            world.OpenField("M03_WATER_CHANNEL");
            foreach (var pair in source)
            {
                Assert.That(water.HasTile(pair.Key), Is.False, "Raised water remained visible at " + pair.Key);
                Assert.That(solid.HasTile(pair.Key), Is.False, "The displayed dry bed still has deep-water collision at " + pair.Key);
                Assert.That(ground.GetTile(pair.Key), Is.Not.SameAs(pair.Value[2]), "The lowered bed did not reveal its dry paving.");
                Assert.That(Blocking(map, water.GetCellCenterWorld(pair.Key)), Is.False);
            }
            int foamOpen = atmosphere.FoamSegmentCount;
            Assert.That(water.GetTile(outside), Is.SameAs(outsideWater)); Assert.That(solid.GetTile(outside), Is.SameAs(outsideCollision));
            world.OpenField("M03_WATER_CHANNEL"); Assert.That(atmosphere.FoamSegmentCount, Is.EqualTo(foamOpen));
            world.SetFieldOpen("M03_WATER_CHANNEL", false);
            foreach (var pair in source)
            {
                Assert.That(water.GetTile(pair.Key), Is.SameAs(pair.Value[0])); Assert.That(solid.GetTile(pair.Key), Is.SameAs(pair.Value[1]));
                Assert.That(ground.GetTile(pair.Key), Is.SameAs(pair.Value[2]));
            }
            Assert.That(atmosphere.FoamSegmentCount, Is.EqualTo(foamBefore), "Repeated water switches accumulated shoreline segments.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AWaterFootprintNeverRemovesAnIndependentBuildingFoundation()
        {
            var map = new GameObject("SyntheticWaterFootprint").transform; map.SetParent(root.transform, false); map.position = new Vector3(768, 1000, 0);
            map.gameObject.AddComponent<Grid>();
            foreach (string name in new[] { "Water", "Ground", "Collision", "Objects" })
            { var child = new GameObject(name, typeof(Tilemap)); child.transform.SetParent(map, false); }
            var water = Layer(map, "Water"); var solid = Layer(map, "Collision");
            var waterTile = ScriptableObject.CreateInstance<Tile>(); owned.Add(waterTile);
            var dryTile = ScriptableObject.CreateInstance<Tile>(); owned.Add(dryTile);
            var collisionTile = ScriptableObject.CreateInstance<Tile>(); collisionTile.colliderType = Tile.ColliderType.Grid; owned.Add(collisionTile);
            var wetCell = new Vector3Int(36, 52, 0); var buildingCell = new Vector3Int(37, 52, 0);
            foreach (var cell in new[] { wetCell, buildingCell }) { water.SetTile(cell, waterTile); solid.SetTile(cell, collisionTile); }
            solid.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            solid.gameObject.AddComponent<CompositeCollider2D>();
            var collider = solid.gameObject.AddComponent<TilemapCollider2D>(); collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            collider.ProcessTilemapChanges(); solid.GetComponent<CompositeCollider2D>().GenerateGeometry();
            var foundation = new GameObject("IndependentFoundation", typeof(BoxCollider2D)); foundation.transform.SetParent(map, false);
            foundation.transform.localPosition = new Vector3(37.5f, 52.5f, 0f); foundation.GetComponent<BoxCollider2D>().size = Vector2.one * .8f;
            var gate = new GameObject("SyntheticWaterGate"); gate.transform.SetParent(map, false);
            var mechanism = gate.AddComponent<DesignPackFieldMechanism>();
            Assert.That(DesignPackCatalog.TryGetInteraction("M03_WATER_CHANNEL", out var node), Is.True);
            mechanism.Configure(node, map, dryTile);
            Assert.That(mechanism.ControlledWaterCells, Is.EqualTo(1));
            gate.SetActive(false);
            Assert.That(water.HasTile(wetCell), Is.False); Assert.That(solid.HasTile(wetCell), Is.False);
            Assert.That(water.GetTile(buildingCell), Is.SameAs(waterTile)); Assert.That(solid.GetTile(buildingCell), Is.SameAs(collisionTile));
            Assert.That(Blocking(map, solid.GetCellCenterWorld(buildingCell)), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PoweringTheConduitChangesTheRealLightAndFlowAndMapUnloadingDoesNotSolveIt()
        {
            var map = Map("M03"); var atmosphere = map.GetComponentInChildren<DesignPackAtmosphere>();
            var mechanism = Mechanism("M03_CONDUIT");
            Assert.That(atmosphere.ConduitPowered, Is.False);
            map.gameObject.SetActive(false);
            Assert.That(mechanism.IsOpen, Is.False, "Unloading a map falsely completed the conduit.");
            Assert.That(atmosphere.ConduitPowered, Is.False);
            map.gameObject.SetActive(true);
            world.OpenField("M03_CONDUIT");
            Assert.That(mechanism.IsOpen, Is.True); Assert.That(atmosphere.ConduitPowered, Is.True);
            var poweredLight = map.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true)
                .Single(light => light.name == "Crystal conduit glow");
            Assert.That(poweredLight.enabled, Is.True);
            world.SetFieldOpen("M03_CONDUIT", false);
            Assert.That(atmosphere.ConduitPowered, Is.False); Assert.That(poweredLight.enabled, Is.False);
            Object.Destroy(mechanism.gameObject);
            yield return null;
            Assert.That(atmosphere.ConduitPowered, Is.False, "Destroying a closed gate must not energize the map during teardown.");
        }

        [UnityTest]
        public IEnumerator TheLighthouseGateUsesItsVisibleTierOpeningAndCannotBeWalkedAround()
        {
            var map = Map("M04"); var barrier = Mechanism("M04_THREE_SEAL_GATE").GetComponent<BoxCollider2D>();
            Assert.That(barrier.size.x, Is.EqualTo(14f));
            Assert.That(map.GetComponentsInChildren<BoxCollider2D>().Count(body => body.name == "LighthouseTierCliffFootprint"), Is.EqualTo(2));
            Assert.That(map.GetComponentsInChildren<SpriteRenderer>().Count(sprite => sprite.name == "LighthouseTierCliffFace"), Is.GreaterThan(10));
            for (int x = 3; x <= 77; x++)
                Assert.That(Blocking(map, map.position + new Vector3(x, 49f, 0)), Is.True, "The closed gate can be bypassed at x=" + x);
            world.OpenField("M04_THREE_SEAL_GATE");
            Assert.That(Blocking(map, map.position + new Vector3(40f, 49f, 0)), Is.False);
            foreach (int x in new[] { 15, 30, 50, 65 })
                Assert.That(Blocking(map, map.position + new Vector3(x, 49f, 0)), Is.True, "Opening the door removed the permanent cliff wing.");
            yield return null;
        }

        private Transform Map(string id)
        { var map = DesignPackCatalog.GetMap(id); return root.transform.Find("DesignPackWorld/" + id + " — " + map.Name); }
        private static Tilemap Layer(Transform map, string name) => map.Find(name).GetComponent<Tilemap>();
        private DesignPackFieldMechanism Mechanism(string id)
            => root.GetComponentsInChildren<DesignPackFieldMechanism>(true).Single(value => value.GateId == id);
        private static bool Blocking(Transform map, Vector2 point)
            => Physics2D.OverlapCircleAll(point, .18f).Any(body => !body.isTrigger && body.transform.IsChildOf(map));
    }
}
