"""Unpack Unity's included TMP Essentials while the batch Editor is closed."""
from pathlib import Path, PurePosixPath
import tarfile

ROOT = Path(__file__).resolve().parents[1]
package = next((ROOT / 'Library/PackageCache').glob('com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage'))
with tarfile.open(package, 'r:gz') as archive:
    members = {member.name: member for member in archive.getmembers()}
    count = 0
    for name, member in members.items():
        if not name.endswith('/pathname'):
            continue
        relative = archive.extractfile(member).read().decode('utf-8').strip().rstrip('\0')
        parts = PurePosixPath(relative.replace('\\', '/')).parts
        if not parts or parts[0] != 'Assets' or '..' in parts or any(':' in part for part in parts):
            raise ValueError('TMP package path is outside Assets')
        destination = ROOT.joinpath(*parts).resolve()
        if (ROOT / 'Assets').resolve() not in destination.parents and destination != ROOT / 'Assets':
            raise ValueError('TMP destination escaped Assets')
        directory = name.rsplit('/', 1)[0]
        asset = members.get(directory + '/asset')
        if asset is not None:
            destination.parent.mkdir(parents=True, exist_ok=True)
            if not destination.exists():
                destination.write_bytes(archive.extractfile(asset).read())
                count += 1
        else:
            destination.mkdir(parents=True, exist_ok=True)
        meta = members.get(directory + '/asset.meta')
        meta_path = Path(str(destination) + '.meta')
        if meta and not meta_path.exists():
            meta_path.write_bytes(archive.extractfile(meta).read())
print(f'Imported {count} included TMP resource files with their original GUIDs and font license.')
