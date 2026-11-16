# Mist Isle Unity reboot — vertical-slice plan

> 2026-11-17 handoff update: the active target is the full first chapter. Watcher is retained for recovery and storage; Shiguang remains a separate capture guide. The four guardians are forest, tide, crystal and lighthouse. The lighthouse Observer also serves as the final boss after the fourth seal battle. The smaller milestones below are historical planning steps, not a claim that content or playthrough duration has been completed. See CHAPTER_ONE_STATUS.md for actual implementation and verification.

## Product direction
The v3.0.1 C/raylib build is the preserved course edition. The Unity reboot is a separate product line intended to become a substantial portfolio project: a polished original creature-collecting exploration RPG, not a larger copy of the old one-screen demo.

## Core loop
Explore connected regions → find materials and wild creatures → prepare food/tools at camp → battle, weaken or befriend → capture → compose a six-creature roster → solve field-ability puzzles → defeat regional seal guardians → unlock the lighthouse.

## Scale targets

| Content | Vertical slice | First full chapter |
|---|---:|---:|
| Connected regions | 3 | 5–6 |
| Traversable screens/rooms | 8–10 | 18–24 |
| Creature species | 12 | 18–24 |
| Starter choices | 3 | 3 |
| Party capacity | 6 | 6 |
| Simultaneous field/battle deployment | Up to 3 | Up to 3, with reserve switching |
| Guardian encounters | 2 | 4–5 |
| Expected first playthrough | 20–30 min | 60–90 min |

These are scope targets, not completed content. Expand the world with meaningful routes, landmarks, shortcuts and optional encounters; do not increase map size by filling it with empty ground.

## Creature roster
Keep the three current companions as the starter trio, but make them the beginning of a larger ecology. The first 12-species slice includes 3 starters, 3 common creatures, 3 uncommon creatures with food preferences/capture conditions, 2 rare creatures tied to optional routes or weather, and 1 guardian-adjacent species unlocked by a puzzle. Expand to 18–24 species after the slice proves fun.

Every species needs a readable silhouette, elemental identity, combat role, preferred food, field utility or collection trait, and authored animation set. Avoid reskins with only stat changes.

## Starter choice
Present the three starters before the player leaves camp. Each explains role, element, signature skill and field ability. The two unchosen starters remain obtainable later through longer side quests, so the choice shapes the opening without permanently locking collection completion.

## Capture loop
1. Battle capture: weaken the creature, apply a relevant status or elemental condition, then use a crafted resonance capsule.
2. Food befriending: learn preferences, gather ingredients, prepare a treat at camp, then offer it. Temperament and food affinity influence success.

A failed attempt changes encounter state (curiosity, wariness, appetite, escape risk) and provides feedback. Avoid one opaque random percentage. Bosses and story guardians are not capturable.

## Campfire crafting
The campfire is a functional hub: cook basic foods, craft capture tools, unlock recipes from the guide/journals/landmarks, use rare ingredients for high-affinity treats, recover the roster and manage party composition.

## Field abilities and regional progression
- Verdant/light: reveal hidden paths, regrow root bridges and soothe wildlife.
- Tide: redirect water, cool heated mechanisms and move floating platforms.
- Crystal: energize conductors, resonate with ancient locks and break mineral barriers.

Each region teaches one ability, tests it in an optional puzzle, then requires it in the guardian seal route. The lighthouse gate physically appears sealed until all regional requirements are met.

## Combat and growth
Retain the readable timing and multi-companion identity of the existing battle system, but add an explicit elemental matchup chart, status effects and field-state interactions, six-member roster with up to three deployed and meaningful reserve switching, XP/bond/level milestones, and evolution that changes silhouette, animation, a signature skill and at least one field interaction.

Start with six elements and tune them through real encounters; avoid an enormous type chart in the first slice.

## Narrative climax
At the final boss threshold, a clearly telegraphed story-only resonance pulse knocks out the deployed companions. The chosen starter's bond triggers an awakening/evolution, revives the party to a controlled amount of HP and grants a counter needed to win the final phase. The player must execute the comeback; the scene must not auto-win.

## Art direction: reducing generic AI appearance
- Lock a character model sheet before producing expressions or poses: front/side/back, palette, proportions, accessories and silhouette.
- Derive portraits from the same model sheet as the in-world sprite; portraits must not invent a different costume or hair color.
- Use consistent line weight and shadow vocabulary; simplify highlights, fabric folds and decorative noise.
- Build modular environment kits and hand-author landmarks and routes.
- Every creature needs a silhouette readable at 48–64px, limited palette, unique movement cycle and distinct idle/attack/capture reactions.
- AI may assist ideation, but final production assets must be curated, corrected and style-checked against one art bible. Do not use unedited one-shot generations as final in-game assets.

## Technical architecture
- Unity 6 LTS + 2D URP for renderer, lighting, post-processing and 2D shadows.
- Tilemap + Collider2D for ground and modular collision; PolygonCollider2D/CompositeCollider2D for irregular structures. Collider geometry is authored explicitly, not guessed from sprite bounds.
- ScriptableObjects for species, skills, items, recipes, encounters, guardians and region definitions.
- Runtime instances for mutable HP, XP, bond, level, capture state and evolution path.
- Services for capture, inventory/crafting, combat resolution, party management and progression, independent of UI.
- Versioned JSON save with schema migration. The old raylib save is not loaded directly; course edition remains independent.
- Scenes: bootstrap, title, coastal camp, regions, battle arena and ending. Additive loading follows after the first slice.
- Edit-mode tests for capture math, type matchups, recipes, party capacity and seal gates; play-mode tests for collision, interaction, scene transitions and save/load.

## Milestones
1. Unity foundation: player controller, camera, tilemap, interaction, save shell, one authored building collider.
2. Fun prototype: one small route, one wild encounter, weakening/capture/feeding, one camp recipe, one creature swap.
3. Starter choice and six-member roster; add first 12 species definitions.
4. Expand to three connected regions and 8–10 rooms; add field abilities and two guardians.
5. Add XP/evolution, elemental combat and final awakening sequence.
6. Art/audio pass, accessibility, balance, profiling and public demo packaging.

## First-slice acceptance
A new player can choose a starter, understand movement/interaction without explanation, explore two meaningful optional branches, gather and craft, capture at least two species, form a six-slot roster, use a field ability to open a route, defeat a guardian, and see a clear progression change. The slice takes 20–30 minutes without relying on skipping dialogue.
