"""Verify the optimized v0.7 swipe/presentation testing APK against fresh results."""
import csv
import json
import shutil
import statistics
from VerifyManualRelease import ROOT, check, main


def measurements():
    results=ROOT/"TestResults"
    campaign=list(csv.DictReader((results/"production-traversal.csv").open(encoding="utf-8-sig")))
    expeditions=list(csv.DictReader((results/"expedition-traversal.csv").open(encoding="utf-8-sig")))
    for name in ("production-traversal", "expedition-traversal"):
        shutil.copy2(results/(name+".csv"),results/(name+"-v7.csv"))
    builds=[float(row["editorBuildMs"]) for row in campaign]
    memory=[float(row["editorAllocatedMiB"]) for row in campaign]
    summaries={
        "campaign-validation-summary-v7.json":{
            "revision":"0.7.0","source":"production-traversal-v7.csv","routes":len(campaign),
            "completed":sum(row["complete"].lower()=="true" for row in campaign),"falls":sum(int(row["falls"]) for row in campaign),
            "physicalDevice":False,"editorBuildMilliseconds":{"minimum":min(builds),"median":statistics.median(builds),"maximum":max(builds)},
            "editorUnityAllocatedMiB":{"minimum":min(memory),"maximum":max(memory)}},
        "expedition-validation-summary-v7.json":{
            "revision":"0.7.0","source":"expedition-traversal-v7.csv","routes":len(expeditions),
            "completed":sum(row["complete"].lower()=="true" for row in expeditions),"falls":sum(int(row["falls"]) for row in expeditions),
            "physicalDevice":False,"wallContacts":sum(int(row["wallContacts"]) for row in expeditions),
            "ceilingContacts":sum(int(row["ceilingContacts"]) for row in expeditions),
            "hiddenGemAndTokenCollected":any(int(row["gems"])>0 and int(row["tokens"])>0 for row in expeditions)}
    }
    for name,data in summaries.items():
        (results/name).write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
    swipe=json.loads((results/"swipe-only-v7.json").read_text())
    check(swipe["stationaryMaximumSpeed"]==0 and swipe["jitterMaximumSpeed"]==0 and swipe["parkedMaximumDrive"]==0,"Stationary touch added motion")
    check(swipe["singleSampleSameDistanceSpeed"]>swipe["fastSameDistanceSpeed"]>swipe["slowSameDistanceSpeed"],"Swipe speed response is not monotonic")
    animation=json.loads((results/"reward-animation-allocation-v7.json").read_text())
    check(animation["managedBytes"]==0,"Isolated animation allocation regression")

if __name__ == "__main__":
    measurements()
    main(version="0.7.0", version_code=7,
         artifact="BallQuest3D-SwipeUpgrade-v0.7.0.apk",
         reports=["TestResults/EditMode-v7.xml", "TestResults/PlayMode-v7-full.xml"],
         result_filename="swipe-upgrade-release.json", expected_tests=106,
         development=False)
