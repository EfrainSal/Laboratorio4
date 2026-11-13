"""Conservación de Resources y enlaces de presentación entre pisos."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/FundidosPisos'
result = []

def check(ok, message):
    result.append(('PASS ' if ok else 'FAIL ') + message)

def text(path):
    return (root / path).read_text(encoding='utf-8-sig')

baseline = json.loads(text('Reports/FundidosPisos/resources-baseline.json'))
changed = [p for p, h in baseline.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
check(not changed, f'{len(baseline)} archivos existentes de Resources intactos: {changed}')
baseline = json.loads(text('Reports/FundidosPisos/escenas-base.json'))
concurrentes = json.loads(text('Reports/FundidosPisos/cambios-concurrentes.json')) if (report / 'cambios-concurrentes.json').exists() else {}
changed = [p for p, h in baseline.items() if p != 'Assets/Scenes/Recepcion.unity' and p not in concurrentes
           and hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
check(not changed, f'Escenas ajenas a la referencia de estilo de Recepcion intactas: {changed}')
check(all(hashlib.sha256((root / p).read_bytes()).hexdigest() == d['sha256_conservado'] for p, d in concurrentes.items()),
      'Ajuste concurrente de la puerta de Entrada conservado sin sobrescribir')
for path in ['Assets/Scripts/Controladores/PuertaNivel.cs', 'Assets/Scripts/Escenarios/Entrada/PuertaSalida.cs',
             'Assets/Scripts/Escenarios/Recepcion/RecepcionSalida.cs', 'Assets/Scripts/Escenarios/Laboratorio/E3Puerta.cs',
             'Assets/Scripts/Escenarios/Jefe/E4PuertaSalida.cs']:
    source = text(path)
    check('TransicionEntrePisos.CargarEscena(' in source and 'SceneManager.LoadScene(' not in source, f'Puerta usa el fundido común: {Path(path).name}')
check('estilosMenu:' in text('Assets/Scenes/Recepcion.unity'), 'Controles vinculados a la hoja de estilos del menú')
check('ConsumirBotiquin' not in text('Assets/Scripts/Menus/PantallaControlesInicio.cs'), 'La pantalla de controles conserva la eliminación del botiquín')
qa = text('Reports/InicioRecepcion/pruebas.txt')
check('ERROR ' not in qa and 'PASS Flujo completo sin errores ni excepciones' in qa
      and 'Hay fundido de salida y entrada' in qa and 'Botón de controles usa el mismo degradado' in qa,
      'Prueba Play de botón compartido y fundido completo aprobada')
progresion = text('Reports/ProgresionEntrada/pruebas.txt')
check('ERROR ' not in progresion and 'PASS Sin errores ni excepciones durante el recorrido' in progresion,
      'Regresión Play de Entrada a Piso3 y conservación de partida aprobada')
(report / 'validacion.txt').write_text('\n'.join(result) + '\n', encoding='utf-8')
print('\n'.join(result))
raise SystemExit(any(r.startswith('FAIL') for r in result))
