# Liberación del disparo automático

La reparación está en `Assets/Scripts/Jugador/PlayerShooting.cs`.

La acción `Attack` es de tipo Button y los prefabs usan PlayerInput con Send Messages. El código instalado de Input System 1.20.0 (`PlayerInput.OnActionTriggered`) envía performed y sólo comunica canceled para acciones de tipo Value. Por eso `OnAttack(InputValue)` almacenaba true al pulsar, pero no recibía el mensaje que debía ponerlo en false al soltar.

Se reprodujo el error antes de modificar el arma: la acción ya indicaba clic liberado y aun así el cargador bajó de 27 a 25 balas (`before-fix.txt`).

El arma ahora consulta `Attack.IsPressed()` del mapa activo del PlayerInput del propio personaje en cada Update. Comprueba que el input y la acción estén habilitados; al cambiar de mapa vuelve a buscar la acción. Se eliminó el estado isShooting que podía quedar retenido. Se conserva la cadencia, munición, recarga, proyectiles, boca de disparo y bloqueo de gameplay existentes. No se modificaron acciones ni bindings.

La revisión parte de MainMenu y ejecuta pulsaciones/liberaciones de un mouse virtual emparejado con PlayerInput, con Jason y Milena. Comprueba mantener el clic, soltar durante más de dos intervalos de disparo, volver a pulsar, clic breve, pausa y reanudación, cambio a machete y vuelta, y desactivar/reactivar PlayerInput. Usa guardados aislados y no publica puntuaciones. Resultados en `playmode.txt`.

Resultado en Unity 6000.6.0f1: **26 comprobaciones aprobadas**, sin errores ni excepciones. Se comprueba que la munición permanezca igual desde la primera actualización posterior a soltar el botón y durante los intervalos siguientes. Tras liberar el clic, Jason conserva 27/27 balas esperadas y Milena 12/12. Los 271 archivos iniciales de Resources, los prefabs, las acciones y la configuración del machete conservan sus hashes anteriores.

Repetir en Unity: `Laboratorio 4 > Disparo > Probar pulsacion y liberacion` (Ctrl+Alt+F9).

Los hashes de `validation.json` comparan los archivos iniciales de Resources, prefabs, acciones y configuración del machete con su estado previo a esta reparación.
