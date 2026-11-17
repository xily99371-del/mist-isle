# Mist Isle Unity — Chapter One

## Current playable build

Target: Unity **6.3 LTS / 6000.3.28f1**, URP **17.3.0**, Windows x64.

Open **this directory**, not the parent Git repository, in Unity Hub. Open `Assets/Scenes/CoastalCamp.unity` and press Play. The **0.4.0 design-pack checkpoint** output is `Builds/DesignPack/Windows-0.4.0/MistIsle.exe`; previous 0.3.0/0.2.x builds remain separate. This branch is `unity/design-pack-world`. See `Docs/MAP_DESIGN_EXECUTION.md` for the full source review and unfinished production work, and `Docs/CHAPTER_ONE_STATUS.md` for actual verification.

The current implementation is a playable chapter prototype, not a production-complete release. It connects starter choice, six-member travel party, three active combatants, wild encounters, feeding/capture, campfire crafting, Watcher recovery/storage, XP/evolution, field-ability side paths, four seals, and the Observer's final awakening encounter.

- **Watcher** remains the camp's recovery and party-storage character. **拾光 / Shiguang** is a separate capture guide, with the approved champagne hair, goggles, yellow bow and scarf.
- The opening now starts at the arrival dock, with Watcher's welcome, Shiguang's introduction, individual companion meetings and a confirmed choice. Preparing food for a frightened stranded crab leads into a guided first feeding/capture and a return to Watcher. The companion travels with the player.
- The traveler and Shiguang use the original `pixel_people.png` artwork as 24 independent direction/animation frames. Traveler, Shiguang and Watcher share a **1.62-unit visible standing height**, measured from each facing's neutral pose rather than transparent canvas size. Each direction keeps one fixed scale and foot offset across its walk frames. Traveler/Shiguang play the authored three columns as **0 → 1 → 0 → 2**, with one `SpriteFrameAnimator.LateUpdate` animation clock. The original front/back steps are subtle and still need newly authored walking poses.
- Dialogue uses the approved high-resolution traveler and Shiguang paintings from `E:/Projects/美术设定/main_character_design_sketch.png` and `guide_character_design_sketch.png`. Their original paper backgrounds are retained as portrait cards to preserve the confirmed artwork; Watcher uses the approved repository portrait from `assets/characters/portraits/watcher_portrait.png`. These are separate from the world pixel actors. Source copies, crop coordinates and processing are recorded in `Docs/ART_IMPORT_MANIFEST.json`.
- The current world follows the supplied five-map design: M00 camp **64×48**, M01 junction **80×56**, M02 forest **96×72**, M03 two-wing ruins **112×80**, M04 lighthouse **80×64**; 16px tiles, PPU16 and one-unit cells. M01 branches freely into forest and ruins; three regional seals open the lighthouse approach. The historical eighteen-room builder remains solely for legacy regression/reference.
- Gathering has persistent real-time refresh; resting does not regenerate the economy. The camp shop buys surplus materials and sells food/capture supplies. Grass encounters can start through actual walking after the tutorial, with cooldown and breathing room; manual investigation remains available. Press **M** for the visited-region chart.
- Root bridges, water levels, conduits and the three-seal door change actual world state. URP local lights, moving water/foam, fireflies and lighthouse light are connected. This is still a first art pass: storage/guide buildings, terrain transitions, interiors and several large regional landmarks need production assets and visual refinement.
- The eighteen collectible forms include three starters, three evolved forms and twelve wild creatures. Six newly added wild designs remain candidates awaiting art approval. The four guardians are 花冠鹿灵, 断潮蟹王, **棱晶蛇王** and 回响观测者. Per user confirmation, 棱晶蛇王 retains the previously approved crystal guardian artwork and stable `crystal-warden` id; the Observer remains the fourth trial and the same character's final phase.
- Chapter length, production animation, complete narrative pacing and final balance still need player testing. A 60–90 minute playthrough has not been established.

| Controls | Action |
|---|---|
| WASD / arrow keys | Four-direction movement |
| E | Interact with the nearest visible reachable target |
| E / Space / Enter | Advance dialogue |
| A / D or left / right, then Enter | Inspect and confirm a companion on the choice screen |
| Esc on the companion choice screen | Return to the courtyard and meet another companion |
| Tab | Party and deployment |
| J | Creature journal |
| M | Visited-region map / return to exploration |
| F5 | Save |
| Esc | Close a menu / open party while exploring |

The traveler answers a request for help that never received a reply. Follow Watcher and Shiguang, choose a willing companion, prepare food and help the injured crab beside M01's stream. Then choose the forest or ruins first, restore their systems and seek their guardians' recognition. The three seals carry those responses to the lighthouse; the fourth trial precedes the Observer's final phase. During the telegraphed silence, call the starter, then command the final counter yourself. The current battle skills, reserve-selection UI and long-term balance still require further work.

Unity saves live in `Application.persistentDataPath/UnityChapter/chapter-01.json`, with schema **4**, persistent refresh deadlines, story/chest flags and map-content migration. Schema 1/2/3 parties, inventory, seals and opening checkpoints are preserved; obsolete absolute coordinates are replaced by safe entrances in the new five-map world. Exact pre-migration files are retained in content-addressed `.before-map-v1.*.history` archives in addition to normal backups. The C edition's `mist_isle_save.dat` is never read or written.

Editor commands: `Mist Isle > Build Chapter One Content`, `Import Approved Art`, `Validate Imported Art`, and `Build Windows Playable`. Content generation preserves an existing entrance scene and authored data. Generated sprite outputs come from the isolated `Assets/Art` inputs; original C assets and `E:/Projects/美术设定` stay untouched.

Run `powershell -File Tools/Verify-Unity.ps1 -BuildWindows` to verify an isolated project copy. Add `-VisualReplay` to run normal Editor Play Mode and capture the opening's UI with the application's own frame capture. This replay uses session commands and explicit placement; physical keyboard/mouse playthrough remains a separate check. The tool retains the copy, logs, NUnit reports and captures for inspection. See `Docs/CHAPTER_ONE_STATUS.md` for current verification and remaining work.

This is the new production line for 雾屿回响 / Mist Isle. The C + raylib v3.0.1 course edition remains frozen and is not replaced by this project.

## Engine baseline
- Unity 6 LTS
- 2D URP, orthographic camera
- C# gameplay/domain layer
- Rigidbody2D + authored Collider2D/CompositeCollider2D for world collision
- Tilemap for ground, navigation hints and large-scale level layout
- Cinemachine for camera follow and room transitions after the first playable slice
- ScriptableObjects for creature species, items, recipes, encounters and skills
- JSON save data with explicit schema migration

Open this folder in Unity Hub after installing Unity 6 LTS with Windows build support. Allow Package Manager to resolve packages before opening scenes.

The original foundation has been extended into the playable chapter flow above. The historical slice targets below remain reference scope; the current development target is the first full chapter.

## Design constraints
- Preserve Mist Isle's identity: misty coastal fantasy, warm campfire hub, lighthouse mystery, three original starter companions and their field abilities.
- Do not copy Pokémon names, UI, creature designs, capture devices, animations, music or exact battle presentation. Creature collection is a genre reference, not content to copy.
- Total collection target: 12 species in the first vertical slice, 18–24 for the first full public chapter.
- Collection capacity: six creatures in the travel roster. Up to three may be deployed together in the current multi-companion battle style; reserve creatures can be switched in at explicit turn costs.
- Build larger connected regions from reusable screens/rooms, not one enormous empty map.
- Every important building uses authored PolygonCollider2D/CompositeCollider2D geometry. Never infer collision solely from sprite dimensions or a single grid cell.
- Keep the v3.0.1 course build and its save files untouched.

## Historical slice scope
1. Coastal camp hub with the capture guide and campfire crafting.
2. Multi-screen coastal route with resource gathering and two wild species.
3. Glowglade region with a field-ability puzzle and guardian.
4. Tidal ruin region with a second guardian and distinct traversal mechanic.
5. Lighthouse approach unlocked by the forest, tide and crystal seals, followed by the Observer's fourth seal and final phase.
6. A 20–30 minute first-playthrough loop with meaningful starter choice, capture, crafting, party composition and one evolved companion.

The frozen course edition is the v3.0.1 release in the parent repository. See the root README for its download.
