# Laboratorio 4 – Escenario 3

## Instalar
1. Copia de seguridad del proyecto (recomendado).
2. **Assets > Import Package > Custom Package…** → `Laboratorio4_Escenario3.unitypackage` → **Import**.
3. Al terminar Unity pregunta **"¿Construir la escena y abrirla ahora?"** → *Construir y abrir*.
   También desde el menú **Laboratorio 4 > Escenario 3 > Construir escena** (o *Abrir escena*).
   La escena queda en `Escenas/E3_Laboratorio.unity` y se agrega a Build Settings.

## Nombres (no chocan con otros escenarios)
- Carpeta propia `Assets/Laboratorio4_Escenario3`. Scripts en el espacio de nombres `Laboratorio4.Escenario3`,
  todas las clases empiezan por `E3` (E3Jugador, E3Sorbo, E3Movedor…). No se modifica nada de otros escenarios.
- En la escena todo cuelga de `E3_Nivel` y empieza por `E3_`: `E3_Mapa`, `E3_ColisionMuros`, `E3_Cornisa`,
  `E3_Puertas`, `E3_Objetos`, `E3_Decorado`, `E3_Sorbos`, `E3_Personajes`, `E3_Sistema`; cámara `E3_Camara`.
  Ninguno usa los nombres que buscan las herramientas del Piso 3 (`00_Fondo`, `01_ColisionMuros`, `Muro_…`, `Entrada`).
- Si mueves `E3_Nivel`, se mueve todo junto (fondo, muros, objetos y personajes).

## Al dar Play (guardián)
En la Consola: `[Escenario 3] Paredes con colisión: 185/185 piezas.` Si faltara alguna, se repone desde
`Datos/E3_Nivel.json`. Si a Jason le pusieran `PlayerMovement` u otro script del Piso 3, se desactiva.

## Muros y balas
- 185 BoxCollider2D en `E3_Nivel/E3_Mapa/E3_ColisionMuros` (capa Default), sin huecos ni solapes.
- Muro más delgado: 0.72 u. 40 000 rayos de prueba: ninguno atraviesa.
- La bala del paquete usa un rayo por fotograma: no atraviesa muros. Si tu compañero usa Rigidbody2D:
  **Collision Detection = Continuous**.
- Las colisiones van a la altura de los pies (perspectiva 3/4): la bala choca con la base de los objetos.

## Perspectiva
- Orden de dibujo = `-y × 100` (la misma fórmula que tus objetos con Piso3V10OrdenVisual).
- La tapa plateada (cornisa) de cada muro va en tiras `E3_Cornisa_###` ordenadas por el borde de su muro:
  si Jason o un Sorbo está detrás, la tapa le cubre los pies; si está delante, se dibuja sobre el muro.
- Objetos = instancias de tus prefabs; en cada instancia la colisión cubre solo la base (se pasa por detrás).

## Jason (E3_Jason)
WASD/flechas · Espacio, J o clic: disparar · **Q o clic derecho: escudo** (3 s, recarga 5 s, no se mueve ni dispara)
· C: ver colisiones · R: reiniciar. Vida 6. Movimiento por barrido: no atraviesa ni se atora; si quedara encimado,
se separa solo.

## Sorbos (15)
9 Sorbos (3 disparos) y 6 Acorazados (4 disparos). Esperan quietos; si ven a Jason (sin muro en medio) o está muy
cerca, lo persiguen rodeando muros y objetos (ruta por rejilla). Atacan y quitan 1 de vida por golpe (el escudo
lo bloquea). Nunca atraviesan muros ni objetos.

## Puertas
`E3_PuertaInicio` (tu Puerta_Entrada) y `E3_PuertaFinal` (tu Puerta_Salida), escala 1.3, en nichos de la pared
norte. Cada una tiene `_Bloqueo` sólido: Jason llega y la topa, no la atraviesa.
La de salida muestra "Acceso al siguiente nivel". Para que cargue el siguiente nivel escribe su nombre en
`E3_PuertaFinal > E3Puerta > Escena Siguiente` (y agrégalo a Build Settings).
