"""Verify the seven requested fixes and the actual 0.10.0 APK artifact."""
import csv,json,re,shutil,xml.etree.ElementTree as ET,zipfile
from VerifyManualRelease import ROOT,read,check,main
REPORTS=['TestResults/EditMode-v10.xml','TestResults/CampaignPilot-v10.xml',
         'TestResults/CampaignTraversal-v10.xml','TestResults/PlayMode-v10-regressions.xml',
         'TestResults/FinalVisuals-v10.xml']
EXPECTED=137
MATERIALS=('Metal','Wood','Glass','Rubber','Stone','Energy')
SURFACES=('Wood','Metal','Stone','Ice','Sand','Glass','Grass','Boost')
def measured(name):return json.loads(read(ROOT/'TestResults'/name))
def verify_evidence():
    cases={}
    for report in REPORTS:
        tree=ET.parse(ROOT/report).getroot();check(int(tree.get('total','0'))>0,'Empty current report: '+report)
        if tree.get('result')!='Passed':
            failing=[c.get('fullname') for c in tree.iter('test-case') if c.get('result')!='Passed']
            check(report.endswith('PlayMode-v10-regressions.xml') and int(tree.get('total'))==98 and failing==['BallQuest.Tests.UpgradeAcceptanceV10Tests.NewBallModelsUseTheirOwnRecordedContactTimbresOnTheSameFloor'], 'Unexpected failed/incomplete run: '+report)
        cases.update({c.get('fullname'):c.get('result') for c in tree.iter('test-case')})
    check(len(cases)==EXPECTED and set(cases.values())=={'Passed'},'Expected 137 distinct passing current cases')
    nav=measured('challenge-navigation-v10.json')
    check(nav['ids']==list(range(1,56)) and nav['chapterCount']==4 and nav['allCoursesResetToFirstPage'],'Actual chapter button order/pagination failed')
    diversity=measured('campaign-diversity-v10.json')
    check(diversity['courses']==55 and diversity['families']==11 and diversity['closestNormalizedShapeDistance']>.08,'Repeated/missing campaign layouts')
    check(diversity['expertCourses']>=50 and diversity['minimumRoadWidth']>=1.6,'Expected harder precision campaign')
    check(measured('material-models-v10.json')['differentBodyImpactsOnSameFloor'],'Materials still share the same floor impact timbre')
    hold=measured('swipe-only-v10.json');gesture=measured('ball-only-gestures-v10.json')
    check(hold['stationaryMaximumSpeed']==0 and hold['parkedMaximumDrive']==0,'Held touch accelerates the ball')
    check(gesture['maximumCameraCommand']==0 and gesture['secondaryFingerDidNotOrbit'],'Finger gestures still control the camera')
    for name,count in (('production-traversal',55),('production-pilot-v10',6),('expedition-traversal',15)):
        path=ROOT/'TestResults'/(name+'.csv');rows=list(csv.DictReader(path.open(encoding='utf-8-sig')))
        check(len(rows)==count and all(r['complete'].lower()=='true' and int(r['falls'])==0 for r in rows),'Missing/failed physical routes: '+name)
        if not name.endswith('-v10'):shutil.copy2(path,path.with_name(name+'-v10.csv'))
    audio=measured('natural-contact-v10.json')
    check(audio['license']=='CC0-1.0' and len(audio['sources'])==5 and len(audio['clips'])==66,'Missing licensed contact audio evidence')
    for clip in audio['clips']:
        check(.005<clip['rms']<.15 and clip['peak']<.9,'Invalid recorded clip dynamics: '+clip['name'])
    seams=measured('natural-loop-seams-v10.json')['loops']
    check(len(seams)==6 and all(s['seamJump']<.02 and s['seamJump']<max(.005,s['localStepRms']*1.5) for s in seams),'Recorded rolling loop boundaries contain discontinuities')
    return cases
def verify_package():
    samples=ROOT/'Assets/_BallQuest/Audio/SFX/NaturalContactV10'
    by_guid={re.search(r'^guid: (\w+)',read(p),re.M).group(1):p.name.removesuffix('.wav.meta') for p in samples.glob('*.wav.meta')}
    check(len(by_guid)==66,'Expected 66 source clips')
    used=set()
    for material in MATERIALS:
        profile=read(ROOT/'Assets/_BallQuest/Resources/BallAudio'/(material+'.asset'))
        check('audioRevision: 10' in profile and 'Natural contact v10' in profile,'Stale contact profile: '+material)
        names={by_guid[g] for g in re.findall(r'fileID: 8300000, guid: (\w+)',profile)}
        expected={f'NaturalV10_{material}_{kind}' for kind in ('Rolling','Sliding','Impact')}|{f'NaturalV10_{material}_On_{s}' for s in SURFACES}
        check(names==expected,'Ball/surface timbre mapping incomplete: '+material);used.update(names)
    with zipfile.ZipFile(ROOT/'BallQuest3D-test.apk') as apk:
        metadata=apk.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
        for symbol in ('CampaignChapters','SculptedObstaclePresentation','BallContactAudio','BallContactFeedback','Piston','SweepArm','RollerDrum'):
            check(symbol.encode() in metadata,'Missing compiled implementation: '+symbol)
        for name in ('UpgradeAcceptanceV10Tests','MaterialAndChaptersV10Tests','AdventureValidationDriver','NaturalContactAudioGenerator'):
            check(name.encode() not in metadata,'Test/Editor code was packaged: '+name)
        payload=b''.join(apk.read(n) for n in apk.namelist() if n.startswith('assets/bin/Data/') and '/Managed/' not in n)
        for name in used|{'brushed-steel','carbon-rubber','copper-core','walnut','CeramicV9','AlloyV9','BasaltV9'}:
            check(name.encode() in payload,'Missing packaged source asset: '+name)
        check(b'Natural contact v10' in payload,'Missing source provenance')
        check(b'FoleyV9_' not in payload,'Old noisy contact clips still shipped')
if __name__=='__main__':
    verify_evidence();verify_package()
    main(version='0.10.0',version_code=10,artifact='BallQuest3D-Materials-v0.10.0.apk',reports=REPORTS,result_filename='materials-release.json',expected_tests=EXPECTED,development=False)
