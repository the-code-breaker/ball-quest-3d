"""Install downloaded Android zip components into the project-local Unity toolchain."""
from pathlib import Path, PurePosixPath
import json
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PLAYER = (ROOT / '.tooling/UnityInstalled/Editor/Data/PlaybackEngines/AndroidPlayer').resolve()
modules = json.loads((ROOT / '.tooling/android-modules.json').read_text())


def destination(module):
    identifier = module['id']
    if identifier.startswith('android-open-jdk'):
        return PLAYER / 'OpenJDK'
    if identifier == 'android-sdk-ndk-tools':
        return PLAYER / 'SDK/tools'
    if identifier.startswith('android-ndk-'):
        return PLAYER / 'NDK'
    if identifier.startswith('cmake-'):
        return PLAYER / 'SDK/cmake' / identifier.removeprefix('cmake-')
    if identifier.startswith('android-sdk-build-tools-'):
        return PLAYER / 'SDK/build-tools' / identifier.removeprefix('android-sdk-build-tools-')
    if identifier.startswith('android-sdk-platform-tools-'):
        return PLAYER / 'SDK/platform-tools'
    if identifier.startswith('android-sdk-platforms-'):
        return PLAYER / 'SDK/platforms' / ('android-' + identifier.removeprefix('android-sdk-platforms-'))
    if identifier.startswith('android-sdk-command-line-tools-'):
        return PLAYER / 'SDK/cmdline-tools' / identifier.removeprefix('android-sdk-command-line-tools-')
    raise ValueError('Unknown Android component: ' + identifier)


for module in modules:
    if module['type'] != 'ZIP':
        continue
    archive = ROOT / '.tooling/downloads' / module['localArchive']
    if not archive.exists():
        print('Not ready: ' + module['id'], flush=True)
        continue
    target_root = destination(module).resolve()
    if PLAYER not in target_root.parents:
        raise ValueError('Android destination is outside the local toolchain')
    with zipfile.ZipFile(archive) as source:
        entries = source.infolist()
        roots = {PurePosixPath(entry.filename.replace('\\', '/')).parts[0] for entry in entries}
        strip_root = len(roots) == 1 and all(len(PurePosixPath(entry.filename).parts) > 1 or entry.is_dir() for entry in entries)
        count = 0
        for entry in entries:
            name = PurePosixPath(entry.filename.replace('\\', '/'))
            parts = name.parts[1:] if strip_root else name.parts
            if not parts:
                continue
            if name.is_absolute() or any(part in ('..', '') or ':' in part for part in parts):
                raise ValueError('Unsafe Android archive path')
            path = target_root.joinpath(*parts).resolve()
            if target_root not in path.parents:
                raise ValueError('Android archive escaped its target folder')
            if entry.is_dir():
                path.mkdir(parents=True, exist_ok=True)
                continue
            if path.exists() and path.stat().st_size == entry.file_size:
                continue
            path.parent.mkdir(parents=True, exist_ok=True)
            temporary = path.with_name(path.name + '.installing')
            with source.open(entry) as incoming, temporary.open('wb') as outgoing:
                shutil.copyfileobj(incoming, outgoing, 4 * 1024**2)
            temporary.replace(path)
            count += 1
    print(f'Installed {module["id"]}: {count} files', flush=True)
