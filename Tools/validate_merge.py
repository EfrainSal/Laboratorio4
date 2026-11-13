"""Read-only audit of Unity references and the user's immutable Resources baseline."""
from pathlib import Path
import collections
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'Reports/resources-baseline.json'

def main():
    baseline = json.loads(BASE.read_text())
    changed = [p for p, sha in baseline.items() if not (ROOT / p).is_file()
               or hashlib.sha256((ROOT / p).read_bytes()).hexdigest() != sha]
    ids = collections.defaultdict(list)
    for folder in ['Assets', 'Packages', 'Library/PackageCache']:
        for p in (ROOT / folder).rglob('*.meta'):
            match = re.search(r'^guid: (\w+)', p.read_text(errors='replace'), re.M)
            if match:
                ids[match[1]].append(p.relative_to(ROOT).as_posix())
    duplicates = {g: paths for g, paths in ids.items() if len([p for p in paths if p.startswith('Assets/')]) > 1}
    missing = collections.defaultdict(set)
    for p in (ROOT / 'Assets').rglob('*'):
        if p.suffix not in {'.unity', '.prefab', '.asset', '.controller', '.anim', '.mat'}:
            continue
        for guid in re.findall(r'guid: ([a-f0-9]{32})', p.read_text(errors='replace')):
            if guid not in ids and not guid.startswith('0000000000000000'):
                missing[guid].add(p.relative_to(ROOT).as_posix())
    result = {
        'protected_files': len(baseline), 'protected_changed_or_missing': changed,
        'duplicate_asset_guids': duplicates,
        'unresolved_guids': {g: sorted(paths) for g, paths in sorted(missing.items())},
    }
    (ROOT / 'Reports/merge-audit.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    return int(bool(changed or duplicates or missing))

if __name__ == '__main__':
    raise SystemExit(main())
