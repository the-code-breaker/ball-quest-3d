# Historical arcade course design — 0.3.0

This document preserves the previous twelve-course design. It does not describe the current 0.4.0 runtime campaign. Use [PRODUCTION_COURSES.md](PRODUCTION_COURSES.md) for the implemented 55-course campaign and five racing tracks, and [QA.md](QA.md) for current validation evidence.

This campaign contains twelve authored routes across four playable worlds. Each course combines a distinct lateral/elevation profile with physical hazards, two turbo runways, two free falls, four checkpoints, coins, a gem and a rare crystal. Routes move generally along global +Z, with smooth lateral curves and ramps; arbitrary-heading switchbacks and fully traversable vertical loops are not implemented. The giant loop structures are decorative landmarks, and the smaller rings frame the actual driving route.

Validation status: all twelve courses passed controlled Unity Rigidbody traversal with real keyboard input, braking near moving hazards, both lower-path landings and finish detection. The recorded run had no falls. This does not establish touch feel or performance on physical phones.

## Courses

| ID | World | Course | Main challenge / mission | First / second drop |
| --- | --- | --- | --- | --- |
| 1 | Sky City | Harbor Launch | Learn upward flicks, coast through the first climb and line up both lower catch paths. Gentle lateral drift introduces the controls. | 8m / 10m |
| 2 | Sky City | Crane Alley | Thread moving gates above the harbor. A stronger alternating bend and rotating hammer change the approach to the second runway. | 10m / 11m |
| 3 | Sky City | Skywheel Sprint | Follow the ring landmarks, hold a clean landing line and navigate the narrower rising middle section. | 11m / 13m |
| 4 | Desert Canyon | Canyon Courier | Follow the canyon ridge, collect the gold and read the swinging hammer before the launch. | 10m / 11m |
| 5 | Desert Canyon | Temple Switchback | Read two broad lateral turns before the temple descent; the middle bridge is narrower than the landing platforms. | 12m / 12m |
| 6 | Desert Canyon | Dune Dive | Build and manage speed for the deepest desert plunge, then recover through the curving middle climb. | 15m / 14m |
| 7 | Snow Peaks | Glacier Glide | Cross ice with measured steering, climb back toward the second runway and keep momentum without excessive lateral drift. | 10m / 12m |
| 8 | Snow Peaks | Frostline Bridge | Balance along a narrow suspended section, time the swinging hammer and read the final moving gate. | 11m / 13m |
| 9 | Snow Peaks | Polar Plunge | Commit to two deep falls over the frozen bay, preserving a straight landing line before the next turn. | 15m / 15m |
| 10 | Neon City | Neon Overdrive | Chain the turbo runways through the illuminated skyline; control the large lateral curve and its rotating hammer. | 10m / 12m |
| 11 | Neon City | Signal Slalom | Read crossing gates and a middle hammer while maintaining a rhythm through the strong alternating turns. | 12m / 14m |
| 12 | Neon City | Final Ascent | Climb the highest starting ridge, manage the narrow rising middle section and master the final descent. | 14m / 15m |

Sky City uses harbor water, windowed towers and cranes. Desert Canyon uses faceted sandstone mesas, arches and palms. Snow Peaks uses glacier mountains, snow caps and fir trees. Neon City uses a darker sky, lit facades, luminous architectural strips and colored landmarks. All worlds retain the reference-inspired wooden racing surface and alternating yellow/black rounded rails.

## Drops, checkpoints and triggers

The first lower catch platform begins 2m before the upper launch lip in plan view and continues 66m beyond it: 68m total length. The second begins 2m before its lip and continues 68m beyond it: 70m total length. Both are 12m wide. They are deliberately generous catches for a ball moving up to approximately 25m/s under turbo, with the largest 15m fall taking roughly 1.75 seconds before contact when starting level with no upward velocity. Actual flight depends on launch velocity, ramp contact, collision and ball profile.

Each upper launch ends with an 8m section without rails. Warning stripes and a DROP AHEAD cue identify the edge. Turbo runways precede both drops. The route includes checkpoints before the launch and near the end of each catch platform; respawning clears momentum through the session/controller systems.

Checkpoint triggers are 14m wide and finish triggers are 12m wide, both 4m high and 1.4m deep. Their trigger coverage intentionally exceeds the narrower ornamental arch so a player using the valid deck edge can still register progress. This was corrected after automated traversal found an edge-of-deck finish miss on Canyon Courier.

## Authoring

`Assets/_BallQuest/Scripts/Editor/ArcadeCampaignGenerator.cs` contains the twelve route profiles, titles, mission strings, dimensions and hazard placement. Its `Generate(GameCatalog catalog)` method runs as part of project generation. It applies the authored campaign when `catalog.contentVersion < 10`, then sets the version to 10. The first six level asset GUIDs are retained while their content is replaced; assets 7-12 are added normally.

After that migration, normal authoring happens in the twelve `LevelDefinition` ScriptableObjects under `Assets/_BallQuest/ScriptableObjects/Levels/`. `mission` is the course description and `dropStarts` contains the two upper launch Z coordinates. `LevelBuilder.PointAt(level, z)` returns the surface center used by runtime route cues and automated driving. Re-running generation at contentVersion 10 preserves subsequent authored asset edits; changing the C# seed alone does not overwrite them.

Curves are sampled into short track pieces with matched centerline endpoints. The rendered deck uses mitered cross-sections; physical road segments use oriented box colliders with a small authored overlap at their joins. Rounded rail visuals follow those edges with separate collision guards. Static endpoint checks found the expected approximately 0.08m overlap between connected segments and exactly two intentional downward transitions per route; this structural check does not replace physics traversal.

## Geometry ownership and performance

`LevelBuilder` creates and disposes the world materials, wood/window textures and generated road, ring, mountain, star and crystal meshes. Track architecture and scenery are grouped under a static root and combined for rendering. The combined runtime meshes are also tracked for cleanup. Decorative primitive colliders are disabled. The actual deck, rail guards and moving hazards retain physical colliders.

Coins share their raised-rim/star meshes within a loaded course, and crystal pickups share a crystal mesh. The authored routes share primitive mesh resources and reusable materials wherever possible. Moving hazards use kinematic Rigidbody motion and compound colliders for the modeled hammer heads. Ball presentation is owned separately by `BallLook`; the physical sphere remains the player Rigidbody.

The four-world lighting pass clones the sky material rather than changing the authored asset and restores the source sky when unloading its instance. This prevents one course from leaving a destroyed material attached to global rendering settings. Actual Android frame time, GPU cost, memory, thermal behavior and long-session loading still require physical-device profiling; no measured mobile FPS is claimed here.


