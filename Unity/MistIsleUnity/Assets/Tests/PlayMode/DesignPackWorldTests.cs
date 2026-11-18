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
    public sealed class DesignPackWorldTests
    {
        private GameObject root;
        private CoastalWorldLayout world;
        private readonly List<string> events = new List<string>();
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            events.Clear(); root = new GameObject("DesignPackWorldTest");
            world = DesignPackWorldBuilder.Build(root.transform, (kind, id) => events.Add(kind + ":" + id));
            Physics2D.SyncTransforms(); yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown() { if (root != null) Object.Destroy(root); yield return null; }

        [UnityTest]
        public IEnumerator FullMapsHaveTenIndependentLayersAndCompositeWaterCollision()
        {
            Assert.That(world.RoomIds, Is.EquivalentTo(new[] { "camp-0", "coast-0", "glowglade-0", "tidal-0", "lighthouse-0" }));
            Assert.That(root.GetComponentsInChildren<Tilemap>().Length, Is.EqualTo(50));
            Assert.That(root.GetComponentsInChildren<CompositeCollider2D>().Length, Is.EqualTo(5));
            foreach (var map in DesignPackCatalog.Maps) {
                Assert.That(world.GetBounds(map.RoomId), Is.EqualTo(map.Bounds));
                var grid = root.transform.Find("DesignPackWorld/" + map.Id + " — " + map.Name).GetComponent<Grid>();
                Assert.That(grid.cellSize, Is.EqualTo(Vector3.one));
                foreach (string layer in DesignPackWorldBuilder.LayerNames) Assert.That(grid.transform.Find(layer), Is.Not.Null, map.Id + "/" + layer);
            }
            Vector2 water = DesignPackCatalog.GetMap("M01").Origin + new Vector2(33f, 10f);
            Assert.That(SolidsAt(water, .1f).Any(), Is.True, "The stream is decorative and has no real collision.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArrivalSpawnsAndAllExitMouthsArePhysicallyOpen()
        {
            foreach (var map in DesignPackCatalog.Maps) {
                Assert.That(SolidsAt(map.Spawn, .24f), Is.Empty, "Blocked initial arrival " + map.Id);
                foreach (var exit in map.Exits) {
                    Assert.That(SolidsAt(exit.Spawn, .24f), Is.Empty, "Blocked paired spawn " + exit.Id);
                    Assert.That(SolidsAt(exit.TriggerRect.center, .24f), Is.Empty, "Coarse boundary masks the exit " + exit.Id);
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrunksGardenFencesAndGuideWorkbenchHaveIndependentPhysicalFootprints()
        {
            foreach (string name in new[] { "TreeTrunkFootprint", "CompanionGardenFenceFootprint", "GuideStationLeftPost", "GuideStationRightPost", "AncientTreeTrunk" }) {
                var bodies = root.GetComponentsInChildren<BoxCollider2D>().Where(body => body.name == name).ToArray();
                Assert.That(bodies, Is.Not.Empty, name + " has no real collision.");
                foreach (var body in bodies) {
                    Assert.That(body.isTrigger, Is.False);
                    Assert.That(Physics2D.OverlapPointAll(body.bounds.center), Does.Contain(body));
                }
            }
            var guideDesk = root.GetComponentsInChildren<PolygonCollider2D>().Single(body => body.name == "Foundation-M00_SHIGUANG_STATION");
            Assert.That(guideDesk.isTrigger, Is.False);
            Assert.That(Physics2D.OverlapPointAll(guideDesk.bounds.center), Does.Contain(guideDesk));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuideStraightSegmentsCannotPassThroughWaterBuildingsOrFurniture()
        {
            string[][] paths = {
                new[] { "campfire-approach", "guide-camp-waypoint", "guide-exit-wait" },
                new[] { "coast-arrival", "coast-waypoint-1", "coast-waypoint-2", "coast-waypoint-3", "tutorial-viewpoint" }
            };
            foreach (var path in paths)
                for (int i = 1; i < path.Length; i++) {
                    Vector2 a = world.GetLandmarkPosition(path[i - 1]);
                    if (path[i - 1] == "coast-arrival") a += Vector2.up * 1.1f;
                    Vector2 b = world.GetLandmarkPosition(path[i]);
                    var hits = Physics2D.CircleCastAll(a, .25f, (b - a).normalized, Vector2.Distance(a, b))
                        .Where(hit => hit.collider != null && !hit.collider.isTrigger && hit.collider.transform.IsChildOf(root.transform)).ToArray();
                    Assert.That(hits, Is.Empty, path[i - 1] + " -> " + path[i] + " crosses solid geometry.");
                }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealSensorFindsBothNpcsThreeStartersShopAndCampfires()
        {
            var player = new GameObject("SensorProbe", typeof(PlayerInteractionSensor)); player.transform.SetParent(root.transform, false);
            int mask = LayerMask.GetMask("Interactable"); Assert.That(mask, Is.Not.EqualTo(0), "Configured game interaction layer is missing.");
            var sensor = player.GetComponent<PlayerInteractionSensor>(); sensor.Configure(mask, .6f);
            foreach (string id in new[] { "watcher", "shiguang", "moss-lumi", "moatail", "crystal-gecko", "M00_SUPPLY_SHOP", "M00_CAMPFIRE", "M04_CAMP" }) {
                var target = world.Interactions.Single(item => item.Id == id);
                player.transform.position = target.transform.position + Vector3.down * .75f;
                Physics2D.SyncTransforms(); yield return null;
                Assert.That(sensor.Current, Is.SameAs(target), "The real line-of-sight sensor cannot select " + id);
                Assert.That(sensor.TryInteractCurrent(), Is.True); Assert.That(events, Does.Contain(target.Kind + ":" + id));
            }
        }

        [UnityTest]
        public IEnumerator ResourcesDisappearAndFieldAndFinalGatesReallyRemoveCollision()
        {
            foreach (string id in new[] { "berry-camp-0-0", "dew-camp-0-1", "berry-coast-0-0", "dew-coast-0-1" }) {
                world.SetCollected(id); var resource = world.Interactions.Single(node => node.Id == id);
                Assert.That(resource.CanInteract, Is.False); Assert.That(resource.GetComponent<Collider2D>().enabled, Is.False);
            }
            foreach (string id in new[] { "M02_ROOT_BRIDGE", "M03_WATER_CHANNEL", "M03_CONDUIT", "M04_THREE_SEAL_GATE" }) {
                var barrier = root.GetComponentsInChildren<BoxCollider2D>().Single(body => body.name == "FieldBarrier-" + id);
                Assert.That(barrier.gameObject.activeInHierarchy, Is.True);
                Assert.That(world.OpenField(id), Is.True); Physics2D.SyncTransforms();
                Assert.That(barrier.gameObject.activeInHierarchy, Is.False, id + " only changed a popup.");
                world.ResetFields(field => field == id);
                Assert.That(world.IsFieldOpen(id), Is.True);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosedExitBarriersKeepTheirInteractionTriggersAvailableAfterOpening()
        {
            foreach (var exit in DesignPackCatalog.Maps.SelectMany(map => map.Exits)) {
                var barrier = root.GetComponentsInChildren<BoxCollider2D>(true).Single(body => body.name == "ExitBarrier-" + exit.Id);
                var interaction = world.Interactions.Single(item => item.Kind == "exit" && item.Id == exit.TargetRoom && world.GetRoomId(item.transform.position) == exit.SourceRoom);
                Assert.That(barrier.gameObject.activeInHierarchy, Is.True, exit.Id);
                Assert.That(interaction.CanInteract, Is.True);
                Assert.That(world.OpenField(exit.Id), Is.True);
                Assert.That(barrier.gameObject.activeInHierarchy, Is.False);
                Assert.That(interaction.CanInteract, Is.True, "Opening the barrier disabled the exit interaction.");
                Assert.That(interaction.GetComponent<Collider2D>().enabled, Is.True);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnteringTheExitWithARealDynamicBodyInvokesTheTransition()
        {
            var exit = DesignPackCatalog.GetExit("camp-0", "coast-0");
            var player = new GameObject("ExitProbe", typeof(PlayerInteractionSensor), typeof(Rigidbody2D), typeof(CircleCollider2D));
            player.transform.SetParent(root.transform, false); player.transform.position = exit.TriggerRect.center;
            player.GetComponent<Rigidbody2D>().gravityScale = 0f; player.GetComponent<CircleCollider2D>().radius = .2f;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(events, Does.Contain("exit:coast-0"), "A player entering the authored exit trigger does not transition.");
        }

        [UnityTest]
        public IEnumerator EveryInteractionAndReturnArrivalHasAWalkableConnectionToTheMapEntrance()
        {
            // Progression is tested in the session. Here unlocked doors isolate geometry errors
            // from intentional state gates, while actor feet and building/water collision stay real.
            world.ResetFields(_ => true); Physics2D.SyncTransforms();
            foreach (var map in DesignPackCatalog.Maps)
            {
                var walkable = new bool[map.Width, map.Height];
                for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
                    walkable[x, y] = !SolidsAt(map.Origin + new Vector2(x + .5f, y + .5f), .22f).Any();
                var reached = new bool[map.Width, map.Height]; var queue = new Queue<Vector2Int>();
                var start = Vector2Int.FloorToInt(map.Spawn - map.Origin); queue.Enqueue(start); reached[start.x, start.y] = true;
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                while (queue.Count > 0) {
                    var point = queue.Dequeue();
                    foreach (var direction in directions) {
                        var next = point + direction;
                        if (next.x < 0 || next.y < 0 || next.x >= map.Width || next.y >= map.Height || reached[next.x, next.y] || !walkable[next.x, next.y]) continue;
                        reached[next.x, next.y] = true; queue.Enqueue(next);
                    }
                }
                var targets = world.Interactions.Where(item => world.GetRoomId(item.transform.position) == map.RoomId)
                    .Select(item => new KeyValuePair<string, Vector2>(item.Id, item.transform.position)).ToList();
                targets.AddRange(DesignPackCatalog.Maps.SelectMany(other => other.Exits).Where(exit => exit.TargetMap == map.Id)
                    .Select(exit => new KeyValuePair<string, Vector2>(exit.Id + " return spawn", exit.Spawn)));
                foreach (var target in targets) {
                    bool accessible = false;
                    Vector2 local = target.Value - map.Origin;
                    for (int y = Mathf.Max(0, Mathf.FloorToInt(local.y) - 2); y <= Mathf.Min(map.Height - 1, Mathf.CeilToInt(local.y) + 2); y++)
                        for (int x = Mathf.Max(0, Mathf.FloorToInt(local.x) - 2); x <= Mathf.Min(map.Width - 1, Mathf.CeilToInt(local.x) + 2); x++)
                            if (reached[x, y] && Vector2.Distance(new Vector2(x + .5f, y + .5f), local) <= 1.2f) accessible = true;
                    Assert.That(accessible, Is.True, map.Id + " contains an unreachable interaction/arrival: " + target.Key);
                }
            }
            yield return null;
        }

        private IEnumerable<Collider2D> SolidsAt(Vector2 point, float radius)
            => Physics2D.OverlapCircleAll(point, radius).Where(body => !body.isTrigger && body.transform.IsChildOf(root.transform));
    }
}
