# Consumibles, feedback del arma y jefe de Nivel1

El juego sigue iniciándose desde **MainMenu**. El jefe está conectado únicamente en **Assets/Scenes/Nivel1.unity**, sobre el objeto original ahora llamado **Jefe_DoctorYiba**. MainMenu y Piso3_V10 conservan su contenido anterior.

## Curación

Los dos prefabs usan `Assets/Settings/Gameplay.inputactions`, mapa Player, con los callbacks originales de PlayerInventory. Las rutas de teclado de los consumibles apuntaban a `digit1/digit2/digit3`; el layout instalado de Keyboard expone esos controles como `1/2/3`. Se corrigieron las rutas conservando los nombres de acciones y las asignaciones:

- **1:** venda; **2:** botiquín; **3:** estimulante.
- Mando: cruceta izquierda, arriba y derecha, respectivamente.
- Las referencias de inventario a salud, movimiento y combate se resuelven en Awake. LevelManager sigue conectando los contadores y las barras del HUD.
- Se necesita tener el consumible en inventario. Vendas y botiquines no se gastan con la vida llena. Pausa, muerte y victoria bloquean su uso. El estimulante mantiene su curación y bufo existentes.

## Disparo del arma independiente

`ArmaIndependiente` tiene conectados **8 sprites de reposo y 8 de disparo**, uno por dirección. Las otras 16 imágenes de `Laboratorio4_Escenario1/Sprites/Arma` son copias byte a byte de estas mismas imágenes; el detalle está en `validation.json`. No son ocho fotogramas temporales que deban reproducirse girando el arma: representan ocho orientaciones.

`PlayerShooting.Shoot` notifica un disparo confirmado. Se muestra su sprite de fogonazo durante 0.12 s y un retroceso de 0.035 unidades, con retorno a reposo. Ambos valores se pueden retocar en **Arma Independiente**, Inspector. No se usan los sprites de disparo del cuerpo de Jason/Milena. Se mantiene la solución de disparo sostenido: soltar el clic detiene la creación de proyectiles.

Las imágenes de disparo tienen un lienzo de 64×64 y las de reposo de 48×48. Una comparación de píxeles confirmó que el arma conserva exactamente su dibujo en la esquina superior izquierda; el margen adicional queda abajo y a la derecha. El transform compensa ese margen y los pivotes para que el cuerpo del arma no salte durante el fogonazo (`registro-sprites.json`). Se conservan los parámetros anteriores de cadera, machete, tamaño, punto de ataque y boca de disparo.

## Doctor Yiba

Se reutilizan **BossAI, BossHealth y BossBullet**, el proyectil existente y ambos prefabs Sorbo. `AnimacionJefeDinamica` conecta las acciones con `Assets/Animations/Personajes/DoctorYiba.asset`: Quieto, Correr, Ataque, BolaDePoder, CampoEnergia y Muerte. La biblioteca incorpora los **288 sprites** disponibles y resuelve las direcciones ausentes mediante el sistema existente de ocho direcciones.

- Fase 1: persecución y zarpazo.
- Fase 2: zarpazo y anillo de proyectiles, al bajar al 66% de vida.
- Fase 3: incorpora invocaciones y zarpazo letal, al bajar al 33%.
- HUD de salud del jefe con referencias explícitas y colocado para no tapar la vida del jugador.
- Al morir se detiene la IA, se retiran los proyectiles y se reproduce Muerte. La victoria y su puntuación se registran una sola vez.
- La pantalla de victoria bloquea el gameplay. Continuar vuelve a MainMenu, ya que la escena `FinalCinematic` referenciada anteriormente no existe en el proyecto.

Se ajustaron sprite, material, collider, punto de disparo, iluminación y barra del jefe exclusivamente en Nivel1. La escena conserva su entorno provisional; esta entrega ensambla el combate y las animaciones, no construye un mapa nuevo. Las rutas de Build Settings se actualizaron para apuntar a las escenas existentes tras el merge.

**Para jugar el combate:** fuera de Play, usa `Laboratorio 4 > Jefe > Jugar Nivel1 desde MainMenu`. Esta opción inicia MainMenu y después abre la escena independiente con GameManager y el jugador preparados. Los ajustes de salud, velocidad, cadencia, ataques e invocaciones están en los componentes de Jefe_DoctorYiba dentro de Nivel1.

## Verificación

Resultado final en Unity 6000.6.0f1: **133 comprobaciones aprobadas, 0 fallos y sin errores/excepciones de runtime**. Los **271 archivos protegidos** conservan sus hashes; MainMenu, Piso3_V10 y los parámetros anteriores del arma/machete también permanecen intactos. No hay GUID duplicados. La única escena que referencia la nueva animación de Doctor Yiba es Nivel1.

`Laboratorio 4 > Integracion > Probar consumibles arma y jefe` prueba ambos personajes con teclado, mouse y mando virtuales emparejados con PlayerInput. Carga el mapa desde MainMenu y después Nivel1, con guardados/ranking aislados: no publica puntuaciones ficticias ni modifica el guardado personal.

Verifica curación, consumo, bufo, pausa, todas las vistas de reposo/disparo, registro entre lienzos, liberación del clic, conservación del machete y ausencia del jefe en el mapa de MainMenu. En Nivel1 comprueba persecución, zarpazo con daño real, fases, proyectiles físicos, invocaciones, muerte, victoria, ranking y retorno al menú. Los ajustes de duración/cadencia usados para observar las animaciones durante la revisión son temporales y no se guardan en los prefabs.

Resultados: `playmode.txt`. Capturas: Jason/Milena disparando y combate/victoria de Doctor Yiba. `python Tools/validate_consumibles_arma_jefe.py` verifica Resources por SHA-256, escenas, configuración previa, GUIDs y resultados de Play Mode. Los **271 archivos iniciales de Resources** deben permanecer intactos.
