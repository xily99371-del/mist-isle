# Mist Isle Unity Reboot

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

The first commit establishes the gameplay/domain foundation. It is not yet a content-complete playable build: the next implementation step is the Unity vertical-slice scene, imported approved character art, and a fully testable lighthouse collider.

## Design constraints
- Preserve Mist Isle's identity: misty coastal fantasy, warm campfire hub, lighthouse mystery, three original starter companions and their field abilities.
- Do not copy Pokémon names, UI, creature designs, capture devices, animations, music or exact battle presentation. Creature collection is a genre reference, not content to copy.
- Total collection target: 12 species in the first vertical slice, 18–24 for the first full public chapter.
- Collection capacity: six creatures in the travel roster. Up to three may be deployed together in the current multi-companion battle style; reserve creatures can be switched in at explicit turn costs.
- Build larger connected regions from reusable screens/rooms, not one enormous empty map.
- Every important building uses authored PolygonCollider2D/CompositeCollider2D geometry. Never infer collision solely from sprite dimensions or a single grid cell.
- Keep the v3.0.1 course build and its save files untouched.

## First playable slice
1. Coastal camp hub with the capture guide and campfire crafting.
2. Multi-screen coastal route with resource gathering and two wild species.
3. Glowglade region with a field-ability puzzle and guardian.
4. Tidal ruin region with a second guardian and distinct traversal mechanic.
5. Locked lighthouse approach whose gate checks all three regional seals.
6. A 20–30 minute first-playthrough loop with meaningful starter choice, capture, crafting, party composition and one evolved companion.

The frozen course edition is the v3.0.1 release in the parent repository. See the root README for its download.
