# Laboratorio 4 – Recepción (escenario antes del Escenario 1)

## Instalar
1. Assets > Import Package > Custom Package... y elige `Laboratorio4_Recepcion.unitypackage`.
2. Al terminar, Unity pregunta si quieres construir y abrir la escena. Di **Sí**.
3. Si dijiste "Después": menú **Laboratorio 4 > Recepción > Construir escena (Recepcion)**.

Todo vive en `Assets/Laboratorio4_Recepcion/`. No toca tus escenas ni tus carpetas.
La escena `Recepcion` se agrega a Build Settings justo antes de `Escenario1_Entrada`.

## Perspectiva 3/4
- Todos los objetos y personajes tienen el pivote en los pies.
- Se dibujan en orden por la altura de sus pies: si Jason está más abajo, queda delante del objeto;
  si está más arriba, queda detrás.
- La colisión de cada objeto está solo en su base, así que Jason puede pasar por detrás de las partes altas.
- El arco detector solo choca en sus dos postes: se cruza por en medio.
- Manchas y papeles van en el piso y señales en la pared: no chocan y siempre quedan detrás de los personajes.
- La cornisa del muro sur se dibuja delante de los personajes.

## Puertas
- `Puerta_Salida_Escenario1` (norte): la zona `Salida_Escenario1` carga la escena `Escenario1_Entrada`.
  Si tu escena se llama distinto, cambia **Escena Destino** en ese objeto.
- `Puerta_Entrada` (sur): es por donde llegan Jason y Milena; está cerrada.

## Controles de prueba
WASD o flechas mueven a Jason con su animación de caminar. Milena lo sigue corriendo con la suya.
