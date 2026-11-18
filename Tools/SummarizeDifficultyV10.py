"""Read-only authored road exposure comparison with the archived v9 preflight."""
import re,json
from VerifyManualRelease import ROOT,read
legacy={x['id']:x for x in json.loads(read(ROOT/'TestResults/crazy-campaign-preflight-v9.json'))}
rows=[];kinds={20:0,21:0,22:0}
for path in sorted((ROOT/'Assets/_BallQuest/ScriptableObjects/Levels').glob('Level*.asset')):
    text=read(path);identity=int(re.search(r'^  id: (\d+)',text,re.M)[1]);block=text.split('  tracks:')[1].split('  obstacles:')[0];main=[]
    for track in block.split('  - position:')[1:]:
        if re.search(r'alternate: 1',track):continue
        main.append(dict(width=float(re.search(r'size: \{x: ([^,]+)',track)[1]),rails=bool(int(re.search(r'rails: (\d+)',track)[1]))))
    for kind in re.findall(r'kind: (\d+)',text.split('  obstacles:')[1].split('  pickups:')[0]):
        if int(kind) in kinds:kinds[int(kind)]+=1
    rows.append(dict(id=identity,oldUnrailedMainSegments=legacy[identity]['unrailedSegments'],newUnrailedMainSegments=sum(not t['rails'] for t in main),oldMinimumWidth=legacy[identity]['minimumWidth'],newMinimumWidth=min(t['width'] for t in main)))
report={'scope':'Serialized main-road geometry compared with archived v9 recipe preflight; exposure/width changes, not measured human difficulty','coursesWithMoreExposedMainSegments':sum(x['newUnrailedMainSegments']>x['oldUnrailedMainSegments'] for x in rows),'oldUnrailedMainSegments':sum(x['oldUnrailedMainSegments'] for x in rows),'newUnrailedMainSegments':sum(x['newUnrailedMainSegments'] for x in rows),'newPistons':kinds[20],'newSweepArms':kinds[21],'newRollerDrums':kinds[22],'courses':rows}
(ROOT/'TestResults/challenge-difficulty-v10.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='courses'},indent=2))
