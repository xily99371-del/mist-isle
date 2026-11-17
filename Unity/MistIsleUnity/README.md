# Mist Isle Unity — Chapter One

## Current playable build

Target: Unity **6.3 LTS / 6000.3.28f1**, URP **17.3.0**, Windows x64.

Open **this directory**, not the parent Git repository, in Unity Hub. Open `Assets/Scenes/CoastalCamp.unity` and press Play. The latest verified opening build is `Builds/OpeningPolish/Windows-0.2.2/MistIsle.exe`; previous opening builds and the earlier chapter prototype are retained separately under `Builds`.

The current implementation is a playable chapter prototype, not a production-complete release. It connects starter choice, six-member travel party, three active combatants, wild encounters, feeding/capture, campfire crafting, Watcher recovery/storage, XP/evolution, field-ability side paths, four seals, and the Observer's final awakening encounter.

- **Watcher** remains the camp's recovery and party-storage character. **拾光 / Shiguang** is a separate capture guide, with the approved champagne hair, goggles, yellow bow and scarf.
- The opening now starts at the arrival dock, with Watcher's welcome, Shiguang's introduction, individual companion meetings and a confirmed choice. Preparing food for a frightened stranded crab leads into a guided first feeding/capture and a return to Watcher. The companion travels with the player.
- The traveler and Shiguang use the original `pixel_people.png` artwork as 24 independent direction/animation frames. The camp and first coast use existing approved pixel creatures and environment cutouts; the title uses the original approved cover with live menu controls.
- The world contains six regions and eighteen connected rooms. The eighteen collectible forms include three starters, three evolved forms and twelve wild creatures. Six newly added wild designs are explicitly candidates awaiting art approval.
- The four guardians are 花冠鹿灵, 断潮蟹王, 辉棘蜥王 and 回响观测者. The Observer also appears in the final phase, preserving the same identity and approved design.
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
| F5 | Save |
| Esc | Close a menu / open party while exploring |

Follow the opening objective through the dock, Watcher's shelter and Shiguang's companion courtyard. Choose a companion, gather berries and dew, make a berry treat at the fire and accompany Shiguang to the coast. Once the rescued crab has been checked by Watcher, the rest of the chapter opens. The forest, tide and crystal seals unlock subsequent regions. At the lighthouse, win the fourth seal before entering the Observer's final phase. During the telegraphed silence, call the starter, then command the final counter yourself.

Unity saves live in `Application.persistentDataPath/UnityChapter/chapter-01.json`, with schema 3 migration, opening checkpoints, a backup and an archive before starting a new journey. Versions 1 and 2 retain existing parties and chapter progress and skip the new opening. The C edition's `mist_isle_save.dat` is never read or written.

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
