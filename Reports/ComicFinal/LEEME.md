# Cómic y combate final: configuración actual

Las viñetas aprobadas permanecen en `Assets/Sprites/CinematicaFinal`, sin modificar sus PNG ni añadir audio al cómic.

## Reencuentro y fundidos

En `Assets/Scenes/Escenario4_Jefe.unity`, selecciona **CinematicaFinal → ComicFinal**:

- `Desplazamiento Ayudante`: posición del protagonista no elegido respecto del punto de aparición del nivel. Valor actual: **(1.8, -0.6)**.
- `Radio Proximidad`: radio del CircleCollider2D Trigger alrededor del compañero. Valor actual: **1 unidad**. Se crea como `TriggerReencuentroCompanero` al iniciar la escena.
- Las cuatro referencias de sprite permiten cambiar las imágenes. La variante inicial sigue la selección de Jason/Milena del GameManager.

Acercarse al compañero activa: gameplay → fundido negro → reencuentro. Clic izquierdo o Esc terminan la lectura con otro fundido hacia el combate. El rugido y la sacudida comienzan al iluminarse la escena.

Derrotar a Yiba activa: fundido negro → últimas palabras → clic y fundido negro → escape → clic y fundido → pantalla existente de Incursión Completada sobre negro. Esc omite la secuencia restante. Las estadísticas y el ranking se registran una sola vez al vencer al jefe.

`Assets/Scripts/Controladores/TransicionEntrePisos.cs` comparte la misma cortina y curva con los cambios de piso. Sus duraciones actuales son 0.35 segundos para oscurecer y 0.4 para iluminar. Los fundidos del cómic no recargan la escena. Descargar el mapa durante un fundido cancela la cortina y restaura el tiempo/audio previos.

## Audio e impacto de Yiba

Selecciona **Jefe_DoctorYiba → BossAI**:

- `Sfx Rugido Inicio`, `Sfx Ataque Pesado`, `Sfx Recarga Energia`, `Sfx Disparo` e `Sfx Invocacion` reutilizan los archivos que ya existían en `Assets/Audio/SFX`.
- `Radio Escucha`: 36 unidades XY; `Volumen Cercano`: 0.8. La distancia se mide al jugador real, sin incluir la Z de la cámara.
- El AudioSource propio usa salida **SFX**, mezcla 2D, prioridad 64, sin Play On Awake, sin Loop y sin Mute. Los disparos del jugador conservan prioridad 0. Sólo se reproduce una voz del jefe a la vez.
- `Duracion Inicio`: 1.1 s. `Duracion Sacudida`: 0.7 s. `Intensidad Sacudida`: 0.16. El componente `SacudidaCamaraJefe` retira el desplazamiento al terminar y antes de cada nuevo seguimiento de cámara.

El cómic sigue siendo silencioso. No se generaron, copiaron ni alteraron archivos de audio. Se respetan los ajustes del mezclador del usuario.

## Navegación y profundidad

En **Jefe_DoctorYiba → NavegacionJefe**:

- `Celda`: 0.3 unidades; `Margen`: 0.07; `Intervalo Ruta`: 0.45 s.
- A* de ocho direcciones, sin cortar diagonales por esquinas. La rejilla es exclusiva de este jefe/escena y usa las colisiones físicas estáticas reales.
- El collider del jefe cubre sus pies: tamaño local (0.6, 0.32), offset (0.144, 0.17), material sin fricción. Su altura visual ya no funciona como cuerpo físico.
- La ruta se actualiza al moverse el objetivo o detectarse un atasco; los casts del cuerpo impiden atravesar muros. El margen de planificación no impide salir de un roce.
- **E4OrdenY** actualiza el orden por la Y de los pies. `Sprite Sort Point` permanece en Pivot. El jefe ya no se dibuja con orden cero detrás de todos los muros.

## Verificación

Menú de Unity: **Laboratorio 4 → Cómic → Probar clímax desenlace y victoria**.

Resultados y capturas en `Reports/ComicFinal`. Trayectorias alrededor de las coberturas físicas en `Reports/YibaFlujo/recorrido-*.csv`. La prueba observa negro completo en los fundidos, ambas variantes, reproducción y señal de audio, sacudida sin deriva, rodeo de dos muros, profundidad y cierre original de victoria. Usa archivos independientes para guardado/ranking y suspende los envíos de prueba a la API.

Los hashes de `Reports/YibaFlujo/protegidos-base.json` permiten verificar que los archivos existentes de Resources y audio permanecen intactos.
