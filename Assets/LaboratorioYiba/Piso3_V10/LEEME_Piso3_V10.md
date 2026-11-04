# Laboratorio Yiba – Piso 3 V10 (fondo rehecho + puertas con bloqueo)

Base: diseño del Piso 3 de la V8 (misma distribución de salas, pasillos, huecos negros y accesos)
y los sprites de la plantilla V8 (37 objetos + 2 puertas). Probado con Unity 6 (6000.x), 2D.

## Qué cambia respecto a V8/V9
- **Fondo nuevo, dibujado limpio**: suelo de placas metálicas pixel art (32 px), muros con
  **cara frontal** en las paredes norte (profundidad 3/4) y cornisa superior en el resto.
  Sin muebles ni puertas pintadas, sin parches. Sangre, grietas y franjas de peligro
  reproducen las posiciones del arte conceptual. Se retiró la mancha central (decisión previa).
- **Zonas de puerta rediseñadas**: entrada (nicho izquierdo, desde Piso 2) y salida (bahía
  superior derecha, hacia Piso 4) tienen pared frontal propia, suelo rebajado, flechas de
  señalización y franja de umbral. Ya no quedan restos de las puertas antiguas.
- **Puertas = sprites independientes** con hijo `BloqueoAcceso` (BoxCollider2D) que impide
  entrar a la zona de la puerta mientras `cerrada = true`.
- **Profundidad**: `Piso3_V10_MurosFrente.png` es la cornisa de las paredes sur; se dibuja
  delante del jugador (orden 20000). Objetos y jugador se ordenan por la Y de sus pies.
- **Colisiones**: 6 contornos EdgeCollider2D siguiendo la base visible de cada pared; en las
  paredes sur el límite entra 8 px bajo la cornisa (los pies quedan tapados, el cuerpo no).
  Muebles con BoxCollider2D en su huella; los que están pegados a una pared se extienden
  hasta ella para que no queden huecos donde esconderse.
- Sombra de contacto suave bajo cada objeto (hijo `Sombra`).

## Cómo importarlo
1. Haz copia de seguridad de tu proyecto.
2. Copia la carpeta `Assets/LaboratorioYiba/Piso3_V10` dentro de tu `Assets/LaboratorioYiba/`.
   (No necesitas V9; no comparte clases con versiones anteriores.)
3. Espera a que compile. Si aparece Safe Mode, revisa el **primer** error rojo
   (puede venir de generadores viejos de otras versiones).
4. Menú **Laboratorio Yiba > Piso 3 V10 > 1 - Verificar archivos**.
5. Menú **Laboratorio Yiba > Piso 3 V10 > 2 - Generar escena**.
   Crea prefabs en `Prefabs/` y la escena `Scenes/Piso3_V10.unity`.
6. Play. WASD/flechas mover, **C** ver colliders, **R** reiniciar posición.

## Ajustes manuales útiles
- Mover/borrar un objeto: selecciónalo en `02_Objetos` (cada uno es un prefab independiente).
- Collider de un objeto: BoxCollider2D del objeto (pivote en los pies).
- Abrir una puerta desde código: `GetComponent<Piso3V10Puerta>().Abrir();`
- Escala: 100 px = 1 unidad. Mapa 3888×1872 px (38.88 × 18.72 unidades).

## Pendiente (no está hecho)
- Validar dentro de Unity con el protocolo de pruebas (este paquete no se ejecutó en el editor).
- Apertura/animación de puertas y cambio de piso; enemigos Zorbos; iluminación dinámica.
- Las rejillas/alcantarillas del concepto no se colocaron (se pidió retirarlas antes).

---
## Actualización V10.1 (Jason + Zorbos + franja retirada)
- Fondo: se quitó solo la franja amarilla horizontal marcada (pasillo superior izquierdo,
  junto al hueco central). El resto del PNG es idéntico píxel a píxel.
- `Art/Personajes/Jason`: quieto 8 direcciones, caminar derecha/izquierda/arriba (9 fotogramas),
  disparo frente/derecha/izquierda (9 fotogramas). `Art/Personajes/Zorbo`: 8 orientaciones.
  Importación: Point, 33.33 PPU (se ven a x3), pivote en los pies.
- `Piso3V10JasonAnimacion`: animación fotograma a fotograma. Espacio / J / clic = disparo
  (solo animación, Jason se detiene mientras dispara).
- `Piso3V10Zorbo`: Zorbo estático con collider; orientación elegible en el Inspector (0-7).

### Cómo aplicarlo SIN perder tu acomodo de objetos
1. Copia la carpeta encima de la anterior (reemplaza archivos).
2. Abre tu escena del Piso 3 (la que ya acomodaste).
3. Menú **Laboratorio Yiba > Piso 3 V10 > 4 - Integrar Jason y Zorbos (escena abierta)**.
   Convierte el jugador actual en Jason (misma posición) y crea `07_Zorbos` con 7 Zorbos
   en zonas libres, evitando tus objetos. Puedes volver a ejecutarlo: reemplaza solo los Zorbos.
4. **No uses "2 - Generar escena"**: reconstruye la escena desde cero y perderías tu acomodo.

Nota: la carpeta de Jason no trae "caminar hacia abajo"; al bajar se muestra el sprite quieto
Sur. Si después haces esos fotogramas, ponlos en `Art/Personajes/Jason/CaminarAbajo/` y vuelve
a ejecutar el paso 4.
