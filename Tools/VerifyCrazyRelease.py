"""Verify the v0.9 authored-campaign, contact Foley and ball-only-input APK."""
import csv
import json
import re
import shutil
import xml.etree.ElementTree as ET
import zipfile
from VerifyManualRelease import ROOT, check, main, read

REPORTS=["TestResults/EditMode-v9.xml","TestResults/CampaignPilot-v9.xml",
         "TestResults/CampaignTraversal-v9.xml","TestResults/PlayMode-v9-regressions.xml",
         "TestResults/FinalTactile-v9.xml"]
EXPECTED_TESTS=132
BALL_MATERIALS=("Metal","Wood","Glass","Rubber","Stone","Energy")
CONTACT_SURFACES=("Wood","Metal","Stone","Ice","Sand","Glass","Grass","Boost")
SURFACE_ROLLING=("Ice","Sand","Glass","Grass")
FOLEY_PROVENANCE="Original procedural contact Foley v9"


def measured(name):
    return json.loads(read(ROOT/"TestResults"/name))


def referenced_foley_clips():
    """Resolve serialized runtime profile GUIDs, rather than counting source WAVs."""
    samples=ROOT/"Assets/_BallQuest/Audio/SFX/ContactFoleyV9"
    profiles=ROOT/"Assets/_BallQuest/Resources/BallAudio"
    by_guid={}
    for meta in samples.glob("*.wav.meta"):
        guid=re.search(r"^guid: ([0-9a-f]{32})$",read(meta),re.MULTILINE)
        check(guid is not None,"Missing Foley source GUID: "+meta.name)
        by_guid[guid.group(1)]=meta.name.removesuffix(".wav.meta")
    check(len(by_guid)==34,"Expected 34 original source Foley WAVs")
    check({path.stem for path in profiles.glob("*.asset")}==set(BALL_MATERIALS),"Unexpected runtime audio profiles")
    referenced=set()
    for material in BALL_MATERIALS:
        profile=read(profiles/(material+".asset"))
        check(re.search(r"^  audioRevision: 9$",profile,re.MULTILINE),"Stale audio profile: "+material)
        check(FOLEY_PROVENANCE in profile,"Missing original Foley provenance: "+material)
        guids=re.findall(r"fileID: 8300000, guid: ([0-9a-f]{32})",profile)
        check(all(guid in by_guid for guid in guids),"Profile references an old or external clip: "+material)
        names={by_guid[guid] for guid in guids}
        expected={f"FoleyV9_{material}_{kind}" for kind in ("Rolling","Sliding","Impact")}
        expected.update(f"FoleyV9_Surface_{surface}_Impact" for surface in CONTACT_SURFACES)
        expected.update(f"FoleyV9_Surface_{surface}_Rolling" for surface in SURFACE_ROLLING)
        check(names==expected,"Incomplete material/surface Foley references: "+material)
        referenced.update(names)
    check(len(referenced)==30,"Expected 30 unique referenced Foley clips")
    return referenced


def verify_current_evidence():
    latest={}
    for path in REPORTS:
        tree=ET.parse(ROOT/path).getroot()
        check(tree.get("result")=="Passed","Current report has not passed: "+path)
        for case in tree.iter("test-case"):
            latest[case.get("fullname")]=case
    check(len(latest)==EXPECTED_TESTS,"Unexpected distinct current Unity case count")
    check(all(case.get("result")=="Passed" for case in latest.values()),"A current case is not passing")
    classes={case.get("classname") for case in latest.values()}
    for name in ("CrazyCampaignV9Tests","ContactFoleyV9Tests","TactileContactV9Tests","BoosterArtV9Tests","BallOnlyGesturesV9Tests","CrazyCampaignNativeViewsV9Tests"):
        check("BallQuest.Tests."+name in classes,"Missing v9 acceptance fixture: "+name)
    diversity=measured("campaign-diversity-v9.json")
    check(diversity["courses"]==55 and diversity["families"]==11 and diversity["distinctTitles"]==55,"Incomplete authored campaign")
    check(diversity["closestNormalizedShapeDistance"]>=.08,"Repeated or scaled route shape")
    check(diversity["railFreePrecisionCourses"]>=50 and diversity["expertCourses"]>=40,"Missing hard/precision content")
    check(1.6<=diversity["minimumRoadWidth"]<=2,"Precision road width differs from the tested design")
    gesture=measured("ball-only-gestures-v9.json")
    check(gesture["maximumCameraCommand"]==0 and gesture["secondaryFingerDidNotOrbit"],"Touch still controls the camera")
    check(gesture["maximumSideSpeed"]>.25 and gesture["maximumForwardSpeed"]>.25 and gesture["viewportError"]<.02,"Circular ball gestures or camera framing failed")
    hold=measured("swipe-only-v9.json")
    check(hold["stationaryMaximumSpeed"]==0 and hold["jitterMaximumSpeed"]==0 and hold["parkedMaximumDrive"]==0,"Held touch added drive")
    spectra=measured("contact-foley-spectrum-v9.json")["signals"]
    check(len(spectra)==6 and all(s["peakToneEnergyFraction"]<.025 and s["peakAutocorrelation"]<.15 for s in spectra),"Rolling audio contains a sustained periodic fundamental")
    contact=measured("tactile-contact-v9.json")
    check(contact["actualHitSurface"]=="Metal" and contact["mutedCollisionAnimated"] and contact["visualOwnerDisposed"],"Contact identity/animation lifecycle failed")
    check(contact["effectCapacity"]==6 and contact["maximumActiveRings"]<=6,"Unbounded contact effects")
    check(contact["fastRollingPitch"]/contact["slowRollingPitch"]<1.15,"Speed still creates a large motor-like pitch sweep")
    booster=measured("booster-presentation-v9.json")
    check(booster["renderersPerPad"]==3 and booster["actualBoostTriggered"] and booster["speed"]>3,"Booster rendering/activation failed")
    for name,count in (("production-traversal",55),("expedition-traversal",15),("production-pilot-v9",6)):
        path=ROOT/"TestResults"/(name+".csv")
        rows=list(csv.DictReader(path.open(encoding="utf-8-sig")))
        check(len(rows)==count and all(r["complete"].lower()=="true" and int(r["falls"])==0 for r in rows),"Incomplete physical route evidence: "+name)
        if not name.endswith("-v9"):
            shutil.copy2(path,path.with_name(name+"-v9.csv"))


def verify_v9_package():
    clips=referenced_foley_clips()
    with zipfile.ZipFile(ROOT/"BallQuest3D-test.apk") as package:
        metadata=package.read("assets/bin/Data/Managed/Metadata/global-metadata.dat")
        for name in ("CrazyCoursePresentation","BallContactFeedback","CourseRouteGraphic","audioRevision"):
            check(name.encode() in metadata,"Missing v9 runtime symbol: "+name)
        for name in ("BallOnlyGesturesV9Tests","TactileContactV9Tests","CrazyCampaignV9Tests","BoosterArtV9Tests","CampaignCaptureOnProgress"):
            check(name.encode() not in metadata,"Test-only code was packaged: "+name)
        payload=b"".join(package.read(name) for name in package.namelist() if name.startswith("assets/bin/Data/") and "/Managed/" not in name)
        for clip in sorted(clips):
            check(clip.encode() in payload,"Missing packaged referenced v9 Foley clip: "+clip)
        check(FOLEY_PROVENANCE.encode() in payload,"Missing packaged original contact Foley provenance")
        for texture in ("CeramicV9","AlloyV9","BasaltV9"):
            check(texture.encode() in payload,"Missing family deck texture: "+texture)


if __name__=="__main__":
    verify_current_evidence()
    verify_v9_package()
    main(version="0.9.0",version_code=9,artifact="BallQuest3D-CrazyTracks-v0.9.0.apk",
         reports=REPORTS,result_filename="crazy-tracks-release.json",expected_tests=EXPECTED_TESTS,development=False)
