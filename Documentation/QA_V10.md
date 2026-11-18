# Quality assurance — material/challenge update 0.10.0

All 137 distinct current Unity cases have passing latest results. All 55 campaign courses and 15 signature-adventure route variants completed force-driven physical validation with zero falls. The optimized APK passed packaging checks. [Delivery](DELIVERY_V10.md), [verified release record](../TestResults/materials-release.json).

| Evidence | Result/scope |
| --- | --- |
| [EditMode-v10.xml](../TestResults/EditMode-v10.xml) | 37/37; includes sequential chapters, exact 66 recorded clip mappings/data, material balls/save migration, route geometry and original core tests |
| [CampaignPilot-v10.xml](../TestResults/CampaignPilot-v10.xml) | 15/15; six representative physical courses, audio, native family/models, boosters and actual UI navigation |
| [CampaignTraversal-v10.xml](../TestResults/CampaignTraversal-v10.xml) | All 55 current harder courses, zero falls; 704.132 wall seconds |
| [PlayMode-v10-regressions.xml](../TestResults/PlayMode-v10-regressions.xml) | Complete 98-case run: 97 passed, one obstacle-renderer budget failure retained verbatim |
| [FinalVisuals-v10.xml](../TestResults/FinalVisuals-v10.xml) | 14/14 after fixing object-local batching; supersedes that one failure without adding distinct tests |
| [Physical route summary](../TestResults/campaign-physical-validation-v10.json), [55-row CSV](../TestResults/production-traversal-v10.csv), [15 adventure variants](../TestResults/expedition-traversal-v10.csv) | Real force/brake navigation through checkpoints and finish triggers, no teleport/velocity writes or disabled hazards |
| [Chapter buttons](../TestResults/challenge-navigation-v10.json), [native chapter view](../TestResults/Previews/V10SequentialChapter.png) | Every displayed challenge ID 1–55 appears exactly once, with proper page reset |
| [Difficulty comparison](../TestResults/challenge-difficulty-v10.json) | Exposed main segments 519 → 953, increased exposure in 50 courses; 23 each piston/sweep/drum; not a human difficulty measurement |
| [Recorded audio metrics](../TestResults/natural-contact-v10.json), [loop seams](../TestResults/natural-loop-seams-v10.json), [credits](References/V10/AUDIO_CREDITS.md) | Five CC0 sources, 66 material/pair clips; mathematical/import checks, not a human audition |
| [Material/obstacle model review](../TestResults/material-models-v10.json) | Four native ball styles, different impacts on one floor, moving machinery models and visible gate lifting |
| [Camera output](../TestResults/centered-camera-v10.json), [gestures](../TestResults/ball-only-gestures-v10.json), [swipe-only](../TestResults/swipe-only-v10.json) | Ball remains centered; touch sends no camera commands; stationary touch adds no drive |
| [Performance](../TestResults/presentation-performance-v10.json), [isolated animation](../TestResults/reward-animation-allocation-v10.json) | Eight-ball Editor mean/median/p95 16.692/16.664/17.455 ms, 301 renderers, 195.47 MiB, one Gen0 in 240 frames; isolated 600-call animation allocates zero managed bytes |

The one failed native renderer assertion exposed a real inefficiency: local meshes were split across spatial chunk boundaries. The final fix uses one material batch per small moving object; scenery retains spatial culling. Raw failed XML/logs are preserved, and latest results per unique test determine the aggregate. Focused runs and route counts are not added as extra Unity cases.

The route/core-physics snapshot matches the validated campaign. Later refinements concern recorded-loop continuity, visual shutter lifting, scenery and object-local render batching; focused regression checks cover those changes. Native overview/inspection poses show actual Unity geometry and are separate from physical completion evidence. Original offline checks also passed: 8,219 asset checks and 144 domain/syntax checks; they are not Unity test cases.

No physical phone was attached. Phone touch comfort, speaker output, subjective sound/difficulty, hardware shimmer and sustained thermal/FPS behavior remain unverified. Earlier versioned reports/APKs are historical; [v9 QA](QA_V9.md) is retained separately.
