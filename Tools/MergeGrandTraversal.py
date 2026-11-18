"""Retain only still-current baseline geometry and supersede all changed late courses."""
from pathlib import Path
import csv
root=Path(__file__).resolve().parents[1]/'TestResults'
with (root/'grand-traversal-baseline-v11.csv').open(encoding='utf-8-sig') as f:
    reader=csv.DictReader(f);fields=reader.fieldnames;early=[r for r in reader if int(r['id'])<=500]
with (root/'grand-late-v11.csv').open(encoding='utf-8-sig') as f:late=list(csv.DictReader(f))
assert len(early)==38 and len(late)==11
assert all(r['complete'].lower()=='true' for r in early+late)
rows=sorted(early+late,key=lambda r:int(r['id']));assert len({r['id'] for r in rows})==49
with (root/'grand-traversal-v11.csv').open('w',encoding='utf-8',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
print('49 current-geometry courses: 38 unchanged early courses plus 11 regenerated late gauntlets.')
