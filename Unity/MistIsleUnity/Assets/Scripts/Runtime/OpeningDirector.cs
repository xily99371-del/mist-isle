using System;
using System.Linq;
using MistIsle.Creatures;
using UnityEngine;

namespace MistIsle.Runtime
{
    // Coordinates the authored first meeting and capture lesson; battle and inventory retain ownership of their rules.
    public sealed class OpeningDirector : MonoBehaviour
    {
        private GameSession session;
        private OpeningStory story;
        private string guideRoom;
        private int guideWaypoint;
        private static readonly string[] CampGuideRoute = { "guide-camp-waypoint", "guide-exit-wait" };
        private static readonly string[] CoastGuideRoute = { "coast-waypoint-1", "coast-waypoint-2", "coast-waypoint-3", "tutorial-viewpoint" };
        private CompanionTrail companion;
        private OpeningStage Stage => session.OpeningPhase;
        private string Partner => session.Catalog.TryGetValue(session.Progress.selectedStarterSpeciesId ?? "", out var s) ? s.displayName : "伙伴";

        public void Initialize(GameSession owner)
        {
            session = owner;
            story = Resources.Load<OpeningStory>("Data/OpeningStory");
            if (story == null) story = OpeningStory.CreateDefault();
        }

        public string Objective
        {
            get
            {
                switch (Stage)
                {
                    case OpeningStage.Arrival: return "循着营地的灯火，踏上雾屿。";
                    case OpeningStage.MeetWatcher: return "去照护屋前，向 Watcher 打个招呼。";
                    case OpeningStage.MeetShiguang: return "到伙伴小院，与戴护目镜的拾光交谈。";
                    case OpeningStage.ChooseCompanion: return "走近小院里的三位伙伴，听听它们的回应。";
                    case OpeningStage.GatherIngredients: return "为受惊的潮壳蟹准备食物：采集野莓与露水。";
                    case OpeningStage.CraftTreat: return "回到篝火旁，制作一份莓果团。";
                    case OpeningStage.FollowGuide: return "跟随拾光，沿东侧小路去海边。";
                    case OpeningStage.FirstEncounter: return "放慢脚步，让潮壳蟹愿意回应你。";
                    case OpeningStage.ReportWatcher: return "带新伙伴回到营地，请 Watcher 检查伤势。";
                    default: return null;
                }
            }
        }

        public string Hint
        {
            get
            {
                if (session.Battle != null && session.Battle.IsCaptureLesson)
                    return session.Battle.CaptureLessonWeakened ? "拾光：它安静下来了。现在可以投喂莓果团，也可以尝试建立共鸣。" : "拾光：先让伙伴轻轻牵制一下，别急着伸手。";
                if (Stage == OpeningStage.GatherIngredients)
                    return "野莓 " + Mathf.Min(2, session.Inventory.GetCount("berry")) + "/2  ·  露水 " + Mathf.Min(1, session.Inventory.GetCount("dew")) + "/1  ·  靠近采集点按 E";
                if (Stage == OpeningStage.ChooseCompanion) return "靠近伙伴按 E；先听介绍，再决定与谁同行。";
                return Stage < OpeningStage.Complete && Stage > OpeningStage.None ? "沿着营地小路探索，靠近人物或物件按 E。" : "";
            }
        }

        public void Begin()
        {
            ClearCompanion();
            guideRoom = null;
            session.World.MoveActorFacing("shiguang", session.World.GetLandmarkPosition("shiguang-home"), Vector2.down);
            foreach (string id in GameCatalog.StarterIds) session.World.SetActorVisible(id, true);
            foreach (var target in session.World.Interactions)
                if (target.Id == "tutorial-coast-crab") target.SetAvailable(true);
            SetStage(OpeningStage.Arrival);
            session.ChangeRoom("camp-0", false);
            Show("arrival", () => SetStage(OpeningStage.MeetWatcher));
        }

        public void Restore()
        {
            guideRoom = null;
            RefreshCompanion();
            if (session.Progress.captureTutorialCompleted) session.World.SetCollected("tutorial-coast-crab");
            if (Stage == OpeningStage.Arrival) Show("arrival", () => SetStage(OpeningStage.MeetWatcher));
            else if (Stage == OpeningStage.GatherIngredients) Show("gather", CheckIngredients);
            else if (Stage == OpeningStage.FirstEncounter)
            {
                SetStage(OpeningStage.FollowGuide);
            }
            if (Stage == OpeningStage.FollowGuide) RestoreGuideAlongRoute();
            if (Stage == OpeningStage.ReportWatcher)
                session.World.MoveActorFacing("shiguang", session.World.GetLandmarkPosition("shiguang-approach"), Vector2.down);
        }

        public bool HandleInteraction(string kind, string id)
        {
            if (Stage == OpeningStage.None || Stage == OpeningStage.Complete)
            {
                if (kind == "starter") { session.Say("拾光", "它们仍在这里等着新的旅人。你已经有一位初始伙伴了，之后还有机会再重逢。"); return true; }
                return false;
            }
            if (kind == "npc" && id == "watcher")
            {
                if (Stage == OpeningStage.MeetWatcher)
                {
                    Show("watcher", () => SetStage(OpeningStage.MeetShiguang));
                }
                else if (Stage == OpeningStage.ReportWatcher)
                {
                    session.Recover(false);
                    Show("report", () => SetStage(OpeningStage.Complete));
                }
                else return false;
                return true;
            }
            if (kind == "npc" && id == "shiguang")
            {
                if (Stage == OpeningStage.MeetShiguang || Stage == OpeningStage.ChooseCompanion)
                {
                    Show("guide", () => SetStage(OpeningStage.ChooseCompanion));
                }
                else if (Stage == OpeningStage.MeetWatcher) session.Say("拾光", "你刚上岸吧？先去照护屋前找 Watcher，他一直在看着栈桥。我们等你暖和些再聊。");
                else if (Stage == OpeningStage.GatherIngredients) Show("gather");
                else if (Stage == OpeningStage.CraftTreat) Show("ingredients");
                else if (Stage == OpeningStage.FollowGuide) session.Say("拾光", "准备好了就跟上我。小家伙在东侧海边的礁石旁，还不敢回到水里。");
                else if (Stage == OpeningStage.ReportWatcher) session.Say("拾光", "先带它回 Watcher 那里吧。它很努力地跟着你，一定也累了。");
                return true;
            }
            if (kind == "starter")
            {
                if (Stage == OpeningStage.ChooseCompanion)
                    Show("meet-" + id, () => { session.FocusStory(session.World.GetLandmarkPosition("starter-meeting")); session.OpenStarterChoice(id); });
                else session.Say("旅人", "它正好奇地望着我。先问问拾光吧。");
                return true;
            }
            if (kind == "campfire" && Stage < OpeningStage.GatherIngredients)
            { session.Say("旅人", "篝火旁很暖和。屋前似乎有人在招呼我。"); return true; }
            if (kind == "encounter")
            {
                if (id == "tutorial-coast-crab" && Stage == OpeningStage.FollowGuide)
                {
                    SetStage(OpeningStage.FirstEncounter);
                    Show("crab", session.BeginCaptureLesson);
                }
                else session.Notify(Stage < OpeningStage.FollowGuide ? "先和拾光一起做好出发的准备。" : "先照顾好海边那位受惊的小家伙吧。");
                return true;
            }
            return false;
        }

        public bool CanTravel(string roomId)
        {
            if (Stage == OpeningStage.None || Stage == OpeningStage.Complete) return true;
            bool allowed = roomId == "camp-0" || ((Stage == OpeningStage.FollowGuide || Stage == OpeningStage.FirstEncounter || Stage == OpeningStage.ReportWatcher) && roomId == "coast-0");
            if (!allowed) session.Notify(Objective);
            return allowed;
        }

        public void OnChosen(string speciesId)
        {
            session.World.SetActorVisible(speciesId, false);
            SetStage(OpeningStage.GatherIngredients);
            RefreshCompanion();
            Show("bond-" + speciesId, () => Show("gather", CheckIngredients));
        }

        public void CheckIngredients()
        {
            if (Stage != OpeningStage.GatherIngredients || session.Inventory.GetCount("berry") < 2 || session.Inventory.GetCount("dew") < 1) return;
            SetStage(OpeningStage.CraftTreat);
            Show("ingredients");
        }

        public void OnCrafted(string recipe)
        {
            if (recipe != "berry-treat" || (Stage != OpeningStage.CraftTreat && Stage != OpeningStage.GatherIngredients)) return;
            session.Inventory.Add("resonance-capsule", 1);
            SetStage(OpeningStage.FollowGuide);
            guideRoom = null;
            Show("food-ready");
        }

        public void OnBattleEnded(BattleOutcome result, bool lesson)
        {
            RefreshCompanion();
            if (!lesson) return;
            if (result == BattleOutcome.Captured)
            {
                session.Progress.captureTutorialCompleted = true;
                session.World.SetCollected("tutorial-coast-crab");
                SetStage(OpeningStage.ReportWatcher);
                Show("captured");
            }
            else
            {
                SetStage(OpeningStage.FollowGuide);
                if (result == BattleOutcome.Defeat)
                    session.World.MoveActorFacing("shiguang", session.World.GetLandmarkPosition("shiguang-home"), Vector2.down);
                if (session.Inventory.GetCount("berry-treat") == 0 && session.Inventory.GetCount("resonance-capsule") == 0)
                    session.Inventory.Add("resonance-capsule", 1);
                session.Say("拾光", "不用着急。让伙伴缓一缓，等你准备好了，我们再一起试一次。");
            }
        }

        public void OnRoomEntered()
        {
            session.ClearStoryFocus();
            if (companion != null) companion.Snap();
            if (Stage >= OpeningStage.FollowGuide && Stage <= OpeningStage.FirstEncounter && session.Progress.roomId == "coast-0")
            {
                session.World.MoveActorFacing("shiguang", session.World.GetLandmarkPosition("coast-arrival") + new Vector2(1.3f, .5f), Vector2.right);
                guideRoom = "coast-0";
                guideWaypoint = 0;
            }
            else if (Stage == OpeningStage.ReportWatcher && session.Progress.roomId == "camp-0")
                session.World.MoveActorFacing("shiguang", session.World.GetLandmarkPosition("shiguang-approach"), Vector2.down);
        }

        public void FocusSpeaker(string speaker)
        {
            if (session == null || session.World == null) return;
            var actor = speaker == "shiguang" || speaker == "watcher" ? session.World.GetActor(speaker) : session.PlayerTransform;
            if (actor != null && session.World.GetRoomId(actor.position) == session.Progress.roomId)
                session.FocusStory(Vector2.Lerp(session.PlayerTransform.position, actor.position, .45f));
        }

        private void Show(string beat, Action after = null) => session.SayPages(story.Get(beat, Partner), after);
        private void SetStage(OpeningStage stage) { session.Progress.openingStage = (int)stage; session.Save(false); }

        private string[] GuideRoute(string room) => room == "camp-0" ? CampGuideRoute : CoastGuideRoute;

        private void RestoreGuideAlongRoute()
        {
            string room = session.Progress.roomId;
            if (room != "camp-0" && room != "coast-0") return;
            var points = GuideRoute(room);
            Vector2 start = room == "camp-0"
                ? session.World.GetLandmarkPosition("campfire-approach") + new Vector2(1.6f, 1.1f)
                : session.World.GetLandmarkPosition("coast-arrival") + new Vector2(1.3f, .5f);
            Vector2 player = session.PlayerTransform.position;
            float bestDistance = float.MaxValue;
            Vector2 waitingPoint = start;
            Vector2 facing = Vector2.right;
            guideWaypoint = 0;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 end = session.World.GetLandmarkPosition(points[i]);
                Vector2 segment = end - start;
                float along = segment.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(player - start, segment) / segment.sqrMagnitude) : 0f;
                Vector2 nearest = start + segment * along;
                float distance = (player - nearest).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    guideWaypoint = i;
                    facing = segment.sqrMagnitude > .001f ? segment.normalized : Vector2.down;
                    // Wait a little ahead on the same route, rather than returning to a distant room entrance.
                    waitingPoint = Vector2.MoveTowards(nearest, end, 1.1f);
                }
                start = end;
            }
            guideRoom = room;
            session.World.MoveActorFacing("shiguang", waitingPoint, facing);
        }

        private void Update()
        {
            if (session == null || session.Panel != GamePanel.Explore) return;
            var guide = session.World.GetActor("shiguang");
            if (guide == null) return;
            if ((Stage == OpeningStage.GatherIngredients || Stage == OpeningStage.CraftTreat) && session.Progress.roomId == "camp-0")
            {
                MoveGuide(guide, session.World.GetLandmarkPosition("campfire-approach") + new Vector2(1.6f, 1.1f), false);
            }
            else if (Stage == OpeningStage.FollowGuide)
            {
                string room = session.Progress.roomId;
                if (room != "camp-0" && room != "coast-0") return;
                if (guideRoom != room) { guideRoom = room; guideWaypoint = 0; }
                string[] points = GuideRoute(room);
                var target = session.World.GetLandmarkPosition(points[Mathf.Min(guideWaypoint, points.Length - 1)]);
                if (Vector2.Distance(guide.position, target) < .12f && guideWaypoint < points.Length - 1) guideWaypoint++;
                else MoveGuide(guide, target, true);
            }
        }
        private void MoveGuide(Transform guide, Vector2 target, bool waitForPlayer)
        {
            if (waitForPlayer && Vector2.Distance(session.PlayerTransform.position, guide.position) > 4.2f
                && RouteProgress(session.PlayerTransform.position) <= RouteProgress(guide.position)) return;
            Vector2 delta = target - (Vector2)guide.position;
            if (delta.sqrMagnitude < .012f) return;
            session.World.MoveActorFacing("shiguang", Vector2.MoveTowards(guide.position, target, 2.05f * Time.deltaTime), delta.normalized);
        }

        private float RouteProgress(Vector2 point)
        {
            string room = session.Progress.roomId;
            Vector2 start = room == "camp-0"
                ? session.World.GetLandmarkPosition("campfire-approach") + new Vector2(1.6f, 1.1f)
                : session.World.GetLandmarkPosition("coast-arrival") + new Vector2(1.3f, .5f);
            float walked = 0f, nearestDistance = float.MaxValue, progress = 0f;
            foreach (string id in GuideRoute(room))
            {
                Vector2 end = session.World.GetLandmarkPosition(id);
                Vector2 segment = end - start;
                float along = segment.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude) : 0f;
                float distance = (point - (start + segment * along)).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    progress = walked + segment.magnitude * along;
                }
                walked += segment.magnitude;
                start = end;
            }
            return progress;
        }
        private void ClearCompanion() { if (companion != null) Destroy(companion.gameObject); companion = null; }
        public void RefreshCompanion()
        {
            if (session.Party.Count == 0) { ClearCompanion(); return; }
            var starter = session.Party.Members.FirstOrDefault(c => c.isStarter) ?? session.Party.Members[0];
            if (companion == null)
            {
                var obj = new GameObject("同行伙伴");
                obj.transform.SetParent(session.transform, false);
                companion = obj.AddComponent<CompanionTrail>();
            }
            companion.Configure(session.PlayerTransform, session.Catalog[starter.speciesId]);
            foreach (var id in GameCatalog.StarterIds)
                session.World.SetActorVisible(id, !session.Party.Members.Any(c => c.speciesId == id || c.speciesId == id + "-evolved"));
        }
    }

    public sealed class CompanionTrail : MonoBehaviour
    {
        private Transform leader;
        private SpriteRenderer visual;
        private readonly System.Collections.Generic.Queue<Vector2> trail = new System.Collections.Generic.Queue<Vector2>();
        private Vector2 lastSample;
        public void Configure(Transform target, CreatureSpecies species)
        {
            leader = target;
            if (visual == null) visual = gameObject.AddComponent<SpriteRenderer>();
            string pixelKey = species.speciesId == "moss-lumi" ? "PixelMossLumi" : species.speciesId == "moatail" ? "PixelMoatail" : species.speciesId == "crystal-gecko" ? "PixelCrystalGecko" : null;
            visual.sprite = pixelKey == null ? Resources.Load<Sprite>(species.spriteResourcePath) : Resources.Load<Sprite>("Art/Creatures/" + pixelKey);
            visual.sharedMaterial = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            if (visual.sprite != null) transform.localScale = Vector3.one * (1.02f / visual.sprite.bounds.size.y);
            Snap();
        }
        public void Snap()
        {
            if (leader == null) return;
            trail.Clear();
            transform.position = leader.position + new Vector3(-.65f, -.25f);
            lastSample = leader.position;
        }
        private void LateUpdate()
        {
            if (leader == null) return;
            if (Vector2.Distance(lastSample, leader.position) > .24f) { trail.Enqueue(leader.position); lastSample = leader.position; }
            while (trail.Count > 70) trail.Dequeue();
            if (trail.Count > 3)
            {
                transform.position = Vector2.MoveTowards(transform.position, trail.Peek(), 4.5f * Time.deltaTime);
                if (Vector2.Distance(transform.position, trail.Peek()) < .08f) trail.Dequeue();
            }
            visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }
}
