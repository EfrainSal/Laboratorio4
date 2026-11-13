"""Comprueba que morir espera al botón y que Resources/escenas no se alteran."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
report = root / 'Reports/FlujoGameOver'
results = []

def read(path):
    return (root / path).read_text(encoding='utf-8-sig')

def check(ok, message):
    results.append(('PASS ' if ok else 'FAIL ') + message)

for name, label in [('resources-baseline', 'Resources'), ('escenas-base', 'escenas')]:
    base = json.loads(read(f'Reports/FlujoGameOver/{name}.json'))
    changed = [p for p, h in base.items() if not (root / p).is_file() or hashlib.sha256((root / p).read_bytes()).hexdigest() != h]
    check(not changed, f'{len(base)} archivos de {label} conservados: {changed}')

gm = read('Assets/Scripts/Controladores/GameManager.cs')
muerte = gm.split('public void TerminarRunPorMuerte', 1)[1].split('public void CompletarRunVictoria', 1)[0]
check('OnRunTerminada?.Invoke' in muerte and 'CargarEscena(' not in muerte and 'IniciarNuevaRun(' not in muerte and 'StartCoroutine(' not in muerte,
      'La muerte termina la run y emite el resumen sin cargar ni reiniciar')
screen = read('Assets/Scripts/Menus/DeathScreenController.cs')
evento = screen.split('private void OnRunTerminada', 1)[1].split('[ContextMenu', 1)[0]
retry = screen.split('private void OnRetry()', 1)[1].split('private void OnMenu()', 1)[0]
check('StartCoroutine(ShowRoutine())' in evento and 'RegresandoTrasDerrota' not in screen and 'RegresarARecepcion' not in gm,
      'La pantalla recibe la derrota y ejecuta la secuencia original de Game Over')
check('CargarEscena("Recepcion", false)' in retry and 'gm.IniciarNuevaRun(character)' in retry and '_retryRequested' in retry,
      'Reintentar reinicia en Recepcion y protege contra dobles pulsaciones')
check('PauseMenuController.PauseBlocked = false' in screen, 'Reintentar libera la pausa anterior antes del fundido')
qa = read('Reports/PersistenciaLootDerrota/pruebas.txt')
check('ERROR ' not in qa and qa.count('PASS Game Over visible y acción pausada antes de reintentar') == 3 and
      qa.count('PASS Esperar en Game Over no cambia de escena ni duplica la derrota') == 3 and
      qa.count('PASS Sólo Reintentar inicia el fundido') == 3,
      'Tres derrotas en Play esperan el botón real de UI Toolkit antes de cargar Recepcion')
progress = read('Reports/ProgresionEntrada/pruebas.txt')
check('ERROR ' not in progress and 'PASS Reintentar en Game Over devuelve a Recepcion' in progress,
      'Regresión de puerta, vida y reintento aprobada')
(report / 'validacion.txt').write_text('\n'.join(results) + '\n', encoding='utf-8')
print('\n'.join(results))
raise SystemExit(any(r.startswith('FAIL') for r in results))
