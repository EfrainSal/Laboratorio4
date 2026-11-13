"""Read-only checks for the merge repair, after the Unity Play Mode suite."""
from pathlib import Path
import collections
import hashlib
import json
import re

root = Path(__file__).resolve().parents[1]
reports = root / 'Reports/Reparacion'
baseline = json.loads((reports / 'resources-baseline.json').read_text())
changed = [p for p, digest in baseline.items() if not (root / p).is_file()
           or hashlib.sha256((root / p).read_bytes()).hexdigest() != digest]
guids = collections.defaultdict(list)
for p in (root / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', p.read_text(errors='replace'), re.M)
    if match:
        guids[match[1]].append(p.relative_to(root).as_posix())
duplicates = {g: paths for g, paths in guids.items() if len(paths) > 1}
positions = json.loads((reports / 'armas-inicial.json').read_text())
preserved = all(re.findall(r'^  cadera:.*$', (root / 'Assets/Prefabs' / (name + '.prefab')).read_text(), re.M) == lines
                for name, lines in positions.items())
controls = json.loads((root / 'Assets/Settings/Gameplay.inputactions').read_text())
actions = {a['name'] for a in controls['maps'][0]['actions']}
play = (reports / 'playmode.txt').read_text()
result = dict(protected_files=len(baseline), modified_or_missing=changed, duplicate_guids=duplicates,
              weapon_positions_preserved=preserved,
              input_actions=sorted(actions),
              jason_pass='JASON: PASS' in play, milena_pass='MILENA: PASS' in play,
              passed_checks=sum(l.startswith('PASS ') for l in play.splitlines()),
              failed_checks=[l for l in play.splitlines() if l.startswith('FAIL ')])
(reports / 'validation.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
raise SystemExit(bool(changed or duplicates or not preserved or result['failed_checks']
                      or not result['jason_pass'] or not result['milena_pass']
                      or not {'Move', 'Attack', 'Reload', 'SwitchWeapon'}.issubset(actions)))
