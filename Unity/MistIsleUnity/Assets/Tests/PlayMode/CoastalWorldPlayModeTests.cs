using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MistIsle.World;
using MistIsle.Player;
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
            player.transform.position = new Vector2(-8f, 1.5f);
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

        [UnityTest]
        public IEnumerator ArrivalDockAndOpeningLandmarksHaveRealWalkingRoutesAndSolidWater()
        {
            Assert.That(world.GetSpawn("camp-0"),Is.EqualTo(world.GetLandmarkPosition("dock-arrival")));
            Vector2 water=new Vector2(-14f,-6f);
            Assert.That(Physics2D.OverlapCircleAll(water,.15f).Any(shape=>!shape.isTrigger&&
                shape is CompositeCollider2D&&shape.transform.IsChildOf(root.transform)),Is.True,
                "The bay is visually water but does not physically stop the player.");
            string[] campTargets={"dock-end","watcher-approach","campfire-approach","shiguang-approach",
                "starter-meeting","berry-camp-0-0","dew-camp-0-1","pollen-camp-0-2","guide-camp-waypoint","guide-exit-wait","coast-exit"};
            foreach(string id in campTargets)
                Assert.That(HasWalkingPath(world.GetSpawn("camp-0"),world.GetLandmarkPosition(id),world.GetBounds("camp-0")),
                    Is.True,"The opening route cannot reach "+id+" with the player's footprint.");
            string[] coastTargets={"coast-waypoint-1","coast-waypoint-2","coast-waypoint-3","tutorial-viewpoint",
                "berry-coast-0-0","dew-coast-0-1","pollen-coast-0-2"};
            foreach(string id in coastTargets)
                Assert.That(HasWalkingPath(world.GetSpawn("coast-0"),world.GetLandmarkPosition(id),world.GetBounds("coast-0")),
                    Is.True,"The coast teaching path cannot reach "+id);
            var tutorial=world.Interactions.Single(item=>item.Kind=="encounter"&&item.Id=="tutorial-coast-crab");
            Assert.That(world.GetRoomId(tutorial.transform.position),Is.EqualTo("coast-0"));
            Assert.That(world.Interactions.Count(item=>item.Kind=="encounter"&&world.GetRoomId(item.transform.position)=="coast-0"),
                Is.EqualTo(1),"Unscripted encounters distract from the first capture lesson.");
            var northernEdges=root.GetComponentsInChildren<BoxCollider2D>().Where(shape=>shape.name=="NorthernCliffFootprint").ToArray();
            Assert.That(northernEdges.Length,Is.EqualTo(32),"Both opening rooms need a physical northern cliff edge.");
            foreach(var edge in northernEdges)
                Assert.That(edge.bounds.min.y,Is.GreaterThan(6.1f),"Northern cliff collision encroached on the task area.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StoryActorsMoveTheirPhysicalFootprintAndTheThreeStartersAreRealInteractables()
        {
            var actor=world.GetActor("shiguang");
            Assert.That(actor,Is.Not.Null);
            foreach(string id in new[] {"watcher","shiguang"})
            {
                var humanRenderer=world.GetActor(id).GetComponentInChildren<SpriteRenderer>();
                Assert.That(ActorVisualSizing.GetVisibleWorldHeight(humanRenderer),
                    Is.EqualTo(ActorVisualSizing.HumanVisibleHeight).Within(.015f),
                    "Transparent canvas margins changed the visible height of "+id);
            }
            var footprint=actor.GetComponent<CapsuleCollider2D>();
            Assert.That(footprint,Is.Not.Null);
            var interaction=world.Interactions.Single(item=>item.Kind=="npc"&&item.Id=="shiguang");
            Vector2 original=actor.position;
            Vector2 triggerOffset=(Vector2)interaction.transform.position-original;
            Vector2 moved=original+new Vector2(-.8f,-.3f);
            world.MoveActorFacing("shiguang",moved,Vector2.left);
            Physics2D.SyncTransforms();
            Assert.That(Vector2.Distance(interaction.transform.position,moved+triggerOffset),Is.LessThan(.001f));
            Assert.That(Vector2.Distance(footprint.bounds.center,moved+footprint.offset),Is.LessThan(.001f));
            world.SetActorVisible("shiguang",false);
            Assert.That(actor.gameObject.activeInHierarchy,Is.False);
            world.SetActorVisible("shiguang",true);
            foreach(string id in new[] {"moss-lumi","moatail","crystal-gecko"})
            {
                Assert.That(world.GetActor(id),Is.Not.Null,"Starter has no actual courtyard entity: "+id);
                var starter=world.Interactions.Single(item=>item.Kind=="starter"&&item.Id==id);
                starter.Interact();
                Assert.That(events,Does.Contain("starter:"+id));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuideWaypointsRemainClearForPhysicalWalkingAfterTheCampAndCoastRebuild()
        {
            Vector2[] campRoute={world.GetLandmarkPosition("campfire-approach")+new Vector2(1.6f,1.1f),world.GetLandmarkPosition("guide-camp-waypoint"),
                world.GetLandmarkPosition("guide-exit-wait")};
            Vector2[] coastRoute={world.GetLandmarkPosition("coast-arrival")+new Vector2(1.3f,.5f),world.GetLandmarkPosition("coast-waypoint-1"),
                world.GetLandmarkPosition("coast-waypoint-2"),world.GetLandmarkPosition("coast-waypoint-3"),
                world.GetLandmarkPosition("tutorial-viewpoint")};
            Transform guide=world.GetActor("shiguang");
            foreach(var route in new[] {campRoute,coastRoute})
                for(int index=1;index<route.Length;index++)
                {
                    Vector2 from=route[index-1]+Vector2.up*.16f,to=route[index]+Vector2.up*.16f;
                    var circleHits=Physics2D.CircleCastAll(from,.16f,(to-from).normalized,Vector2.Distance(from,to));
                    var capsuleHits=Physics2D.CapsuleCastAll(from,new Vector2(.45f,.32f),CapsuleDirection2D.Horizontal,0f,
                        (to-from).normalized,Vector2.Distance(from,to));
                    var obstacles=circleHits.Concat(capsuleHits)
                        .Where(hit=>!hit.collider.isTrigger&&hit.collider.transform.IsChildOf(root.transform)&&
                            !hit.collider.transform.IsChildOf(guide)).ToArray();
                    Assert.That(obstacles,Is.Empty,"The guide's authored straight segment crosses physical geometry: "+
                        route[index-1]+" -> "+route[index]);
                }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SensorSeesActorsAndBothCampfireLayoutsWithoutTheirOwnBodiesOccludingThem()
        {
            var player=new GameObject("RealConversationSensor",typeof(PlayerInteractionSensor));
            player.transform.SetParent(root.transform,false);
            var sensor=player.GetComponent<PlayerInteractionSensor>();
            sensor.Configure(Physics2D.AllLayers,1.4f);
            string[] targets={"watcher","shiguang","moss-lumi","moatail","crystal-gecko","campfire-camp-0","campfire-glowglade-0"};
            foreach(string id in targets)
            {
                var target=world.Interactions.Single(item=>item.Id==id);
                Vector2 approach=(Vector2)target.transform.position+
                    (id=="campfire-glowglade-0"?Vector2.up:Vector2.down)*.95f;
                player.transform.position=approach;
                Physics2D.SyncTransforms();
                yield return null;
                Assert.That(sensor.Current,Is.SameAs(target),
                    "The real proximity/line-of-sight query cannot acquire "+id+" from an open approach.");
            }
            var watcher=world.Interactions.Single(item=>item.Id=="watcher");
            player.transform.position=watcher.transform.position+Vector3.down;
            var wall=new GameObject("ActualWallBetweenConversation",typeof(BoxCollider2D));
            wall.transform.SetParent(root.transform,false);
            wall.transform.position=watcher.transform.position+Vector3.down*.5f;
            wall.GetComponent<BoxCollider2D>().size=new Vector2(1.5f,.12f);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(sensor.Current,Is.Not.SameAs(watcher),"Ignoring a target's own body also ignored a real wall.");
        }

        private bool HasWalkingPath(Vector2 start,Vector2 goal,Rect bounds)
        {
            const float step=.5f;
            int width=Mathf.RoundToInt(bounds.width/step),height=Mathf.RoundToInt(bounds.height/step);
            Vector2Int Cell(Vector2 point)=>new Vector2Int(Mathf.Clamp(Mathf.RoundToInt((point.x-bounds.xMin)/step),0,width-1),
                Mathf.Clamp(Mathf.RoundToInt((point.y-bounds.yMin)/step),0,height-1));
            Vector2 Point(Vector2Int cell)=>new Vector2(bounds.xMin+cell.x*step,bounds.yMin+cell.y*step);
            var from=Cell(start); var target=Cell(goal);
            var pending=new Queue<Vector2Int>(); var seen=new HashSet<Vector2Int>();
            pending.Enqueue(from);seen.Add(from);
            Vector2Int[] directions={Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
            while(pending.Count>0)
            {
                var at=pending.Dequeue();
                if(at==target) return true;
                foreach(var direction in directions)
                {
                    var next=at+direction;
                    if(next.x<0||next.y<0||next.x>=width||next.y>=height||!seen.Add(next)) continue;
                    bool blocked=Physics2D.OverlapCircleAll(Point(next)+Vector2.up*.17f,.22f)
                        .Any(shape=>!shape.isTrigger&&shape.transform.IsChildOf(root.transform));
                    if(!blocked) pending.Enqueue(next);
                }
            }
            return false;
        }
    }
}
