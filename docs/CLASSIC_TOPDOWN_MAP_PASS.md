# Classic top-down RPG map pass — revision 3

This pass responds to in-game scale and navigation feedback.

## Character and prop scale
- The heroine is rendered at 48×68 logical pixels, rather than occupying nearly two full tile heights. The 32×48 source sprite remains point-sampled; it is not enlarged to compensate for insufficient pixel detail.
- Companion sprites use a 44×48 logical footprint with proportionally smaller ground shadows.
- Large house and shelter props have been reduced so they read as landmarks without consuming the playable clearing.
- Tree canopies and minor flora have been reduced. They frame routes and clearings instead of forming walls across them.

## Route-first terrain layout
- Coastal Camp now has one continuous sand path from the southern spawn into the campfire clearing, with deliberate branches to the western beach and eastern exits.
- Glowglade Grove uses a single winding trail from the western entrance to the creek crossing, with a short spur to the eastern discovery area. The cottage and shelter sit in separate grass clearings.
- Tidal Ruins uses a broad, connected stone processional, a central shrine court and two small reflecting pools. Sand is limited to intentional worn edges.
- Lighthouse Courtyard uses a continuous central stone forecourt from the southern entrance to the lighthouse and guardian arena. Garden beds remain at the perimeter.

The four maps remain 18×12 cells at 56px per cell. Story markers, entrances, exits and interaction coordinates are retained. Tiled remains the source of truth; generated C map data must be rebuilt from the edited TMX files.
