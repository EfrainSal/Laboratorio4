"""Verifica el perfil de iluminación y la conservación de los assets protegidos."""
import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/OscuridadPisos'
results = []

def check(ok, message):
    results.append(('PASS ' if ok else 'FAIL ') + message)

def content(path):
    return (root / path).read_text(encoding='utf-8-sig')

baseline = json.loads(content('Reports/OscuridadPisos/resources-baseline.json'))
changed = [p for p, h in baseline.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
check(not changed, f'{len(baseline)} archivos preexistentes de Resources intactos: {changed}')
scenes = json.loads(content('Reports/OscuridadPisos/escenas-base.json'))
for name in ['Piso3_V10', 'MainMenu', 'Cinematicas', 'Galeria_Personajes']:
    p = f'Assets/Scenes/{name}.unity'
    check(hashlib.sha256((root / p).read_bytes()).hexdigest() == scenes[p], f'{name} conservada sin cambios')

maps = ['Recepcion', 'Escenario1_Entrada', 'Piso3_V10', 'E3_Laboratorio', 'Escenario4_Jefe',
        'Nivel1', 'Debugscene', 'Prueba 1', 'Piso3_Modular', 'Piso3_Final_Modular']
for name in maps:
    text = content(f'Assets/Scenes/{name}.unity')
    lights = [b for b in re.split(r'(?=--- !u!)', text) if 'guid: 073797afb82c5a1438f328866b10b3f0' in b]
    check(all(float(re.search(r'm_Intensity: ([\d.]+)', b)[1]) == 0 for b in lights if 'm_LightType: 4' in b),
          f'{name}: iluminación global a 0')
    for i, b in enumerate(l for l in lights if 'm_LightType: 3' in l):
        check('m_Intensity: 3\n' in b and 'm_PointLightOuterRadius: 10\n' in b and 'm_FalloffIntensity: 0.715\n' in b,
              f'{name}: luz local {i + 1} conserva el perfil de Piso3')
    if name == 'Escenario1_Entrada':
        renderers = [b for b in re.split(r'(?=--- !u!)', text) if b.startswith('--- !u!212 ') and 'm_Materials:' in b]
        check(bool(renderers) and all('guid: a97c105638bdf8b4a8650670310a4cd3' in b for b in renderers),
              'Todas las piezas serializadas de Entrada usan el material iluminado de Piso3')

for path in ['Assets/Editor/IntegrarInicioRecepcion.cs', 'Assets/Editor/IntegrarProgresionEntrada.cs', 'Assets/Editor/IntegrarConsumiblesArmaJefe.cs']:
    check('luz.lightType = Light2D.LightType.Global; luz.intensity = 0;' in content(path),
          f'La reconstrucción conserva oscuridad: {Path(path).name}')
qa = content('Reports/OscuridadPisos/pruebas.txt') if (report / 'pruebas.txt').exists() else ''
check('ERROR ' not in qa and 'PASS Recorrido de iluminación sin errores ni excepciones' in qa and
      qa.count('render iluminado junto al jugador y negro fuera de alcance') == 8,
      'Prueba de render real en los ocho mapas principales aprobada')
(report / 'validacion.txt').write_text('\n'.join(results) + '\n', encoding='utf-8')
print('\n'.join(results))
raise SystemExit(any(r.startswith('FAIL') for r in results))
