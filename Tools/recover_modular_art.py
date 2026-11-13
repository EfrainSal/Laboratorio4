"""Recover exact pre-merge art from local Git/LFS, outside protected Resources."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[1]
PREFIX = 'Assets/Resources/Piso3/'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def main():
    recovered = []
    for old in git('ls-tree', '-r', '--name-only', '4c19464', PREFIX).decode().splitlines():
        rel = old[len(PREFIX):]
        if not (rel.startswith('Props') or rel.startswith('Tiles')):
            continue
        dest = ROOT / 'Assets/Game/Art/Modular' / rel
        if dest.exists():
            continue
        data = git('show', '4c19464:' + old)
        if data.startswith(b'version https://git-lfs'):
            oid = re.search(rb'oid sha256:(\w+)', data)[1].decode()
            obj = ROOT / '.git/lfs/objects' / oid[:2] / oid[2:4] / oid
            if not obj.exists():
                raise RuntimeError('Falta objeto LFS local: ' + old)
            data = obj.read_bytes()
            assert hashlib.sha256(data).hexdigest() == oid
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        recovered.append(dest.relative_to(ROOT).as_posix())
    # Los tiles existentes en Resources siguen intactos. Las escenas usan copias nuevas.
    mapping = {}
    for old in (ROOT / PREFIX / 'GeneratedTiles').glob('*.asset'):
        dest = ROOT / 'Assets/Game/Data/Modular/GeneratedTiles' / old.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        sprite_meta = ROOT / 'Assets/Game/Art/Modular/Tiles' / (old.stem + '.png.meta')
        if not sprite_meta.exists():
            continue
        guid = re.search(r'^guid: (\w+)', sprite_meta.read_text(), re.M)[1]
        tile_guid = uuid.uuid5(uuid.NAMESPACE_URL, 'Laboratorio4/Modular/' + old.name).hex
        original_guid = re.search(r'^guid: (\w+)', Path(str(old) + '.meta').read_text(), re.M)[1]
        mapping[original_guid] = tile_guid
        text = re.sub(r'm_Sprite: \{[^}]*\}', f'm_Sprite: {{fileID: 21300000, guid: {guid}, type: 3}}', old.read_text())
        assert not dest.exists()
        dest.write_text(text)
        Path(str(dest) + '.meta').write_text('fileFormatVersion: 2\nguid: ' + tile_guid + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    for p in (ROOT / 'Assets/Game/Scenes').rglob('*.unity'):
        text = p.read_text()
        updated = text
        for old, new in mapping.items():
            updated = updated.replace(old, new)
        if text != updated:
            p.write_text(updated)
    for p in (ROOT / 'Assets/Game/Editor/Modular').glob('*.cs'):
        text = p.read_text()
        text = text.replace('Assets/Resources/Piso3/GeneratedTiles', 'Assets/Game/Data/Modular/GeneratedTiles')
        text = text.replace('Assets/Resources/Piso3', 'Assets/Game/Art/Modular')
        text = text.replace('AssetDatabase.CreateFolder("Assets/Game/Art/Modular","GeneratedTiles")', 'System.IO.Directory.CreateDirectory(dir)')
        text = text.replace('/Resources/Piso3/', '/Game/Art/Modular/')
        p.write_text(text)
    (ROOT / 'Reports/recovered-art.json').write_text(json.dumps(recovered, indent=2))
    print('Recovered', len(recovered), 'art files; created', len(mapping), 'unprotected tile copies.')

if __name__ == '__main__':
    main()
