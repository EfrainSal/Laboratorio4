"""Planifica la migración por tipo de asset. Unity aplica los movimientos preservando GUIDs."""
from pathlib import Path
import hashlib
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / 'Reports/ReestructuracionRaiz'
SOURCES = ['Assets/Laboratorio4_Escenario1', 'Assets/Laboratorio4_Escenario3',
           'Assets/Laboratorio4_Escenario4', 'Assets/Laboratorio4_Recepcion', 'Assets/LaboratorioYiba']
LEVELS = [('Assets/Laboratorio4_Escenario1', 'Entrada', 'Sprites', 'Datos', 'Scenes', 'Scripts'),
          ('Assets/Laboratorio4_Escenario3', 'Laboratorio', 'Arte', 'Datos', 'Escenas', 'Scripts'),
          ('Assets/Laboratorio4_Escenario4', 'Jefe', 'Art', 'Data', 'Scenes', 'Scripts'),
          ('Assets/Laboratorio4_Recepcion', 'Recepcion', 'Sprites', 'Datos', 'Scenes', 'Scripts'),
          ('Assets/LaboratorioYiba/Piso3_V10', 'Piso3', 'Art', 'Data', 'Scenes', 'Runtime')]

def safe(path):
    p = (ROOT / path).resolve()
    assert p.is_relative_to(ROOT), path
    assert not p.is_relative_to((ROOT / 'Assets/Resources').resolve()), path
    return p

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def plan():
    REPORT.mkdir(parents=True, exist_ok=True)
    assert not (REPORT / 'before.json').exists(), 'Ya existe un inventario; no sobrescribir el estado anterior.'
    moves = []
    for source, level, art, data, scenes, runtime in LEVELS:
        has_animation = (ROOT / f'{source}/{art}/Personajes/Personajes_Animaciones.json').exists()
        if has_animation:
            moves.append((f'{source}/{data}', f'Assets/Settings/Escenarios/{level}'))
        # Archivos que pertenecen a categorías distintas de su contenedor original.
        for archive in sorted((ROOT / source).rglob('*.zip')):
            moves.append((archive.relative_to(ROOT).as_posix(), f'Assets/Documentation/PaquetesOriginales/{level}/{archive.name}'))
        animation = f'{source}/{art}/Personajes/Personajes_Animaciones.json'
        if (ROOT / animation).exists():
            moves.append((animation, f'Assets/Settings/Escenarios/{level}/Personajes_Animaciones.json'))
        for old, new in [(f'{runtime}/Editor', f'Editor/Escenarios/{level}'),
                         ('Editor', f'Editor/Escenarios/{level}'),
                         (runtime, f'Scripts/Escenarios/{level}'),
                         (art, f'Sprites/Escenarios/{level}'),
                         (data, f'Settings/Escenarios/{level}'),
                         ('Prefabs', f'Prefabs/Escenarios/{level}'),
                         ('_Referencias', f'Sprites/Referencias/{level}')]:
            if old == data and has_animation: continue
            if (ROOT / source / old).exists(): moves.append((f'{source}/{old}', f'Assets/{new}'))
        for scene in sorted((ROOT / source / scenes).glob('*.unity')):
            moves.append((scene.relative_to(ROOT).as_posix(), f'Assets/Scenes/{scene.name}'))
        for doc in sorted((ROOT / source).glob('*.md')):
            moves.append((doc.relative_to(ROOT).as_posix(), f'Assets/Documentation/Escenarios/{level}/{doc.name}'))
    for old, new in moves:
        assert safe(old).exists(), old
        assert not safe(new).exists(), f'Colisión de destino: {new}'
    replacements = sorted(moves, key=lambda m: len(m[0]), reverse=True)

    def destination(path):
        for old, new in replacements:
            if path == old or path == old + '.meta' or path.startswith(old + '/'):
                return new + path[len(old):]
        return path

    before = {}
    for p in sorted((ROOT / 'Assets').rglob('*')):
        if p.is_file():
            path = p.relative_to(ROOT).as_posix()
            before[path] = {'sha256': sha(p), 'destination': destination(path)}
    (REPORT / 'before.json').write_text(json.dumps(before, indent=2), encoding='utf-8')

    def adapt(path, text):
        original = text
        # Rutas absolutas de proyecto en integradores, escenas y datos.
        for old, new in replacements:
            text = text.replace(old, new)
        if path.endswith('Piso3V10Generador.cs'):
            text = text.replace('"Assets/LaboratorioYiba/Piso3_V10"', '"Assets"')
            text = text.replace('CarpetaPersonajes + "/Personajes_Animaciones.json"', '"Assets/Settings/Escenarios/Piso3/Personajes_Animaciones.json"')
            for a, b in [('/Art', '/Sprites/Escenarios/Piso3'), ('/Data/', '/Settings/Escenarios/Piso3/'), ('/Prefabs/', '/Prefabs/Escenarios/Piso3/')]:
                text = text.replace('Raiz + "' + a, 'Raiz + "' + b)
        if path.endswith('E4Constructor.cs'):
            text = text.replace('"Assets/Laboratorio4_Escenario4"', '"Assets"').replace('Raiz + "/Data/', 'Raiz + "/Settings/Escenarios/Jefe/')
            text = text.replace('vive en Assets/Laboratorio4_Escenario4', 'se distribuye entre las carpetas principales de Assets')
        if path.endswith('E3Constructor.cs'):
            text = text.replace('"Assets/Laboratorio4_Escenario3"', '"Assets"')
            for a, b in [('/Arte', '/Sprites/Escenarios/Laboratorio'), ('/Datos/', '/Settings/Escenarios/Laboratorio/'), ('/Escenas', '/Scenes')]:
                text = text.replace('Raiz + "' + a, 'Raiz + "' + b)
        if path.endswith('ConstruirEscenario1.cs'):
            text = re.sub(r"root = Path.GetDirectoryName\(Path.GetDirectoryName\(jsonPath\)\).Replace\([^;]+;", 'root = "Assets";', text)
        if path.endswith('AbrirEscenario1.cs'):
            text = re.sub(r'static string Raiz\(string rutaEscena\) => [^;]+;', 'static string Raiz(string rutaEscena) => "Assets";', text)
            text = text.replace('            Limpiar(raiz);', '            // La limpieza del paquete antiguo no se ejecuta sobre la estructura compartida.')
            text = text.replace('"/Datos/', '"/Settings/Escenarios/Entrada/').replace('"/Sprites/', '"/Sprites/Escenarios/Entrada/')
        if path.endswith('ConstruirRecepcion.cs'):
            text = re.sub(r"return Path.GetDirectoryName\(Path.GetDirectoryName\(AssetDatabase.GUIDToAssetPath\(g\)\)\).Replace\([^;]+;", 'return "Assets";', text)
            text = text.replace('"/Datos/', '"/Settings/Escenarios/Recepcion/')
            text = text.replace('"Sprites/', '"Sprites/Escenarios/Recepcion/').replace('{raiz}/Sprites/', '{raiz}/Sprites/Escenarios/Recepcion/')
            text = text.replace('raiz + "/Sprites"', 'raiz + "/Sprites/Escenarios/Recepcion"')
            text = text.replace('Todo queda dentro de Assets/Laboratorio4_Recepcion', 'Los assets se distribuyen entre las carpetas principales de Assets')
        # Los layouts de Entrada y Recepción usan rutas relativas al directorio Assets.
        for source, level, art, data, _, _ in LEVELS:
            if path.startswith(source + '/' + data + '/') and level in ('Entrada', 'Recepcion'):
                text = text.replace('"Sprites/', f'"Sprites/Escenarios/{level}/')
                if level == 'Entrada': text = text.replace('"Datos/', '"Settings/Escenarios/Entrada/')
        return text if text != original else None

    rewrites = []
    candidates = list((ROOT / 'Assets').rglob('*')) + list((ROOT / 'Tools').rglob('*.py')) + list((ROOT / 'ProjectSettings').glob('*.asset'))
    candidates += [ROOT / 'Reports/ProgresionEntrada/LEEME.md', ROOT / 'Reports/ConsumiblesArmaJefe/LEEME.md']
    for p in candidates:
        if not p.is_file() or p.suffix not in ('.cs', '.json', '.asset', '.unity', '.prefab', '.py', '.md', '.asmdef', '.uxml', '.uss'): continue
        path = p.relative_to(ROOT).as_posix()
        if path.startswith('Assets/Resources/') or path == 'Tools/restructure_root_assets.py': continue
        content = p.read_text(encoding='utf-8-sig')
        updated = adapt(path, content)
        if path == 'Tools/unify_assets.py':
            updated = '"""La migración antigua a Assets/Game queda retirada: se conserva la estructura raíz."""\nif __name__ == "__main__":\n    raise SystemExit("Consulta Reports/ReestructuracionRaiz/LEEME.md. La migración raíz ya tiene su propio manifiesto.")\n'
        if updated is not None:
            dest = destination(path)
            backup = REPORT / 'backup' / path
            backup.parent.mkdir(parents=True, exist_ok=True); backup.write_bytes(p.read_bytes())
            stage = REPORT / 'staged' / dest
            stage.parent.mkdir(parents=True, exist_ok=True)
            # Conservar BOM y saltos originales; el reemplazo no toca metas ni binarios.
            binary = p.read_bytes(); newline = '\r\n' if b'\r\n' in binary else '\n'
            payload = updated.replace('\r\n', '\n').replace('\n', newline).encode('utf-8')
            if binary.startswith(b'\xef\xbb\xbf'): payload = b'\xef\xbb\xbf' + payload
            stage.write_bytes(payload)
            rewrites.append({'path': dest, 'staged': stage.relative_to(ROOT).as_posix()})
    manifest = {'moves': [{'source': a, 'destination': b} for a, b in moves], 'rewrites': rewrites, 'roots': SOURCES}
    (REPORT / 'plan.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    relocated = [p for p in before if any(p.startswith(s + '/') for s in SOURCES) and not p.endswith('.meta')]
    uncovered = [p for p in relocated if before[p]['destination'] == p]
    assert not uncovered, uncovered
    print(f'Plan: {len(moves)} movimientos, {len(relocated)} archivos de contenido, {len(rewrites)} ajustes de rutas. Inventario: {len(before)} archivos.')

if __name__ == '__main__':
    if sys.argv[1:] == ['plan']: plan()
    else: raise SystemExit('Uso: python Tools/restructure_root_assets.py plan')
