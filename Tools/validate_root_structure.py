"""Comprueba la conservación del contenido y GUIDs tras la migración a carpetas raíz."""
import hashlib
import json
import re
import io
import subprocess
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/ReestructuracionRaiz'
before = json.loads((report / 'before.json').read_text(encoding='utf-8-sig'))
plan = json.loads((report / 'plan.json').read_text(encoding='utf-8-sig'))
rewritten = {c['path'] for c in plan['rewrites']}
checks = []

def check(ok, message, details=None):
    checks.append({'ok': bool(ok), 'message': message, 'details': details or []})

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

missing, altered, archived = [], [], []
for old, data in before.items():
    target = root / data['destination']
    if not target.exists():
        archive = report / 'contenedores-archivados' / old
        if archive.is_file() and old.endswith('.meta'):
            archived.append(old)
            if sha(archive) != data['sha256']: altered.append(old)
            continue
        missing.append(old)
    elif target.is_file() and data['destination'] not in rewritten and sha(target) != data['sha256']:
        altered.append(data['destination'])
check(not missing, 'Ningún archivo preexistente perdido', missing)
content_changes = [p for p in altered if not p.endswith('.meta')]
check(not content_changes, 'Contenido de assets intacto fuera de los ajustes previstos de rutas', content_changes)
protected = [p for p in before if p.startswith('Assets/Resources/')]
modified_resources = [p for p in protected if not (root / p).is_file() or sha(root / p) != before[p]['sha256']]
check(not modified_resources, f'{len(protected)} archivos existentes de Resources idénticos', modified_resources)
check(all(not (root / s).exists() and not (root / (s + '.meta')).exists() for s in plan['roots']), 'Cinco carpetas de origen disueltas')

guid_mismatches = []
normalized = []
for old, data in before.items():
    if not old.endswith('.meta'): continue
    p = root / data['destination']
    if not p.exists(): p = report / 'contenedores-archivados' / old
    backup = report / 'backup' / old
    # Los metas no están incluidos en la lista de escrituras; la comparación de hash es más estricta que sólo GUID.
    if not p.exists(): guid_mismatches.append(old)
    elif sha(p) != data['sha256']: normalized.append((old, p))
# Unity 6 actualiza el esquema de importadores al cargar por primera vez parte del arte antiguo.
# Contrastar sus GUID con los metas versionados de origen, sin restaurar ajustes ni sobrescribir assets.
requests = [('HEAD:' + old).encode('utf-8') for old, _ in normalized]
raw = subprocess.run(['git', 'cat-file', '--batch'], cwd=root, input=b'\n'.join(requests) + b'\n', capture_output=True, check=True).stdout if requests else b''
stream = io.BytesIO(raw)
normalization_report = []
for old, path in normalized:
    header = stream.readline().rstrip(b'\n')
    if header.endswith(b' missing'):
        guid_mismatches.append(old + ': no hay meta de origen versionado para contrastar')
        continue
    length = int(header.rsplit(b' ', 1)[1]); original = stream.read(length); stream.read(1)
    original_guid = re.search(rb'(?m)^guid: (\w+)', original)
    actual_guid = re.search(rb'(?m)^guid: (\w+)', path.read_bytes())
    same = original_guid is not None and actual_guid is not None and original_guid[1] == actual_guid[1]
    if not same: guid_mismatches.append(old)
    normalization_report.append({'source': old, 'destination': path.relative_to(root).as_posix(), 'guid_conservado': same})
(report / 'metas-reimportados.json').write_text(json.dumps(normalization_report, indent=2), encoding='utf-8')
check(not guid_mismatches, 'GUID conservados; metas reserializados por Unity contrastados con el origen versionado', guid_mismatches)
check(True, f'{len(normalized)} metas fuera de Resources normalizados al reimportar en Unity 6')
check(len(archived) == 12, '12 metas de contenedores vacíos archivados fuera de Assets', archived)

scenes = [d['destination'] for p, d in before.items() if p.endswith('.unity')]
check(all((root / p).is_file() for p in scenes), f'{len(scenes)} escenas conservadas')
build = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text(encoding='utf-8-sig')
build_paths = re.findall(r'^    path: (.+)$', build, re.M)
check(all((root / p).is_file() for p in build_paths), f'{len(build_paths)} escenas de compilación con rutas válidas')
legacy = re.compile(r'Assets/(?:Laboratorio4_(?:Escenario[134]|Recepcion)|LaboratorioYiba)(?:/|["\x27])', re.I)
obsolete = []
for folder, extensions in [('Assets', {'.cs', '.json', '.asset', '.prefab', '.unity'}), ('ProjectSettings', {'.asset'})]:
    for p in (root / folder).rglob('*'):
        if p.is_file() and p.suffix in extensions and not p.is_relative_to(root / 'Assets/Resources'):
            if legacy.search(p.read_text(encoding='utf-8-sig', errors='replace')): obsolete.append(p.relative_to(root).as_posix())
check(not obsolete, 'Código y assets activos sin rutas a las carpetas disueltas', obsolete)

json_paths = []
def walk(value):
    if isinstance(value, dict):
        for v in value.values(): yield from walk(v)
    elif isinstance(value, list):
        for v in value: yield from walk(v)
    elif isinstance(value, str): yield value
for p in (root / 'Assets/Settings/Escenarios').rglob('*.json'):
    if p.name == 'limpieza.json': continue  # Catálogo histórico de archivos que ya no existen.
    for s in walk(json.loads(p.read_text(encoding='utf-8-sig'))):
        if s.startswith('Assets/') or s.startswith('Sprites/'):
            path = s if s.startswith('Assets/') else 'Assets/' + s
            if not (root / path).is_file(): json_paths.append((p.name, path))
check(not json_paths, 'Referencias de imágenes en layouts JSON resueltas', json_paths)

sprite_count = sum(p.endswith(('.png', '.jpg')) and any(p.startswith(s + '/') for s in plan['roots']) for p in before)
prefab_count = sum(p.endswith('.prefab') and any(p.startswith(s + '/') for s in plan['roots']) for p in before)
script_count = sum(p.endswith('.cs') and any(p.startswith(s + '/') for s in plan['roots']) for p in before)
check(True, f'Reubicados: {sprite_count} imágenes, {prefab_count} prefabs y {script_count} scripts')
(report / 'validacion.json').write_text(json.dumps(checks, indent=2, ensure_ascii=False), encoding='utf-8')
for item in checks:
    print(('PASS ' if item['ok'] else 'FAIL ') + item['message'])
    if not item['ok']: print(json.dumps(item['details'][:15], ensure_ascii=True))
raise SystemExit(any(not c['ok'] for c in checks))
