"""Validación de assets y protección de Resources; ejecutar desde la raíz del proyecto."""
import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent
report = root / 'Reports/ProgresionEntrada'
checks = []

def check(condition, message):
    checks.append(('PASS ' if condition else 'FAIL ') + message)

def read(path):
    return (root / path).read_text(encoding='utf-8-sig')

for baseline in ('resources-baseline.json', 'protected-baseline.json'):
    original = json.loads((report / baseline).read_text(encoding='utf-8-sig'))
    changed = [name for name, sha in original.items()
               if not (root / name).is_file() or hashlib.sha256((root / name).read_bytes()).hexdigest() != sha]
    check(not changed, f'{baseline}: {len(original)} archivos intactos; diferencias={changed}')

entry = read('Assets/Scenes/Escenario1_Entrada.unity')
for i in (1, 2, 3):
    guid = re.search(r'^guid: (\w+)', read(f'Assets/Prefabs/Spawner_lvl{i}.prefab.meta'), re.M)[1]
    check(f'm_SourcePrefab: {{fileID: 100100000, guid: {guid},' in entry, f'Entrada referencia prefab original Spawner_lvl{i}')
check('_gameSceneName: Escenario1_Entrada' in read('Assets/Scenes/MainMenu.unity'), 'MainMenu inicia en Entrada')
check('path: Assets/Scenes/Escenario1_Entrada.unity' in read('ProjectSettings/EditorBuildSettings.asset'), 'Entrada incluida en compilación')
check('nombreSiguienteEscena: Piso3_V10' in read('Assets/Prefabs/PuertaTransicion.prefab'), 'Puerta enlazada a Piso3_V10')
card_guid = re.search(r'^guid: (\w+)', read('Assets/Sprites/TarjetaAcceso.png.meta'), re.M)[1]
check(card_guid in read('Assets/Prefabs/TarjetaAcceso.prefab'), 'Tarjeta usa sprite raíz Sprites/TarjetaAcceso.png')
for actor in ('Jason', 'Milena'):
    asset = read(f'Assets/Prefabs/Player_{actor}.prefab')
    check('invertirFiloMachete: 1' in asset and 'escalaMachete: 0.5' in asset and 'mostrarAlcance: 0' in asset, f'{actor}: filo invertido, tamaño mitad, sin círculo')
    for array in ('spritesApuntar', 'spritesDisparo'):
        sprites = re.search(rf'  {array}:\n((?:  - .*\n)+)', asset)
        check(sprites is not None and len(re.findall(r'guid: \w+', sprites[1])) == 8, f'{actor}: ocho sprites {array}')
actions = json.loads(read('Assets/Settings/Gameplay.inputactions'))
bindings = [b for m in actions['maps'] for b in m['bindings']]
for action, key in [('ConsumirVenda', '1'), ('ConsumirEstimulante', '2'), ('ConsumirBotiquin', '3')]:
    check(any(b['action'] == action and b['path'] == f'<Keyboard>/{key}' for b in bindings), f'{action} en tecla {key}')
boss_guid = re.search(r'^guid: (\w+)', read('Assets/Scripts/Jefe/BossAI.cs.meta'), re.M)[1]
for scene in ('Assets/Scenes/MainMenu.unity', 'Assets/Scenes/Escenario1_Entrada.unity', 'Assets/Scenes/Piso3_V10.unity'):
    check(boss_guid not in read(scene), f'Jefe ausente de {Path(scene).stem}')
check(boss_guid in read('Assets/Scenes/Nivel1.unity'), 'Jefe presente exclusivamente en Nivel1')
runtime = (report / 'pruebas.txt').read_text(encoding='utf-8-sig') if (report / 'pruebas.txt').exists() else ''
check('PASS Sin errores ni excepciones durante el recorrido' in runtime and 'ERROR ' not in runtime, 'Prueba Play de recorrido completo aprobada')
(report / 'validacion-assets.txt').write_text('\n'.join(checks) + '\n', encoding='utf-8')
print('\n'.join(checks))
raise SystemExit(any(c.startswith('FAIL') for c in checks))
