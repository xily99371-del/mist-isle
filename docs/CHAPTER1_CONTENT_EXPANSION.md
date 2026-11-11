# Chapter 1 content expansion — complete RPG slice

This milestone moves the project beyond the initial movement-and-collect prototype. The first chapter now has a broader gameplay loop built around exploration, collecting, wildlife encounters, companion acquisition, puzzle progression and a final boss.

## New systems
- Inventory screen (`I`) with fruit, shells, crystals, health and companion levels.
- Island route map (`M`) with progression indicators for camp, grove, ruins and lighthouse courtyard.
- Field journal (`J`) tracks the firefly discovery, Leafling help, wild mote encounter, companion collection and current objective.
- Persistent save/load (`F5` / `F9`), including collected pickups, discoveries, companion ownership and XP/levels.
- Autosave after key progression events and after defeating the wild grove mote.

## Grove activity
- A drifting firefly can be discovered and recorded.
- Feed the Leafling one fruit to reveal a hidden star crystal.
- A wild grove mote blocks the old path; defeat it once to clear the encounter and receive two fruits.
- Added pixel props: mushroom clusters, fallen log, rune stone and flower patch.
- Wildlife participates in the isometric depth sort with the player and environment.

## Companion battle synergy
- Moss Lumi: heals the player and adds a small light attack.
- Moatail: water attack applies Soaked.
- Crystal Gecko: electric attack deals bonus damage against Soaked targets.
- Enemy turn feedback, round count, active companion XP and level-ups.

## Controls
WASD/arrow keys move; Shift dash; E interact; Tab cycle companions; J journal; I inventory; M map; F5 save; F9 load. In battle, 1/2/3 select skills and Enter confirms.

## Remaining work
The chapter still needs further balancing and a human playthrough. The current visuals are original first-pass pixel art; a larger hand-authored sprite set, environmental animation, sound/music, weather/day-night and Raspberry Pi 5 performance validation remain future milestones.
