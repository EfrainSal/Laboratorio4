"""Verify protected assets, boss scene isolation, linked art and Unity acceptance results."""
from pathlib import Path
import collections
import hashlib
import json
import re

root = Path(__file__).resolve().parents[1]
reports = root / 'Reports/ConsumiblesArmaJefe'
def digest(path):
    p = root / path
    return hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else None
baseline = json.loads((reports / 'resources-baseline.json').read_text())
scenes = json.loads((reports / 'scene-baseline.json').read_text())
changed = [p for p, h in baseline.items() if digest(p) != h]
scene_changes = [p for p, h in scenes.items() if digest(p) != h]
weapons = json.loads((reports / 'weapon-baseline.json').read_text())
preserved = {}
for name, original in weapons.items():
    s = (root / 'Assets/Prefabs' / (name + '.prefab')).read_text()
    preserved[name] = re.findall(r'^  (?:cadera|anchoArma|rotacionReposoMachete|posicionManoMachete|agarreMachete|attackPoint|attackRange|firePoint|salidaDisparo):.*$', s, re.M) == original
guids = collections.defaultdict(list)
for p in (root / 'Assets').rglob('*.meta'):
    m = re.search(r'^guid: (\w+)', p.read_text(errors='replace'), re.M)
    if m: guids[m[1]].append(p.relative_to(root).as_posix())
duplicates = {g: p for g, p in guids.items() if len(p) > 1}
boss_guid = re.search(r'^guid: (\w+)', (root / 'Assets/Scripts/Jefe/AnimacionJefeDinamica.cs.meta').read_text(), re.M)[1]
boss_scenes = [p.relative_to(root).as_posix() for p in (root / 'Assets').rglob('*.unity') if boss_guid in p.read_text()]
build = re.findall(r'^    path: (.*)$', (root / 'ProjectSettings/EditorBuildSettings.asset').read_text(), re.M)
play = (reports / 'playmode.txt').read_text()
originals = root / 'Assets/Sprites/Escenarios/Entrada/Arma'
canonical = root / 'Assets/Sprites/Escenarios/Piso3/Armas/ArmaLaboratorio4'
copies = {}
for p in originals.glob('*.png'):
    copies[p.name] = [q.relative_to(canonical).as_posix() for q in canonical.rglob('*.png') if q.read_bytes() == p.read_bytes()]
result = dict(protected_files=len(baseline), modified_or_missing_resources=changed,
              protected_scenes_and_machete_changed=scene_changes, existing_weapon_configuration_preserved=preserved,
              duplicate_guids=duplicates, doctor_yiba_scenes=boss_scenes,
              build_scene_paths_exist=all((root / p).is_file() for p in build),
              unique_weapon_sprites=len(list(canonical.rglob('*.png'))), legacy_weapon_copies=copies,
              boss_sprite_frames=len(list((root / 'Assets/Sprites/Escenarios/Piso3/Personajes/DoctorYiba').rglob('*.png'))),
              playmode_pass=play.startswith('RESULTADO: PASS'),
              passed_checks=sum(l.startswith('PASS ') for l in play.splitlines()),
              failed_checks=[l for l in play.splitlines() if l.startswith('FAIL ')])
(reports / 'validation.json').write_text(json.dumps(result, indent=2))
print(json.dumps({k: v for k, v in result.items() if k != 'legacy_weapon_copies'}, indent=2))
assert not changed and not scene_changes and all(preserved.values()) and not duplicates
assert boss_scenes == ['Assets/Scenes/Nivel1.unity'] and result['build_scene_paths_exist']
assert all(copies.values()) and result['playmode_pass'] and not result['failed_checks']
