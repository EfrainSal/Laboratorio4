using System;
using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    [Serializable]
    public class Piso3V10FramesDireccion
    {
        public Sprite[] frames = new Sprite[0];
    }

    [Serializable]
    public class Piso3V10Animacion
    {
        public string nombre = "Quieto";
        public float fps = 10f;
        public bool bucle = true;
        [Tooltip("0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste")]
        public Piso3V10FramesDireccion[] direcciones = new Piso3V10FramesDireccion[8];
    }

    /// <summary>
    /// Animador fotograma a fotograma en 8 direcciones para TODOS los personajes
    /// (Jason, Zorbo, Zorbo2, Milena, Doctor Yiba, arma).
    /// Las animaciones salen de las carpetas  Art/Personajes/&lt;Personaje&gt;/&lt;Animacion&gt;/&lt;d&gt;_&lt;Direccion&gt;/.
    /// Si una animación no trae alguna dirección (p. ej. Caminar de Jason tiene 4) usa la más cercana,
    /// dando prioridad a izquierda/derecha en las diagonales.
    /// Uso:  personaje.Reproducir("Caminar", 2);   // 2 = Este
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Piso3V10Personaje8Dir : MonoBehaviour
    {
        public Piso3V10Animacion[] animaciones = new Piso3V10Animacion[0];
        public string animacionInicial = "Quieto";
        [Range(0, 7)] public int direccion = 0;
        [Tooltip("Repite también las animaciones sin bucle (galería de pruebas).")]
        public bool forzarBucle = false;
        public float velocidad = 1f;

        public string AnimacionActual { get { return actual != null ? actual.nombre : ""; } }
        /// <summary>True cuando una animación sin bucle (Disparo, Ataque, Muerte...) llegó a su último fotograma.</summary>
        public bool Terminada { get; private set; }
        public int FotogramaActual { get; private set; }
        public int TotalFotogramas { get { Sprite[] f = actual != null ? Frames(actual, direccion) : null; return f != null ? f.Length : 0; } }

        private SpriteRenderer sr;
        private Piso3V10Animacion actual;
        private float tiempo;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (actual == null) Reproducir(animacionInicial, direccion, true);
        }

        private void OnValidate()
        {
            sr = GetComponent<SpriteRenderer>();
            Piso3V10Animacion a = Buscar(animacionInicial);
            Sprite[] f = a != null ? Frames(a, direccion) : null;
            if (sr != null && f != null && f.Length > 0) sr.sprite = f[0];
        }

        public bool Tiene(string nombre)
        {
            if (animaciones == null) return false;
            foreach (Piso3V10Animacion a in animaciones) if (a != null && a.nombre == nombre) return true;
            return false;
        }

        public void Reproducir(string nombre, int dir8, bool reiniciar = false)
        {
            Piso3V10Animacion a = Buscar(nombre);
            if (a == null) return;
            dir8 = ((dir8 % 8) + 8) % 8;
            if (a != actual || reiniciar) { tiempo = 0f; Terminada = false; FotogramaActual = 0; }
            actual = a;
            direccion = dir8;
            Mostrar();
        }

        private void Update()
        {
            if (actual == null) return;
            tiempo += Time.deltaTime * velocidad;
            Mostrar();
        }

        private void Mostrar()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            Sprite[] f = Frames(actual, direccion);
            if (f == null || f.Length == 0) return;
            int i = Mathf.FloorToInt(tiempo * Mathf.Max(0.01f, actual.fps));
            if (actual.bucle || forzarBucle) i %= f.Length;
            else if (i >= f.Length) { i = f.Length - 1; Terminada = true; }
            FotogramaActual = i;
            sr.sprite = f[i];
        }

        private Piso3V10Animacion Buscar(string nombre)
        {
            if (animaciones == null) return null;
            foreach (Piso3V10Animacion a in animaciones)
                if (a != null && a.nombre == nombre) return a;
            return animaciones.Length > 0 ? animaciones[0] : null;
        }

        /// <summary>Fotogramas de la dirección pedida o de la más cercana (en diagonal prefiere este/oeste).</summary>
        public static Sprite[] Frames(Piso3V10Animacion a, int dir8)
        {
            if (a == null || a.direcciones == null || a.direcciones.Length == 0) return null;
            bool ladoDerecho = dir8 == 1 || dir8 == 3;
            int[] orden = ladoDerecho ? new[] { 0, 1, -1, 2, -2, 3, -3, 4 } : new[] { 0, -1, 1, -2, 2, -3, 3, 4 };
            // diagonales: SE(1)->E(2), NE(3)->E(2), NO(5)->O(6), SO(7)->O(6)
            if (dir8 == 3) orden = new[] { 0, -1, 1, -2, 2, -3, 3, 4 };
            if (dir8 == 7) orden = new[] { 0, -1, 1, -2, 2, -3, 3, 4 };
            if (dir8 == 5) orden = new[] { 0, 1, -1, 2, -2, 3, -3, 4 };
            foreach (int k in orden)
            {
                int d = (((dir8 + k) % 8) + 8) % 8;
                if (d < a.direcciones.Length && a.direcciones[d] != null && a.direcciones[d].frames != null && a.direcciones[d].frames.Length > 0)
                    return a.direcciones[d].frames;
            }
            return null;
        }
    }
}
