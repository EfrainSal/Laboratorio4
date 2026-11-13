# Integración de entrada, Sorbos, tarjeta y machete

Inicia con **Play en Assets/Scenes/MainMenu.unity** y selecciona Nueva partida. Jason y Milena aparecen en `Escenario1_Entrada` con el HUD y los controles del juego. Los personajes y Sorbos del prototipo permanecen en la escena, desactivados para evitar controladores duplicados.

## Configurar los tres spawns

Abre `Assets/Laboratorio4_Escenario1/Scenes/Escenario1_Entrada.unity`. En la jerarquía:

`GameplayEntrada > SpawnersSorbos > Spawner_lvl1 / Spawner_lvl2 / Spawner_lvl3`

Cada objeto es una instancia del prefab original de `Assets/Prefabs`. Usa el componente **EnemySpawner**, definido en **Assets/Scripts/Enemigos/EnemySpawner.cs**:

| Campo del Inspector | Función | Valores actuales |
|---|---|---|
| Cooldown Tiempo | Segundos entre oleadas mientras el jugador está cerca | lvl1: 25; lvl2/lvl3: 10 |
| Activation Radius | Distancia del jugador que activa la oleada | 7 |
| Min Enemies / Max Enemies | Cantidad aleatoria de Sorbos por oleada | 3 / 6 |
| Spawn Radius | Dispersión alrededor del centro | 3 |
| Enemy Prefabs | Tipos de Sorbos que puede generar | Los configurados en cada prefab original |
| Evitar Obstaculos | Busca posiciones libres de muros y objetos sólidos | Activado en Entrada |

Para mover un spawn, cambia **Transform > Position** de su instancia. Centros actuales: lvl1 `(13,-11,0)`, lvl2 `(24,-12,0)`, lvl3 `(43.4,-13.7,0)`. Cambia una instancia para ajustar sólo esta escena; cambia el prefab para modificar sus valores compartidos. No hace falta tocar Resources.

Se conserva el algoritmo original de proximidad, oleadas y cooldown. Sólo se añadió la identificación del spawner de origen y la comprobación opcional de obstáculos.

## Cuota, tarjeta y puerta

En `GameplayEntrada > HUDGameplay > LevelManager`, el componente **LevelManager** (`Assets/Scripts/Controladores/LevelManager.cs`) permite ajustar:

- **Enemigos Para Avanzar: 10**. El Sorbo que completa esta cuota suelta una sola tarjeta en su posición.
- **Spawners Requeridos: 0**. No se exige recorrer los tres spawns antes del dropeo.
- **Contar Solo Enemigos De Spawners: activado**. Las bajas ajenas a estas oleadas no cuentan para la tarjeta.
- **Llave Prefab: TarjetaAcceso**. Usa el sprite existente `Assets/Sprites/TarjetaAcceso.png`.

Recoge la tarjeta caminando sobre ella. En la salida del noreste, pulsa **F** o **A/Cruz del mando**, según `Gameplay/Player/Interact`. `GameplayEntrada > PuertaTransicion` reutiliza la puerta de Debugscene; su componente **PuertaNivel** (`Assets/Scripts/Controladores/PuertaNivel.cs`) apunta a **Piso3_V10**. No cambia de piso sin tarjeta. La consume una vez y conserva salud, consumibles, munición, puntos de mejora, nivel y XP.

Guardar y continuar conserva la escena y la cuota parcial de bajas. Si se guardó después de generar la tarjeta pero antes de recogerla, el siguiente Sorbo puede volver a soltarla para que el progreso no quede bloqueado. Los guardados antiguos sin nombre de escena siguen entrando en Piso3_V10.

## Consumibles y disparo

`Assets/Settings/Gameplay.inputactions`, mapa `Player`, está vinculado al **PlayerInput** de ambos prefabs:

| Acción | Teclado | Mando |
|---|---|---|
| Consumir Venda | 1 | Cruceta izquierda |
| Consumir Estimulante / adrenalina | 2 | Cruceta derecha |
| Consumir Botiquín | 3 | Cruceta arriba |

Botiquín y estimulante compartían la tecla 2. Se separaron para evitar consumir los dos a la vez. Hay tres pickups junto a la entrada; la curación requiere inventario y las vendas/botiquines requieren vida faltante. El botiquín reutiliza el gráfico del mapa en lugar del cuadrado azul del prototipo.

El subfusil conserva sus **8 sprites de apuntado y 8 sprites de disparo**, con fogonazo y retroceso al generar cada bala. Soltar el clic corta los disparos. Q cambia de arma y R recarga. No se usan animaciones corporales de disparo.

## Ajustar el machete

Abre **Assets/Prefabs/Player_Jason.prefab** o **Assets/Prefabs/Player_Milena.prefab** y selecciona el objeto raíz. Componente **ArmaIndependiente**, script **Assets/Scripts/Jugador/ArmaIndependiente.cs**, sección **Machete: postura y agarre**:

- **Invertir Filo Machete: activado** refleja el filo y compensa la diagonal del arte. Conserva la punta orientada hacia afuera y el mango en la mano; no gira el arma 180 grados.
- **Escala Machete: 0.5** reduce sólo el machete a la mitad. Usa 1 para recuperar el tamaño anterior.
- **Rotacion Reposo Machete: 225** ajusta la inclinación adicional, con compensación automática del reflejo. Se refleja también al apuntar al otro lado.
- **Posicion Mano Machete** mueve el mango respecto a la cadera.
- **Agarre Machete** define el punto del mango en coordenadas normalizadas del sprite.

El componente **MacheteAnimacion** (`Assets/Scripts/Jugador/MacheteAnimacion.cs`) tiene **Mostrar Alcance: desactivado** para ocultar el círculo amarillo. El tajo permanece animado. El tamaño visual no altera el radio físico `PlayerCombat > Attack Range`.

No se cambiaron los offsets de la cadera ni la salida de bala del subfusil.

## Dr. Yiba y verificación

El jefe ya ensamblado permanece **únicamente en Assets/Scenes/Nivel1.unity**, con su biblioteca de animación y mecánicas existentes. No se insertó en MainMenu, Entrada ni Piso3. Su escena, scripts principales y biblioteca permanecen idénticos a los del inicio de esta tarea. Debugscene también permanece intacta.

La prueba `Laboratorio 4 > Progresion > Probar recorrido completo` comprueba el recorrido con teclado y mouse virtuales, los 8 fogonazos, curación, cambio de arma, recarga, quota, guardado/continuación, tarjeta, puerta, persistencia y Game Over. Usa archivos de guardado/ranking separados en esta carpeta y no publica puntuaciones de prueba.

- Resultado: `pruebas.txt`.
- Captura real de Play: `machete-corregido.png`.
- Validación de referencias y protección: ejecutar `python Tools/validate_progresion_entrada.py` desde la raíz.
- Los **271 archivos preexistentes de Assets/Resources** se verifican con SHA-256 contra `resources-baseline.json`; no se modificó ninguno.

El menú `Integrar entrada tarjeta y machete` reconstruye la integración de Entrada con estos valores iniciales. No lo ejecutes para editar valores a mano: usa los componentes descritos arriba para conservar tus ajustes.
