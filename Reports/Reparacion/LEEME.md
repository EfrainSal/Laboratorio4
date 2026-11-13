# Reparación de mecánicas después del merge

El juego sigue arrancando desde `Assets/Scenes/MainMenu.unity`. Se conservan las posiciones de cadera de Jason (-0.04, 0.24) y Milena (-0.04, 0.25), los puntos de disparo y las referencias originales. No se movieron carpetas.

## Controles y munición

`Assets/Settings/Gameplay.inputactions` está conectado al PlayerInput de ambos prefabs. Movimiento: WASD/flechas; disparo/tajo: clic izquierdo; cambiar arma: Q; recargar: R; habilidad: espacio; interactuar: E; consumibles: 1/2/3. También hay bindings para mando. El reinicio R del prototipo ya no actúa sobre su jugador desactivado. La munición restaurada al continuar o cambiar de piso no se vuelve a rellenar en Start.

## Machete y daño

`Assets/Scripts/Jugador/MacheteAnimacion.cs` reproduce ocho fotogramas de `Assets/Sprites/Combate/Machete_Tajo.png` y anima la rotación del item. El indicador usa `PlayerCombat.attackPoint.position` y `attackRange`, exactamente como OverlapCircleAll; se muestra al equipar machete y se oculta con subfusil, pausa y muerte. El alcance considera las hitboxes de los enemigos, no sólo el centro de sus sprites. El golpe aplica daño una vez por enemigo aunque tenga varios colliders. Los proyectiles tampoco duplican un impacto entre su barrido y OnTriggerEnter2D.

`RetroalimentacionDano` está en ambos jugadores y los dos Sorbos: destello rojo para daño al jugador, ámbar para daño al enemigo y número flotante con el daño aplicado. No modifica los sprites originales ni usa animaciones de disparo del cuerpo.

Arte generado con la habilidad imagegen, modo integrado, generación y edición de transparencia. Prompt: sprite sheet pixel art 4×2, ocho fotogramas de estela de tajo de machete, sin personaje ni arma, creciente marfil/plata con borde ámbar y chispas finales; fondo y márgenes totalmente transparentes. Se corrigió el fondo del primer resultado usando imagegen. PNG final: 1774×887, alpha de esquina 0, importación Point y sin compresión.

## Rankings

`RankingManager.cs` migra ranking.dat antiguo sin duplicar sus copias de puntuación/tiempo. Registra derrotas y victorias en puntuaciones; mejores tiempos sólo para victorias. Guarda una cola de envíos independiente del top 10, con identificador único, escritura temporal y respaldo `.bak`. Reintenta desde el menú y al terminar partidas. Sólo hay una sincronización simultánea y cada confirmación vuelve a leer el archivo actual para conservar partidas nuevas.

Se conserva la URL y credencial existentes. El DTO envía los seis campos requeridos por RankingRequest: playerName, score, time, characterId (Jason=1/Milena=2), date y uploaded. Acepta respuestas HTTP 2xx; errores conservan la cola y quedan en UltimoError/UltimoEstadoHttp y la consola. Una respuesta perdida después de una inserción remota puede duplicar un envío: la API actual no documenta una clave de idempotencia.

La API real respondió HTTP 400 a cuerpos deliberadamente inválidos sin crear registros; confirmó campos requeridos y tipos numéricos. No se publicaron puntuaciones ficticias. La aceptación de envíos, los errores HTTP y los reintentos se verifican contra un servidor HTTP local con el mismo contrato; eso no equivale a confirmar una inserción en producción.

## Validación reproducible

Resultado: **70 comprobaciones aprobadas, 0 fallos**, Jason y Milena, Unity 6000.6.0f1. Resources: **271 archivos iniciales intactos**, sin GUID duplicados y posiciones del arma conservadas (`validation.json`).

Unity: `Laboratorio 4 > Reparacion > Probar mecanicas y rankings` (Ctrl+Alt+F7). Prueba Jason y Milena desde MainMenu con Keyboard/Mouse virtuales emparejados con PlayerInput, guardados y ranking aislados en esta carpeta. El servidor local simula 503, 201, 200 y 204, incluyendo una partida guardada durante un envío. La opción HTTP del editor se restaura al finalizar. `playmode.txt` contiene los resultados y los PNG muestran tajo/feedback en una zona vacía de la escena, para aislar las colisiones del ensayo. `payloads-prueba.jsonl` contiene cuerpos ficticios, sin credenciales.

`python Tools/validate_reparacion.py` compara SHA-256 de los 271 archivos iniciales de Resources, busca GUID duplicados y comprueba posiciones de armas y resultados de Play Mode. Las herramientas antiguas de integración no deben ejecutarse para ajustar el arma: estos cambios ya están aplicados a los prefabs.
