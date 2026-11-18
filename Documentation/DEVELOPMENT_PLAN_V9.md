# v0.9 campaign, tactile feedback and ball-only input plan

The user requests genuinely different, difficult levels; natural rolling/contact sound and animation; finger gestures that control only the ball; stronger boosters; reference research and generated concept art; and a complete testing APK.

## Native implementation

1. Replace all55 campaign layouts in place, preserving level IDs and saved currency/unlocks. Author11 spatial families with5 different footprints each, including rail-free1.65–2.3m precision segments, full reversals, spirals, layered overpasses, controlled drops, timed obstacles and boost/braking lines. Test normalized route shapes to reject repeated/scaled layouts, safe entries/checkpoints, separated crossings and landing support.
2. Make main PLAY continue the campaign directly. The previous PLAY action repeatedly opened only the five featured adventures; those remain separately accessible. Show actual route silhouettes, names and difficulty in level selection.
3. Remove all touch-to-camera mapping. A stable primary finger steers in any direction; a second finger cannot orbit or displace the primary anchor. Desktop drag also controls the ball. The gameplay camera automatically follows physical travel and maintains central framing; fixed/orbit selection is removed from normal controls.
4. Replace periodic motor-like rolling loops with original aperiodic friction/contact grains, restrained pitch variation and short material impacts selected from the actual object struck. Add bounded surface-normal impact ripples/chips/sparks, including when SFX is muted. Preserve physical transforms and contact forces.
5. Build the booster concept as real shared low-profile geometry: recessed graphite assembly, raised inset plates, ceramic/metal lips, clear directional light and distinct boost/launch/conveyor variants. Review native captures for actual depth, not merely mesh counts.
6. Use family-specific native cliffs, tower cores, gears, struts, archipelago and spires instead of repeating the old skyline/three distant loops. Keep spatial batching and reject supports/scenery that intrude on returning roads.

## Art and references

[Generated concepts and exact prompts](ArtDirection/V9/README.md) were produced with built-in image_gen. Official publisher/store screenshots were reviewed as references. Concept art is identified separately from real gameplay captures; proprietary game art/audio is not shipped.

## Acceptance and delivery

Run structural/diversity and signal-quality tests; focused input/audio/VFX/booster/menu checks; a physical pilot across representative hard courses; then all55 campaign traversals,15 expedition variants, racing and retained regression checks. Test drivers may issue ordinary force/brake commands but may not teleport, rewrite velocity, disable obstacles or alter gravity to succeed. Inspect actual native camera images and contact/boost activation, record honest Editor allocation/performance limits, and build/verify v0.9.0/code9 APK with the same testing certificate. Physical phone comfort/audio/FPS remain unverified without a connected device.
