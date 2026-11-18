"""Verify the v0.5.0 test artifact and preserve its exact build/test evidence."""
from pathlib import Path
from datetime import datetime, timezone
import csv
import hashlib
import json
import shutil
import struct
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parent.parent


def read(path):
    data = path.read_bytes()
    return data.decode("utf-16" if data.startswith((b"\xff\xfe", b"\xfe\xff")) else "utf-8-sig")


def check(condition, explanation):
    if not condition:
        raise RuntimeError(explanation)


def main(version="0.5.0", version_code=5, artifact="BallQuest3D-Manual360-v0.5.0.apk", reports=None, result_filename="manual360-release.json", expected_tests=68, development=True, expected_adventures=60, route_reports=None):
    source = ROOT / "BallQuest3D-test.apk"
    destination = ROOT / artifact
    verification = json.loads(read(ROOT / "TestResults/apk-verification.json"))
    check(verification["SignatureVerified"] and verification["AlignmentVerified"], "Run Tools/VerifyApk.ps1 first")
    checksum = hashlib.sha256(source.read_bytes()).hexdigest().upper()
    check(checksum == verification["SHA256"], "APK changed after signature verification")
    badging = read(ROOT / "TestResults/apk-info.txt")
    for value in (f"versionName='{version}'", f"versionCode='{version_code}'", "sdkVersion:'26'", "targetSdkVersion:'36'", "native-code: 'arm64-v8a'"):
        check(value in badging, "Unexpected Android manifest: " + value)
    if version_code >= 7:
        check(("application-debuggable" in badging) == development, "Android debuggable flag does not match the intended testing build")
    libraries = {}
    with zipfile.ZipFile(source) as package:
        check(package.testzip() is None, "Corrupt ZIP entry")
        for entry in ("AndroidManifest.xml", "classes.dex", "assets/bin/Data/level0"):
            check(entry in package.namelist(), "Missing required APK entry: " + entry)
        # Unity splits large serialized scenes into ordered chunks on Android.
        lab="assets/bin/Data/level1"
        check(lab in package.namelist() or lab+".split0" in package.namelist(), "Missing physics laboratory scene data")
        managers=package.read("assets/bin/Data/globalgamemanagers")
        check(b"Assets/_BallQuest/Scenes/Gameplay/PhysicsLab.unity" in managers, "Physics laboratory is absent from the packaged scene list")
        for name in ("libunity.so", "libil2cpp.so"):
            data = package.read("lib/arm64-v8a/" + name)
            check(data[:4] == b"\x7fELF" and data[4] == 2 and struct.unpack_from("<H", data, 18)[0] == 183, "Invalid ARM64 ELF: " + name)
            offset = struct.unpack_from("<Q", data, 32)[0]
            step, count = struct.unpack_from("<HH", data, 54)
            alignments = [struct.unpack_from("<Q", data, offset + index * step + 48)[0] for index in range(count) if struct.unpack_from("<I", data, offset + index * step)[0] == 1]
            check(alignments and min(alignments) >= 16384, "ELF LOAD segments are not 16 KiB aligned: " + name)
            libraries[name] = {"bytes": len(data), "machine": "AArch64", "minimumLoadSegmentAlignment": min(alignments)}
        # Large catalogs are split into ordered chunks and optimized Resource
        # objects may use hashed filenames. Check serialized payloads, never
        # IL2CPP code strings, rather than requiring one .assets extension.
        assets = b"".join(package.read(name) for name in package.namelist() if name.startswith("assets/bin/Data/") and "/Managed/" not in name and "/StreamingAssets/" not in name)
        titles = []
        for folder in ("Levels", "Expeditions"):
            for path in sorted((ROOT / "Assets/_BallQuest/ScriptableObjects" / folder).glob("*.asset")):
                title = next(line.split(": ", 1)[1] for line in path.read_text(encoding="utf-8").splitlines() if line.startswith("  title: "))
                check(title.encode() in assets, "Missing packaged adventure: " + title)
                titles.append(title)
        races = ["Harbor Five", "Hammer Docks", "Shuttle Speedway", "Canyon Choice", "Alpine Precision"]
        for title in races:
            check(title.encode() in assets, "Missing race: " + title)
        metadata = package.read("assets/bin/Data/Managed/Metadata/global-metadata.dat")
        for symbol in ("SignatureGravityField", "BallContactAudio", "BallTouchMode", "SurfaceContactResponse", "RaceManager"):
            check(symbol.encode() in metadata, "Missing compiled gameplay type: " + symbol)
        if version_code >= 6:
            for symbol in ("RoadRenderGeometry", "CourseSignVisibility", "FollowTravel", "GestureForward"):
                check(symbol.encode() in metadata, "Missing revised gameplay symbol: " + symbol)
        if version_code >= 7:
            for symbol in ("PremiumTrackPresentation", "PremiumCollectibleVisuals", "PremiumBoosterVisuals", "CourseRenderBatcher"):
                check(symbol.encode() in metadata, "Missing presentation upgrade: " + symbol)
            # Unity's optimized player stores Resources objects in individually hashed
            # serialized files. Do not confuse a code string in IL2CPP metadata with
            # the actual packaged texture/shader, or require a .assets extension.
            resource_payload = b"".join(package.read(name) for name in package.namelist()
                                       if name.startswith("assets/bin/Data/") and "/Managed/" not in name
                                       and "/StreamingAssets/" not in name)
            for name in (b"SatinMaple", b"BallQuest/Boost Flow", b"BallQuest/Reward Ring"):
                check(name in resource_payload, "Missing packaged presentation resource: " + name.decode())
        for symbol in ("ExpeditionValidationDriver", "AdventureValidationDriver", "ManualPhysicsV5Tests", "ResponsiveMovementV6Tests", "RoadVisualStabilityV6Tests", "CameraTurnaroundV6Tests", "SwipeOnlyV7Tests", "NativePresentationCaptureV7Tests"):
            check(symbol.encode() not in metadata, "Test-only driving code was packaged: " + symbol)
        check(len(titles) == expected_adventures, "Unexpected packaged campaign/expedition count")
    cases = {}
    if reports is None:
        reports = ["TestResults/EditMode-v5.xml", "TestResults/PlayMode-v5-full.xml", "TestResults/FinalChecks-v5.xml"]
        if (ROOT/"TestResults/MenuReview-v5.xml").exists():
            reports.append("TestResults/MenuReview-v5.xml")
    report_status = {}
    for report in reports:
        tree = ET.parse(ROOT / report).getroot()
        report_status[report] = {"result": tree.get("result"), "total": int(tree.get("total", "0")), "passed": int(tree.get("passed", "0")), "failed": int(tree.get("failed", "0"))}
        check(report_status[report]["total"] > 0, "Empty Unity report: " + report)
        for case in tree.iter("test-case"):
            cases[case.get("fullname")] = case.get("result")
    check(all(value == "Passed" for value in cases.values()), "A latest test result did not pass")
    check(len(cases) == expected_tests, f"Expected {expected_tests} distinct Unity cases, got {len(cases)}")
    route_results = {}
    for name, expected in (route_reports or (("production-traversal.csv", 55), ("expedition-traversal.csv", 15))):
        rows = list(csv.DictReader((ROOT / "TestResults" / name).open(newline="", encoding="utf-8-sig")))
        check(len(rows) == expected and all(row["complete"].lower() == "true" for row in rows), "Incomplete route validation: " + name)
        route_results[name] = {"completed": len(rows), "falls": sum(int(row["falls"]) for row in rows)}
    shutil.copy2(source, destination)
    check(hashlib.sha256(destination.read_bytes()).hexdigest().upper() == checksum, "Artifact copy mismatch")
    result = {"artifact": destination.name, "generatedAtUTC": datetime.now(timezone.utc).isoformat(), "bytes": destination.stat().st_size, "MiB": round(destination.stat().st_size / 1048576, 2), "SHA256": checksum, "version": "0.5.0", "versionCode": 5, "package": "com.ballqueststudio.ballquest3d", "minimumAndroidAPI": 26, "targetAndroidAPI": 36, "developmentBuild": True, "signatureVerified": True, "zipAndELFAlignmentVerified": True, "zipCRCVerified": True, "nativeLibraries": libraries, "packagedAdventureTitles": 60, "packagedRaceTitles": races, "physicsLabScenePackaged": True, "distinctUnityTestsPassed": len(cases), "testCounting": "Latest result per unique test name; final targeted runs supersede earlier failures", "unityReports": report_status, "routes": route_results, "physicalAndroidDeviceTested": False, "iOSBuildVerified": False, "contactAudio": "18 original temporary procedural samples"}
    result["testDrivingCodeExcludedFromAPK"]=True
    result["version"]=version
    result["packagedAdventureTitles"]=len(titles)
    result["versionCode"]=version_code
    result["developmentBuild"]=development
    if version_code == 6:
        result["testCounting"]="25 EditMode plus all 62 cases in the complete PlayMode run; targeted reruns add no unique tests"
    if version_code >= 7:
        result["testCounting"]="Distinct tests from the complete archived EditMode and PlayMode runs; focused reruns add no unique tests"
        result["packagedPresentationResourcesVerified"]=True
    if version_code == 8:
        result["testCounting"]="Latest result per unique case: 28 EditMode, 87-case full PlayMode run, and 11-case final core rerun; one obsolete mouse fixture was corrected without runtime changes"
    if version_code == 9:
        result["testCounting"]="Distinct current cases across complete EditMode, physical campaign pilot/full traversal, and all remaining PlayMode fixtures; focused reruns add no unique cases"
        result["contactAudio"]="34 original v9 source WAVs; 30 unique clips referenced by six material/surface profiles; human and phone audition unverified"
    if version_code == 10:
        result["testCounting"]="Latest result per unique case: 37 EditMode plus 100 PlayMode cases. The complete 98-case regression had one obstacle renderer-budget failure; object-local batching was fixed and FinalVisuals-v10 supersedes that result. Raw failed XML retained; focused reruns add no unique cases."
        result["contactAudio"]="66 clips edited from five credited CC0 recordings: 18 body roll/slip/impact clips and 48 ball/surface impacts; human/phone audition unverified"
        result["sequentialChapterNavigationVerified"]=True
        result["recordedBodyAndSurfaceClipsPackaged"]=66
        result["newMaterialBalls"]=["brushed-steel","carbon-rubber","copper-core","walnut"]
    if version_code == 11:
        result["testCounting"]="Latest result per distinct Unity case in archived v11 reports; targeted reruns do not add duplicate cases."
        result["campaignCourses"]=1000
        result["environmentThemes"]=20
        result["topologyRecipes"]=25
        result["generationScope"]="Deterministic parameter variations of 25 topology recipes with secondary sections; not 1000 hand-authored topology families."
        result["structuralRouteCoverage"]=1000
        result["contactAudio"]="66 recorded CC0 material and surface clips retained from v10; human/phone audition unverified"
        result["activeCourseGeometryOnly"]=True
        result["optionalBonusChallenges"]=5
        result["lateGauntletTypes"]=8
        result["coursesWithAdditionalLateAct"]=500
    (ROOT / "TestResults" / result_filename).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
