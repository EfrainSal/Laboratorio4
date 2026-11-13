# Botón de controles y fundidos entre pisos

El botón **Entendido · Comenzar** de la ayuda comparte la clase **btn-menu**, la hoja `Assets/UI/MainMenuStyles.uss` y el generador de degradado de MainMenu. Conserva el borde lateral, la tipografía, el resaltado rojo, el desplazamiento de foco y los estados hover/pulsado/desactivado del menú. Su clase `intro-continue` sólo ajusta el espacio dentro del panel de controles.

El componente `PantallaControlesInicio` de Recepcion referencia la hoja del menú en **Estilos Menu**. La fila de botiquín continúa eliminada.

## Cambio de piso

`Assets/Scripts/Controladores/TransicionEntrePisos.cs` administra una cortina negra única que permanece entre escenas:

1. Bloquea controles y pausa, detiene el tiempo y oscurece la escena actual.
2. Carga el destino de forma asíncrona con la cortina completamente negra.
3. Espera la inicialización del jugador, el HUD y el mapa.
4. Ilumina la escena de destino y libera los controles y los clics.

El fundido usa tiempo no escalado y cubre el HUD del juego, UI Toolkit y las interfaces IMGUI de los mapas del prototipo. Las solicitudes repetidas se ignoran durante la misma transición. No se alteran las condiciones de tarjeta, jefe derrotado o proximidad de las puertas.

Lo utilizan `PuertaNivel`, la salida original de Recepcion, `PuertaSalida` de Entrada, `E3Puerta` del laboratorio y `E4PuertaSalida` del mapa del jefe. MainMenu mantiene su inicio en Recepcion y la tarjeta de recepción sigue requiriendo **F frente al panel**.

Duraciones por defecto: **0.35 s para oscurecer** y **0.40 s para iluminar**, más el tiempo de carga del mapa. Son los campos públicos `duracionOscurecer` y `duracionIluminar` del componente creado en Play bajo `DontDestroyOnLoad > TransicionEntrePisos`; sus valores iniciales se editan en el script.

Validación: `Laboratorio 4 > Recepción > Probar flujo de inicio` verifica el estilo compartido y registra capturas antes, durante y después del fundido. `Laboratorio 4 > Progresion > Probar recorrido completo` comprueba la transición Entrada → Piso3 y la conservación de los datos. Los guardados y rankings de prueba están aislados y no se publican resultados.

Ejecuta `python Tools/validate_fundidos_pisos.py` para comprobar los enlaces y la conservación de los 271 archivos preexistentes de Resources. Los inventarios de esta carpeta son de lectura; no se modifica Resources.

Resultado final: **86 comprobaciones aprobadas** en `Reports/InicioRecepcion/pruebas.txt` y **60 comprobaciones aprobadas** en `Reports/ProgresionEntrada/pruebas.txt`, ambas sin errores ni excepciones. Las capturas `controles-estilo-menu-0.png`, `negro-al-cargar-0.png` e `iluminando-0.png` muestran el botón y el ciclo del fundido. Se conservó el ajuste concurrente del usuario a la posición de la puerta de Entrada, documentado en `cambios-concurrentes.json`.
