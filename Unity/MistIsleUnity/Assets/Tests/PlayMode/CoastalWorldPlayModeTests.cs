using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MistIsle.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MistIsle.Tests
{
    public sealed class CoastalWorldPlayModeTests
    {
        private GameObject root;
        private CoastalWorldLayout world;
        private readonly List<string> events = new List<string>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            events.Clear();
            root = new GameObject("WorldPlayModeTest");
            world = CoastalWorldBuilder.Build(root.transform, (kind, id) => events.Add(kind + ":" + id));
            Physics2D.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllRoomsAreReachableAndFourSealsLeadToTheObserversFinalPhase()
        {
            var ids = world.RoomIds.ToArray();
            Assert.That(ids.Length, Is.EqualTo(18));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(18));
            var visited = new HashSet<string> { "camp-0" };
            var pending = new Queue<string>();
            pending.Enqueue("camp-0");
            while (pending.Count > 0)
            {
                string room = pending.Dequeue();
                foreach (var exit in world.Interactions.Where(item => item.Kind == "exit" &&
                    world.GetRoomId(item.transform.position) == room))
                {
                    Assert.That(ids, Does.Contain(exit.Id), "Exit points to a nonexistent room.");
                    if (visited.Add(exit.Id)) pending.Enqueue(exit.Id);
                }
            }
            Assert.That(visited.Count, Is.EqualTo(18), "A room cannot be reached from the starting camp.");
            var guardians = world.Interactions.Where(item => item.Kind == "guardian").ToArray();
            Assert.That(guardians.Select(item => item.Id), Is.EquivalentTo(new[] {
                "glowglade-warden", "tidal-warden", "crystal-warden", "lighthouse-warden" }));
            var observer = guardians.Single(item => item.Id == "lighthouse-warden");
            Assert.That(world.GetRoomId(observer.transform.position), Is.EqualTo("lighthouse-1"));
            var boss = world.Interactions.Single(item => item.Kind == "boss" && item.Id == "final-boss");
            Assert.That(world.GetRoomId(boss.transform.position), Is.EqualTo("lighthouse-2"));
            var bossArt = root.GetComponentsInChildren<SpriteRenderer>().Single(item => item.name == "LighthouseFinalBoss").sprite;
            Assert.That(bossArt, Is.EqualTo(Resources.Load<Sprite>("Art/Creatures/GuardianLighthouse")),
                "The final phase must retain the approved Observer design.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryArrivalPointIsClearOfSolidWorldGeometry()
        {
            foreach (string room in world.RoomIds)
            {
                Vector2 spawn = world.GetSpawn(room);
                Assert.That(world.GetBounds(room).Contains(spawn), Is.True, "Spawn outside " + room);
                Collider2D[] obstacles = Physics2D.OverlapCircleAll(spawn, .25f)
                    .Where(shape => !shape.isTrigger && shape.transform.IsChildOf(root.transform)).ToArray();
                Assert.That(obstacles, Is.Empty, "Arrival would place the player inside collision in " + room);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DynamicPlayerStopsAtTheAuthoredHouseWall()
        {
            var house = root.GetComponentsInChildren<PolygonCollider2D>()
                .Single(shape => shape.name == "ShelterFootprint" && world.GetRoomId(shape.transform.position) == "camp-0");
            var player = new GameObject("CollisionTestPlayer", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            player.transform.SetParent(root.transform, false);
            player.transform.position = new Vector2(-7f, 1.5f);
            var body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var shape = player.GetComponent<CapsuleCollider2D>();
            shape.size = new Vector2(.4f,.4f);
            Physics2D.SyncTransforms();
            for (int tick = 0; tick < 60; tick++)
            {
                body.linearVelocity = new Vector2(0f, 4f);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity = Vector2.zero;
            Assert.That(body.position.y, Is.GreaterThan(2f), "Player never reached the house approach.");
            Assert.That(body.position.y, Is.LessThan(house.bounds.max.y), "Player passed through the house footprint.");
            Assert.That(shape.Distance(house).distance, Is.GreaterThanOrEqualTo(-.02f), "Player penetrated the authored wall.");
        }

        [UnityTest]
        public IEnumerator SensorTriggersResourcesAndExitsAndCollectedResourcesStayUnavailable()
        {
            var player = new GameObject("InteractionTestPlayer", typeof(PlayerInteractionSensor));
            player.transform.SetParent(root.transform, false);
            var sensor = player.GetComponent<PlayerInteractionSensor>();
            sensor.Configure(~0, .6f);
            var resource = world.Interactions.First(item => item.Kind == "resource" &&
                world.GetRoomId(item.transform.position) == "camp-0");
            player.transform.position = resource.transform.position;
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(sensor.Current, Is.SameAs(resource), "Nearby pickup was not discovered by the real interaction sensor.");
            sensor.Current.Interact();
            Assert.That(events, Does.Contain("resource:" + resource.Id));
            world.SetCollected(resource.Id);
            yield return null;
            Assert.That(resource.CanInteract, Is.False);
            Assert.That(resource.GetComponent<Collider2D>().enabled, Is.False);
            int count = events.Count;
            resource.Interact();
            Assert.That(events.Count, Is.EqualTo(count), "A collected resource fired a second pickup.");
            world.ResetCollected(id => id == resource.Id);
            Assert.That(resource.CanInteract, Is.False, "Loading the collection state respawned a collected pickup.");

            var exit = world.Interactions.Single(item => item.Kind == "exit" && item.Id == "coast-0" &&
                world.GetRoomId(item.transform.position) == "camp-0");
            player.transform.position = exit.transform.position;
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(sensor.Current, Is.SameAs(exit));
            sensor.Current.Interact();
            Assert.That(events, Does.Contain("exit:coast-0"));
        }

        [UnityTest]
        public IEnumerator FieldAbilitiesPhysicallyOpenOptionalRoutesAndSaveStateRestoresTheirCollision()
        {
            string[] regions = { "coast", "glowglade", "tidal", "crystal" };
            foreach (string region in regions)
            {
                string id = "field-" + region;
                var gate = root.GetComponentsInChildren<BoxCollider2D>(true)
                    .Single(shape => shape.name == "FieldBarrier-" + id);
                Vector2 center = gate.transform.position;
                Assert.That(world.IsFieldOpen(id), Is.False);
                Assert.That(Physics2D.LinecastAll(center + Vector2.down, center + Vector2.up)
                    .Any(hit => hit.collider == gate), Is.True, "Closed " + id + " has no physical barrier.");
                var cache = world.Interactions.Single(item => item.Kind == "resource" &&
                    item.Id.EndsWith("-field-cache") && world.GetRoomId(item.transform.position) == region + "-1");
                Assert.That(cache.CanInteract, Is.True, "Treasure should remain visible inside the closed side route.");
                Assert.That(Vector2.Distance(cache.transform.position, center + Vector2.down * .4f), Is.GreaterThan(2.25f),
                    "The treasure can be collected through the closed gate.");
                Assert.That(world.OpenField(id), Is.True);
                Physics2D.SyncTransforms();
                Assert.That(world.IsFieldOpen(id), Is.True);
                Assert.That(gate.gameObject.activeInHierarchy, Is.False);
                Assert.That(Physics2D.LinecastAll(center + Vector2.down, center + Vector2.up)
                    .Any(hit => hit.collider == gate), Is.False, "Opened " + id + " still blocks movement.");
                Vector2 roomCenter = world.GetBounds(region + "-1").center;
                Assert.That(Physics2D.LinecastAll(roomCenter + Vector2.left*11f, roomCenter + Vector2.right*11f)
                    .Any(hit => !hit.collider.isTrigger && hit.collider.transform.IsChildOf(root.transform)), Is.False,
                    "An optional ability gate blocks the region's main route.");
            }
            world.ResetFields(id => id == "field-glowglade");
            Physics2D.SyncTransforms();
            Assert.That(world.IsFieldOpen("field-glowglade"), Is.True);
            Assert.That(world.IsFieldOpen("field-tidal"), Is.False);
            Assert.That(world.IsFieldOpen("field-coast"), Is.False);
            Assert.That(world.IsFieldOpen("field-crystal"), Is.False);
            var restored = root.GetComponentsInChildren<BoxCollider2D>(true)
                .Single(shape => shape.name == "FieldBarrier-field-tidal");
            Assert.That(restored.gameObject.activeInHierarchy, Is.True, "Loading a closed gate did not restore its collider.");
            yield return null;
        }
    }
}
