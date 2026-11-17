# Arma entre pisos y avisos de interacción

## Cambios aplicados

- `Assets/Scripts/Controladores/GameManager.cs`: `armaCuerpoACuerpoPendiente` guarda el arma actual al capturar una transición; una partida nueva limpia este estado. Sólo se captura al jugador activo de `LevelManager`, sin cambiar el ayudante.
- `Assets/Scripts/Controladores/SaveData.cs`: `armaCuerpoACuerpo` conserva el arma también al guardar y continuar. Un archivo antiguo sin este campo sigue usando subfusil, sin migración destructiva.
- `Assets/Scripts/Controladores/LevelManager.cs`: restaura el arma después de enlazar el HUD y antes de limpiar los datos pendientes. Respeta la selección al ejecutarse Start. Vida, consumibles y munición conservan su comportamiento anterior.
- `Assets/Scripts/Menus/AvisoInteraccionUI.cs`: una única interfaz por jugador activo, creada en ejecución. Muestra **Oprime F para interactuar con la puerta / leer la nota / abrir el botiquín**, con la presentación de recepción y sin ocupar la franja del objetivo. Usa el rango real de las interacciones, no añade colliders ni amplía la distancia.
- `InteraccionJugador.cs`: sigue las notas y botiquines en contacto, resuelve múltiples colliders y prioriza la nota; cierra su lectura al alejarse. Un botiquín abierto deja de anunciarse.
- `PuertaNivel.cs`: registra las puertas activas y evita que una pulsación F actúe también sobre la puerta cuando hay una nota o botiquín prioritarios. Filtra el contacto del ayudante.
- Al pulsar F sin tarjeta, el mensaje de tarjeta sustituye al texto de interacción en el mismo panel; no se muestra otra franja debajo. Al expirar, vuelve la indicación F. El aviso anterior sólo sirve de respaldo en escenas sin panel de interacción.
- Se ocultan los prompts durante pausa, lectura, derrota, victoria, mejoras y fundido entre escenas. El icono de arma del HUD sigue siendo una imagen estática orientada a la derecha.

## Comprobaciones

**68 comprobaciones correctas y 0 errores en Play Mode** (`pruebas.txt`). Incluyen Jason y Milena, sustitución del aviso F por el de tarjeta sin duplicarlo, retorno del texto F al expirar, transición real con F y tarjeta de Recepcion a Escenario1_Entrada y a Piso3_V10, machete y subfusil, vida a 63 conservada, HUD estático correcto, entrada/salida de rango, pausa y reanudación, lectura/cierre de nota, apertura de botiquín, guardado cifrado y continuación con machete, compatibilidad con guardados antiguos y limpieza al iniciar otra partida.

Menú de repetición: **Laboratorio 4 > Partida > Probar arma persistente y avisos F**. Los archivos de guardado y ranking se aíslan bajo esta carpeta y se suspende el envío automático al ranking durante la prueba. Se restaura la escena que estaba abierta. Las capturas PNG muestran los avisos reales.

Los archivos existentes de Resources y las escenas guardadas se comparan contra `resources-base.json` y `escenas-base.json`. La prueba no guarda escenas ni modifica assets de Resources.

## Material narrativo

El cómic y su audio están exclusivamente en la carpeta externa de previsualización indicada en la respuesta. No se añadió una escena, prefab, Timeline, audio o vínculo narrativo al proyecto Unity. Su integración queda pendiente de revisión del usuario.
