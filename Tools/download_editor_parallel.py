"""Resume the official Unity download with HTTP ranges; no installer is executed here."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import shutil
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
downloads = ROOT / '.tooling/downloads'
manifest = json.loads((ROOT / '.tooling/unity-release.json').read_text())
url = next(d['url'] for d in manifest['results'][0]['downloads'] if d['platform'] == 'WINDOWS' and d['architecture'] == 'X86_64')
target = downloads / 'UnitySetup64-6000.0.65f1.exe'
partial = target.with_name(target.name + '.partial')
with urllib.request.urlopen(urllib.request.Request(url, headers={'Range': 'bytes=0-0'}), timeout=30) as response:
    if response.status != 206:
        raise RuntimeError('Server does not support range downloads')
    total = int(response.headers['Content-Range'].split('/')[-1])
if target.exists() and target.stat().st_size == total:
    print('Editor download already complete', flush=True)
    raise SystemExit(0)
base = partial.stat().st_size if partial.exists() else 0
remaining = total - base
count = 6
partsize = (remaining + count - 1) // count


def fetch(index):
    start = base + index * partsize
    end = min(total - 1, start + partsize - 1)
    part = downloads / f'editor-range-{index}.part'
    expected = end - start + 1
    current = part.stat().st_size if part.exists() else 0
    if current == expected:
        return part
    if current > expected:
        raise RuntimeError('Range plan changed; retain previous download before restarting')
    request = urllib.request.Request(url, headers={'Range': f'bytes={start + current}-{end}'})
    with urllib.request.urlopen(request, timeout=90) as response:
        if response.status != 206 or not response.headers['Content-Range'].startswith(f'bytes {start + current}-'):
            raise RuntimeError('Unexpected range response')
        reported = time.monotonic()
        with part.open('ab') as output:
            while True:
                data = response.read(4 * 1024**2)
                if not data:
                    break
                output.write(data)
                current += len(data)
                if time.monotonic() - reported > 15:
                    print(f'RANGE {index + 1}: {current / 1024**2:.0f}/{expected / 1024**2:.0f} MiB', flush=True)
                    reported = time.monotonic()
    if current != expected:
        raise RuntimeError('Incomplete range')
    print(f'Range {index + 1} ready', flush=True)
    return part


print(f'Resuming {base / 1024**2:.0f}/{total / 1024**2:.0f} MiB with {count} connections', flush=True)
with ThreadPoolExecutor(max_workers=count) as executor:
    parts = list(executor.map(fetch, range(count)))
assembled = target.with_name(target.name + '.assembling')
with assembled.open('wb') as output:
    if partial.exists():
        with partial.open('rb') as source:
            shutil.copyfileobj(source, output, 8 * 1024**2)
    for part in parts:
        with part.open('rb') as source:
            shutil.copyfileobj(source, output, 8 * 1024**2)
if assembled.stat().st_size != total:
    raise RuntimeError('Assembled download has the wrong size')
assembled.replace(target)
print('READY ' + str(target), flush=True)
