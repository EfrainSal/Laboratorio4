using System;

namespace Laboratorio4.Escenario3
{
    // Datos del nivel (Datos/E3_Nivel.json). Unidades de mundo, Y negativa hacia abajo, origen = E3_Nivel.
    [Serializable] public class E3Rect { public float cx, cy, w, h; }
    [Serializable] public class E3Imagen { public string archivo; public float x, y; public int orden; }
    [Serializable] public class E3Objeto { public string nombre, prefab, guid; public float x, y, cw, ch; }
    [Serializable] public class E3SorboDato { public string nombre, tipo; public float x, y; }
    [Serializable] public class E3PuertaDato
    {
        public string nombre, sprite, spriteNombre;
        public float x, y, escala;
        public E3Rect colision, aviso;
        public bool final;
    }
    [Serializable] public class E3Punto { public float x, y; }
    [Serializable] public class E3Limites { public float x0, x1, y0, y1; }

    [Serializable]
    public class E3NivelDatos
    {
        public int version;
        public float celda;
        public int columnas, filas;
        public float ancho, alto;
        public E3Imagen[] fondo;
        public E3Imagen[] cornisa;
        public E3Rect[] muros;
        public E3Objeto[] objetos;
        public E3Objeto[] decorado;
        public E3SorboDato[] sorbos;
        public E3PuertaDato[] puertas;
        public E3Punto jason;
        public E3Limites limites;
        public string suelo;
    }
}
