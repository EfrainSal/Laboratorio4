"""Conservación de Resources y referencias de vida, loot y derrota."""
import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/PersistenciaLootDerrota'
results = []

def read(path):
    return (root / path).read_text(encoding='utf-8-sig')

def check(ok, message):
    results.append(('PASS ' if ok else 'FAIL ') + message)

baseline = json.loads(read('Reports/PersistenciaLootDerrota/resources-baseline.json'))
changed = [p for p, h in baseline.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
check(not changed, f'{len(baseline)} archivos preexistentes de Resources intactos: {changed}')
scenes = json.loads(read('Reports/PersistenciaLootDerrota/escenas-base.json'))
user_changes = json.loads(read('Reports/PersistenciaLootDerrota/cambios-usuario.json')) if (report / 'cambios-usuario.json').exists() else {}
changed = [p for p, h in scenes.items() if hashlib.sha256((root / p).read_bytes()).hexdigest() != user_changes.get(p, {}).get('sha256_guardado', h)]
check(not changed, f'Escenas conservadas, incluidos los cambios abiertos del usuario: {changed}')
guid_venda = re.search(r'guid: (\w+)', read('Assets/Prefabs/Vendas.prefab.meta'))[1]
guid_pickup = re.search(r'guid: (\w+)', read('Assets/Scripts/Jugador/ItemPickup.cs.meta'))[1]
check(guid_pickup in read('Assets/Prefabs/Vendas.prefab') and 'tipoDeObjeto: 0' in read('Assets/Prefabs/Vendas.prefab'), 'Vendas reutiliza el pickup de inventario existente')
for name in ['Enemy', 'Enemy_Melee', 'Enemy_Ranged']:
    text = read(f'Assets/Prefabs/{name}.prefab')
    check(f'vendaDropPrefab: {{fileID: 2547609757957364918, guid: {guid_venda}, type: 3}}' in text and
          'vendaDropChance: 20' in text and 'ammoDropPrefab:' in text, f'{name}: vendas al 20 % y munición conservada')
gm = read('Assets/Scripts/Controladores/GameManager.cs')
check('DontDestroyOnLoad(gameObject)' in gm and 'vidaMaximaPendiente' in gm, 'Estado de vida vive en el singleton persistente')
check('CapturarDatosDeTransicion()' in read('Assets/Scripts/Controladores/TransicionEntrePisos.cs'), 'Los cambios de piso capturan los datos antes de cargar')
muerte = gm.split('public void TerminarRunPorMuerte', 1)[1].split('public void CompletarRunVictoria', 1)[0]
pantalla = read('Assets/Scripts/Menus/DeathScreenController.cs')
retry = pantalla.split('private void OnRetry()', 1)[1].split('private void OnMenu()', 1)[0]
check('OnRunTerminada?.Invoke' in muerte and 'CargarEscena(' not in muerte and 'IniciarNuevaRun(' not in muerte and
      'CargarEscena("Recepcion", false)' in retry and 'gm.IniciarNuevaRun(character)' in retry,
      'La derrota muestra Game Over y sólo Reintentar reinicia en Recepcion')
build = read('ProjectSettings/EditorBuildSettings.asset')
check(re.search(r'enabled: 1\s+path: Assets/Scenes/Recepcion.unity', build) is not None, 'Recepcion habilitada en Build Settings')
qa = read('Reports/PersistenciaLootDerrota/pruebas.txt') if (report / 'pruebas.txt').exists() else ''
check('ERROR ' not in qa and 'PASS Recorrido completo sin errores ni excepciones' in qa,
      'Prueba Play de persistencia, drops y retorno tras derrota aprobada')
progresion = read('Reports/ProgresionEntrada/pruebas.txt')
check('ERROR ' not in progresion and 'PASS Transición conserva vida y munición' in progresion and
      'PASS Reintentar en Game Over devuelve a Recepcion' in progresion,
      'Regresión de F, tarjeta, transferencia de vida y derrota aprobada')
(report / 'validacion.txt').write_text('\n'.join(results) + '\n', encoding='utf-8')
print('\n'.join(results))
raise SystemExit(any(r.startswith('FAIL') for r in results))
