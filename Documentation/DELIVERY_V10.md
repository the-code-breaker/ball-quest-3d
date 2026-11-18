# Material and challenge update — 0.10.0

**[BallQuest3D-Materials-v0.10.0.apk](../BallQuest3D-Materials-v0.10.0.apk)** — **42.85 MiB**, 44,935,115 bytes, version 0.10.0/code 10, ARM64, Android 8+ (API 26), target API 36. SHA-256: `9E08C9A0CFFFEB9EB903654E8E1C3314BD517C30178DEF0F9F343874300F1AFE`. Android build, signature, ZIP/native 16 KiB alignment, CRC, version, scenes, all 66 recorded clips, four new ball definitions and runtime/test-code checks passed. The generic BallQuest3D-test.apk is identical. [Verified release record](../TestResults/materials-release.json).

## Requested fixes

- Recorded material contact audio replaces the synthetic friction bed. Metal and rubber rolling use CC0 recordings of those balls; wood, stone, glass and energy use separately processed interpretations. Six body profiles retain their identity on every floor. Forty-eight paired impact clips combine the ball with the struck surface instead of replacing it with one shared floor sound. Slip is quieter and low-energy road-seam contacts are suppressed.

- Challenge chapters now display 1–14, 15–28, 29–42 and 43–55. Visual biomes remain mixed, but they no longer filter numbers out of progression. Switching lists resets pagination. Challenge 55 returns to challenges rather than silently continuing into the separate signature adventures.

- Difficulty rises earlier through narrower exposed sections, fewer rails and additional moving machinery. Course 1 introduces control, 2–4 demand balance, 5–22 are hard and 23–55 combine extreme mechanics. These are design categories, not human difficulty measurements. Safe spawn pads, checkpoint pads and broad drop catches remain.

- The campaign contains 23 piston carriages, 23 oscillating sweep arms and 23 ribbed roller drums across the courses. Existing crates/doors get beveled shells; hammers and rotors get capped barrels, collars and mechanical detail. Their render geometry follows the same physical bodies.

- Eleven native scenery recipes replace repetitive walls: lagoon terraces, desert hoodoos/arches, orbital observatories, alpine peaks, hanging gardens, volcanic islands, brass gearworks, city cranes, ancient courts, canyon wind turbines and crystal monoliths. Family palettes and skies distinguish them. Decorative geometry has no gameplay colliders and uses spatial mesh batches.

- Brushed Steel and Carbon Rubber are free additions; Copper Core and Turned Walnut can be unlocked with earned coins. Their material selection explicitly chooses the correct recorded sound profile. Existing owned selections, currency, challenge unlocks and control preferences persist.

- Coins have a deeper bevel, larger raised star, faceted rim and brighter two-tone metal. Boosters have ribbed graphite trays, champagne direction plates and recessed cyan/amber/jade lighting. Rewards still use two renderers and boosters three with shared course resources.

## Executed evidence

All 137 distinct current Unity cases passed: 37 EditMode and 100 PlayMode. The 15-case pilot/presentation run passed, including six representative physical routes with zero falls, all chapter pages clicked in order, recorded metal/rubber/wood impacts distinguished on the same floor, native models, booster activation, contact/mute lifecycle and family renders. The full 55-course traversal passed in 704.132 wall seconds with zero falls. All 15 retained signature-adventure variants also completed with zero falls.

The 98-case regression run passed 97 cases and found one real presentation-budget issue: small local obstacle geometry was unnecessarily split at the spatial batch origin, producing 14 visible renderers for a hammer. Object-local batches now group solely by material, while scenery keeps spatial culling. The final 14-case rerun passed, covering the failed case, mechanisms, gate opening, booster/reward effects and current performance. Its result supersedes the single failed case when counting unique current tests. The original failed XML is retained in PlayMode-v10-regressions.xml, and the fix/rerun in FinalVisuals-v10.xml; the run is not described as an all-green 98-case report.

Final eight-ball Editor measurements: 301 active course renderers, 276 collider components, mean/median frame times 16.692/16.664 ms, p95 17.455 ms, Unity allocation 195.47 MiB, one Gen0 collection during 240 measured frames. Whole-frame managed peak was 453,092 bytes. Renderer counters were unavailable. The isolated warmed 80-coin/one-booster animation check allocated zero managed bytes across 600 calls. These measurements include Editor overhead and do not establish Android frame timing.

The source preparation pipeline, five recording credits/licenses and 66 clip metrics are retained in [audio credits](References/V10/AUDIO_CREDITS.md), [source processing](../Tools/PrepareNaturalAudioV10.py) and [audio metrics](../TestResults/natural-contact-v10.json). A dry [material audition](../TestResults/Audio/NaturalContactV10-Audition.wav) is available. No human or physical-phone sound audition has been performed. [Online visual references](References/V10/VISUAL_REFERENCES.md) are documentation only; other game art/audio is not shipped.

Native review captures under TestResults/Previews use the prefix V10. The family overviews and supported inspection poses show actual Unity geometry; the force-driven traversal establishes physical completion separately. Captures are not generated concept illustrations.

## Practical limits

Editor traversal proves solvability under the validation driver's ordinary force/brake policy. It does not prove phone touch comfort, subjective sound quality, perceived difficulty, hardware shimmer or sustained thermal performance. Existing historical best times refer to older geometry. This is an Android testing update, not a store release or an exact reproduction of another game.



The signing certificate matches v0.9, so installing this higher-version APK is expected to preserve application data. A physical update was not performed. [Certificate comparison](../TestResults/signing-compatibility-v10.json), [ADB device check](../TestResults/adb-devices-v10.txt): no device attached.
