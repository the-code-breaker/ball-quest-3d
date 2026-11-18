"""Snapshot/compare physical campaign data independently of later art-only refinements.

This checks serialized geometry/physics data and unchanged core controller source. It
does not replace scene simulation or prove that arbitrary LevelBuilder edits are safe.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
FIELDS=('id','spawn','finish','checkpoints','tracks','obstacles','killHeight','allowJump',
        'dropStarts','dropPoints','dropDirections','routePoints','routeWidths','alternateFinishes')


def sha(value):return hashlib.sha256(value).hexdigest().upper()


def current():
    courses={}
    for path in sorted((ROOT/'Assets/_BallQuest/ScriptableObjects/Levels').glob('Level*.asset')):
        text=path.read_text(encoding='utf-8-sig')
        identity=re.search(r'^  id: (\d+)$',text,re.M)
        if not identity or not 1<=int(identity.group(1))<=55:continue
        payload={}
        for field in FIELDS:
            match=re.search(r'^  '+field+r':[^\n]*(?:\n(?!  [A-Za-z_][A-Za-z_0-9]*:)[^\n]*)*',text,re.M)
            payload[field]=match.group(0) if match else ''
        courses[identity.group(1)]=sha(json.dumps(payload,sort_keys=True).encode())
    if len(courses)!=55:raise RuntimeError('Expected exactly55 campaign definitions')
    core={}
    for relative in ('Scripts/Player/BallController.cs','Scripts/Player/BallGroundDetector.cs',
                     'Scripts/Player/SurfaceContactResponse.cs','Scripts/Player/BallProfile.cs'):
        path=ROOT/'Assets/_BallQuest'/relative
        core[relative]=sha(path.read_bytes())
    materials={str(p.relative_to(ROOT)):sha(p.read_bytes()) for p in (ROOT/'Assets/_BallQuest/Resources/TrackPhysics').glob('*.physicMaterial')}
    return {'scope':'Serialized campaign geometry, physics material files, and core controller source; renderer/audio tags and LevelBuilder implementation require separate review',
            'courses':courses,'corePhysicsSource':core,'physicsMaterials':materials}


if __name__=='__main__':
    parser=argparse.ArgumentParser();group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--write');group.add_argument('--verify');args=parser.parse_args();snapshot=current()
    if args.write:
        path=ROOT/args.write
        if path.exists():
            if json.loads(path.read_text(encoding='utf-8-sig'))!=snapshot:raise RuntimeError('Refusing to replace a different existing physics snapshot')
        else:path.write_text(json.dumps(snapshot,indent=2)+'\n',encoding='utf-8')
        print('Saved55-course physical-data snapshot: '+str(path.relative_to(ROOT)))
    else:
        baseline=json.loads((ROOT/args.verify).read_text(encoding='utf-8-sig'))
        if baseline!=snapshot:raise RuntimeError('Campaign geometry/core physics differ from the baseline; review and simulate the changed data')
        print('All55 serialized campaign geometry signatures, core physics source and track physics materials match the validated baseline.')
