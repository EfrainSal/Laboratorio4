"""Validate the gameplay integration without writing Unity assets."""
from pathlib import Path
import collections
import hashlib
import json
import re
import subprocess

root = Path(__file__).resolve().parents[1]
reports = root / 'Reports/Gameplay'
baseline = json.loads((reports / 'resources-baseline.json').read_text())
changed = [p for p, digest in baseline.items() if not (root / p).is_file()
           or hashlib.sha256((root / p).read_bytes()).hexdigest() != digest]
guids = collections.defaultdict(list)
for p in (root / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', p.read_text(errors='replace'), re.M)
    if match:
        guids[match[1]].append(p.relative_to(root).as_posix())
duplicates = {g: paths for g, paths in guids.items() if len(paths) > 1}
play = (reports / 'playmode.txt').read_text()
debug_unchanged = subprocess.run(['git', 'diff', '--quiet', '--', 'Assets/Scenes/Debugscene.unity'], cwd=root).returncode == 0
result = {
    'protected_files': len(baseline),
    'modified_or_missing': changed,
    'duplicate_guids': duplicates,
    'debugscene_unchanged': debug_unchanged,
    'jason_pass': 'JASON: PASS' in play,
    'milena_pass': 'MILENA: PASS' in play,
    'failed_checks': [line for line in play.splitlines() if line.startswith('FAIL ')],
    'passed_checks': sum(line.startswith('PASS ') for line in play.splitlines()),
}
(reports / 'protection.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
raise SystemExit(bool(changed or duplicates or not debug_unchanged or result['failed_checks']
                      or not result['jason_pass'] or not result['milena_pass']))
