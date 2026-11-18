"""Download the exact Android dependencies advertised by Unity's release API."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import sys

from fetch_unity_toolchain import download

ROOT = Path(__file__).resolve().parents[1]
release = json.loads((ROOT / '.tooling/unity-release.json').read_text())
editor = next(d for d in release['results'][0]['downloads'] if d['platform'] == 'WINDOWS' and d['architecture'] == 'X86_64')
android = next(module for module in editor['modules'] if module['id'] == 'android')
modules = []


def visit(module):
    modules.append(module)
    for child in module.get('subModules', []):
        visit(child)


visit(android)
for module in modules:
    module['localArchive'] = module['url'].split('/')[-1]
(ROOT / '.tooling/android-modules.json').write_text(json.dumps(modules, indent=2))
with ThreadPoolExecutor(max_workers=5) as pool:
    list(pool.map(download, [(module['localArchive'], module['url']) for module in modules]))
print('All matching Android build dependencies are downloaded.', flush=True)
