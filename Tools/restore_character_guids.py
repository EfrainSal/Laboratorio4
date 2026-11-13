from pathlib import Path
import json
import re
import subprocess

root = Path(__file__).resolve().parents[1]
old_ids = set()
for p in (root / 'Backups/IntegracionAnterior/Game').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', p.read_text(errors='replace'), re.M)
    if match:
        old_ids.add(match[1])
rows = []
for line in subprocess.check_output(['git', 'ls-tree', '-r', 'HEAD', 'Assets'], cwd=root, text=True).splitlines():
    info, path = line.split('\t', 1)
    if path.endswith('.meta') and not path.startswith('Assets/Resources/') and (root / path).exists():
        rows.append((info.split()[2], path))
proc = subprocess.Popen(['git', 'cat-file', '--batch'], cwd=root, stdin=subprocess.PIPE, stdout=subprocess.PIPE)
data = proc.communicate(('\n'.join(sha for sha, path in rows) + '\n').encode())[0]
pos = 0
restored = []
failed = []
for sha, path in rows:
    end = data.index(b'\n', pos)
    size = int(data[pos:end].split()[-1])
    pos = end + 1
    prior = re.search(rb'^guid: (\w+)', data[pos:pos + size], re.M)
    pos += size + 1
    p = root / path
    text = p.read_text(errors='replace')
    current = re.search(r'^guid: (\w+)', text, re.M)
    if prior and current and prior[1].decode() in old_ids and prior[1].decode() != current[1]:
        try:
            p.write_text(text.replace('guid: ' + current[1], 'guid: ' + prior[1].decode(), 1), encoding='utf-8')
            restored.append(path)
        except OSError as error:
            failed.append([path, str(error)])
(root / 'Reports/Personajes/restored-guids-final.json').write_text(json.dumps({'restored': restored, 'failed': failed}, indent=2))
print('Restored:', len(restored), 'Failed:', len(failed))
raise SystemExit(bool(failed))
