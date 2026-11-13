# Laboratorio 4 – Escenario 1 (Entrada) · versión 6

## Qué pasó con las paredes (y por qué no volverá a pasar)
La herramienta del Piso 3 (`Laboratorio Yiba > Piso 3 V10`) hacía cosas al dar Play en cualquier escena:
1. En versiones anteriores a la V16.1 metía en el Escenario 1 el fondo y los muros del Piso 3 y le cambiaba el
   movimiento a Jason (añadía `Piso3V10Movimiento` y desactivaba su controlador).
2. La V16.1 limpia esos "restos" buscándolos por nombre: `00_Fondo`, `01_ColisionMuros`, cornisa…
   Pero las paredes del Escenario 1 también se llamaban `01_ColisionMuros`, así que **las borró**. Por eso
   Jason atraviesa todo: la escena se quedó sin colisión de paredes.

Arreglo de la v6:
- Todos los grupos del Escenario 1 empiezan por `E1_` (`E1_Mapa`, `E1_ColisionMuros`, `E1_Objetos`…) y las
  paredes llevan el componente `MurosEscenario1`. Ninguna herramienta del Piso 3 los confunde ya.
- **GuardianEscenario1** (en `SeleccionPersonaje`) revisa la escena al dar Play, antes que nada:
  - si faltan colisiones de pared, las vuelve a crear (74 piezas, desde `Datos/escenario1_layout.json`);
  - quita restos del Piso 3 que se hayan colado;
  - deja a Jason con su `ControladorJugador`, desactiva scripts `Piso3...` que le hayan puesto y
    restaura su cápsula (0.34 × 0.16).
  - En la Consola: `[Escenario 1] Paredes con colisión: 74/74 piezas.`
- Nada de este paquete toca escenas del Piso 3, y la herramienta de reparación se niega a actuar fuera de
  `Escenario1`.

## Instalar
**Assets > Import Package > Custom Package...** → `Laboratorio4_Escenario1_v6.unitypackage` → **Import**.
- Opción A (recomendada): importa todo. La escena `Escenario1_Entrada` se reemplaza por la buena.
- Opción B (si cambiaste cosas a mano en la escena y quieres conservarlas): en la ventana de importación
  desmarca `Scenes/Escenario1_Entrada.unity`, importa, abre tu escena y usa
  **Laboratorio 4 > Reparar Escenario 1 (escena abierta)**: repone las paredes, quita restos del Piso 3,
  arregla a Jason, añade el guardián y guarda.
Comprobación: dale Play, pulsa **C**: las paredes deben verse con contorno rojo.

## Paredes y disparos
Todo lo que no es suelo tiene colisión + marco alrededor del mapa. 40 000 rayos de prueba: ninguno atraviesa.
Pared más delgada 0.72 u. Bala con Raycast: no atraviesa. Bala con Rigidbody2D: Collision Detection = Continuous.
Muros en `E1_Mapa/E1_ColisionMuros` (capa Default).

## Jason Smith
WASD/flechas · Espacio/J/clic disparar · **Q o clic derecho: escudo** · C colisiones · R reiniciar.

## Otros
Objetos = instancias de tus prefabs. 6 Sorbos y 4 Acorazados. Perspectiva: cornisa delante del jugador también
en las paredes entre salas (v5). Escala 100 px = 1 u, personajes ×3.
