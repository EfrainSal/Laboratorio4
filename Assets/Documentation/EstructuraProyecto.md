# Estructura del proyecto

La raíz de trabajo de Unity es **Assets**. El contenido de las cinco carpetas de importación se integró en las carpetas principales existentes, sin crear una raíz paralela `Game`.

| Contenido | Ubicación |
|---|---|
| Escenas | `Assets/Scenes` |
| Scripts de juego existentes | `Assets/Scripts` (Jugador, Enemigos, Controladores, Menus, Jefe, Animacion...) |
| Scripts propios de los mapas importados | `Assets/Scripts/Escenarios/{Entrada,Laboratorio,Jefe,Recepcion,Piso3}` |
| Herramientas de construcción y reparación de mapas | `Assets/Editor/Escenarios/{Entrada,Laboratorio,Jefe,Recepcion,Piso3}` |
| Prefabs principales de jugadores, enemigos, consumibles y spawns | `Assets/Prefabs` |
| Prefabs de objetos y puertas del mapa Piso3 | `Assets/Prefabs/Escenarios/Piso3` |
| Sprites propios de cada mapa y sus variantes | `Assets/Sprites/Escenarios/{Entrada,Laboratorio,Jefe,Recepcion,Piso3}` |
| Imágenes de referencia | `Assets/Sprites/Referencias` y las referencias originales dentro del arte de Piso3 |
| Layouts y datos JSON de mapas y animaciones | `Assets/Settings/Escenarios/{Entrada,Laboratorio,Jefe,Recepcion,Piso3}` |
| Manuales importados y archivo original de objetos | `Assets/Documentation/Escenarios` y `Assets/Documentation/PaquetesOriginales` |

`Assets/Animations`, `Audio`, `Materials`, `UI`, `UI Toolkit`, `TextMesh Pro`, `Resources` y los demás directorios de la raíz conservan su estructura. Las variantes de arte se mantienen separadas por escenario para que los nombres repetidos no sobrescriban assets distintos.

## Escenas trasladadas

- `Assets/Scenes/Recepcion.unity`
- `Assets/Scenes/Escenario1_Entrada.unity`
- `Assets/Scenes/E3_Laboratorio.unity`
- `Assets/Scenes/Escenario4_Jefe.unity`
- `Assets/Scenes/Piso3_V10.unity`
- `Assets/Scenes/Galeria_Personajes.unity`

`MainMenu`, `Debugscene`, `Nivel1` y las demás escenas principales mantienen sus nombres y ubicaciones. Inicia la partida desde **Assets/Scenes/MainMenu.unity**. La reubicación no cambia el recorrido del juego ni añade al jefe a ningún mapa.

## Referencias y mantenimiento

Los movimientos se realizaron mediante `AssetDatabase.MoveAsset`, conservando los archivos `.meta` y sus GUID. Se actualizaron las rutas de los integradores, los constructores, los layouts JSON, las herramientas de validación y la lista de escenas de compilación. Las llamadas que cargan escenas por nombre siguen usando los mismos nombres.

Los **271 archivos preexistentes de Assets/Resources** permanecen idénticos. No se trasladó contenido hacia esa carpeta ni se modificó su contenido.

Para verificar la integración:

- Unity: `Laboratorio 4 > Estructura > Verificar referencias raiz`.
- Desde la raíz del proyecto: `python Tools/validate_root_structure.py`.
- Prueba del juego: `Laboratorio 4 > Progresion > Probar recorrido completo` (guardado y ranking de prueba aislados, sin publicar puntuaciones).

El historial está en **Reports/ReestructuracionRaiz**: `plan.json` relaciona las rutas originales y nuevas; `before.json` contiene el inventario SHA-256; `validacion.json` y `verificacion-unity.txt` guardan los resultados. Los únicos restos de los contenedores originales son sus metas de carpeta sin referencias, archivados fuera de Assets en `contenedores-archivados`.

Los manuales de cada paquete en `Documentation/Escenarios` se conservan como documentación de origen; sus instrucciones de instalación antiguas no deben usarse para volver a crear las carpetas de importación. Esta guía describe la organización vigente.
