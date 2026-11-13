# Tarjeta y panel de recepción

La Tarjeta de Acceso está en el suelo al lado derecho de la puerta de salida de `Assets/Scenes/Recepcion.unity`.

1. Recoge la tarjeta caminando sobre ella. Se añade una sola unidad al inventario y permaneces en Recepcion.
2. Acércate al panel de la puerta. El indicador muestra **F · USAR TARJETA**.
3. Pulsa **F** para consumir la tarjeta y cargar `Escenario1_Entrada`.

Sin tarjeta o lejos del panel no se puede avanzar. Guardar y continuar conserva la tarjeta sin duplicarla. La recepción no incrementa el contador del primer piso.

- Ubicación editable: `GameplayRecepcion > TarjetaSalidaRecepcion > Transform`.
- Destino y zona de interacción: `GameplayRecepcion > PanelSalidaRecepcion > PuertaNivel`.
- Recogida: `Assets/Scripts/Escenarios/Recepcion/TarjetaSalidaRecepcion.cs`.
- Puerta: `Assets/Scripts/Controladores/PuertaNivel.cs`, con **Avanzar Contador Piso** desactivado exclusivamente en recepción.

La interfaz de controles conserva su diseño y hoja de estilos; sólo cambiaron las instrucciones del objetivo. La tarjeta se coloca a 0.12 unidades del borde derecho del sprite de la puerta y 0.55 unidades por debajo de su borde inferior.

Validación: **70 comprobaciones de recepción** con Jason y Milena y **60 de regresión** en Entrada, todas aprobadas. Se verificaron la ruta física hasta la salida, la recogida sin transición, el rechazo sin tarjeta o a distancia, la pulsación de F frente al panel, el guardado sin duplicados y la conservación de datos.

Los inventarios SHA-256 de esta carpeta protegen los 271 archivos existentes de Resources, las escenas de Entrada/Piso3/jefes y la hoja de estilos. Ejecuta `python Tools/validate_panel_recepcion.py` para comprobarlos. Las capturas muestran la ubicación real y el indicador de F en Play.
