# Archived character candidates — not runtime artwork

These experimental v9 characters were rejected as a replacement for the official release. They are retained here only as source history. The game loads the original v1.0.0 sheets named in `assets/characters/runtime_art.json`; normal builds validate those approved files by SHA256. Candidate PNGs live in `candidates/` and are excluded from playable packages.

## Original production notes

# Refined character artwork — v9

These are the original transparent imagegen outputs. They are retained for future
editing; imported candidate sheets are archived under `candidates/`.

The new art follows the established v7 portraits: the heroine remains a female
explorer with brown hair, a blue ribbon, an ivory coat and no hat. The Watcher
retains his olive cap, flower and feather, cream shirt and green scarf.

Both generation prompts requested a transparent four-column, four-row sheet,
with south, west, east and north rows and a four-frame walking cycle. The visual
direction was refined, warm indie pixel art, detailed clothing, expressive faces,
readable silhouettes, and consistent identity rather than geometric characters.

`python tools/import_character_sheets.py` cuts the authored figures at transparent
gutters, scales each actor consistently, and aligns the feet. It creates 256×320
runtime PNGs with 64×80 cells. It does not draw or reconstruct character imagery.
Normal builds do not run this import or load these candidate images.

The three companion source images use two columns and two rows for four idle
poses. The importer assembles each into a 256×64 strip (64×64 cells), with a
common foot baseline at y=60. Moss Lumi keeps the leaf hat and daisies, Moatail
keeps the cyan ears and water tail, and Crystal Gecko keeps the purple crystal
spines. Idle poses include breathing, a blink, and subtle ear/leaf/tail movement.
