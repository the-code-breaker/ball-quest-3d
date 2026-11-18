"""Fetch official build dependencies into the project-local, ignored .tooling folder."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
DIRECTORY = ROOT / '.tooling/downloads'
DIRECTORY.mkdir(parents=True, exist_ok=True)


def download(item):
    name, url = item
    path = DIRECTORY / name
    if path.exists():
        print('PRESENT ' + name, flush=True)
        return
    partial = path.with_name(path.name + '.partial')
    start = partial.stat().st_size if partial.exists() else 0
    request = urllib.request.Request(url, headers={'Range': f'bytes={start}-'} if start else {})
    with urllib.request.urlopen(request, timeout=90) as response:
        if start and response.status != 206:
            start = 0
        total = start + int(response.headers.get('Content-Length', 0))
        print(f'DOWNLOAD {name}: {total / 1024**2:.1f} MiB', flush=True)
        written = start
        reported = time.monotonic()
        with partial.open('ab' if start else 'wb') as output:
            while True:
                chunk = response.read(4 * 1024**2)
                if not chunk:
                    break
                output.write(chunk)
                written += len(chunk)
                if time.monotonic() - reported > 10:
                    print(f'PROGRESS {name}: {written / 1024**2:.1f}/{total / 1024**2:.1f} MiB', flush=True)
                    reported = time.monotonic()
        if total and written != total:
            raise RuntimeError('Incomplete download: ' + name)
    partial.replace(path)
    print('READY ' + name, flush=True)


if __name__ == '__main__':
    manifest = json.loads((ROOT / '.tooling/unity-release.json').read_text(encoding='utf-8'))
    editor = next(d for d in manifest['results'][0]['downloads'] if d['platform'] == 'WINDOWS' and d['architecture'] == 'X86_64')
    items = [('UnitySetup64-6000.0.65f1.exe', editor['url']),
             ('7zr.exe', 'https://www.7-zip.org/a/7zr.exe'),
             ('7zip-x64.msi', 'https://www.7-zip.org/a/7z2501-x64.msi')]
    with ThreadPoolExecutor(max_workers=3) as executor:
        list(executor.map(download, items))
