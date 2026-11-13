# Migración a la estructura raíz

Se disolvieron `Laboratorio4_Escenario1`, `Laboratorio4_Escenario3`, `Laboratorio4_Escenario4`, `Laboratorio4_Recepcion` y `LaboratorioYiba` dentro de Assets. El nombre real del cuarto escenario era `Laboratorio4_Escenario4` (con la s), y se trasladó esa carpeta.

La guía vigente está en `Assets/Documentation/EstructuraProyecto.md`.

- **3.166 archivos de contenido** reubicados mediante **36 operaciones** de Unity, sin sobrescribir archivos de destino.
- Se conservan los GUID y los archivos binarios originales. Unity 6 normaliza el formato de algunos importadores `.meta` al reimportar; el validador contrasta sus GUID con los metas versionados de origen y registra el detalle en `metas-reimportados.json`.
- Se actualizaron **19 archivos de texto** con rutas: herramientas, layouts, validadores, notas de integración y Build Settings.
- La limpieza automática del antiguo paquete de Entrada se desactivó para que no opere sobre las carpetas compartidas.
- Los contenedores vacíos y sus **12 metas** sin referencias se archivaron fuera de Assets; no se eliminó ningún archivo de contenido.
- La herramienta antigua que movía todo a `Assets/Game` quedó retirada; esa organización no corresponde a la solicitada.
- Los **271 archivos existentes de Resources** se verifican por SHA-256 contra el inventario del inicio de esta tarea.

Archivos de revisión:

| Archivo | Contenido |
|---|---|
| `plan.json` | Cada movimiento origen/destino y cada ajuste de texto |
| `before.json` | Inventario de los 7.519 archivos preexistentes de Assets |
| `backup` | Contenido anterior de los archivos de texto ajustados |
| `staged` | Contenido de reemplazo aplicado durante la migración |
| `movimientos-aplicados.txt` | Registro de las 36 operaciones completadas |
| `contenedores-archivados` | Metadatos de los contenedores vacíos retirados |
| `validacion.json` | Conservación de contenido, metas, escenas y referencias JSON |
| `verificacion-unity.txt` | Resolución de GUID y carga de escenas y prefabs en Unity |

La migración es de una sola ejecución; no se debe volver a aplicar el manifiesto sobre el proyecto ya organizado. Para revisar el resultado se utilizan los validadores, que son de lectura.

## Resultado de la revisión

- Validación estática completa aprobada; ningún archivo de contenido perdido y Resources idéntico.
- Unity resolvió los **3.552 GUID** trasladados y cargó **13 escenas** de la carpeta principal sin scripts faltantes; también importó los **25 prefabs** y verificó los generadores y las **10 escenas de compilación**.
- Al reimportar, Unity normalizó **934 metas fuera de Resources**, conservando sus GUID. El inventario completo conserva **16 escenas**, incluidas las que no están en la carpeta principal.
- Prueba real desde MainMenu: **60 comprobaciones aprobadas, sin errores**, registrada en `pruebas-play.txt`.
