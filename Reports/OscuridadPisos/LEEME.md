# Oscuridad de Piso3 en los mapas

La referencia es la luz 2D incluida en `Assets/Prefabs/Player_Jason.prefab`, también presente en Milena: intensidad **3**, radio exterior **10**, caída **0.715** y color blanco. `Piso3_V10` no tiene una luz global que ilumine uniformemente el mapa.

Se atenuaron a **0** las luces globales de Recepción, Entrada, Nivel1 y Debugscene. Prueba 1 ya tenía su luz global apagada. Los jugadores de prototipo de Recepción, Entrada, E3, E4 y los dos mapas modulares reciben una copia de la misma luz local. Los jugadores reales que se generan desde MainMenu conservan su luz original sin duplicados. Las piezas de Entrada que usaban `Sprites/Default` pasan al material iluminado que usa Piso3 para responder a la luz.

`Piso3_V10` se conserva como referencia. MainMenu, Cinematicas, la galería, las copias de recuperación y la plantilla de escenas mantienen su presentación; las interfaces de controles, HUD y fundido no cambian.

Para ajustar el alcance o brillo, selecciona el hijo **Spot Light 2D** de los prefabs **Player_Jason / Player_Milena**, o **LuzJugador_Piso3** en los personajes de prototipo. En **Light 2D** cambia **Outer Radius**, **Intensity** o **Falloff Intensity**. Mantén los dos personajes con los mismos valores si deseas que vean la misma distancia. La oscuridad general se controla en la propiedad **Intensity** de la luz global del mapa (0 reproduce Piso3).

Herramientas: `Laboratorio 4 > Iluminación > Revisar oscuridad de los mapas`, `Aplicar oscuridad de Piso3 a los mapas`, y `Probar oscuridad en Play`. La integración aplica cambios en las escenas, sin guardar modificaciones en prefabs ni archivos de Resources. Los scripts de reconstrucción de Recepción, Entrada y Nivel1 conservan la luz global a 0.

Validación final: **42 comprobaciones aprobadas sin errores ni excepciones** en los ocho mapas principales durante Play. En cada uno se renderizaron muestras con el material iluminado: blanco junto a la luz (1.000) y negro fuera del radio (0.000). Las capturas permiten revisar el aspecto real de cada mapa y la lectura del HUD. Los dos mapas modulares se verificaron en sus archivos de escena; conservan sus controles de prueba existentes.

`python Tools/validate_oscuridad_pisos.py` aprobó **29 comprobaciones**, incluida la conservación por SHA-256 de los **271 archivos preexistentes de Resources**, de Piso3_V10 y de las escenas de presentación. Los resultados están en `pruebas.txt` y `validacion.txt`.
