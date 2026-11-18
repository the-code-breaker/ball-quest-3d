# Production course and obstacle authoring

This document records the implemented authoring data. Unity validation completed all 55 adventure courses with every ordered checkpoint and zero falls using ordinary force-based control commands. All five racing courses completed with physical competitors. See [DELIVERY_V4.md](DELIVERY_V4.md) for evidence, test limits and the verified Android APK.

The campaign seed contains 55 explicitly composed modular recipes. Each recipe selects geometry and collider/hazard modules. First-12 asset GUIDs and numeric progression IDs are retained. The one-time seed runs below catalog contentVersion21; subsequent normal authoring edits the LevelDefinition ScriptableObjects.

| ID | Course | Difficulty | Authored modules |
| --- | --- | --- | --- |
| 1 | Harbor Launch | Beginner | Roll, Right, Roll, Slalom |
| 2 | Crane Alley | Beginner | Left, Right, Roll, Left |
| 3 | Skywheel Sprint | Beginner | Grip, Hill, Right, Roll |
| 4 | Canyon Courier | Beginner | Roll, Split, Left, Roll |
| 5 | Temple Switchback | Beginner | Slalom, Left, Right, Grip |
| 6 | Dune Dive | Easy | Roll, Pendulum, Right, Grip |
| 7 | Glacier Glide | Easy | Left, Hammer, Grip, Right |
| 8 | Frostline Bridge | Easy | Right, Narrow, Left, Hill |
| 9 | Polar Plunge | Easy | Roll, Jump, Grip, Right |
| 10 | Neon Overdrive | Easy | Roll, Turbo, Left, Hill |
| 11 | Signal Slalom | Easy | Slalom, Wall, Right, Narrow |
| 12 | Final Ascent | Easy | Hill, Ice, Grip, Left |
| 13 | Wheel Window | Easy | Roll, Wheel, Right, Narrow |
| 14 | Harbor Weave | Easy | Left, Right, Hammer, Slalom |
| 15 | Gate Graduation | Easy | Narrow, Wall, Jump, Grip, Pendulum |
| 16 | Falling Footsteps | Medium | Roll, Falling, Grip, Left, Slalom |
| 17 | Lift Above | Medium | Hill, Lift, Right, Hammer, Grip |
| 18 | Shuttle Crossing | Medium | Left, Shuttle, Narrow, Wall, Right |
| 19 | Push And Pass | Medium | Roll, Push, Left, Wheel, Grip |
| 20 | Door Rhythm | Medium | Right, Door, Hill, Pendulum, Roll |
| 21 | Rolling Quarry | Medium | Slalom, Roller, Right, Narrow, Grip |
| 22 | Banked Lookout | Medium | Left, Spiral, Grip, Wall, Roll |
| 23 | Three Lane Choice | Medium | Split, Slalom, Push, Right, Jump |
| 24 | Crush Clock | Medium | Roll, Crush, Grip, Wheel, Narrow |
| 25 | Bounce Board | Medium | Right, Bounce, Roll, Hammer, Grip |
| 26 | Conveyor Curve | Medium | Left, Conveyor, Right, Wall, Shuttle |
| 27 | Vertical Motion | Medium | Wheel, Hill, Forward, Grip, Pendulum |
| 28 | Fragile Bridge | Medium | Narrow, Falling, Right, Lift, Grip |
| 29 | First Freefall | Medium | Hill, Drop, Grip, Slalom, Wall |
| 30 | Midterm Circuit | Medium | Hammer, Jump, Shuttle, Door, Grip, Split |
| 31 | Crane Rush | Hard | Turbo, Pendulum, Right, Door, Narrow, Jump |
| 32 | Ice Gauntlet | Hard | Ice, Wall, Grip, Hammer, Narrow, Left |
| 33 | Long Leap | Hard | Hill, Jump, Turbo, Jump, Grip, Push |
| 34 | Switchback Surge | Hard | Turbo, Left, Right, Zigzag, Pendulum, Grip |
| 35 | Falling Chicane | Hard | Falling, Left, Wall, Falling, Right, Grip |
| 36 | Sky Elevator | Hard | Lift, Narrow, Hammer, Forward, Hill, Jump |
| 37 | Twin Rolling | Hard | Roller, Split, Wall, Roller, Narrow, Grip |
| 38 | Double Door | Hard | Door, Left, Wheel, Right, Door, Turbo |
| 39 | Hammer Tips | Hard | Pendulum, Narrow, Hammer, Grip, Narrow, Shuttle |
| 40 | Dune Drop | Hard | Turbo, Hill, Drop, Grip, Roll, Crush |
| 41 | Neon Crossfire | Hard | Door, Wheel, Push, Zigzag, Blade, Grip |
| 42 | Spiral Overlook | Hard | Spiral, Hammer, Grip, Circle, Right, Jump |
| 43 | Lane Splitter | Hard | Split, Hill, Roller, Split, Wall, Narrow |
| 44 | Glacier Shuttle | Hard | Ice, Shuttle, Grip, Wheel, Ice, Forward |
| 45 | Hanging Gauntlet | Hard | Pendulum, Right, Pendulum, Left, Hammer, Pendulum |
| 46 | Catch And Climb | Hard | Drop, Grip, Hill, Lift, Door, Narrow |
| 47 | Crush Alley | Hard | Crush, Narrow, Push, Right, Crush, Wheel |
| 48 | Speed Weave | Hard | Turbo, Zigzag, Blade, Grip, Turbo, Split |
| 49 | Skyline Steps | Hard | Bounce, Falling, Grip, Jump, Hill, Forward |
| 50 | Grand Tour | Hard | Hammer, Lift, Ice, Door, Drop, Grip, Split |
| 51 | Skyline Mastery | Expert | Split, Wheel, Roller, Narrow, Pendulum, Jump, Drop |
| 52 | Fragile Spiral | Expert | Falling, Spiral, Grip, Door, Circle, Falling, Right |
| 53 | Precision Relay | Expert | Narrow, Push, Shuttle, Blade, Narrow, Grip, Jump |
| 54 | Bonus Coin Rush | Expert | Turbo, Split, Hill, Jump, Grip, Drop, Roll |
| 55 | Summit Finale | Expert | Hill, Spiral, Wheel, Lift, Falling, Grip, Pendulum, Drop |

## Actual module behavior

- Ramps and hill crests use static mitered meshes with matching collision surfaces; jumps use short real gaps and long lower landing zones. No jump button or unavailable transformation is required.
- Narrow bridges preserve a safe physical lane while requiring controlled steering. High-grip, slippery, ice and bouncy sections use separate authored PhysicMaterial assets.
- Timed doors cycle through open green, passable amber warning and solid red phases. Players and AI receive the same collisions. No invisible damage phases are used.
- Falling/collapsing deck sections warn after contact, then release their Rigidbody under ordinary gravity. Explicit retry/reset restores their body and materials before input is enabled.
- Side shuttles, fore/aft bridges, vertical elevators and circular decks use interpolated kinematic Rigidbody movement and high-grip contact materials. The shared ball controller samples support-body point velocity without transform parenting.
- Rolling stone hazards are dynamic bodies driven by bounded forces along a predictable patrol, with gravity and collisions intact. Pushing/crushing walls, horizontal hammer arms, swinging hammers, vertical rotors and rotating blades have separate visible machinery and collision patterns.
- Courses 22, 42, 52 and 55, plus racing course 4, include a banked 210-degree climbing spiral with 12m total rise. Ordered three-dimensional route points support navigation and progress through turns that reverse world-Z direction. Support piers are excluded where they would obscure a lower road.
- Split modules build a broad outside line plus a narrower high-grip inside branch with extra collectibles. Main-route progress metadata and race alternate-route entry/exit metadata are distinct from the physical geometry.
- Boosters use their configured world direction through the shared controller boost API. They do not assign a path position or bypass collisions.

## Dedicated racing courses

| ID | Course | Strategic composition |
| --- | --- | --- |
| 1 | Harbor Five | Wide five-ball start, broad curves, hill, turbo runout and final left bend. |
| 2 | Hammer Docks | Swinging and rotating machinery, moving wall and a final boost section. |
| 3 | Shuttle Speedway | Side-moving and fore/aft platforms, climbing approach and competitive open lanes. |
| 4 | Canyon Choice | Narrow inside branch, wider main line, moving wall and a true banked climbing spiral. |
| 5 | Alpine Precision | Narrow bridge, vertical wheel, elevator, natural ramp jump and final sprint. |

Race definitions reference separate course assets with course IDs1001-1005 and race career IDs1-5. Default opponent count is four; the race system may configure3-7. Alternate-route waypoints match physically built branch geometry. Ordered route arcs, gates and actual physics determine ranking.

## Ownership and retry

LevelBuilder owns runtime materials, surface fallbacks, meshes and textures and releases them when the course is destroyed. Six contact-material assets are shared authored resources. Static road/scenery geometry is batched; dynamic platform and hazard objects remain separate Rigidbody actors. SpawnCompetitor reuses the same sphere, BallController, BallLook and trail setup as the player.

Solo checkpoint reset calls LevelBuilder.ResetAtCheckpoint only after positioning the sphere at its saved safe checkpoint, while control remains disabled. This resets ICourseResettable machinery and reprojects the solo path follower. Race recovery must not reset shared world hazards for one competitor; race restart may reset the whole course.

No production device frame-time, thermal or GPU claims are implied by this authoring implementation.
