# Laboratorio Yiba – Piso 3 (paquete V17, carpeta Piso3_V10)

## V17 – acceso a la puerta del Piso 4
- La caja `BloqueoAcceso` de `Puerta_Salida` ya no tapa el área de franjas amarillas: ahora es solo una
  franja en la base de la puerta (ancho de la puerta x 0.27). Puedes entrar al área y llegar hasta la
  puerta; detrás de ella sigue la pared, así que no se pasa por detrás.
- Se aplica solo al dar Play en las escenas del Piso 3 (o con el menú 3). Respeta dónde y a qué escala
  tengas la puerta. La puerta de entrada (Piso 2) no cambia. Las paredes no cambian.
- Simulado con tu escena: se llega hasta delante de la puerta y no hay ningún punto alcanzable detrás.
Archivos: Data/Piso3_V10_Datos.json, Editor/Piso3V10Generador.cs, Editor/Piso3V10AutoReparar.cs.

## V16.1 – el Piso 3 se metía en otras escenas (Escenario1_Entrada)
La auto-reparación al dar Play decidía que una escena era "del Piso 3" si el jugador tenía
`Piso3V10Movimiento`. JasonSmith lo tiene, así que en Escenario1 se creaba el fondo del Piso 3 (oculto
debajo de todo) y sus 242 muros invisibles: chocabas con paredes del Piso 3 en el Escenario 1.
- Ahora solo se toca una escena si su NOMBRE contiene "Piso3" (Piso3_V10, Piso3_Modular, ...).
- En cualquier otra escena, al dar Play se QUITA lo que se metió por error (fondo del Piso 3,
  01_ColisionMuros, cornisa) y lo dice en la Consola. También: menú
  `Laboratorio Yiba > Piso 3 V10 > Quitar restos del Piso 3 de esta escena`.
- Los menús 3, 4 y 9 no hacen nada fuera de las escenas del Piso 3.
Archivos: Editor/Piso3V10AutoReparar.cs, Editor/Piso3V10Generador.cs, Editor/Piso3ModularReparar.cs.

## V16 – barreras invisibles en zonas abiertas (revisado con TU escena Piso3_V10.unity)
- Se reconstruyeron todas las colisiones de tu escena (176 trazos de muro + 23 muebles + 2 puertas) y se
  simuló el movimiento: la geometría deja pasar por todo el piso.
- En el video Jason se para en medio de pasillos abiertos, justo a la altura donde terminan trazos del
  muro de 1 px de alto (había 55). Los muros eran UN PolygonCollider2D con 176 trazos que Unity triangula
  todos juntos; con trazos tan finos tocándose en T eso puede crear barreras invisibles.
- Arreglo: los muros ahora son 242 cajas simples (`Muro_###`, hijos de `01_ColisionMuros`), sin
  triangular. Coinciden con el dibujo (±1 px), pasan la prueba 48/48 y la simulación: se llega a todo.
  Se rehacen solos al dar Play (auto-reparación). Con la tecla C se ven en rojo, caja por caja.
- Diagnóstico nuevo: si Jason empuja medio segundo sin avanzar, la Consola escribe
  "[Piso3 Atasco] ... Delante: 'X' ..." con el nombre del collider que lo frena.
Archivos: Data/Piso3_V10_Datos.json, Editor/Piso3V10Generador.cs, Runtime/Piso3V10Colisiones.cs,
Piso3V10Navegacion.cs, Piso3V10Depuracion.cs, Piso3V10Movimiento.cs, Piso3V10Barrido.cs.

## V15.2 – se trababa en bordes/esquinas y vibraba pegado a las paredes
Causa: la física 2D de Unity (Box2D) deja los barridos ~0.015 DENTRO del borde de la pared y el
margen era 0.01, así que el personaje quedaba rozando la pared:
- la separación lo sacaba y el siguiente paso lo volvía a meter → **vibraba**;
- a veces el barrido empezaba "tocando" y Unity lo da como choque de frente en cualquier dirección →
  **se trababa** en bordes y esquinas.
Arreglo (`Runtime/Piso3V10Barrido.cs`):
- Margen real a paredes 0.03 (se fuerza aunque la escena tenga 0.01 guardado).
- Si el barrido empieza tocando, se usa la normal real del contacto: alejarse o deslizar ya no se bloquea.
- Esquina interior: deja de rebotar entre las dos paredes.
- Rodeo de esquinas: si vas recto y te topas con un borde de hasta 0.2 (20 px), resbala por él.
- Jason ya no "camina en el sitio" empujando una pared: se queda quieto mirando hacia ella.
Simulado sobre las 176 piezas de pared con las reglas de Box2D: separaciones por frame (vibración)
de ~1500 a 2–11 en 4000 pasos, sin entrar en paredes.
Archivos: Piso3V10Barrido.cs, Piso3V10Movimiento.cs, Piso3V10JasonAnimacion.cs, Piso3V10Zorbo.cs.

## V15.1 – LA causa de "atraviesan paredes y se atoran" (Jason y Zorbos)
Error en `Runtime/Piso3V10Barrido.cs`, función `Separar` (la que saca al personaje si queda rozando
una pared): empujaba en el sentido CONTRARIO (`pointA - pointB`). Al tocar una pared el personaje se
metía un poco más en cada frame (el doble cada vez), quedaba atorado dentro y terminaba saliendo por
el otro lado. Pasaba con Jason y con los Zorbos porque los dos usan ese mismo código.
- Corregido: `pointB - pointA` (sale hacia fuera), máximo 0.3 por frame.
- Red de seguridad nueva: si un paso terminara dentro de algo sólido y antes no lo estaba, no se da.
- Solo cambia ese archivo. Copia la carpeta encima y dale Play.

## V15 – arreglo para las escenas MODULARES (Piso3_Final_Modular / Piso3_Modular)
Tus escenas no usan el fondo dibujado V10: están hechas con **Grid + Tilemaps** (01_Suelo,
02_Detalles, 03_Paredes_Colision). Por eso los arreglos V13/V14 no las tocaban. Lo que fallaba:

| Escena | Problema | Efecto |
|---|---|---|
| Final_Modular | `Jugador_Prueba` se mueve con `Piso3TestMovement` (sin barrido de física) | atraviesa paredes y, al empujarlo la física, vibra/se atora |
| Final_Modular | no hay Jason ni Zorbos (solo los marcadores Spawn_Zorbo) | — |
| Modular | Jason y los 7 Zorbos están en y = -2 … -16 (coordenadas del fondo V10), **fuera del mapa de tiles** | ahí no hay ninguna pared; al dar Play V14 creaba además el fondo V10 encima: dos mapas mezclados |
| Modular | la cámara sigue a `Jugador_Prueba` (sin sprite, `Piso3TestPlayer`) | mueves algo invisible; Jason se mueve fuera de cámara |
| Modular | hueco en la pared del borde (x 119, y 13–18) | se sale al vacío |
| Las dos | una caja por tile sin unir | se engancha en las costuras |
| Las dos | paredes con un solo orden (10000 = tapan siempre / 5 = nunca), objetos en orden 10 y collider del tamaño de todo el sprite | sin perspectiva, no se pasa por detrás de nada (0 de 36) |

**Arreglo (automático al dar Play en una escena modular del Piso 3, o menú
`Laboratorio Yiba > Piso 3 V10 > 9 - Arreglar escena modular (tilemaps)`)**
1. Paredes: todas las celdas con collider *Grid* (cuadro completo) y unidas en un `CompositeCollider2D`
   (Polygons) con `Rigidbody2D` Static: macizas, sin costuras.
2. `03b_Bordes_Invisibles`: cierra el suelo que daba al vacío.
3. `03c_Paredes_Dibujo`: las paredes se dibujan por filas, cada una con orden -Y×100 como personajes
   y objetos. Si estás detrás (arriba) de una pared te tapa; si estás delante, tú la tapas.
   Suelo -32000, detalles -31000.
4. Objetos: collider solo en la base (35 % altos, 45–60 % mesas/cajas), orden por la base,
   `Piso3YSort` desactivado. Simulado: se puede pasar por detrás de los 38 objetos.
5. Jason (Kinematic con barrido) en `Entrada_desde_Piso2` (se crea si no existe), la cámara lo sigue,
   el jugador de prueba se desactiva y los Zorbos se colocan en los `Spawn_Zorbo` (se crean 7 si no hay).
6. Comprobación al dar Play: Consola "[Piso3 Modular] Paredes sólidas: X/X celdas".
   Tecla **C** = ver colliders.

Si vuelves a pintar paredes en `03_Paredes_Colision`, dale Play o usa el menú 9: las filas de dibujo y los
bordes se rehacen solos. No mueve tus objetos ni borra nada (el jugador de prueba solo queda desactivado).

---
# Historial V14

## V14 – colisiones ancladas al fondo real + Zorbos que rodean paredes
**Causa probable de "atraviesan paredes y se atoran"**: los muros invisibles se creaban en
coordenadas fijas (origen 0,0). Si en la escena el fondo está movido, escalado, con otro pivote/PPU o
importado más pequeño, los muros quedaban desfasados del dibujo: se atraviesan las paredes que se ven
y se choca con paredes que no se ven.

**Arreglo**
- Muros (`01_ColisionMuros`) y cornisa son ahora HIJOS del fondo (`Piso3_V10_Fondo`) y se calculan
  con su pivote, PPU y tamaño reales. Si mueves o escalas el fondo, todo va con él.
- Prueba automática al dar Play: la Consola dice "[Piso3] Colisiones alineadas con el dibujo: 48/48".
  Si sale DESALINEADAS, manda captura.
- En la vista Scene los muros se ven en rojo (Gizmos activado) sin dar Play.
- Al dar Play (o menú **0 - Revisar y arreglar escena ahora**) se escribe un **Informe** en la
  Consola: posición/escala del fondo, jugadores encontrados, scripts desactivados, Zorbos.
- Colisión de los personajes más ancha (Jason 0.44, Zorbo 0.48×1.1) para que el dibujo no se meta
  en las paredes laterales. Comprobado que siguen pasando por todos los pasillos.
- Orden de dibujo válido aunque el fondo esté en cualquier posición (fondo -32000, decals -31000,
  sombras -30000; resto por Y).

**Zorbos**: te notan si te ven (4.5) o si estás muy cerca (2); te persiguen en línea recta si te
ven y, si hay pared/mueble en medio o se atoran, siguen una ruta (A* sobre los colliders reales) que
los rodea. Simulado: 105/105 rutas encontradas detrás de paredes.

**Si sigue fallando**: manda captura de la Consola con las líneas "[Piso3 V14] Informe" y
"[Piso3] Colisiones ...", y si puedes el archivo de tu escena (.unity).

---
# Historial V13

## V13 – arreglo de Jason + Zorbos que persiguen y atacan
**Qué pasaba con Jason (quieto, recto y atravesando todo)**
- El proyecto Laboratorio4 trae `PlayerMovement`, que mueve con `transform.Translate`: ignora la física,
  por eso atravesaba paredes, objetos y Zorbos, y no anima nada.
- Además, al copiar V12 sin usar el menú 3/4, a Jason le faltaba su animador nuevo y se quedaba en la
  pose quieta.

**Cómo se arregla ahora (automático)**
- Al pulsar **Play** se revisa la escena del Piso 3 y se arregla sola: desactiva `PlayerMovement` (u
  otro script de movimiento) en Jason, le deja `Piso3V10Movimiento` con colisiones, carga todas sus
  animaciones y configura los sprites si hace falta. No mueve nada de lugar. Lo que arregla sale en la
  Consola; guarda la escena (Ctrl+S) después. Se puede apagar en el menú ("Auto-reparar al dar Play")
  o ejecutar a mano: **0 - Revisar y arreglar escena ahora**.
- Aunque no estés en el editor, `Piso3V10Movimiento` apaga cualquier otro script de movimiento del
  mismo objeto al iniciar.
- Muros: ahora un `PolygonCollider2D` sólido simple (sin CompositeCollider2D).

**Zorbos (los 7 del mapa)**
- Te ven si estás a menos de 4.5 unidades y sin muro/objeto en medio; te persiguen deslizándose por
  las paredes (no atraviesan nada); al alcanzarte atacan y te quitan 1 de vida en el fotograma del golpe.
- Si te pierden de vista van al último sitio donde te vieron. Si les disparas, te persiguen.
- Vida: Zorbo 3 balas → animación de muerte y desaparece. Jason 6 golpes → animación de muerte;
  **R** revive.
- Todo se ajusta en el Inspector (`Piso3V10Zorbo`: radios, velocidad, daño, vida, espera entre ataques).

---
# Historial V12

## Novedades V12
**Todos los personajes en carpetas, con todos sus movimientos**
`Art/Personajes/<Personaje>/<Animacion>/<n>_<Direccion>/` (0 Sur, 1 Sureste, 2 Este, 3 Noreste,
4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste). 968 fotogramas, pivote en los pies por dirección.

| Personaje | Animaciones |
|---|---|
| Jason (principal) | Quieto 8 · Caminar 4 (ahora con caminar hacia abajo) · Disparo 3 · Habilidad 4 · Muerte 4 |
| Zorbo | Quieto 8 · Caminar 8 · Ataque 4 · Muerte 4 |
| Zorbo2 (carpeta sorbos2) | Quieto 4 · Correr 4 · Ataque 4 (a distancia) · Muerte 4 |
| Milena | Quieto 8 · Correr 8 · CorrerConArma 8 · Disparo 4 · Habilidad 4 · Muerte 6 |
| Doctor Yiba | Quieto 8 · Correr 8 · Ataque 8 · BolaDePoder 4 · CampoEnergia 8 · Muerte 4 |
| Arma Laboratorio4 (`Art/Armas`) | Apuntar 8 · Disparar 8 · Girar (para recogerla) |

Todos usan `Piso3V10Personaje8Dir` (`Reproducir("Caminar", 2)`); si falta una dirección usa la
más cercana (en diagonal, este/oeste).

**Jason**: WASD/flechas caminar · Espacio/J/clic izq. disparar · **Q/K/clic der. habilidad** ·
**M probar muerte** · R reiniciar/revivir · C ver colliders.
**Zorbos**: siguen quietos en el mapa (los 7). Ya tienen `Caminar(dir)`, `Atacar(dir)`, `Morir()`
listos para la IA. Parpadean en rojo al recibir bala.

**Perspectiva paredes / personajes**
- La cornisa ya no es una sola capa encima de todo: son 91 tramos, cada uno ordenado por la Y de su
  borde inferior. Quien está en la sala de abajo (p. ej. Doctor Yiba, 2 m de alto) se dibuja DELANTE
  de la pared delgada; quien está en la sala de arriba queda detrás con los pies tapados.
- Todos los personajes: pivote en los pies, orden por Y, cápsula en los pies, sombra.

**Menús** (Laboratorio Yiba > Piso 3 V10): 1 Verificar · 2 Escena completa · 3 Reparar escena
abierta (no mueve nada) · 4 Integrar Jason y Zorbos · 5 Prefabs de personajes · **6 Galería de
personajes** (escena con todas las animaciones y direcciones) · 7 Reconfigurar sprites ·
**8 Borrar sprites antiguos de V10/V11** (úsalo después de 2 o 3).

**Cómo aplicarlo**: copia la carpeta encima de la tuya → 1 → abre tu escena → **3** (conserva tu
acomodo) o **2** (escena nueva) → **6** para revisar animaciones → **8** para limpiar.

---
# Historial V11

Diseño: estilo de la imagen de referencia (V11.1): caras de muro color óxido café oscuro con
sangre, residuo verde y arañazos; topes y contornos gris neutro con una línea clara fina y halo
gris suave. Misma distribución de objetos, puertas, Jason y Zorbos. Unity 6 (6000.x).
Solo cambian Piso3_V10_Fondo.png y Piso3_V10_MurosFrente.png: colisiones y scripts = V11.

## Qué corrige V11
1. **Esquinas que "desfasaban" al personaje**
   - Había salientes de suelo de 1 celda metidos en la pared y escalones de 8–16 px en la
     colisión (139 de 284 tramos). Se quitaron: ahora 15 tramos cortos y ninguno de 8 px.
   - El jugador ya no es Dynamic empujándose contra muros: es Kinematic y se mueve por
     "barrido" (lanza su cápsula, se detiene antes de tocar y desliza). No puede vibrar ni
     meterse en una pared.
   - La cornisa delantera ya no incluye las cuñas laterales de las esquinas que cortaban al
     personaje de lado; solo la franja que está justo debajo del suelo.
2. **Paredes sin colisión / balas que pasaban de largo**
   - Antes los muros eran líneas sin grosor. Ahora TODO lo que no es suelo es sólido
     (176 piezas unidas con CompositeCollider2D). Comprobado: 0 puntos fuera del suelo sin
     colisión y 0 puntos del suelo bloqueados por error.
   - Bala nueva `Piso3V10Bala`: barre con un rayo cada frame, así que nunca atraviesa un muro.
     Viaja por el suelo y se dibuja a la altura del arma (perspectiva 3/4).
3. **No se podía pasar por detrás de algunos objetos**
   - Los colliders cubrían 40–60 % de la altura del sprite y 5 objetos se estiraban hasta la
     pared (silla 84 %, botiquín 95 %, cajas 95 %). Ahora solo cubren la base que pisa el suelo
     (22 % muebles altos, 45 % mesas/máquinas, ~25 % tanques/barriles/plantas). Un hueco detrás de
     un objeto solo se cierra si el jugador no cabe (< 30 px). Simulado: se pasa por detrás de los 23.

## Personajes nuevos
- `Art/Personajes/Milena`: Quieto, Correr, CorrerConArma, Disparo (4 dir.), Muerte (6 dir.).
- `Art/Personajes/DoctorYiba`: Quieto, Correr, Ataque (8 direcciones).
- Fotograma a fotograma, pies alineados entre animaciones, x3 como Jason.
- Prefabs `Prefabs/Personajes/Milena` y `DoctorYiba` con `Piso3V10Personaje8Dir`
  (`Reproducir("Correr", 2)`; si falta una dirección usa la más cercana). No se colocan en la
  escena: arrástralos donde los necesites.
- Jason ahora dispara balas reales (Espacio / J / clic) en el fotograma del fogonazo.
  Los Zorbos parpadean en rojo al recibir un impacto (sin vida ni daño todavía).

## Cómo aplicarlo
1. Copia de seguridad del proyecto.
2. Copia `Assets/LaboratorioYiba/Piso3_V10` encima de la tuya (reemplazar archivos).
   No borres la carpeta antes: así se conservan las referencias (.meta).
3. Espera a que compile y usa **Laboratorio Yiba > Piso 3 V10 > 1 - Verificar archivos**.
4. Elige:
   - **Escena nueva con el diseño de la captura** → `2 - Generar escena completa (diseño boceto v4)`.
   - **Conservar tu escena acomodada** → ábrela y usa `3 - Reparar escena abierta`.
     No mueve nada: cambia los muros por los sólidos, corrige la base de los objetos y deja
     a Jason con el movimiento nuevo. El fondo y la cornisa se actualizan solos al copiar.
5. `5 - Crear prefabs de Milena y Doctor Yiba` (el paso 2 ya los crea).
6. Play: WASD/flechas, Espacio/J/clic disparar, **C** ver colliders, **R** reiniciar.

## Si en tu proyecto las balas o el jugador son de otro script
- Balas con Rigidbody2D: pon **Collision Detection = Continuous** y un collider no-trigger,
  o crea la bala con `Piso3V10Bala.Disparar(...)`.
- Si Jason también tiene otro script que dispara, desactiva uno para no lanzar dos balas.
- Si tu jugador usa otro script de movimiento con Rigidbody2D Dynamic, en esquinas puede volver
  a vibrar: usa `Piso3V10Movimiento` o copia su lógica de barrido.

## Pendiente
- Probar dentro de Unity: este paquete se verificó fuera del editor (sintaxis C# y API de
  Unity 6000.0, geometría, simulación de recorrido y de balas), no ejecutándolo.
- Vida/daño, IA de Zorbos, apertura de puertas, uso de Milena y Doctor Yiba en la historia.
