# Inicio de partida en Recepción

Flujo: **MainMenu → Recepcion → recoger tarjeta junto a la salida → F en el panel → Escenario1_Entrada**. Se usan los nombres reales de los assets existentes, respetando sus mayúsculas y guiones bajos; no se crearon copias de las escenas.

En MainMenu, el componente **MainMenuController > Game Scene Name** apunta a `Recepcion`. Continuar sigue usando la escena registrada en el guardado.

## Escena y jugador

`Assets/Scenes/Recepcion.unity` contiene **GameplayRecepcion**:

- **InicioGameplayRecepcion** instancia el prefab de Jason o Milena elegido en el menú y conecta cámara y HUD. Los personajes del prototipo siguen presentes en `05_Personajes`, desactivados para evitar controles duplicados.
- **HUDGameplay** reutiliza la interfaz del juego, el inventario, la salud, el menú de pausa y Game Over.
- La salida automática del prototipo permanece desactivada; el avance lo controla el panel de la puerta al pulsar F con la tarjeta.

## Pantalla de controles

`GameplayRecepcion > PantallaControles` usa el componente **PantallaControlesInicio**:

- Oscurece la pantalla y muestra una interfaz de estilo industrial con teclas, acciones y objetivo.
- Los elementos visuales se generan por código con UI Toolkit; no requieren imágenes nuevas en Resources.
- Lee las asignaciones de `Gameplay/Player` del PlayerInput activo. Cambia entre teclado/mouse y mando al cambiar el dispositivo.
- Muestra movimiento, apuntado, ataque/disparo, cambio de arma, recarga, habilidad de Jason/Milena, interacción, consumibles y pausa.
- Detiene el tiempo, el cronómetro y los controles mientras se muestra. **Enter**, **A/Cruz**, **Start** o el botón **Entendido · Comenzar** cierran la ayuda con un fundido y restauran el juego.
- Después queda un aviso del objetivo en la parte inferior, sin bloquear el mouse.

Archivos editables:

- Comportamiento y texto: `Assets/Scripts/Menus/PantallaControlesInicio.cs`.
- Colores, tamaño y disposición: `Assets/UI/Inicio/ControlesInicio.uss`.
- Escala de pantalla y orden de dibujo: `Assets/UI/Inicio/ControlesPanelSettings.asset`.
- Duración del fundido: campo **Duracion Fundido** del componente en la escena.

## Tarjeta y panel de salida

`GameplayRecepcion > TarjetaSalidaRecepcion` reutiliza el sprite y la configuración física del prefab **TarjetaAcceso**.

- La tarjeta está en el suelo, **al lado derecho de la puerta de salida**, con 0.12 unidades de separación respecto al borde de su sprite.
- Cambia su ubicación mediante **Transform > Position** de esta instancia.
- El componente **TarjetaSalidaRecepcion** (`Assets/Scripts/Escenarios/Recepcion/TarjetaSalidaRecepcion.cs`) sólo recoge la tarjeta y la guarda una vez en el inventario. Recogerla no carga otra escena ni bloquea al jugador.
- Después acércate a **GameplayRecepcion > PanelSalidaRecepcion** y pulsa **F** (`Gameplay/Player/Interact`; A/Cruz en mando). Su componente **PuertaNivel** apunta a **Nombre Siguiente Escena: Escenario1_Entrada**.
- Sin tarjeta o fuera del alcance del panel, F no permite salir. La tarjeta se consume sólo al aceptar la interacción.
- **Avanzar Contador Piso: desactivado** mantiene Entrada como primer piso; las puertas de los otros mapas conservan el valor activado.
- Guardar y continuar conserva la tarjeta del inventario sin hacerla reaparecer en el suelo.
- Conserva personaje, vida, consumibles, munición, experiencia y mejoras. La tarjeta se consume; la tarjeta y cuota propias del siguiente piso empiezan de manera independiente.
- Recepción actúa como prólogo: no incrementa el contador del piso, por lo que Entrada conserva el índice original de la partida.

La integración modifica únicamente MainMenu y Recepcion; Entrada, Piso3 y los escenarios del jefe se mantienen intactos. Resources se verifica con SHA-256 contra el inventario de esta carpeta.

Para reproducir las pruebas: `Laboratorio 4 > Recepción > Probar flujo de inicio`. Usa archivos de guardado y ranking de prueba aislados, restaura las preferencias utilizadas y no publica puntuaciones. `pruebas.txt` contiene el resultado; los PNG muestran la interfaz real en Play. Validación adicional: `python Tools/validate_inicio_recepcion.py`.

Resultado: **70 comprobaciones del flujo inicial aprobadas sin errores**, con Jason y Milena, teclado/mouse y mando. Incluyen recogida sin transición, F sin tarjeta, F lejos del panel, proximidad, nueva pulsación de F frente a la puerta, guardado sin duplicados y conservación de datos. Las capturas de la ubicación y el indicador F están en `Reports/PanelRecepcion`.
