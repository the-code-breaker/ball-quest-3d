from pathlib import Path
import csv,json,re,xml.etree.ElementTree as ET
from VerifyManualRelease import read
root=Path(__file__).resolve().parents[1]
release=json.loads((root/'TestResults/grand-release-v11.json').read_text(encoding='utf-8'))
views=json.loads((root/'TestResults/grand-native-views-v11.json').read_text(encoding='utf-8'))['views']
routes=list(csv.DictReader((root/'TestResults/grand-traversal-v11.csv').open(encoding='utf-8-sig')))
count=release['distinctUnityTestsPassed'];size=release['MiB'];artifact=release['artifact'];sha=release['SHA256']
renderers=max(v['activeRenderers'] for v in views);build=max(v['editorCourseBuildMs'] for v in views)
structural_checks=int(re.search(r'(\d+) structural checks passed',read(root/'TestResults/GrandAssets-v11.txt'))[1])
def put(path,text): (root/path).write_text(text,encoding='utf-8')
for filename in ('QA','BUILDS'):
    old=root/f'Documentation/{filename}.md';archive=root/f'Documentation/{filename}_V10.md'
    if old.exists() and not archive.exists():archive.write_bytes(old.read_bytes())
put('README.md',f'''# Ball Quest 3D — Thousand Worlds 0.11.0

**[Download the testing APK]({artifact})** — **{size} MiB**, Android 8+, ARM64. [Delivery notes](Documentation/DELIVERY_V11.md) · [Verified release record](TestResults/grand-release-v11.json).

- **1,000 consecutive campaign levels**, organized into 20 chapters of 50, with actual route previews and complete pagination.
- **25 topology recipes and 20 environment themes** with parameter and secondary-section variations: helices, hairpins, overpasses, narrow beams, drops, moving bridges, gates, forks and boost sections.
- Five optional bonus objectives: flawless finish, coin hunt, time trial, treasure and boost use. Progress appears in the HUD; each bonus pays once and persists.
- Original generated chapter illustrations and prop concepts, with new native scenery meshes, road materials, lighting and supports.
- Only the active course expands its detailed road geometry. The improved swipe controls, automatic camera, modeled coins/boosters and recorded material audio remain in place.

All **{count} distinct current Unity cases passed** after a save-test setup correction. All **1,000 routes passed structural checks**; the **49-course physical sample** spans all layout recipes and environment themes. The 15 special-adventure routes also completed. These are procedural course variations, not 1,000 individually hand-authored topology families.

No Android phone was attached. Human difficulty, feel and sustained device performance remain unverified. The generic BallQuest3D-test.apk is identical to the named APK. Earlier APKs and reports are historical.

[QA and scope](Documentation/QA.md) · [Build handoff](Documentation/BUILDS.md) · [Research and implementation plan](Documentation/DEVELOPMENT_V11.md) · [Generated art prompts](Documentation/ArtDirection/V11/README.md) · [Native world views](Documentation/ArtDirection/V11/NATIVE_VIEWS.md)
''')
put('Documentation/DELIVERY_V11.md',f'''# Thousand Worlds 0.11.0 delivery

[Testing APK](../{artifact}) — {release['bytes']:,} bytes ({size} MiB), version 0.11.0/code 11. SHA-256: `{sha}`.

The campaign now contains exactly 1,000 serialized, contiguous course IDs. The first 55 assets retain their identities. All chapter and level pages are reachable; next-level navigation follows consecutive IDs. The five signature adventures remain a separate collection with explicit load/retry/next navigation, so campaign level 56 cannot replace Sky Maze accidentally. Save validation now retains campaign unlocks and records through level 1000.

The courses combine 25 topology recipes, six secondary-section choices, independent dimensional seeds and 20 environment themes. Every route has a distinct translation/scale-normalized descriptor. Later courses narrow exposed beams toward 1.7m and introduce mixed machinery and precision sections. This is deterministic procedural variation; related recipe structures recur.

Every course from 501 through 1000 includes a full additional checkpointed act, chosen from eight gauntlet types: terraces and needles, mixed machinery, twin freefalls, a rising helix, reverse hairpins, shuttle/fragile crossings, boost-to-balance and forked machinery. This makes the second half more substantial than seed-only reprises.

Lagoon, desert, alpine, neon city, orbital, volcano, candy, bamboo, temple, mushrooms, coral, pirate, clockwork, autumn, crystal, cloud palace, storm, circuit, toy and canyon themes change landmark silhouettes, palette, lighting, road finish and supports. Generated chapter illustrations are included in the native UI. The original prop sheet guides simpler real-time Unity meshes; illustrations are not gameplay screenshots. [Exact prompts and assets](ArtDirection/V11/README.md), [prop prompt](ArtDirection/V11/PropConcepts.md), [native views](ArtDirection/V11/NATIVE_VIEWS.md).

Each course offers a flawless, coin, time, treasure or boost objective. Objectives are optional, show live progress and grant a persisted one-time coin bonus. Normal progression is available when the bonus is missed. Collection quotas are checked against actual placed rewards and pads. Campaign milestone missions now extend through 1000 clears.

Only the active course clones and expands its collision roads. The catalog retains compact routes, widths and flags; unloading releases the expanded definition. Scenery uses shared materials and spatial mesh batches. The 22-view Editor review measured at most {renderers} active renderers and {build:.2f}ms course construction. These are Editor measurements, not Android frame-rate guarantees. Existing swipe-only propulsion, automatic ball-following camera and 66 recorded CC0 ball/surface clips are retained.

All {count} distinct current Unity tests passed after final reruns. All 1000 routes received structural checks; 49 representative courses covered all 25 recipes and 20 environments using ordinary force/control commands, with {sum(int(r['falls']) for r in routes)} total falls. The six-course pilot and all 15 signature-adventure variants completed too. Tests also cover sequential button navigation, record 999/1000 persistence, optional bonuses/replay protection, active-course memory ownership, controls, camera, audio, obstacles and racing.

APK signature, manifest, ZIP CRC, ARM64 libraries, 16 KiB ZIP/ELF alignment and serialized package content were verified. The package contains all 1000 campaign titles, five signature adventures, five race definitions, generated atlas and recorded audio; test-driving code is excluded. No physical Android device was connected. Not all 1000 courses were physically traversed, and human challenge balance, audio preference, temperature and sustained phone performance remain unverified.

[Machine-readable release evidence](../TestResults/grand-release-v11.json) · [QA details](QA.md) · [Online references and plan](DEVELOPMENT_V11.md).
''')
failures=[]
for report in release['unityReports']:
    tree=ET.parse(root/report)
    failures.extend(f"- `{report}`: `{c.get('fullname')}` — {c.findtext('failure/message','').strip().replace(chr(10),' ')}" for c in tree.iter('test-case') if c.get('result')!='Passed')
put('Documentation/QA.md',f'''# QA — 0.11.0

The final release verifier found **{count} distinct current passing Unity cases**, counting the latest result for each unique test name. Raw earlier failures remain in their archived XML files; final focused reruns supersede them.

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
- `TestResults/GrandAssets-v11.txt`: {structural_checks} offline metadata/reference/geometry checks. Offline domain checks are separately recorded in `GrandDomain-v11.txt`.
- `TestResults/grand-release-v11.json`: APK checksum, signature/manifest/alignment/CRC/package checks and test accounting.

## Earlier failures and correction

The new reload test initially inspected a newly constructed SaveStore before calling Load. Calling Load corrected that fixture; this was not a runtime progress-loss fix. Any raw failed cases are listed below, and must have a later passing result for release verification to succeed.

{chr(10).join(failures) if failures else 'No failed cases in the archived report set.'}

## Limits

All 1000 routes were structurally checked, not physically played end-to-end. The recipes intentionally recur with geometry/secondary-section variations; normalized uniqueness is not a proof of wholly different player experience. Automation does not establish human difficulty, sound preference or swipe comfort. Native images are rendered Editor inspection poses and debug overviews. No phone was attached, so Android pacing, thermals, battery and sustained frame rate remain unverified. The APK is signed with the existing testing certificate; it is not a store release.
''')
put('Documentation/BUILDS.md',f'''# Build handoff — 0.11.0

[Testing APK](../{artifact}) — {size} MiB, {release['bytes']:,} bytes, ARM64 IL2CPP, Android API 26 minimum / 36 target, optimized nondevelopment testing build. Package `com.ballqueststudio.ballquest3d`, version `0.11.0` / code `11`.

SHA-256: `{sha}`. The generic `BallQuest3D-test.apk` is identical. Install over the prior testing APK to retain existing progress; the existing signing certificate is preserved.

```powershell
$unity = "$PWD\\.tooling\\UnityInstalled\\Editor\\Unity.exe"
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check Generate
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check EditMode
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check PlayMode
.\\Tools\\UnityChecks.ps1 -Unity $unity -Check AndroidTest
.\\Tools\\VerifyApk.ps1 -Apk Builds/Android/BallQuest3D-test.apk
python Tools/VerifyGrandRelease.py
```

Only one Unity process can open this project. Archive current raw reports before generic files are overwritten. The release verifier uses the exact report paths in `Tools/VerifyGrandRelease.py`; a full fresh passing run may replace those slices after its scope and distinct-test count are checked. Do not substitute prior-version results. The physical campaign sample contains 49 courses, not all 1000.

Both BallQuest and PhysicsLab scenes are packaged. Native test drivers and Editor generators are excluded. [Release evidence](../TestResults/grand-release-v11.json) · [QA](QA.md) · [Changes](DELIVERY_V11.md). No physical phone test or iOS build was performed.
''')
print(f'Wrote v11 delivery, QA, README and build handoff for {count} passing distinct cases.')
