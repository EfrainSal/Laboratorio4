"""Conservación de assets y comprobación de la corrección de tarjeta/panel."""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/PanelRecepcion'
result = []
for file in ('resources-baseline.json', 'protegidos.json'):
    baseline = json.loads((report / file).read_text(encoding='utf-8-sig'))
    changed = [p for p, h in baseline.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
    result.append(('FAIL ' if changed else 'PASS ') + f'{len(baseline)} archivos protegidos de {file} intactos: {changed}')
pickup = (root / 'Assets/Scripts/Escenarios/Recepcion/TarjetaSalidaRecepcion.cs').read_text(encoding='utf-8-sig')
result.append(('PASS ' if 'SceneManager' not in pickup else 'FAIL ') + 'Recoger la tarjeta no carga escenas')
scene = (root / 'Assets/Scenes/Recepcion.unity').read_text(encoding='utf-8-sig')
result.append(('PASS ' if 'value: PanelSalidaRecepcion' in scene and 'panelSalida:' in scene else 'FAIL ') + 'Panel de salida e indicador vinculados en Recepcion')
runtime = (root / 'Reports/InicioRecepcion/pruebas.txt').read_text(encoding='utf-8-sig')
result.append(('PASS ' if 'PASS Flujo completo sin errores ni excepciones' in runtime and 'ERROR ' not in runtime
               and 'F frente al panel con tarjeta carga Entrada' in runtime else 'FAIL ') + 'Pruebas Play de proximidad, tarjeta, guardado y F aprobadas')
(report / 'validacion.txt').write_text('\n'.join(result) + '\n', encoding='utf-8')
print('\n'.join(result))
raise SystemExit(any(r.startswith('FAIL') for r in result))
