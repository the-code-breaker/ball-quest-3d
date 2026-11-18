# Fifty-five distinct elevated courses

The v9 campaign replaces the old forward-only recipe assembly while retaining level IDs and asset GUIDs. The generator runs once at catalog content version 22, after the legacy campaign/profile/race upgrades. It changes only campaign definitions; the five dedicated race tracks and five signature adventures retain their definitions.

Every course has an explicit authored route body. Its heading follows actual turns, allowing return travel, large arcs, hairpins, stacked paths and crossings. Five different layouts belong to each of eleven interleaved families; consecutive levels move to a different family rather than replaying the same road with a new obstacle.

| Family | First course ID | Main challenge |
| --- | --- | --- |
| Sky Ribbon | 1 | Sweeping curves, crests and exposed connecting ribbons |
| Knife Edge | 2 | Long rail-free balance beams and deliberate direction changes |
| Orbit Tower | 3 | Ascending/descending coils and stacked orbit exits |
| Switchback | 4 | True returning hairpins and climbing terraces |
| Figure Eight | 5 | Crossing routes with physical vertical separation |
| Island Descent | 6 | Controlled freefalls onto separate lower catch roads |
| Clockwork | 7 | Timed doors, hammers, pendulums and moving shuttles |
| Catwalk | 8 | Exposed rooftop beams, ice balconies and optional side rewards |
| Sky Labyrinth | 9 | Forks, optional risky inside lines, courtyards and returning bends |
| Velocity Canyon | 10 | Accelerators followed by braking platforms and tight bends |
| Finale | 11 | Longer combinations of the preceding precision mechanics |

The next variant of each family appears eleven IDs later. The fifty-five titles and route bodies are individually authored; variants are not just uniform resizes. The generated [route atlas](LEVEL_ATLAS_V9.svg) shows actual route footprints for comparison.

The first source-geometry preflight reports 122.6–397.8 metre routes and at most 204 route segments. Generated-asset tests confirm forty-four courses marked hard/expert and fifty-one containing rail-free precision sections, including optional lines. Exposed beams narrow to 1.65–2.3 metres around a one-metre-diameter ball. Entries are 6.2 metres wide, authored checkpoint pads are six metres wide, and normal approaches are wider than their precision sections. A route name or difficulty flag does not itself prove human difficulty.

Thirteen drops span 4.5–7.5 metres vertically. Their physical lower catch roads are at least eight metres wide and 31–38 metres long, selected per drop so each exceeds `25 * sqrt(2 * height / 9.81) + 3` metres. The highest 7.5-metre falls always receive 38-metre catches. Machinery is placed on wider decision platforms, while exposed balance sections remain clear enough for deliberate steering. Boosters use the actual route heading and a broad braking plateau before the next bend.

`CrazyCampaignV9Tests` compares 32 equidistant route samples after removing position and uniform scale. Every pair must differ by more than 0.08 normalized root-mean-square units; this catches duplicate road footprints even when resized. Additional tests check entries/checkpoints/precision bounds, drop catches and at least four metres of separation at remote route crossings. Source preflight found and corrected two same-height crossings before Unity generation.

The campaign validation driver remains test-only and uses the shared physical command interface. It chooses lower speeds and shorter look-ahead on narrow roads, brakes before upcoming precision sections, compensates physical slopes/resistance, and accelerates across collapsing tiles. It does not write player pose or velocity, disable hazards, or alter gravity.

## Executed physical validation

The six-course pilot (IDs 2/5/6/7/47/55) completed all routes with zero falls in 72.30 wall seconds: [pilot XML](../TestResults/CampaignPilot-v9.xml), [six-row CSV](../TestResults/production-pilot-v9.csv). It includes needles, a figure eight, freefall, machinery, an expert coil and the final gauntlet.

The subsequent complete campaign run passed all fifty-five routes with **zero falls**, without requiring any driver or level changes after that pilot. The Unity case passed in **659.805 wall seconds**: [full XML](../TestResults/CampaignTraversal-v9.xml), [55-row traversal CSV](../TestResults/production-traversal-v9.csv), [machine-readable summary](../TestResults/campaign-physical-validation-v9.json). Each route crossed its ordered checkpoints and actual finish trigger. The separate pilot is not counted as another fifty-five courses.

Actual quarter-route camera frames were captured for IDs 1–11 and 45–55 after the shipping camera's LateUpdate, including [the final bare beam](../TestResults/Previews/V9Campaign55Actual.png), [the expert coil](../TestResults/Previews/V9Campaign47Actual.png), and [the first island descent](../TestResults/Previews/V9Campaign6Actual.png). Those automated-input art/physics views show an empty HUD progress bar because the existing human CoursePathFollower intentionally ignores ExternalControl; the test driver's ordered route distance and physical traversal are recorded independently. Human input uses the normal CoursePathFollower path.

The generated-asset diversity test also passed: all 55 titles and normalized footprints are distinct; the closest pair differs by 0.105357 normalized RMS units, above the 0.08 duplicate threshold. The serialized physical-data/core-controller snapshot matches the validated baseline. [Diversity JSON](../TestResults/campaign-diversity-v9.json), [physical-data baseline](../TestResults/campaign-physics-baseline-v9.json). Rendering and audio refinements made later require separate review; the snapshot does not certify arbitrary LevelBuilder edits.

This is Editor simulation through the shared physical controller. It establishes solvability under this input policy, not phone touch comfort, player difficulty ratings or sustained Android performance. Existing unlocks and historical per-ID records are retained; old best times describe the prior route geometry.
