"""Conservación de assets y verificación de la prueba visual de avisos/HUD."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/AvisosYVidaJefe'
results = []

def read(path):
    return (root / path).read_text(encoding='utf-8-sig')

def check(ok, message):
    results.append(('PASS ' if ok else 'FAIL ') + message)

for name, label in [('resources-baseline', 'Resources'), ('escenas-base', 'Scenes')]:
    baseline = json.loads(read(f'Reports/AvisosYVidaJefe/{name}.json'))
    changed = [p for p, h in baseline.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
    check(not changed, f'{len(baseline)} archivos de {label} intactos: {changed}')

check('EstiloAvisoRecepcion.Aplicar(objetivo, textoObjetivo)' in read('Assets/Scripts/Menus/PantallaControlesInicio.cs') and
      'EstiloAvisoRecepcion.Aplicar(aviso.panel, aviso.texto)' in read('Assets/Scripts/Menus/AvisoPuertaUI.cs'),
      'Recepción y avisos de puerta comparten la presentación')
door = read('Assets/Scripts/Controladores/PuertaNivel.cs')
check('AvisoPuertaUI.Mostrar(this, mensajeFaltaLlave, duracionMensaje)' in door and 'textoMensajeUI.gameObject.SetActive(true)' not in door,
      'La puerta muestra el aviso nuevo y mantiene oculta la franja gris')
boss = read('Assets/Scripts/Jefe/BossHealth.cs')
check('AcomodarBarraVida()' in boss and 'CalculateRelativeRectTransformBounds' in boss and 'barra.anchorMin = barra.anchorMax = barra.pivot = Vector2.one' in boss,
      'La barra del jefe usa el espacio libre del HUD y el anclaje derecho')
qa = read('Reports/AvisosYVidaJefe/pruebas.txt') if (report / 'pruebas.txt').exists() else ''
check('ERROR ' not in qa and 'PASS Avisos y vida del jefe sin errores ni excepciones' in qa and
      qa.count('fondo, borde, texto y tipografía iguales a recepción') == 4 and qa.count('distribución sin solapamiento') == 6,
      'Prueba Play de cuatro avisos y dos HUD del jefe aprobada')
(report / 'validacion.txt').write_text('\n'.join(results) + '\n', encoding='utf-8')
print('\n'.join(results))
raise SystemExit(any(r.startswith('FAIL') for r in results))
