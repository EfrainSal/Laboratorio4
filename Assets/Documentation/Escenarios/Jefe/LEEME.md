# Laboratorio 4 · Escenario 4 · Laboratorio del Doctor Yiba (jefe final)

Probado para Unity 6 (6000.6.x), 2D / URP 2D. Todo vive en `Assets/Laboratorio4_Escenario4`, con el prefijo `E4_`
en la escena y el namespace `Laboratorio4.Escenario4`: **no toca nada de los escenarios 1, 2 y 3**.

## Cómo importarlo
1. Haz copia de seguridad del proyecto.
2. **Assets → Import Package → Custom Package…** y elige `Laboratorio4_Escenario4_Jefe.unitypackage`. Importa todo.
3. Al terminar de compilar aparece **"¿Construir la escena del jefe final y abrirla ahora?"** → *Construir*.
   Si no aparece: **Laboratorio 4 → Escenario 4 (Jefe) → Construir escena**.
4. Dale Play. En la Consola debe salir `[Escenario 4] Escena lista … Paredes con colisión: 66/66 piezas`.
   Si algo falla, usa **Laboratorio 4 → Escenario 4 (Jefe) → Verificar archivos**.

La escena queda en `Scenes/Escenario4_Jefe.unity` y se agrega sola a Build Settings
(para que la puerta del escenario 3 pueda cargarla con el nombre `Escenario4_Jefe`).

## Actualizaciones
Al importar una versión nueva, la escena del Escenario 4 (esté donde esté, aunque la hayas movido o copiado) se
reconstruye sola al abrirla o al dar Play, y la Consola lo avisa. También puedes forzarlo con
**Laboratorio 4 → Escenario 4 (Jefe) → Construir escena** con la escena abierta.

## Controles
WASD/flechas caminar · Espacio, J o clic izquierdo disparar · **Q, K o clic derecho: escudo** (3 s, recarga 6 s) ·
C ver colisiones · R reiniciar.

## Combate
- **Doctor Yiba** (80 de vida): se activa cuando Jason se acerca. Se mueve por su plataforma y alterna
  - **Bola de poder** (2 de daño). Con menos de la mitad de vida lanza 3 en abanico y se mueve más rápido.
  - **Campo de energía** (2 de daño en área) si te pegas a él.
- **Los 4 muros de cobertura son bajos y tienen perspectiva 3/4**: si te pones detrás, el muro tapa la mitad de abajo del
  personaje y se ve de la cintura para arriba; si estás delante, te dibujas encima del muro. Su colisión es solo su base real.
- **Los 4 muros de cobertura detienen las bolas de poder** (igual que los demás muros y los objetos).
  El escudo de Jason también las bloquea.
- **Jason** (10 de vida, jugable) y **Milena** (8 de vida): Milena sigue a Jason y le dispara al Doctor
  cuando lo tiene a tiro y sin muros de por medio. Si se queda atorada lejos, aparece junto a Jason.
- Cuando el Doctor muere, la luz de la **Puerta_Salida** pasa de roja a verde. Al llegar frente a ella aparece
  el mensaje final, o carga la escena que pongas en `E4_Puerta_Salida_01 → E4PuertaSalida → Escena Destino`.

## Qué hay en la carpeta
| Carpeta | Contenido |
|---|---|
| `Art/Fondo` | `E4_Fondo.png` (piso, paredes muy oxidadas, sangre, plataforma) y `Cornisa/` (7 tramos de la cornisa sur y los 4 muros de cobertura como sprites propios, ordenados por su base para la perspectiva 3/4). |
| `Art/Objetos`, `Art/Puertas` | Tus sprites (copias con GUID nuevo, para no chocar con los que ya tienes). |
| `Art/Personajes` | Jason, Milena y Doctor Yiba con todas sus animaciones y los pies como pivote. |
| `Art/Efectos` | Bala, impactos, bola de poder, escudo y luces de la puerta. |
| `Data/E4_Datos.json` | Posiciones de todo, 66 piezas de muro, colisiones de objetos, zonas del jefe y de la salida. |
| `Scripts` | `E4Jugador`, `E4Companera`, `E4Jefe`, `E4Proyectil`, `E4Vida`, `E4Anim`, `E4OrdenY`, `E4PuertaSalida`, `E4Camara`, `E4HUD`, `E4Depuracion` y el constructor en `Editor/`. |

## Detalles técnicos
- Escala: 100 px = 1 unidad (fondo y objetos); personajes a 33.33 px/unidad (x3), como en los otros escenarios.
  Mapa: x 0 … 43.44, y 0 … −32.58.
- Orden de dibujo: fondo −32000 → sombras −31000 → objetos, personajes y cornisa por `-y * 100`.
- Los objetos solo chocan en su base (puedes pasar por detrás). Los que están pegados a la pared cierran el hueco hasta ella.
- Los proyectiles revisan todo su recorrido en cada fotograma: no atraviesan muros.
- Si en tu proyecto URP 2D los sprites se ven negros, agrega **Light 2D → Global Light 2D** a la escena.
- `Milena/Disparo/4_Norte` de tus sprites la muestra de frente: si quieres, cámbiala en `Art/Personajes/Milena/Disparo/4_Norte`.
