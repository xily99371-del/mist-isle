# Windows playtest candidate and scope freeze

This milestone is intended to close the feature-expansion phase and move the project into local Windows playtesting.

## Three resolved development tracks
- Character animation: four-direction player walk cycle, animated idle companion sprites, companion follow interpolation, and independent bob/idle timing.
- Map/world: four hand-authored top-down scenes linked by the camp hub and progression gates: coastal camp, winding grove trail, tidal ruins processional and lighthouse boss courtyard. Each scene uses layered 2.5D-style props, depth sorting, authored collision and clear landmark-driven routes.
- RPG loop: journal, inventory, route map with current-area marker, resource collection, companion recruitment, elemental combat synergy, XP/levels, persistent save/load, weather tint and day/night ambience, and lighthouse ending sequence.
- Companion polish: all three recruited companions use their authored four-frame pixel idle sheets and follow in a facing-aware fan formation with collision-aware catch-up movement.
- Boss polish: the guardian has three escalating attack phases; wet status dampens one incoming attack and is consumed, with a matching core-state telegraph.

## Important boundary
The world uses four hand-authored areas connected through the coastal hub. The current sprite sets provide walk and idle loops, not a complete production animation library. Weather is a lightweight visual overlay, not a full particle/weather simulation. These limits are intentionally frozen for the first complete playtest candidate.

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
12. Complete the boss battle; check phase changes, charged-strike warning, one-hit wet-status interruption and core health bar. Enemy lyrics must follow the song continuously from courtyard entry through dialogue and battle. Winning must show a silent, fully black screen for one second, then start post-battle dialogue with Morning News at 01:13. Closing the final line must start the ending automatically without restarting or seeking the music. Hold Enter through the victory to confirm the first dialogue line is not skipped; completed saves can still replay the ending at the shrine.
13. Open the island map and verify current-area marker, unlocked/locked legend and route graph. Check audio/device behavior, resolution, input and frame pacing on the user's Windows PC.

## Stop condition
After the Windows package is built and the user completes the checklist (or reports issues), stop adding features and fix only bugs/usability problems found during playtesting. Raspberry Pi 5 porting follows after Windows behavior is accepted.


## Remote-first local sync
Use `docs/LOCAL_SYNC.md` to update the local checkout from `main` with a fast-forward pull. Read `docs/PROJECT_STATUS.md` for the current runtime artwork and verification status.


## Updated ending and official artwork acceptance

- Run `RUN_WINDOWS.bat --ending-preview` to view all 28 seconds without changing save files.
- Confirm walking actors, lantern ignition, dawn panorama and the group homecoming shot; music stays continuous.
- `Esc` skips to the completion frame; `P` replays; the preview exits on confirmation.
- Check the original v1.0.0 heroine/Watcher sheets and three companion idle strips, including their original display proportions. Verify the continuous ground texture in all four maps; separated checkerboard grass blocks must not appear.
- The three wide ending illustrations must be included in packaged `assets/cinematics/ending_v9/`.
- Confirm that the static SemiBold Chinese font loads; lyrics, ending captions and completion controls remain clear on bright illustrations.
- Long bilingual lyrics must not cover the Boss phase title or core-state hint.
- Resize the desktop window through 1280×720, 1600×900, 1920×1080 and 2560×1440 where the display allows. Chinese HUD, dialogue, route signs and ending captions should use the available physical pixels; mouse commands and the lyric highlight must stay aligned.
- Start from a fresh title, enter camp, return to the title and start again. The first playable terrain must match the same camp after leaving and re-entering it; area transitions must not show an empty or stale ground frame.
- Stay in Glowglade for several minutes. The original 120 desktop fireflies must continue circulating; they must not disappear with total game time. Weather mapping, colors and fade timing remain those of v1.0.0.
