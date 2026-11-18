# Thousand-course campaign implementation

The requested scope is at least 1,000 playable courses, meaningful variation, new challenges, online research and generated art, followed by a testing APK.

1. Expand the existing catalog to 1,000 contiguous IDs, retaining the first 55 asset identities and prior save progress. Keep five signature adventures in their separate collection. Raise save validation limits and test level 999/1000 recovery.
2. Build 25 deterministic topology recipes with parameter variation and six secondary section types. Combine bends, ascending helices, elevated crossings, narrow beams, timed gates, moving bridges, drops, branches, boost straights and fragile crossings. Make later levels narrower and more demanding while retaining supported starts, checkpoints and catches.
3. Introduce 20 environment identities, including lagoon, desert, alpine, city, orbit, volcano, candy, bamboo, temple, mushrooms, coral, pirates, clockwork, autumn, crystals, cloud palace, storms, circuits, toys and canyon. Use distinct meshes, lighting, road finishes, supports and palettes.
4. Add optional per-course flawless, coin, time, treasure and boost objectives. Show progress in the HUD and pay a saved one-time bonus. Normal completion remains available when the bonus is missed.
5. Keep all courses as compact route data. Expand only the active course's collision geometry, then release that instance on unloading. Use shared materials and static mesh batches for scenery. Preserve the improved swipe controls, automatic camera and recorded material audio.
6. Generate original world illustrations and prop concepts. Use the atlas in the native chapter browser and the concept sheet to guide native meshes. Record exact prompts and distinguish illustrations from gameplay captures.
7. Validate every route structurally, run save/reward/navigation tests, physically traverse a representative sample, inspect native renders, run retained controls/audio/racing regressions, then compile and verify an ARM64 testing APK.

## Reference research

- [Going Balls official Android listing](https://play.google.com/store/apps/details?id=com.pronetis.ironball2&hl=en): swipe control, elevated routes, obstacles and varied surroundings informed the design direction.
- [Kenney Rolling Ball Assets](https://kenney.nl/assets/rolling-ball-assets): readable reward and obstacle silhouettes. Reference only; no downloaded proprietary game artwork is shipped.
- [Kenney Platformer Kit](https://kenney.nl/assets/platformer-kit): cohesive modular shapes and material separation informed mesh construction. Reference only.

The 1,000 courses are generated variations of 25 topology recipes, not 1,000 individually hand-authored topology families. Structural uniqueness checks reject translated/scaled route duplicates; they do not establish that every course feels entirely unlike all others. Final delivery notes record the actual physical test scope and any device limitations.

A subsequent within-theme shape comparison identified near-reprises in the second half. Content version 31 adds a full extra act to every course after 500, chosen from eight gauntlet types: extended terraces and needles, mixed machinery, twin freefalls, rising helix, reverse hairpins, shuttle/fragile bridges, boost-to-balance and forked machinery. Eleven late courses cover all eight additions and the final levels. Their current physics evidence supersedes late entries from the earlier baseline run; unchanged early-course evidence remains applicable.
