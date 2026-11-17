using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistIsle.Runtime;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MistIsle.Tests
{
    public sealed class DesignPackFoundationTests
    {
        private GameObject root;
        private Rigidbody2D body;
        private CapsuleCollider2D feet;
        private PhysicsMaterial2D material;
        private string saveDirectory;
        private readonly Dictionary<Camera, bool> previousCameras = new Dictionary<Camera, bool>();
        private readonly Dictionary<AudioListener, bool> previousListeners = new Dictionary<AudioListener, bool>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousCameras.Clear(); previousListeners.Clear();
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) previousCameras.Add(camera, camera.enabled);
            foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) previousListeners.Add(listener, listener.enabled);
            root = new GameObject("DesignPackFoundationTests");
            DesignPackWorldBuilder.Build(root.transform, (_, __) => { });
            var probe = new GameObject("ActualTravelerFootprintProbe", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            probe.transform.SetParent(root.transform, false);
            body = probe.GetComponent<Rigidbody2D>(); body.gravityScale = 0f; body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            feet = probe.GetComponent<CapsuleCollider2D>(); feet.direction = CapsuleDirection2D.Horizontal;
            feet.size = new Vector2(.52f, .34f); feet.offset = new Vector2(0f, .17f);
            material = new PhysicsMaterial2D("FrictionlessFoundationProbe") { friction = 0f, bounciness = 0f }; feet.sharedMaterial = material;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!previousCameras.ContainsKey(camera)) Object.Destroy(camera.gameObject);
            yield return null;
            foreach (var pair in previousCameras) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in previousListeners) if (pair.Key != null) pair.Key.enabled = pair.Value;
            if (material != null) Object.DestroyImmediate(material);
            if (saveDirectory != null && Directory.Exists(saveDirectory))
            {
                string parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string full = Path.GetFullPath(saveDirectory);
                if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("MistIsle-FoundationSort-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Refusing to remove an unverified test save directory.");
                Directory.Delete(full, true);
            }
            saveDirectory = null;
        }

        [UnityTest]
        public IEnumerator TheShopCounterBlocksBodiesButAllFourAwningSidesStayWalkable()
        {
            var polygon = Foundation("M00_SUPPLY_SHOP");
            Assert.That(polygon.points.Length, Is.GreaterThan(4), "The coarse map zone was reused as an exact footprint.");
            yield return ClearLeg(new Vector2(12.5f,26.35f),new Vector2(20.5f,26.35f));
            yield return ClearLeg(new Vector2(12.5f,26.35f),new Vector2(12.5f,29.7f));
            yield return ClearLeg(new Vector2(20.5f,26.35f),new Vector2(20.5f,29.7f));
            yield return ClearLeg(new Vector2(12.5f,29.7f),new Vector2(20.5f,29.7f));
            foreach (var point in new[] {new Vector2(14f,27.5f),new Vector2(16.5f,28.2f),new Vector2(19.3f,28.2f)})
                yield return SolidLeg(new Vector2(point.x,25.8f),point,polygon);
            Assert.That(polygon.OverlapPoint(new Vector2(16.5f,34f)), Is.False, "Transparent space above the shop is still a solid wall.");
        }

        [UnityTest]
        public IEnumerator TheHouseKeepsFrontStepsAndBothRoofSidesOpenWhileItsWallsRemainSolid()
        {
            var polygon = Foundation("M00_TRAVELER_HUT");
            Assert.That(polygon.points.Length, Is.GreaterThan(4));
            yield return ClearLeg(new Vector2(24.05f,36.45f),new Vector2(34.6f,36.45f));
            yield return ClearLeg(new Vector2(24.05f,36.45f),new Vector2(24.05f,40.1f));
            yield return ClearLeg(new Vector2(34.6f,36.45f),new Vector2(34.6f,40.1f));
            yield return ClearLeg(new Vector2(24.05f,40.1f),new Vector2(34.6f,40.1f));
            foreach (var point in new[] {new Vector2(26.2f,37.5f),new Vector2(29f,38f),new Vector2(31.5f,37.6f)})
                yield return SolidLeg(new Vector2(point.x,36f),point,polygon);
            Assert.That(polygon.OverlapPoint(new Vector2(24.2f,41.5f)), Is.False);
            Assert.That(polygon.OverlapPoint(new Vector2(33.7f,42.3f)), Is.False);
        }

        [UnityTest]
        public IEnumerator TheProvisionalStorageTentKeepsItsDoorBayAndRearClearButBlocksTheClothAndCrates()
        {
            var polygon = Foundation("M00_STORAGE");
            Assert.That(polygon.points.Length, Is.GreaterThan(4));
            yield return ClearLeg(new Vector2(46.3f,34.95f),new Vector2(55f,34.95f));
            yield return ClearLeg(new Vector2(46.3f,34.95f),new Vector2(46.3f,39.2f));
            yield return ClearLeg(new Vector2(55f,34.95f),new Vector2(55f,39.2f));
            yield return ClearLeg(new Vector2(46.3f,39.2f),new Vector2(55f,39.2f));
            yield return ClearLeg(new Vector2(51.35f,34.95f),new Vector2(51.35f,36.6f));
            yield return SolidLeg(new Vector2(47.4f,34.6f),new Vector2(47.4f,36f),polygon);
            yield return SolidLeg(new Vector2(51.2f,39.4f),new Vector2(51.2f,38f),polygon);
            yield return SolidLeg(new Vector2(55.2f,37f),new Vector2(53.7f,37f),polygon);
            Assert.That(polygon.OverlapPoint(new Vector2(51f,41.7f)), Is.False);
        }

        [UnityTest]
        public IEnumerator WalkableFloorArtStaysBelowTheActualTravelerAtTheAuthoredDockSpawn()
        {
            Object.Destroy(root); yield return null;
            foreach (var pair in previousCameras) if (pair.Key != null) pair.Key.enabled = false;
            foreach (var pair in previousListeners) if (pair.Key != null) pair.Key.enabled = false;
            root = new GameObject("FoundationSortActualSession");
            var session = root.AddComponent<GameSession>();
            saveDirectory = Path.Combine(Path.GetTempPath(), "MistIsle-FoundationSort-" + Guid.NewGuid().ToString("N"));
            session.SaveDirectoryOverride = saveDirectory;
            yield return null; session.NewJourney(); yield return null;
            Assert.That((Vector2)session.PlayerTransform.position, Is.EqualTo(new Vector2(9f,5f)));
            var traveler = session.PlayerTransform.GetComponentInChildren<SpriteRenderer>();
            foreach (string name in new[] { "M00_SPAWN_DOCK_Art", "M01_BRIDGE_Art", "M01_LOOKOUT_Art", "M02_WATERFALL_LOOKOUT_Art" })
            {
                var floor = root.GetComponentsInChildren<SpriteRenderer>().Single(renderer => renderer.name == name);
                Assert.That(floor.sortingOrder, Is.LessThan(traveler.sortingOrder), name + " is drawn in front of the spawned traveler.");
                Assert.That(floor.sortingOrder, Is.GreaterThan(-31500), name + " disappeared under its terrain tilemaps.");
            }
            var dock = root.GetComponentsInChildren<SpriteRenderer>().Single(renderer => renderer.name == "M00_SPAWN_DOCK_Art");
            var bridge = root.GetComponentsInChildren<SpriteRenderer>().Single(renderer => renderer.name == "M01_BRIDGE_Art");
            Assert.That(Resources.Load<Sprite>("Art/MapDesignAdditions/Dock"), Is.Not.Null, "The standalone approved dock was not imported.");
            Assert.That(dock.sprite, Is.SameAs(Resources.Load<Sprite>("Art/MapDesignAdditions/Dock")), "The spawn dock silently fell back to the old rope-platform image.");
            Assert.That(bridge.sprite, Is.SameAs(Resources.Load<Sprite>("Art/MapDesignAdditions/Bridge")));
        }

        private PolygonCollider2D Foundation(string id)
            => root.GetComponentsInChildren<PolygonCollider2D>().Single(value => value.name == "Foundation-" + id);
        private void Place(Vector2 point)
        { body.linearVelocity = Vector2.zero; body.position = point; body.transform.position = point; Physics2D.SyncTransforms(); }
        private IEnumerator MoveToward(Vector2 target)
        {
            Vector2 direction = (target - body.position).normalized;
            int steps = Mathf.CeilToInt(Vector2.Distance(body.position,target) / (4.2f * Time.fixedDeltaTime)) + 5;
            for (int i = 0; i < steps && Vector2.Dot(target - body.position,direction) > .025f; i++)
            { body.linearVelocity = direction * 4.2f; yield return new WaitForFixedUpdate(); }
            body.linearVelocity = Vector2.zero;
        }
        private IEnumerator ClearLeg(Vector2 start, Vector2 end)
        {
            Place(start); yield return new WaitForFixedUpdate();
            yield return MoveToward(end);
            Assert.That(Vector2.Distance(body.position,end), Is.LessThan(.15f), "An air wall stopped a real traveler body: " + start + " -> " + end + ", stopped at " + body.position);
        }
        private IEnumerator SolidLeg(Vector2 start, Vector2 target, Collider2D wall)
        {
            Place(start); yield return new WaitForFixedUpdate();
            yield return MoveToward(target);
            Assert.That(Vector2.Distance(body.position,target), Is.GreaterThan(.2f), "The traveler passed through " + wall.name);
            Assert.That(feet.IsTouching(wall), Is.True, "The tested movement did not actually contact " + wall.name);
        }
    }
}
