"""Offline structural checks. This is NOT a physics simulation or a claim of level solvability."""
from pathlib import Path
import json
import math
import re
import struct

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
checks = 0


def check(condition, message):
    global checks
    if not condition:
        raise AssertionError(message)
    checks += 1
    print("PASS", message)


metas = list(ASSETS.rglob("*.meta"))
by_guid = {}
for path in metas:
    match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8"), re.M)
    if match:
        check(match[1] not in by_guid, "unique GUID: " + str(path.relative_to(ROOT)))
        by_guid[match[1]] = path
package_guids = set()
cache = ROOT / 'Library/PackageCache'
if cache.exists():
    for path in cache.rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(encoding='utf-8'), re.M)
        if match:
            package_guids.add(match[1])
for path in ASSETS.rglob("*"):
    if not path.is_file() or path.suffix == ".meta": continue
    check(Path(str(path) + ".meta").exists(), "metadata: " + str(path.relative_to(ROOT)))
    if path.suffix not in (".asset", ".unity", ".prefab"): continue
    for item in re.findall(r"guid: ([0-9a-f]{32})", path.read_text(encoding="utf-8")):
        check(item in by_guid or item in package_guids or item.startswith('0000000000000000'),
              "resolved serialized reference: " + str(path.relative_to(ROOT)))

for path in list(ASSETS.rglob("*.asmdef")) + [ROOT / "Packages/manifest.json"]:
    json.loads(path.read_text(encoding="utf-8"))
    check(True, "valid JSON: " + str(path.relative_to(ROOT)))

def vector(content):
    return {key.strip():float(value) for key,value in (part.split(':') for part in content.split(','))}

def section(content,start,end):
    return content.split('  '+start+':',1)[1].split('  '+end+':',1)[0]

levels=[]
for path in sorted((ASSETS/'_BallQuest/ScriptableObjects/Levels').glob('Level*.asset')):
    content=path.read_text(encoding='utf-8')
    level={key:int(re.search(r'^  '+key+r': (\d+)',content,re.M)[1]) for key in ('id','world')}
    level.update({key:vector(re.search(r'^  '+key+r': \{([^}]+)\}',content,re.M)[1]) for key in ('spawn','finish')})
    level['tracks']=[]
    for match in re.finditer(r'- position: \{([^}]+)\}\s+size: \{([^}]+)\}\s+rotation: \{([^}]+)\}\s+style: (\d+)\s+rails: (\d+)',section(content,'tracks','obstacles')):
        level['tracks'].append(dict(position=vector(match[1]),size=vector(match[2]),rotation=vector(match[3]),style=int(match[4])))
    level['obstacles']=[]
    for match in re.finditer(r'- kind: (\d+)\s+position: \{([^}]+)\}\s+size: \{([^}]+)\}',section(content,'obstacles','pickups')):
        level['obstacles'].append(dict(kind=int(match[1]),position=vector(match[2]),size=vector(match[3])))
    level['pickups']=re.findall(r'- kind: (\d+)',section(content,'pickups','parSeconds'))
    level['checkpoints']=[vector(value) for value in re.findall(r'\{([^}]+)\}',section(content,'checkpoints','tracks'))]
    level['dropCount']=len(re.findall(r'\{([^}]+)\}',section(content,'dropPoints','dropDirections')))
    level['routeCount']=len(re.findall(r'\{([^}]+)\}',section(content,'routePoints','routeWidths')))
    if '  streamedCampaign: 1' in content:
        points=[vector(v) for v in re.findall(r'\{([^}]+)\}',section(content,'routePoints','routeWidths'))]
        widths=[float(v) for v in re.findall(r'^  - ([\d.]+)',section(content,'routeWidths','difficulty'),re.M)]
        packed=bytes.fromhex(re.search(r'^  roadFlags: (\w+)',content,re.M)[1]);flags=struct.unpack('<'+'i'*(len(packed)//4),packed)
        check(len(points)==len(widths)==len(flags)+1,f"level {level['id']} compact arrays agree")
        for i,(a,b) in enumerate(zip(points,points[1:])):
            if flags[i]&256:continue
            dx,dy,dz=(b[k]-a[k] for k in ('x','y','z'));distance=math.sqrt(dx*dx+dy*dy+dz*dz)
            pitch=-math.atan2(dy,math.hypot(dx,dz));yaw=math.atan2(dx,dz)
            up={'x':math.sin(yaw)*math.sin(pitch),'y':math.cos(pitch),'z':math.cos(yaw)*math.sin(pitch)}
            level['tracks'].append(dict(position={k:(a[k]+b[k])/2-up[k]*.4 for k in a},size=dict(x=widths[i],y=.8,z=distance+.08),rotation=dict(x=math.degrees(pitch),y=math.degrees(yaw),z=0),style=(flags[i]>>1)&15))
        level['compact']=True
        level['challenge']=int(re.search(r'^  challenge: (\d+)',content,re.M)[1])
    levels.append(level)
levels.sort(key=lambda level:level['id'])
campaign=dict(levels=levels)
check([level['id'] for level in levels]==list(range(1,1001)),'one thousand contiguous generated adventure course assets')
check(set(level['world'] for level in levels)==set(range(4)),'four playable worlds in the adventure campaign')
race_assets=list((ASSETS/'_BallQuest/ScriptableObjects/Races').glob('Race[0-9][0-9].asset'))
check(len(race_assets)==5,'five dedicated race definitions')


def support(piece, x, z):
    pos, size = piece["position"], piece["size"]
    angle = math.radians(piece.get("rotation", {}).get("x", 0))
    yaw = math.radians(piece.get("rotation", {}).get("y", 0))
    nx,ny,nz=math.sin(yaw)*math.sin(angle),math.cos(angle),math.cos(yaw)*math.sin(angle)
    y=pos['y']+(size['y']/2-nx*(x-pos['x'])-nz*(z-pos['z']))/ny
    localx=(x-pos['x'])*math.cos(yaw)-(z-pos['z'])*math.sin(yaw)
    if abs(localx)>size['x']/2-.6:return None
    localz=(x-pos['x'])*math.sin(yaw)*math.cos(angle)-(y-pos['y'])*math.sin(angle)+(z-pos['z'])*math.cos(yaw)*math.cos(angle)
    if abs(localz) > size["z"] / 2 + .09:
        return None
    return y


for level in campaign["levels"]:
    geometric_challenge=any(t['size']['x']<=2.4 for t in level['tracks'])
    check(bool(level["tracks"]) and (bool(level["obstacles"]) or geometric_challenge or level.get('challenge',0)>0) and bool(level["pickups"]) and len(level["checkpoints"]) >= 1,
          f"level {level['id']} includes track, challenge, pickups and checkpoints")
    check(level['routeCount']>3,f"level {level['id']} has an ordered 3D route")
    pieces = level["tracks"] + [dict(position=o["position"], size=o["size"], rotation={}) for o in level["obstacles"] if o["kind"] in (3, 9)]
    for track in pieces:
        check(min(track["size"].values()) > 0, f"level {level['id']} positive track dimensions")
    for name, point in [("spawn", level["spawn"]), ("finish", level["finish"])] + [("checkpoint", p) for p in level["checkpoints"]]:
        heights = [h for p in pieces if (h := support(p, point["x"], point["z"])) is not None]
        check(any(0.4 <= point["y"] - h <= 1.6 for h in heights), f"level {level['id']} {name} sits above a deck")
    # The full compact-course drop/catch tests run in Unity using quaternion geometry.
    if level.get('compact'):continue
    drops=0
    for index,(previous,current) in enumerate(zip(level['tracks'],level['tracks'][1:])):
        a,b=previous['position'],current['position']
        if a['y']-b['y']>3 and abs(previous['rotation']['x'])<.01 and abs(current['rotation']['x'])<.01:
            drops+=1; height=a['y']-b['y']
            travel=25*math.sqrt(2*height/9.81)
            catch_length=0
            for piece in level['tracks'][index+1:]:
                if abs(piece['position']['y']-b['y'])>.02 or piece['size']['x']<7.9:break
                catch_length+=piece['size']['z']
            check(catch_length>travel+3,f"level {level['id']} drop {drops} has a generous turbo-speed catch platform")
            check(current['size']['x']>=8,f"level {level['id']} drop {drops} has wide landing clearance")
    check(drops==level['dropCount'],f"level {level['id']} drop metadata matches authored transitions")

print(f"{checks} structural checks passed. This script does not simulate physics; Unity traversal results are recorded separately.")
