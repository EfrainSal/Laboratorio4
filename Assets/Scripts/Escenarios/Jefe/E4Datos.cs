using System;

namespace Laboratorio4.Escenario4
{
    // Datos del escenario (Data/E4_Datos.json). Coordenadas en unidades de Unity: x hacia la derecha, y negativa hacia abajo.
    [Serializable] public class E4Pos { public float x; public float y; }
    [Serializable] public class E4Rect { public float x; public float y; public float w; public float h; }   // centro y tamaño

    [Serializable]
    public class E4ObjetoDato
    {
        public string nombre;
        public string sprite;
        public float x;
        public float y;
        public float colW;
        public float colH;
        public float colY;
        public bool colision = true;
        public bool sombra = true;
    }

    [Serializable] public class E4CornisaDato { public string sprite; public float x; public float y; public int orden; }
    [Serializable] public class E4DirDato { public int d; public string[] frames; }

    [Serializable]
    public class E4AnimDato
    {
        public string personaje;
        public string nombre;
        public float fps;
        public bool bucle;
        public E4DirDato[] dirs;
    }

    [Serializable]
    public class E4Datos
    {
        public int version;
        public float ancho;
        public float alto;
        public string fondo;
        public E4CornisaDato[] cornisas;
        public E4Rect[] muros;
        public E4ObjetoDato[] objetos;
        public E4ObjetoDato puertaEntrada;
        public E4ObjetoDato puertaSalida;
        public E4Pos jason;
        public E4Pos milena;
        public E4Pos doctor;
        public E4Pos luzSalida;
        public E4Rect zonaJefe;
        public E4Rect zonaSalida;
        public E4AnimDato[] animaciones;
        public string spriteSombra;
        public string spriteBala;
        public string spriteImpacto;
        public string spriteBola;
        public string spriteImpactoBola;
        public string spriteEscudo;
        public string spriteLuzRoja;
        public string spriteLuzVerde;
    }
}
