# Chapter 1 polish milestone — animation, world, growth, combat and finale

## Character animation
- Player walk sheet is rendered as a four-direction, four-frame loop with nearest-neighbor filtering.
- Three companion idle sheets are animated independently while followers bob and move with damped follow motion.
- The camp keeper, wild Leafling, Firefly and grove mote have separate animated/idle presentation.
- Environmental props and characters are depth-sorted by isometric ground position so the player can move behind trees and props.

## World presentation
- Day/night tint advances on a seven-minute cycle.
- Weather changes periodically between clear air, rain and firefly-rich ambience.
- Rain uses lightweight screen-space streaks; firefly weather adds drifting points of light.
- Grove has a dedicated palette and denser pixel props: mushroom clusters, fallen log, rune stone and flower patches.
- Lighthouse finale uses a warm animated halo and orbiting motes.

## Companion growth
- Companion XP is awarded for victories; 50 XP advances a level.
- Skill effects scale with companion level:
  - Moss Lumi: healing and light damage.
  - Moatail: water damage.
  - Crystal Gecko: electric damage, with a bonus against Soaked targets.
- Active companion level and XP are visible in the HUD and retained in saves.

## Combat
- Moatail applies Soaked.
- Crystal Gecko consumes the Soaked condition for a stronger electric strike.
- Moss Lumi restores player HP while contributing a small attack.
- Enemy turns show round count and incoming damage feedback.
- Wild mote, grove guard and lighthouse boss use different HP/damage profiles and visual treatments.

## Chapter finale
- After the lighthouse boss is defeated, returning to the shrine triggers a companion dialogue sequence.
- The dialogue hands off into the ending scene.
- The ending adds a rotating ring of light motes around the restored lighthouse.

## Current validation boundary
Windows CI compiles and packages the C application and runtime assets. This is not a substitute for human playtesting. Full multi-room map streaming, a larger authored walk/attack/hurt sprite set, more distinct enemy AI patterns, music/SFX mixing, and Raspberry Pi 5 frame-time profiling remain further polish work.
