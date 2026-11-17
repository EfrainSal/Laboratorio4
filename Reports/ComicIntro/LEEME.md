# Introducción visual de Laboratorio 4

## Flujo integrado

**Menú principal > confirmar personaje > IntroduccionComic > Recepcion.** El menú inicia la partida con el personaje elegido y carga la nueva escena `Assets/Scenes/IntroduccionComic.unity`, habilitada en Build Settings.

Cada recorrido muestra ocho viñetas: cuatro comunes de llegada e infiltración y cuatro de la ruta de Jason o Milena. La elección se toma de `GameManager.personajeSeleccionado`, sin cambiarla al terminar o saltar.

**Clic izquierdo:** avanza una viñeta por pulsación; mantenerlo no provoca avance continuo. El clic de confirmación del menú no omite la primera imagen. **Esc:** salta toda la secuencia desde cualquier viñeta. El último clic o Esc llevan a recepción con el fundido existente.

Abajo se muestra `siguiente >>` y `Esc · Saltar cinemática`, sobre un degradado oscuro generado en memoria. La imagen mantiene sus proporciones y los textos de navegación quedan fuera de los bocadillos. No hay encabezados de página en las viñetas visibles.

## Arte aprobado

Ubicación definitiva: **`Assets/Sprites/CinematicaIntro/`**.

- `Llegada.png`: llegada, misión, malla y entrada a recepción.
- `RutaJason.png`: Jason dentro, Milena fuera y hallazgo del ducto.
- `RutaMilena.png`: Milena dentro, Jason fuera y hallazgo del ducto.

Se reutilizan exactamente los PNG aprobados; no se regeneraron personajes ni armas. Cada página se importa como cuatro subassets Sprite, excluyendo sus 48 píxeles superiores de título y los márgenes/gutters exteriores. Se mantienen las cartelas narrativas y diálogos. Filtro Point, sin compresión ni mipmaps. El recorte se guarda en los metadatos de importación, sin modificar los píxeles del arte aprobado.

Los arrays de `IntroduccionComic` están enlazados en la escena, en orden de lectura izquierda/derecha y arriba/abajo. Los GUID de las viñetas se conservan si se vuelve a ejecutar la integración.

## Audio

**No se generó ni importó ningún archivo de audio.** Los efectos de la previsualización externa no se copiaron al proyecto. La escena no tiene AudioSource, AudioListener, clips ni dependencias de pistas. El reproductor silencia el audio persistente mientras está visible y restaura el volumen/pausa anteriores al cargar recepción. El silencio también se restaura correctamente al omitir con Esc.

## Archivos de lógica

- `Assets/Scripts/Menus/IntroduccionComic.cs`: selección de ruta, reproducción, controles, interfaz y restauración de estado.
- `Assets/Scripts/Menus/MainMenuController.cs`: nueva partida apunta a la introducción. **Continuar** conserva su escena guardada y **Reintentar** sigue yendo directamente a recepción; no se cambiaron los pisos posteriores.
- `Assets/Editor/IntegrarIntroduccionComic.cs`: menú **Laboratorio 4 > Cómic > Integrar introducción visual**. Importa los recortes, crea/enlaza la escena y habilita su entrada en Build Settings. No edita Resources ni añade audio.
- `Assets/Editor/ProbarIntroduccionComic.cs`: menú **Laboratorio 4 > Cómic > Probar rutas clic Esc y silencio**. Guardados/ranking de pruebas aislados y preferencias del usuario restauradas. Capturas y resultados en esta carpeta.

Durante el cómic la acción está detenida y el cronómetro de la partida no avanza. Al llegar a recepción se conserva la pantalla de controles existente con su propia pausa.

## Verificación final

**65 comprobaciones correctas, 0 errores/excepciones en Play Mode** (`pruebas.txt`). Se recorren las ocho viñetas de ambos personajes desde el callback real de confirmación del menú, se comprueba que un clic mantenido no repite avances, se omite con Esc desde el inicio, mitad y final, se verifica la geometría visible de ambos indicadores en cada viñeta, y se llega a recepción conservando la selección. También se comprueba silencio durante la lectura, restauración del audio previo y que Continuar no repite el cómic.

`proteccion.txt`: **271 archivos existentes de Resources sin diferencias**; **28 archivos de audio originales sin diferencias y ningún archivo de audio añadido**. Los tres PNG integrados tienen el mismo SHA-256 que los originales aprobados. Capturas PNG en esta carpeta para revisar el resultado dentro del juego.
