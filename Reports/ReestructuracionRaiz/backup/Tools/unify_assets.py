"""One-time merge migration. Preserves GUIDs and never writes existing Resources."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
MOVES = [
    ('Assets/Laboratorio4_Escenario1/Scripts/Editor', 'Assets/Game/Editor/Escenario1'),
    ('Assets/Laboratorio4_Escenario1/Scripts', 'Assets/Game/Runtime/Escenario1'),
    ('Assets/Laboratorio4_Escenario1/Sprites', 'Assets/Game/Legacy/Escenario1/Sprites'),
    ('Assets/Laboratorio4_Escenario1/Datos', 'Assets/Game/Legacy/Escenario1/Datos'),
    ('Assets/Laboratorio4_Escenario1/Scenes', 'Assets/Game/Scenes/Escenario1'),
    ('Assets/Laboratorio4_Escenario1/_Referencias', 'Assets/Game/Legacy/Escenario1/Referencias'),
    ('Assets/LaboratorioYiba/Piso3_V10/Art', 'Assets/Game/Art'),
    ('Assets/LaboratorioYiba/Piso3_V10/Data', 'Assets/Game/Data/Piso3'),
    ('Assets/LaboratorioYiba/Piso3_V10/Runtime', 'Assets/Game/Runtime/Piso3'),
    ('Assets/LaboratorioYiba/Piso3_V10/Editor', 'Assets/Game/Editor/Piso3'),
    ('Assets/LaboratorioYiba/Piso3_V10/Prefabs', 'Assets/Game/Prefabs/Piso3'),
    ('Assets/LaboratorioYiba/Piso3_V10/Scenes', 'Assets/Game/Scenes/Piso3'),
    ('Assets/Scripts', 'Assets/Game/Runtime/Gameplay'),
    ('Assets/Editor', 'Assets/Game/Editor/Modular'),
    ('Assets/Prefabs', 'Assets/Game/Prefabs/Gameplay'),
    ('Assets/Scenes', 'Assets/Game/Scenes/Gameplay'),
    ('Assets/Sprites', 'Assets/Game/Art/Interfaz'),
    ('Assets/Animations', 'Assets/Game/Animations'),
    ('Assets/Audio', 'Assets/Game/Audio'),
    ('Assets/Materials', 'Assets/Game/Materials'),
    ('Assets/UI', 'Assets/Game/UI'),
    ('Assets/UI Toolkit', 'Assets/Game/UI Toolkit'),
]

def safe(path):
    p = (ROOT / path).resolve()
    assert p.is_relative_to(ROOT / 'Assets')
    assert not p.is_relative_to(ROOT / 'Assets/Resources')
    return p

if __name__ == '__main__':
    for src, dst in MOVES:
        s, d = safe(src), safe(dst)
        if not s.exists():
            continue
        assert not d.exists(), d
        d.parent.mkdir(parents=True, exist_ok=True)
        s.rename(d)
        meta = Path(str(s) + '.meta')
        if meta.exists():
            meta.rename(Path(str(d) + '.meta'))
    extensions = {'.cs', '.json', '.asset', '.unity', '.prefab', '.md', '.txt'}
    for base in ['Assets', 'ProjectSettings']:
        for p in (ROOT / base).rglob('*'):
            if not p.is_file() or p.suffix not in extensions:
                continue
            if p.is_relative_to(ROOT / 'Assets/Resources'):
                continue
            try:
                original = p.read_text(encoding='utf-8-sig')
            except UnicodeError:
                continue
            text = original
            for src, dst in MOVES:
                text = text.replace(src, dst)
            if p.name == 'Piso3V10Generador.cs':
                text = text.replace('"Assets/LaboratorioYiba/Piso3_V10"', '"Assets/Game"')
                text = text.replace('Raiz + "/Data/', 'Raiz + "/Data/Piso3/')
                text = text.replace('Raiz + "/Prefabs/', 'Raiz + "/Prefabs/Piso3/')
                text = text.replace('Raiz + "/Scenes/', 'Raiz + "/Scenes/Piso3/')
            if text != original:
                p.write_text(text, encoding='utf-8')
    (ROOT / 'Reports/asset-moves.json').write_text(json.dumps(MOVES, indent=2))
    print(f'Migrated {len(MOVES)} roots; Resources excluded.')
