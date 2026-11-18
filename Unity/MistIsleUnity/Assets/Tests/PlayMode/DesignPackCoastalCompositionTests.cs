using System.Collections;
using System.Linq;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class DesignPackCoastalCompositionTests
    {
        private GameObject root;
        private Transform camp;
        private Tilemap water, collision, ground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("CoastalCompositionTests");
            DesignPackWorldBuilder.Build(root.transform, (_, __) => { });
            camp = root.transform.Find("DesignPackWorld").GetChild(0);
            ground = camp.Find("Ground").GetComponent<Tilemap>();
            water = camp.Find("Water").GetComponent<Tilemap>();
            collision = camp.Find("Collision").GetComponent<Tilemap>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompositionPreservesEveryTileAndEveryAuthoredCollider()
        {
            var bounds = new BoundsInt(0, 0, 0, 64, 48, 1);
            var waterBefore = water.GetTilesBlock(bounds);
            var solidBefore = collision.GetTilesBlock(bounds);
            var groundBefore = ground.GetTilesBlock(bounds);
            var colliders = camp.GetComponentsInChildren<Collider2D>(true).ToArray();
            var composition = DesignPackCoastalComposition.Attach(camp);
            Assert.That(composition.HasBlendedGround, Is.True);
            Assert.That(composition.CliffSegmentCount, Is.GreaterThan(20));
            CollectionAssert.AreEqual(waterBefore, water.GetTilesBlock(bounds));
            CollectionAssert.AreEqual(solidBefore, collision.GetTilesBlock(bounds));
            CollectionAssert.AreEqual(groundBefore, ground.GetTilesBlock(bounds));
            CollectionAssert.AreEqual(colliders, camp.GetComponentsInChildren<Collider2D>(true));
            Assert.That(composition.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoastalRockFacesFollowSolidWaterBanksAndKeepLandingAndShellApproachClear()
        {
            var shell = DesignPackCatalog.GetMap("M00").Nodes.Single(node => node.SourceId == "M00_GATHER_SHELL");
            var shoreCell = Vector3Int.FloorToInt(shell.ApproachPosition + Vector2.down * 1.5f);
            Assert.That(water.HasTile(shoreCell) && collision.HasTile(shoreCell), Is.True,
                "A gathering access spur generated an unplanned pier in deep water.");
            var composition = DesignPackCoastalComposition.Attach(camp);
            var mesh = composition.GetComponentsInChildren<MeshFilter>().Single(item => item.name == "Gray rock faces at solid water banks").sharedMesh;
            var points = mesh.vertices;
            Assert.That(points.Length, Is.EqualTo(composition.CliffSegmentCount * 4));
            for (int index = 0; index < points.Length; index += 4)
            {
                Vector2 top = (points[index + 2] + points[index + 3]) * .5f;
                var waterCell = Vector3Int.FloorToInt(top + Vector2.down * .25f);
                Assert.That(water.HasTile(waterCell) && collision.HasTile(waterCell), Is.True);
                Assert.That(water.HasTile(waterCell + Vector3Int.up), Is.False);
                Assert.That(Vector2.Distance(top, new Vector2(9f, 5f)), Is.GreaterThanOrEqualTo(5f));
                Assert.That(Vector2.Distance(top, new Vector2(55f, 7f)), Is.GreaterThanOrEqualTo(3.2f));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GardenClumpsKeepEveryInteractionApproachAndMainWalkOpen()
        {
            var composition = DesignPackCoastalComposition.Attach(camp);
            Assert.That(composition.DecorationAnchors.Count, Is.GreaterThanOrEqualTo(5));
            foreach (var point in composition.DecorationAnchors)
            {
                Assert.That(water.HasTile(Vector3Int.FloorToInt(point)), Is.False);
                Assert.That(collision.HasTile(Vector3Int.FloorToInt(point)), Is.False);
                foreach (var node in DesignPackCatalog.GetMap("M00").Nodes)
                    Assert.That(Vector2.Distance(point, node.ApproachPosition), Is.GreaterThanOrEqualTo(2.8f), node.Id);
            }
            Assert.That(DesignPackCoastalComposition.Attach(camp), Is.SameAs(composition));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingTheCompositionRestoresTheUnmodifiedGroundPresentation()
        {
            var renderer = ground.GetComponent<TilemapRenderer>();
            var originalMaterial = renderer.sharedMaterial;
            var originalColor = ground.color;
            var composition = DesignPackCoastalComposition.Attach(camp);
            var meshes = composition.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
            Object.Destroy(composition.gameObject);
            yield return null;
            Assert.That(renderer.sharedMaterial, Is.SameAs(originalMaterial));
            Assert.That(ground.color, Is.EqualTo(originalColor));
            Assert.That(meshes.All(mesh => mesh == null), Is.True);
            Assert.That(camp.Find("Coastal camp composition"), Is.Null);
        }
    }
}
