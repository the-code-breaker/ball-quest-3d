"""Verify the actual thousand-course APK and current, measured v11 evidence."""
import csv,json,re,xml.etree.ElementTree as ET,zipfile
from VerifyManualRelease import ROOT,read,check,main
from VerifyMaterialsRelease import verify_package

REPORTS=['TestResults/GrandEditMode-v11.xml','TestResults/GrandAcceptance-v11.xml',
         'TestResults/GrandPlayMode-v11.xml','TestResults/GrandFinal-v11.xml','TestResults/GrandVisualFinal-v11.xml']

def verify():
    cases={}
    for report in REPORTS:
        tree=ET.parse(ROOT/report).getroot();check(int(tree.get('total','0'))>0,'Empty report: '+report)
        cases.update({c.get('fullname'):c.get('result') for c in tree.iter('test-case')})
    check(len(cases)>=141 and set(cases.values())=={'Passed'},'Current distinct Unity results must all pass')
    nav=json.loads(read(ROOT/'TestResults/challenge-navigation-v11.json'))
    check(nav['ids']==list(range(1,1001)) and nav['chapterCount']==20 and nav['allCoursesResetToFirstPage'],'Navigation skipped or repeated a course')
    structure=json.loads(read(ROOT/'TestResults/grand-structure-v11.json'))
    check(structure['courses']==1000 and structure['uniqueNormalizedRoutes']==1000 and structure['biomes']==20 and structure['topologies']==25,'Catalog structure differs from requested scope')
    with (ROOT/'TestResults/grand-traversal-v11.csv').open(encoding='utf-8-sig') as f:rows=list(csv.DictReader(f))
    check(len(rows)==49 and all(r['complete'].lower()=='true' for r in rows),'Incomplete current-geometry physical sample')
    catalog=list(csv.DictReader((ROOT/'TestResults/grand-campaign-catalog-v11.csv').open(encoding='utf-8-sig')))
    sampled={r['id'] for r in rows};selected=[r for r in catalog if r['id'] in sampled]
    check(len({r['archetype'] for r in selected})==25 and len({r['environment'] for r in selected})==20,'Physical sample misses a recipe/environment')
    check({(int(r['id'])//13)%8 for r in rows if int(r['id'])>500}==set(range(8)),'Late physical sample misses an additional act type')
    for id in range(501,1001):
        asset=read(ROOT/f'Assets/_BallQuest/ScriptableObjects/Levels/Level{id:04d}.asset')
        checkpoints=asset.split('  checkpoints:',1)[1].split('  tracks:',1)[0]
        check(len(re.findall(r'\{x:',checkpoints))>=2,f'Late course {id} lacks its additional checkpointed act')
    verify_package()
    signer='a18f9841149654ef939ca3bf9530fbe8590c427ce801b98955234cca353db8db'
    check(signer in read(ROOT/'TestResults/apk-signature.txt').lower(),'Signing certificate changed from the previous testing APK')
    with zipfile.ZipFile(ROOT/'BallQuest3D-test.apk') as apk:
        metadata=apk.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
        for name in ('GrandWorldPresentation','CampaignGeometry','ChallengeRules','challengeCompleted','PlayExpedition'):
            check(name.encode() in metadata,'Missing compiled campaign implementation: '+name)
        for name in ('GrandCampaignV11Tests','GrandCampaignGenerator','GrandArtImporter'):
            check(name.encode() not in metadata,'Editor/test code packaged: '+name)
        payload=b''.join(apk.read(n) for n in apk.namelist() if n.startswith('assets/bin/Data/') and '/Managed/' not in n)
        check(b'WorldAtlasV11' in payload,'Generated illustration atlas missing from packaged resources')
    main(version='0.11.0',version_code=11,artifact='BallQuest3D-ThousandWorlds-v0.11.0.apk',reports=REPORTS,
         result_filename='grand-release-v11.json',expected_tests=len(cases),development=False,expected_adventures=1005,
         route_reports=[('grand-traversal-v11.csv',49),('grand-pilot-v11.csv',6),('expedition-traversal.csv',15)])
if __name__=='__main__':verify()
