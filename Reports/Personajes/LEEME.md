# Integración de personajes desde MainMenu

Iniciar el juego desde `Assets/Scenes/MainMenu.unity`. Su destino continúa siendo `Piso3_V10`; se conserva la estructura original de carpetas.

Los prefabs originales `Assets/Prefabs/Player_Jason.prefab` y `Player_Milena.prefab` utilizan bibliotecas independientes en `Assets/Animations/Personajes`. Las fuentes originales contienen 664 sprites en `Assets/LaboratorioYiba/Piso3_V10/Art/Personajes`: Jason 142, Milena 270, Sorbo A 144 y Sorbo B 108. La integración actual excluye los sprites de Disparo del jugador y utiliza el arma independiente; Milena corre sin arma dibujada en el cuerpo. Las acciones con vistas cardinales usan la vista dibujada más cercana al mirar en diagonal.

`Enemy_Melee.prefab` corresponde al Sorbo A (arte Zorbo): patrulla, persecución y golpe. `Enemy_Ranged.prefab` corresponde al Sorbo B (arte Zorbo2): patrulla, persecución, retirada, carga y proyectil. Los ataques conectan durante el fotograma de impacto. El daño detiene temporalmente las IA por retroceso; la muerte cancela ataques y conserva el sprite hasta terminar la secuencia.

La escena de destino tenía un Jason de pruebas que ignoraba la selección del menú. El componente `InicioPersonajesDesdeMenu` mantiene sus puntos de aparición, desactiva los prototipos durante Play e instancia el prefab elegido por `GameManager`. La cámara y LevelManager se enlazan con el jugador nuevo. Los enemigos aparecen mediante los spawners originales del proyecto. El escenario conserva su distribución y su material visual. La integración vigente del HUD, Game Over, arma y spawners se describe en `Reports/Gameplay/LEEME.md`.

Las herramientas están en **Laboratorio 4 > Personajes**:

- **Integrar para MainMenu:** enlaza bibliotecas, configura los cuatro prefabs y conecta la escena de destino. Guardar el trabajo abierto antes de ejecutarlo.
- **Validar prefabs:** comprueba los estados y los fotogramas de las ocho orientaciones.
- **Probar MainMenu con Jason y Milena:** ejecuta dos partidas de validación desde el menú y regresa al MainMenu en modo edición. No crea escenas alternativas. Usa el mismo método de inicio de run y la misma escena de destino que el menú; evita pulsar Confirmar para conservar el archivo de guardado personal. La comprobación de IA aísla temporalmente los actores del mobiliario durante la prueba.

Resultados: `prefabs.txt` y `mainmenu-play.txt` contienen las comprobaciones aprobadas. `protection.json` confirma que los 271 archivos que existían en `Assets/Resources` conservan su SHA-256 y que no hay GUID duplicados.

Tras deshacer la intervención anterior permanecía una copia de `Assets/Game` que duplicaba clases y hacía que Unity reasignara GUID de los originales. Ese resto se conservó fuera de Assets en `Backups/IntegracionAnterior/Game`; se recuperaron los GUID originales, sin escribir en Resources. No se ejecutó la migración de carpetas anterior ni se modificó el arte existente.
