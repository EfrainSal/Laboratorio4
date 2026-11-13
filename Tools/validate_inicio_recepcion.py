"""Verifica el flujo inicial y los archivos protegidos; ejecutar desde la raíz."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/InicioRecepcion'
checks = []

def check(ok, message):
    checks.append(('PASS ' if ok else 'FAIL ') + message)

def text(path):
    return (root / path).read_text(encoding='utf-8-sig')

for name in ('resources-baseline.json', 'escenas-protegidas.json'):
    baseline = json.loads(text('Reports/InicioRecepcion/' + name))
    changed = [p for p, sha in baseline.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != sha]
    check(not changed, f'{len(baseline)} archivos protegidos de {name} intactos: {changed}')
check('_gameSceneName: Recepcion' in text('Assets/Scenes/MainMenu.unity'), 'Nueva partida empieza en Recepcion')
scene = text('Assets/Scenes/Recepcion.unity')
for component in ('InicioGameplayRecepcion', 'PantallaControlesInicio', 'TarjetaSalidaRecepcion'):
    check(component in scene, f'Recepcion contiene {component}')
check('propertyPath: nombreSiguienteEscena' in scene and 'value: Escenario1_Entrada' in scene, 'Panel apunta al nombre real Escenario1_Entrada')
check('value: PanelSalidaRecepcion' in scene and 'panelSalida:' in scene, 'Panel de salida conectado con el objetivo de recepción')
build = text('ProjectSettings/EditorBuildSettings.asset')
check('path: Assets/Scenes/Recepcion.unity' in build and 'path: Assets/Scenes/Escenario1_Entrada.unity' in build, 'Ambas escenas disponibles en compilación')
runtime = text('Reports/InicioRecepcion/pruebas.txt') if (report / 'pruebas.txt').exists() else ''
check('PASS Flujo completo sin errores ni excepciones' in runtime and 'ERROR ' not in runtime
      and 'F frente al panel con tarjeta carga Entrada' in runtime, 'Prueba Play de Jason y Milena con tarjeta y F aprobada')
(report / 'validacion.txt').write_text('\n'.join(checks) + '\n', encoding='utf-8')
print('\n'.join(checks))
raise SystemExit(any(c.startswith('FAIL') for c in checks))
