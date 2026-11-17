# Object Atlas Slicing

Use `03_Objects/Common/Objects_Environment_Atlas.png` as a Multiple Sprite atlas. The `AssetManifest.json` file contains approximate starting rectangles and pivots.

Recommended workflow:
1. Open Sprite Editor.
2. Verify each listed rectangle against visible sprite bounds.
3. Adjust rectangles to avoid neighboring sprites and transparent gutters.
4. Set bottom-center pivots for trees, houses, lamps, bridges and other props.
5. Add colliders as separate PolygonCollider2D or CompositeCollider2D components; do not use the sprite's full rectangle as a collider by default.
6. Keep the original `Objects_Environment_Atlas_Source.png` as a visual reference.

The region folders contain thematic crops for quick reference; they are not separate, fully unique object libraries yet.
