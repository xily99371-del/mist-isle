# Windows playtest candidate and scope freeze

This milestone is intended to close the feature-expansion phase and move the project into local Windows playtesting.

## Three resolved development tracks
- Character animation: four-direction player walk cycle, animated idle companion sprites, companion follow interpolation, and independent bob/idle timing.
- Map/world: four hand-authored top-down scenes linked by the camp hub and progression gates: coastal camp, winding grove trail, tidal ruins processional and lighthouse boss courtyard. Each scene uses layered 2.5D-style props, depth sorting, authored collision and clear landmark-driven routes.
- RPG loop: journal, inventory, route map with current-area marker, resource collection, companion recruitment, elemental combat synergy, XP/levels, persistent save/load, weather tint and day/night ambience, and lighthouse ending sequence.
- Companion polish: all three recruited companions use their authored four-frame pixel idle sheets and follow in a facing-aware fan formation with collision-aware catch-up movement.
- Boss polish: the guardian has three escalating attack phases; wet status dampens one incoming attack and is consumed, with a matching core-state telegraph.

## Important boundary
The map remains a single connected scene rather than separate streamed maps. The current sprite sets provide walk and idle loops, not a complete production animation library. Weather is a lightweight visual overlay, not a full particle/weather simulation. These limits are intentionally frozen for the first complete playtest candidate.

## Windows playtest acceptance checklist
1. Launch from the packaged folder with assets present.
2. Start a new game and advance the opening dialogue.
3. Walk in all four directions; verify sprite rows and nearest-neighbor rendering.
4. Rescue Moss Lumi at camp; verify its pixel idle animation, follow spacing and party UI.
5. Speak with the keeper; rest at the campfire.
6. Collect a fruit, feed the Leafling, and collect the revealed crystal.
7. Discover the Firefly and register it in the journal.
8. Trigger and defeat the grove mote encounter.
9. Recruit Moatail and Crystal Gecko using their resource requirements; verify the three companions form a fan behind the player when turning and moving.
10. Open journal, inventory and island map; save with F5, close/relaunch, load with F9.
11. Activate the ruin with the three companions and tide shell.
12. Complete the boss battle; check phase changes, charged-strike warning, one-hit wet-status interruption and core health bar. Return to the shrine and watch the ending.
13. Open the island map and verify current-area marker, unlocked/locked legend and route graph. Check audio/device behavior, resolution, input and frame pacing on the user's Windows PC.

## Stop condition
After the Windows package is built and the user completes the checklist (or reports issues), stop adding features and fix only bugs/usability problems found during playtesting. Raspberry Pi 5 porting follows after Windows behavior is accepted.


## Remote-first local sync
Use `docs/LOCAL_SYNC.md` to force the local checkout to the latest `polish/full-game-character-art` branch. The GitHub branch is the source of truth; do not hand-copy generated assets between checkouts.
