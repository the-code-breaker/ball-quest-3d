# QA — 0.11.0

The final release verifier found **141 distinct current passing Unity cases**, counting the latest result for each unique test name. Raw earlier failures remain in their archived XML files; final focused reruns supersede them.

## Evidence

- `TestResults/GrandEditMode-v11.xml`: all catalog routes, geometry expansion, crossings, starts/finishes/checkpoints, challenge quotas, chapters, save migration, audio and domain checks.
- `TestResults/GrandAcceptance-v11.xml`: early campaign acceptance, all 1000 UI buttons, 20 native environments, six physical pilot courses and bonus/ownership tests.
- `TestResults/GrandPlayMode-v11.xml`: complete retained gameplay regression and the 41-course baseline sample; 38 early courses retain their geometry. Its three late-course results are superseded after the expanded late acts.
- `TestResults/GrandFinal-v11.xml`: final save-load setup correction, current native models/render budgets, UI, challenge/ownership checks, a repeated six-course pilot and 11 rebuilt late gauntlets.
- `TestResults/GrandVisualFinal-v11.xml`: all 20 environments and final levels after bringing local scenery closer to the road while retaining clearance and renderer budgets.
- `TestResults/grand-structure-v11.json`: exactly 1000 compact serialized courses, 20 biomes, 25 recipes and 1000 distinct normalized route descriptors.
- `TestResults/challenge-navigation-v11.json`: actual native button IDs 1–1000, 20 chapters, all page boundaries and list reset.
- `TestResults/grand-traversal-v11.csv`: 49 representative physical courses, every recipe and biome represented; ordinary control forces, no teleport completion.
- `TestResults/grand-native-views-v11.json`: 22 native course inspections with Editor construction/memory and renderer counts. Inspection poses are separate from traversal evidence.
- `TestResults/GrandAssets-v11.txt`: 137033 offline metadata/reference/geometry checks. Offline domain checks are separately recorded in `GrandDomain-v11.txt`.
- `TestResults/grand-release-v11.json`: APK checksum, signature/manifest/alignment/CRC/package checks and test accounting.

## Earlier failures and correction

The new reload test initially inspected a newly constructed SaveStore before calling Load. Calling Load corrected that fixture; this was not a runtime progress-loss fix. Any raw failed cases are listed below, and must have a later passing result for release verification to succeed.

- `TestResults/GrandAcceptance-v11.xml`: `BallQuest.Tests.GrandCampaignV11Tests.OnlyTheActiveCourseExpandsItsCollisionGeometryAndSpecialIdsRemainSeparate` — Expected: 1000   But was:  1
- `TestResults/GrandPlayMode-v11.xml`: `BallQuest.Tests.GrandCampaignV11Tests.OnlyTheActiveCourseExpandsItsCollisionGeometryAndSpecialIdsRemainSeparate` — Expected: 1000   But was:  1

## Limits

All 1000 routes were structurally checked, not physically played end-to-end. The recipes intentionally recur with geometry/secondary-section variations; normalized uniqueness is not a proof of wholly different player experience. Automation does not establish human difficulty, sound preference or swipe comfort. Native images are rendered Editor inspection poses and debug overviews. No phone was attached, so Android pacing, thermals, battery and sustained frame rate remain unverified. The APK is signed with the existing testing certificate; it is not a store release.
