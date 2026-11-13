# Postura y agarre del machete

La configuración está aplicada en `Assets/Prefabs/Player_Jason.prefab` y `Assets/Prefabs/Player_Milena.prefab`.

En Unity, abre el prefab y selecciona su objeto raíz. En el componente **Arma Independiente**, sección **Machete: postura y agarre**, puedes modificar:

| Campo | Valor inicial | Qué ajusta |
| --- | --- | --- |
| Rotacion Reposo Machete | 225 | Inclinación de la hoja respecto al apuntado; se refleja automáticamente al apuntar a la izquierda. Al apuntar horizontalmente, 225 deja la hoja levantada. |
| Posicion Mano Machete | X 0.25, Y 0 | Ubicación del mango respecto a la cadera. X avanza hacia el objetivo; Y es perpendicular al apuntado. |
| Agarre Machete | X 0.78, Y 0.80 | Punto del mango dentro del rectángulo del sprite, en coordenadas normalizadas de 0 a 1 desde abajo a la izquierda. Es el punto fijo alrededor del que gira el arma. |

Los valores del prefab se guardan para futuras partidas. Si los pruebas en Play, anótalos y aplícalos al prefab después de salir de Play.

`Assets/Scripts/Jugador/ArmaIndependiente.cs` compensa el pivote central del sprite para sostenerlo por el mango. Refleja tanto la postura como el giro del tajo al apuntar a la izquierda. `MacheteAnimacion` calcula el tajo antes de aplicar el transform del arma para evitar un fotograma de retraso. Se conserva la animación configurada por el usuario (0 a -120 grados), el círculo oculto, las alturas de cadera, tamaño de arma y puntos de disparo/ataque existentes. El sprite y su importador no se modificaron.

Validación en Unity 6000.6.0f1 desde MainMenu: **26 comprobaciones aprobadas**, sin errores ni excepciones. Se verificaron el agarre, la hoja levantada, el sentido del golpe, el círculo oculto y el regreso al subfusil con su salida de bala. Durante esta revisión se ralentiza temporalmente el tajo a un segundo para poder inspeccionarlo; la duración guardada en los prefabs se conserva. Capturas de reposo y tajo para ambos personajes y ambas direcciones en esta carpeta.

Repetir revisión: `Laboratorio 4 > Machete > Revisar postura y agarre` (Ctrl+Alt+F8). Usa guardados aislados y no publica puntuaciones.

`validation.json` compara por SHA-256 los 271 archivos protegidos de Resources y comprueba que la configuración previa del arma permanece igual.
