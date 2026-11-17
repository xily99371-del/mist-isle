using System;
using System.Collections.Generic;
using System.Linq;
using MistIsle.Creatures;
using MistIsle.Items;
using MistIsle.Player;
using MistIsle.World;
using UnityEngine;

namespace MistIsle.Runtime
{
    public enum GamePanel { Title, Starter, Explore, Dialogue, Campfire, Watcher, Party, Storage, Journal, Battle, Ending }

    public sealed partial class GameSession : MonoBehaviour
    {
        public Dictionary<string, CreatureSpecies> Catalog { get; private set; }
        public List<CampfireRecipe> Recipes { get; private set; }
        public Inventory Inventory { get; private set; } = new Inventory();
        public PartyRoster Party { get; private set; } = new PartyRoster();
        public CompanionStorage Storage { get; private set; } = new CompanionStorage();
        public WorldSaveData Progress { get; private set; } = new WorldSaveData();
        public GamePanel Panel { get; private set; } = GamePanel.Title;
        public BattleSession Battle { get; private set; }
        public ChapterSaveStore Saves { get; private set; }
        public CoastalWorldLayout World { get; private set; }
        public string DialogueTitle { get; private set; }
        public string DialogueText { get; private set; }
        public string Toast { get; private set; }
        public float ToastUntil { get; private set; }
        public string RoomName => World == null ? "海岸营地" : World.GetRoomName(Progress.roomId);
        public string Objective => opening != null && opening.Objective != null ? opening.Objective : Progress.chapterCompleted ? "雾散之后：继续探索，补全伙伴图鉴。" :
            !Progress.seals.HasSeal("glowglade-warden") ? "前往辉光林地，寻找根系圣所的封印守卫。" :
            !Progress.seals.HasSeal("tidal-warden") ? "穿过潮汐遗迹，解除潮心祭坛的封印。" :
            !Progress.seals.HasSeal("crystal-warden") ? "前往鸣晶高地，解除晶能封印。" :
            !Progress.seals.HasSeal("lighthouse-warden") ? "三印已齐：前往灯塔，接受回响观测者的第四封印试炼。" : "四枚封印已齐：进入灯芯圣殿，回应雾海的呼唤。";

        private TopDownPlayerMotor motor;
        private PlayerInteractionSensor sensor;
        private CoastalCamera follow;
        private Action afterDialogue;
        private string encounterId;
        private AudioSource music;
        private float autosaveAt;
        public string SaveDirectoryOverride { get; set; }
        private GamePanel previousPanel;
        private static readonly string[] RegionIds = { "camp", "coast", "glowglade", "tidal", "crystal", "lighthouse" };

        private void Start()
        {
            Catalog = GameCatalog.CreateSpecies();
            var authored = Resources.LoadAll<CreatureSpecies>("Data/Species");
            foreach (var species in authored) Catalog[species.speciesId] = species;
            Recipes = GameCatalog.CreateRecipes();
            var authoredRecipes = Resources.LoadAll<CampfireRecipe>("Data/Recipes");
            if (authoredRecipes.Length > 0) Recipes = new List<CampfireRecipe>(authoredRecipes);
            Saves = new ChapterSaveStore(SaveDirectoryOverride ?? Environment.GetEnvironmentVariable("MIST_ISLE_SAVE_DIRECTORY"));
            World = CoastalWorldBuilder.Build(transform, HandleWorldAction, id => Progress.collectedNodeIds.Contains(id));
            CreatePlayer();
            CreateCamera();
            opening = gameObject.AddComponent<OpeningDirector>();
            opening.Initialize(this);
            gameObject.AddComponent<ChapterInterface>().Session = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = .22f;
            ChangeRoom("camp-0", false);
            SetPanel(GamePanel.Title);
            autosaveAt = Time.unscaledTime + 30f;
        }

        private void CreatePlayer()
        {
            var player = new GameObject("Traveler", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            player.transform.SetParent(transform, false);
            var collider = player.GetComponent<CapsuleCollider2D>();
            collider.size = new Vector2(.52f, .34f);
            collider.offset = new Vector2(0, .17f);
            collider.direction = CapsuleDirection2D.Horizontal;
            var material = new PhysicsMaterial2D("TravelerFrictionless") { friction = 0f, bounciness = 0f };
            collider.sharedMaterial = material;
            motor = player.AddComponent<TopDownPlayerMotor>();
            sensor = player.AddComponent<PlayerInteractionSensor>();
            sensor.Configure(LayerMask.NameToLayer("Interactable") >= 0 ? LayerMask.GetMask("Interactable") : Physics2D.AllLayers, 1.8f);
            var visual = new GameObject("TravelerSprite", typeof(SpriteRenderer));
            visual.transform.SetParent(player.transform, false);
            var sprite = visual.GetComponent<SpriteRenderer>();
            var frames = Resources.LoadAll<Sprite>("Art/Actors/ApprovedTraveler");
            if (frames.Length == 0) throw new InvalidOperationException("The approved pixel_people traveler frames must be imported before play.");
            Array.Sort(frames, (a, b) => {
                int.TryParse(a.name.Substring(a.name.LastIndexOf('_') + 1), out var ai);
                int.TryParse(b.name.Substring(b.name.LastIndexOf('_') + 1), out var bi);
                return ai.CompareTo(bi);
            });
            sprite.sharedMaterial = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            var animation = player.AddComponent<TravelerPresentation>();
            if (frames.Length >= 12)
            {
                int count = frames.Length / 4;
                animation.Down = frames.Take(count).ToArray();
                animation.Left = frames.Skip(count).Take(count).ToArray();
                animation.Right = frames.Skip(count * 2).Take(count).ToArray();
                animation.Up = frames.Skip(count * 3).Take(count).ToArray();
            }
            else animation.Down = animation.Left = animation.Right = animation.Up = frames;
            if (frames.Length > 0)
            {
                sprite.sprite = frames[0];
                float height = frames[0].bounds.size.y;
                visual.transform.localScale = Vector3.one * (1.62f / Mathf.Max(.01f, height));
            }
        }

        private void CreateCamera()
        {
            var camera = Camera.main;
            if (camera == null) camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.backgroundColor = new Color(.19f, .28f, .32f);
            follow = camera.gameObject.AddComponent<CoastalCamera>();
            follow.Target = motor.transform;
        }

        private void Update()
        {
            if (Catalog == null) return;
            if (Panel == GamePanel.Dialogue && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            { AdvanceDialogue(); return; }
            if (Panel != GamePanel.Title && Panel != GamePanel.Starter) Progress.playSeconds += Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Panel == GamePanel.Explore) OpenParty();
                else if (Panel == GamePanel.Dialogue) AdvanceDialogue();
                else if (Panel != GamePanel.Battle && Panel != GamePanel.Starter && Panel != GamePanel.Title) ClosePanel();
            }
            if (Panel == GamePanel.Explore)
            {
                if (Input.GetKeyDown(KeyCode.Tab)) OpenParty();
                if (Input.GetKeyDown(KeyCode.J)) SetPanel(GamePanel.Journal);
                if (Input.GetKeyDown(KeyCode.F5)) Save();
                if (Time.unscaledTime > autosaveAt)
                {
                    Save(false);
                    autosaveAt = Time.unscaledTime + 30f;
                }
            }
        }

        public void NewJourney()
        {
            try { Saves.ArchiveBeforeNewJourney(); }
            catch (Exception ex) { Notify("旧进度备份失败，已保留原存档：" + ex.Message); return; }
            Progress = new WorldSaveData();
            Party = new PartyRoster();
            Inventory = new Inventory();
            World.ResetCollected(_ => false);
            World.ResetFields(_ => false);
            Storage = new CompanionStorage();
            opening.Begin();
        }

        public void ChooseStarter(string id)
        {
            if (Panel != GamePanel.Starter || !string.IsNullOrEmpty(Progress.selectedStarterSpeciesId)) return;
            if (!Catalog.TryGetValue(id, out var species) || !species.isStarterSpecies) return;
            Party.TryAdd(new CreatureInstance(species, 3, true));
            Progress.selectedStarterSpeciesId = id;
            Discover(id);
            opening.OnChosen(id);
        }

        public void ContinueJourney()
        {
            if (!Saves.TryLoad(Catalog, out var loaded, out var message)) { Notify(message); return; }
            if (loaded.roster.Count == 0 && (loaded.openingStage <= 0 || loaded.openingStage > (int)OpeningStage.ChooseCompanion))
            { Notify("存档没有有效伙伴，原文件已保留。可选择新旅程并自动备份。 "); return; }
            Progress = loaded;
            Storage = new CompanionStorage(loaded.storedRoster);
            Party = new PartyRoster();
            foreach (var creature in loaded.roster) Party.TryAdd(creature);
            if (loaded.deployedIds.Count > 0) Party.TrySetDeployment(loaded.deployedIds, out _);
            Inventory = new Inventory();
            for (int i = 0; i < Math.Min(loaded.inventoryItemIds.Count, loaded.inventoryCounts.Count); i++)
                if (!string.IsNullOrEmpty(loaded.inventoryItemIds[i]) && loaded.inventoryCounts[i] > 0)
                    Inventory.Add(loaded.inventoryItemIds[i], loaded.inventoryCounts[i]);
            World.ResetCollected(id => Progress.collectedNodeIds.Contains(id));
            World.ResetFields(id => Progress.completedFieldIds.Contains(id));
            if (!ChangeRoom(Progress.roomId, false)) ChangeRoom("camp-0", false);
            var savedPoint = new Vector2(Progress.playerX, Progress.playerY);
            bool blocked = Physics2D.OverlapCircleAll(savedPoint + new Vector2(0f, .17f), .3f)
                .Any(shape => !shape.isTrigger && !shape.transform.IsChildOf(motor.transform));
            if (Progress.hasPosition && follow.Bounds.Contains(savedPoint) && !blocked)
            { motor.GetComponent<Rigidbody2D>().position = savedPoint; motor.transform.position = savedPoint; }
            follow.Snap();
            SetPanel(GamePanel.Explore);
            opening.Restore();
            Notify(message);
        }

        public void SetPanel(GamePanel panel)
        {
            Panel = panel;
            if (motor != null) motor.InputEnabled = panel == GamePanel.Explore;
            if (sensor != null) sensor.InputEnabled = panel == GamePanel.Explore;
            if (panel != GamePanel.Explore && motor != null) motor.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }

        public void ClosePanel()
        {
            if (Panel == GamePanel.Dialogue) { AdvanceDialogue(); return; }
            var callback = afterDialogue;
            afterDialogue = null;
            SetPanel(GamePanel.Explore);
            callback?.Invoke();
        }

        public void Say(string title, string text, Action afterward = null)
        {
            string speaker = title.Contains("拾光") ? "shiguang" : title.Contains("Watcher") ? "watcher" : title == "旅人" ? "traveler" : "narrator";
            var pages = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Select(words => new StoryLine(speaker, words)).ToArray();
            SayPages(pages, afterward);
            if (speaker == "narrator") DialogueTitle = title;
        }

        public void Notify(string text)
        {
            Toast = text;
            ToastUntil = Time.unscaledTime + 5f;
        }

        public void HandleWorldAction(string kind, string id)
        {
            if (Panel != GamePanel.Explore) return;
            if (opening.HandleInteraction(kind, id)) return;
            PlaySound(kind == "resource" ? "pickup" : "interact");
            switch (kind)
            {
                case "exit": ChangeRoom(id, true); break;
                case "npc":
                    if (id == "watcher") SetPanel(GamePanel.Watcher);
                    else Say("拾光 · 捕获引导", "削弱野生伙伴后使用共鸣胶囊，或者试着投喂莓果点心。观察它的偏好，失败也能积累了解。\n\n旅队最多六只，最多三只参与战斗。未选择的初始伙伴，可在完成区域探索后通过营地的伙伴重逢获得。\n\n" + Objective);
                    break;
                case "campfire": SetPanel(GamePanel.Campfire); break;
                case "resource":
                    if (Progress.collectedNodeIds.Contains(id)) return;
                    string item = id.Split('-')[0];
                    Inventory.Add(item, 2);
                    Progress.collectedNodeIds.Add(id);
                    World.SetCollected(id);
                    Notify("拾取 " + GameCatalog.ItemName(item) + " ×2");
                    opening.CheckIngredients();
                    Save(false);
                    break;
                case "encounter": StartEncounter(id, BattleKind.Wild); break;
                case "guardian":
                    if (Progress.seals.HasSeal(id)) Say("封印已解除", "这里的守卫已经认可了你。继续前进吧。 ");
                    else StartEncounter(id, BattleKind.Guardian);
                    break;
                case "boss":
                    if (!Progress.seals.CanEnterLighthouse) Say("四印之门", "需要四枚区域封印，灯塔的入口才能开启。 ");
                    else if (Progress.chapterCompleted) Say("雾散之后", "灯塔已经重新点亮。伙伴们仍在等待新的旅程。 ");
                    else StartEncounter("final-boss", BattleKind.FinalBoss);
                    break;
                case "field": UseFieldAbility(id); break;
                case "sign": Say("旅途手记", id); break;
            }
        }

        public bool ChangeRoom(string roomId, bool save)
        {
            if (!World.RoomIds.Contains(roomId)) roomId = "camp-0";
            if (opening != null && !opening.CanTravel(roomId)) return false;
            string region = roomId.Split('-')[0];
            int index = Array.IndexOf(RegionIds, region);
            if (index < 0) roomId = "camp-0";
            string[] seals = { "glowglade-warden", "tidal-warden", "crystal-warden", "lighthouse-warden" };
            int required = roomId == "lighthouse-2" ? 4 : Mathf.Clamp(index - 2, 0, 3);
            for (int i = 0; i < required; i++)
                if (!Progress.seals.HasSeal(seals[i]))
                {
                    Say("前路仍被浓雾笼罩", "先解除前一区域的封印，再沿着新的光前进。\n\n" + Objective);
                    return false;
                }
            Progress.roomId = roomId;
            motor.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            var spawn = World.GetSpawn(roomId);
            motor.GetComponent<Rigidbody2D>().position = spawn;
            motor.transform.position = spawn;
            Physics2D.SyncTransforms();
            follow.Bounds = World.GetBounds(roomId);
            follow.Snap();
            ChangeMusic(region);
            opening?.OnRoomEntered();
            if (save) { Notify(RoomName); Save(false); }
            return true;
        }

        private void PlaySound(string key)
        {
            var sound = Resources.Load<AudioClip>("Art/Audio/" + key);
            if (sound != null && music != null) music.PlayOneShot(sound, .35f);
        }

        private void ChangeMusic(string region)
        {
            if (music == null) return;
            string key = region == "camp" ? "camp" : region == "glowglade" ? "grove" : region == "lighthouse" ? "enemy" : "ruins";
            var clip = Resources.Load<AudioClip>("Art/Audio/" + key);
            if (clip != null && music.clip != clip) { music.clip = clip; music.Play(); }
        }

        private void StartEncounter(string id, BattleKind kind)
        {
            if (!Party.Members.Any(c => c.currentHp > 0)) { Say("伙伴需要休息", "回到营地找 Watcher，或在篝火旁恢复体力。 "); return; }
            encounterId = id;
            string speciesId = id;
            bool lesson = id == "tutorial-coast-crab" && !Progress.captureTutorialCompleted && OpeningPhase == OpeningStage.FirstEncounter;
            if (id == "tutorial-coast-crab") speciesId = "wild-03";
            string region = Progress.roomId.Split('-')[0];
            int index = Mathf.Max(0, Array.IndexOf(RegionIds, region) - 1);
            if (kind == BattleKind.Wild && id != "tutorial-coast-crab")
            {
                int[][] pools = { new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8, 9 }, new[] { 10, 11 }, new[] { 11, 12 } };
                var pool = pools[Mathf.Clamp(index, 0, pools.Length - 1)];
                var candidates = pool.Select(n => "wild-" + n.ToString("00"))
                    .Where(key => Catalog.ContainsKey(key) && Catalog[key].contentReady).ToArray();
                if (candidates.Length == 0) candidates = Catalog.Values.Where(s => s.capturable && s.contentReady && s.speciesId.StartsWith("wild-"))
                    .Select(s => s.speciesId).ToArray();
                if (candidates.Length == 0) { Notify("这片区域暂时没有可遭遇的伙伴。 "); return; }
                speciesId = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            }
            if (!Catalog.TryGetValue(speciesId, out var species)) { Notify("这场遭遇的数据尚未就绪。 "); return; }
            Discover(speciesId);
            int level = lesson ? 1 : kind == BattleKind.Wild ? 2 + index * 2 : kind == BattleKind.Guardian ? 4 + index * 2 : 12;
            var wild = new CreatureInstance(species, level, false);
            Battle = new BattleSession(Party, Catalog, Inventory, wild, null, kind, level, lesson);
            SetPanel(GamePanel.Battle);
        }

        public void Act(BattleAction action, string item = null)
        {
            if (Battle == null || Panel != GamePanel.Battle) return;
            var result = Battle.TryAct(action, item);
            if (!result.Success) Notify(result.Message);
        }

        public void SwapBattle(string outgoing, string incoming)
        {
            if (Battle == null) return;
            var result = Battle.TrySwap(outgoing, incoming);
            if (!result.Success) Notify(result.Message);
        }

        public void LeaveBattle()
        {
            if (Battle == null || Battle.Outcome == BattleOutcome.Ongoing) return;
            var outcome = Battle.Outcome;
            var kind = Battle.Kind;
            bool lesson = Battle.IsCaptureLesson;
            if (outcome == BattleOutcome.Captured) { Progress.captureTutorialCompleted = true; Discover(Battle.Wild.speciesId); }
            foreach (var member in Party.Members) Discover(member.speciesId);
            Battle = null;
            SetPanel(GamePanel.Explore);
            if (outcome == BattleOutcome.Victory && kind == BattleKind.Guardian)
            {
                Progress.seals.RecordGuardianVictory(encounterId);
                Inventory.Add("resonance-capsule", 3);
                Inventory.Add("berry-treat", 2);
                Say("区域封印解除", "雾气散开，一条新的道路显露出来。\n\n获得共鸣胶囊 ×3、莓果点心 ×2。\n\n" + Objective);
            }
            else if (outcome == BattleOutcome.Victory && kind == BattleKind.FinalBoss)
            {
                Progress.chapterCompleted = true;
                SetPanel(GamePanel.Ending);
            }
            else if (outcome == BattleOutcome.Defeat)
            {
                ChangeRoom("camp-0", false);
                Recover(false);
                Say("Watcher · 归航之灯", "我把你们接回了营地。伙伴们已经恢复，也没有丢失收集到的物品。准备好了再出发吧。 ");
            }
            opening.OnBattleEnded(outcome, lesson);
            Save(false);
        }

        private void Discover(string id)
        {
            if (!Progress.discoveredSpeciesIds.Contains(id)) Progress.discoveredSpeciesIds.Add(id);
        }

        public void Recover(bool showMessage = true)
        {
            foreach (var c in Party.Members) c.currentHp = CreatureInstance.MaxHp(Catalog[c.speciesId], c.level);
            Progress.collectedNodeIds.Clear();
            World.ResetCollected(_ => false);
            if (showMessage) Notify("Watcher 照料了全部伙伴。休息过后，沿途资源也重新出现。 ");
            Save(false);
        }

        public void Craft(CampfireRecipe recipe)
        {
            if (CraftingService.TryCraft(Inventory, recipe, out var reason)) { opening.OnCrafted(recipe.recipeId); Save(false); }
            Notify(reason);
        }

        public void OpenParty() { previousPanel = Panel; SetPanel(GamePanel.Party); }
        public void ReturnFromParty() { SetPanel(previousPanel == GamePanel.Watcher ? GamePanel.Watcher : GamePanel.Explore); Save(false); }
        public void ToggleDeployment(CreatureInstance creature)
        {
            var ids = new List<string>(Party.DeployedIds);
            if (!ids.Remove(creature.instanceId)) ids.Add(creature.instanceId);
            if (!Party.TrySetDeployment(ids, out var reason)) Notify(reason);
            else Save(false);
        }

        public void Evolve(CreatureInstance creature)
        {
            if (GrowthRules.TryEvolve(creature, Catalog, out var target)) { Discover(target.speciesId); Notify("伙伴进化为 " + target.displayName + "！"); Save(false); }
            else Notify("进化需要达到物种要求的等级与亲密度。 ");
        }

        public void OpenStorage() { SetPanel(GamePanel.Storage); }
        public void Deposit(CreatureInstance creature)
        {
            if (Storage.TryDeposit(Party, creature.instanceId, out var reason)) Save(false);
            Notify(reason);
        }
        public void Withdraw(CreatureInstance creature)
        {
            if (Storage.TryWithdraw(Party, creature.instanceId, out var reason)) Save(false);
            Notify(reason);
        }

        public void ReuniteStarter(string id)
        {
            if (Progress.seals.Count < 2) { Notify("完成两个区域封印后，拾光会带来新的伙伴线索。 "); return; }
            if (Party.Members.Any(c => c.speciesId == id || c.speciesId == id + "-evolved")) { Notify("这位伙伴已经在队伍中。 "); return; }
            var c = new CreatureInstance(Catalog[id], 5, false);
            if (!Party.TryAdd(c)) { Notify("旅队已经满员。 "); return; }
            Discover(id);
            Notify("循着拾光的线索，" + Catalog[id].displayName + "回应了你的邀请。 ");
            Save(false);
        }

        private void UseFieldAbility(string id)
        {
            if (Progress.completedFieldIds.Contains(id)) { Say("旧路新生", "这条支线已经探索过了。 "); return; }
            string required = id.Contains("tidal") || id.Contains("coast") ? "tide" : id.Contains("crystal") || id.Contains("lighthouse") ? "crystal" : "verdant";
            bool available = Party.Members.Any(c => {
                var s = Catalog[c.speciesId];
                return s.fieldAbilityId != null && (s.fieldAbilityId.Contains(required) ||
                    (required == "verdant" && s.primaryElement == ElementType.Verdant) ||
                    (required == "tide" && s.primaryElement == ElementType.Tide) ||
                    (required == "crystal" && s.primaryElement == ElementType.Crystal));
            });
            if (!available) { Say("伙伴探索能力", "这里需要" + (required == "tide" ? "引水" : required == "crystal" ? "共鸣" : "藤蔓生长") + "能力。收集不同伙伴后再回来吧，这条支线不影响主路。 "); return; }
            Progress.completedFieldIds.Add(id);
            World.OpenField(id);
            Inventory.Add("resonance-capsule", 2);
            Inventory.Add("pollen", 3);
            Say("伙伴打开了隐藏支路", "你们一起发现了遗留物资：共鸣胶囊 ×2、花粉 ×3。\n\n与伙伴的羁绊，让原本不可达的地方成为新的道路。 ");
            foreach (var c in Party.Members) c.bond += 5;
            Save(false);
        }

        public void Save(bool feedback = true)
        {
            if (Saves == null || (Party.Count == 0 && Progress.openingStage == 0)) return;
            Progress.roster = new List<CreatureInstance>(Party.Members);
            Progress.storedRoster = new List<CreatureInstance>(Storage.Members);
            Progress.deployedIds = new List<string>(Party.DeployedIds);
            Progress.inventoryItemIds.Clear();
            Progress.inventoryCounts.Clear();
            foreach (var stack in Inventory.Entries) { Progress.inventoryItemIds.Add(stack.itemId); Progress.inventoryCounts.Add(stack.count); }
            Progress.playerX = motor.transform.position.x;
            Progress.playerY = motor.transform.position.y;
            Progress.hasPosition = true;
            Progress.schemaVersion = WorldSaveData.CurrentSchemaVersion;
            if (!Saves.Save(Progress, out var error)) Notify(error);
            else if (feedback) Notify("旅程已保存。 ");
        }

        private void OnApplicationQuit() { if (Panel != GamePanel.Title && Panel != GamePanel.Starter && Panel != GamePanel.Battle) Save(false); }
        public string InteractionPrompt => Panel == GamePanel.Explore && sensor != null && sensor.Current != null ? "E · " + sensor.Current.InteractionPrompt : null;
        public void Quit() { Save(false); Application.Quit(); }
    }
}
