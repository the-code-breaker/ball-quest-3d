"""Compare normalized route shapes within each theme, independently of route titles/colors."""
from pathlib import Path
import bisect,json,math,re
root=Path(__file__).resolve().parents[1]
groups={};summaries=[]
for path in (root/'Assets/_BallQuest/ScriptableObjects/Levels').glob('Level*.asset'):
    text=path.read_text(encoding='utf-8');id=int(re.search(r'^  id: (\d+)',text,re.M)[1]);env=int(re.search(r'^  environment: (\d+)',text,re.M)[1])
    section=text.split('  routePoints:',1)[1].split('  routeWidths:',1)[0]
    points=[tuple(float(x) for x in v) for v in re.findall(r'\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}',section)]
    distances=[0]
    for a,b in zip(points,points[1:]):distances.append(distances[-1]+math.dist(a,b))
    scale=max(max(p[k] for p in points)-min(p[k] for p in points) for k in range(3));descriptor=[]
    for i in range(40):
        d=distances[-1]*i/39;j=min(len(points)-2,max(0,bisect.bisect_right(distances,d)-1));t=(d-distances[j])/max(.00001,distances[j+1]-distances[j])
        descriptor.append(tuple((points[j][k]+(points[j+1][k]-points[j][k])*t-points[0][k])/scale for k in range(3)))
    groups.setdefault(env,[]).append((id,descriptor))
for env,courses in sorted(groups.items()):
    closest=(float('inf'),0,0)
    for i,(id,a) in enumerate(courses):
        for other,b in courses[i+1:]:
            distance=math.sqrt(sum(sum((u-v)**2 for u,v in zip(p,q)) for p,q in zip(a,b))/len(a))
            closest=min(closest,(distance,id,other))
    summaries.append(dict(environment=env,courses=len(courses),closestNormalizedRms=closest[0],closestIds=list(closest[1:])))
report=dict(scope='40 equal-arc-length samples normalized by translation and maximum bounding dimension, compared within each environment. Distances are geometric evidence, not a human perception threshold.',themes=summaries)
(root/'TestResults/grand-diversity-v11.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Within-theme closest normalized RMS:',min(s['closestNormalizedRms'] for s in summaries))
