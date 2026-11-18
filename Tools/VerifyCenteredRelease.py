"""Verify the v0.8 progressive-swipe and centered-camera testing APK."""
import csv
import json
import shutil
import statistics
import xml.etree.ElementTree as ET
import zipfile
from VerifyManualRelease import ROOT, check, main, read


EXPECTED_TESTS = 115
REPORTS = ["TestResults/EditMode-v8.xml", "TestResults/PlayMode-v8-full.xml", "TestResults/FinalChecks-v8.xml"]


def load_measurement(name):
    return json.loads(read(ROOT / "TestResults" / name))


def verify_measurements():
    results = ROOT / "TestResults"
    complete_classes = set()
    latest_cases = {}
    for report in REPORTS:
        tree = ET.parse(ROOT / report).getroot()
        if report.endswith("PlayMode-v8-full.xml"):
            check(int(tree.get("total", "0")) == 87, "Expected all 87 cases in the unmodified complete PlayMode report")
        else:
            check(tree.get("result") == "Passed", "Acceptance report has not passed: " + report)
        for case in tree.iter("test-case"):
            latest_cases[case.get("fullname")] = case.get("result")
            if report.endswith("PlayMode-v8-full.xml"):
                complete_classes.add(case.get("classname", ""))
    check(len(latest_cases) == EXPECTED_TESTS, "Unexpected distinct current test count")
    for name, result in latest_cases.items():
        check(result == "Passed", "Latest result has not passed: " + str(name))
    for fixture in ("ProgressiveSwipeV8Tests", "CenteredCameraV8Tests", "ShippingControlsV8Tests"):
        check("BallQuest.Tests." + fixture in complete_classes, "New acceptance fixture missing from complete PlayMode report: " + fixture)

    swipe = load_measurement("progressive-swipes-v8.json")
    for field, minimum, maximum in (("firstStrokeSpeed", 1.2, 2.3),
                                     ("fourthStrokeSpeed", 4.5, 7.5),
                                     ("eighthStrokeSpeed", 8.5, 12.5),
                                     ("twelfthStrokeSpeed", 10.5, 14.3)):
        check(minimum <= swipe[field] <= maximum, "Progressive stroke speed is outside acceptance range: " + field)
    check(swipe["firstStrokeSpeed"] < swipe["fourthStrokeSpeed"] < swipe["eighthStrokeSpeed"] <= swipe["twelfthStrokeSpeed"],
          "Repeated strokes did not progressively build speed")
    check(1.5 <= swipe["maximumSingleLargeStrokeSpeed"] <= 2.7, "One oversized stroke can add excessive speed")
    check(swipe["maximumParkedDrive"] == 0, "A parked finger added drive")
    check(.089 <= swipe["minimumCadenceBudget"] <= swipe["maximumCadenceBudget"] <= .091,
          "Stroke budget differs across sampling cadences")

    held = load_measurement("swipe-only-v8.json")
    check(held["stationaryMaximumSpeed"] == 0 and held["jitterMaximumSpeed"] == 0 and held["parkedMaximumDrive"] == 0,
          "Stationary touch or jitter added motion")
    check(held["noCommandReplay"], "Pointer command replay guard failed")

    camera = load_measurement("centered-camera-v8.json")
    check(camera["frames"] >= 750, "Centered output measurement did not exercise the full motion sequence")
    check(camera["maximumHorizontalViewportError"] < .008 and camera["maximumVerticalViewportError"] < .012,
          "Camera composition exceeds centered output tolerance")
    check(camera["firstSmallTurnStepDegrees"] > .05, "Camera still delays small deliberate turns")
    check(camera["maximumHeadingStepDegrees"] < 4.08, "Camera heading exceeded smooth turn-rate bound")
    check(camera["settledHeadingErrorDegrees"] < 2 and camera["physicalJumpObserved"],
          "Camera direction/jump framing acceptance failed")

    shipping = load_measurement("shipping-controls-v8.json")
    check(.9 <= shipping["firstStrokeSpeed"] <= 2.6, "Shipping first stroke is too strong or unresponsive")
    check(shipping["firstStrokeSpeed"] + 2 < shipping["fourStrokeSpeed"] < 9,
          "Shipping repeated strokes did not build speed gradually")
    check(shipping["maxHorizontalError"] < .025 and shipping["maxVerticalError"] < .035 and shipping["falls"] == 0,
          "Shipping camera lost the ball or the fixture fell")
    check(shipping["headingChangeAfterRightSwipe"] > 5, "Shipping view did not respond to a rightward stroke")
    animation = load_measurement("reward-animation-allocation-v8.json")
    check(animation["managedBytes"] == 0, "Isolated reward animation allocation regression")

    campaign = list(csv.DictReader((results / "production-traversal.csv").open(encoding="utf-8-sig")))
    expeditions = list(csv.DictReader((results / "expedition-traversal.csv").open(encoding="utf-8-sig")))
    for rows, expected in ((campaign, 55), (expeditions, 15)):
        check(len(rows) == expected and all(row["complete"].lower() == "true" and int(row["falls"]) == 0 for row in rows),
              "A retained route did not finish without falls")
    for name in ("production-traversal", "expedition-traversal"):
        shutil.copy2(results / (name + ".csv"), results / (name + "-v8.csv"))
    builds = [float(row["editorBuildMs"]) for row in campaign]
    memory = [float(row["editorAllocatedMiB"]) for row in campaign]
    summaries = {
        "campaign-validation-summary-v8.json": {
            "revision": "0.8.0", "source": "production-traversal-v8.csv", "routes": len(campaign),
            "completed": len(campaign), "falls": 0, "physicalDevice": False,
            "editorBuildMilliseconds": {"minimum": min(builds), "median": statistics.median(builds), "maximum": max(builds)},
            "editorUnityAllocatedMiB": {"minimum": min(memory), "maximum": max(memory)}},
        "expedition-validation-summary-v8.json": {
            "revision": "0.8.0", "source": "expedition-traversal-v8.csv", "routes": len(expeditions),
            "completed": len(expeditions), "falls": 0, "physicalDevice": False,
            "wallContacts": sum(int(row["wallContacts"]) for row in expeditions),
            "ceilingContacts": sum(int(row["ceilingContacts"]) for row in expeditions),
            "hiddenGemAndTokenCollected": any(int(row["gems"]) > 0 and int(row["tokens"]) > 0 for row in expeditions)}
    }
    for name, data in summaries.items():
        (results / name).write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def verify_new_packaged_code():
    with zipfile.ZipFile(ROOT / "BallQuest3D-test.apk") as package:
        metadata = package.read("assets/bin/Data/Managed/Metadata/global-metadata.dat")
        for symbol in ("maximumStrokeDrive", "ballViewportHeight"):
            check(symbol.encode() in metadata, "Missing v0.8 runtime behavior symbol: " + symbol)
        for symbol in ("ProgressiveSwipeV8Tests", "CenteredCameraV8Tests", "ShippingControlsV8Tests"):
            check(symbol.encode() not in metadata, "Test fixture was packaged: " + symbol)


if __name__ == "__main__":
    verify_measurements()
    verify_new_packaged_code()
    main(version="0.8.0", version_code=8,
         artifact="BallQuest3D-Centered-v0.8.0.apk", reports=REPORTS,
         result_filename="centered-release.json", expected_tests=EXPECTED_TESTS,
         development=False)
