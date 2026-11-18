# Material and challenge update — 0.10.0

This change implements the seven requested fixes in the existing Unity game. Ball input, force tuning and automatic camera controls remain the v9 implementation.

1. Replace the audible synthetic friction hiss with edited CC0 recordings of real rolling balls. Preserve the ball's timbre when it strikes different surfaces. Reduce the sliding layer and suppress small road-seam contact chatter. Retain source credits and a dry audition; signal tests do not establish subjective quality.
2. Increase difficulty earlier: first course introduces control, courses 2–4 demand balance, course 5 onward carries hard sections and course 23 onward combines extreme mechanics. Narrow existing exposed roads without changing safe entries/drop catches, remove some guard rails, and place machinery on wider roads. Validate actual physical completion after geometry changes.
3. Add piston carriages, oscillating sweep arms and ribbed roller drums. Replace plain cargo/door bodies and refine existing hammer/rotor shells with capped barrels, collars, bolts and bevels. New moving machinery uses physical kinematic bodies; cosmetics add no colliders.
4. Give all 11 route families different landmark recipes: lagoon, desert hoodoos, orbital observatory, snowy alpine peaks, hanging gardens, volcanoes, brass machinery, neon skyline/cranes, ancient courts, canyon turbines, crystal storm. Batch scenery and keep it outside roads.
5. Add Brushed Steel, Carbon Rubber, Copper Core and Turned Walnut balls, with explicit material sound identity. Steel and Rubber are available without spending coins. Preserve player currency, selected owned items, unlocks, and control preferences.
6. Use online imagery to guide original minted coin and manufactured booster models: deeper coin bevels, raised faceted star, sculpted rim, bright two-tone metal, ribbed dark booster trays and champagne direction plates. Keep two renderers per reward and three per booster. This is native mesh work, not an image placed on a flat card.
7. Replace biome-filtered challenge menus with contiguous chapters 1–14, 15–28, 29–42, 43–55. Keep asset IDs and progress stable. Reset pagination when changing lists and verify every on-screen challenge button across all chapter pages.

Validation: complete EditMode suite, real UI navigation and material-ball integration tests, native family/asset renders, retained controls/camera/audio/performance/racing regressions, all 55 campaign courses and 15 signature-adventure routes. Archive versioned XML/logs and measurements before another run overwrites generic files. Build a nondevelopment ARM64 testing APK, verify manifest/signature/16 KiB alignment/CRC/content, and check ADB device availability.

No Google login or Tracksy changes are part of this update. Earlier illustrated concepts are design references; new native captures show the actual game. A phone is needed to establish touch comfort, speaker output and sustained device performance.
