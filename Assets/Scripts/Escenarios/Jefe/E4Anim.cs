using System;
using UnityEngine;

namespace Laboratorio4.Escenario4
{
    [Serializable]
    public class E4FramesDir
    {
        public Sprite[] frames = new Sprite[0];
    }

    [Serializable]
    public class E4Animacion
    {
        public string nombre = "Quieto";
        public float fps = 10f;
        public bool bucle = true;
        [Tooltip("0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste")]
        public E4FramesDir[] dirs = new E4FramesDir[8];
    }

    /// <summary>
    /// Animación fotograma a fotograma en 8 direcciones. Si una animación no trae alguna dirección
    /// usa la más cercana (en las diagonales prefiere Este/Oeste).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class E4Anim : MonoBehaviour
    {
        public E4Animacion[] animaciones = new E4Animacion[0];
        public string inicial = "Quieto";
        [Range(0, 7)] public int direccion = 0;

        private SpriteRenderer sr;
        private E4Animacion actual;
        private Sprite[] frames = new Sprite[0];
        private int dirUsada = -1;
        private float t;

        public string Actual { get { return actual != null ? actual.nombre : ""; } }
        public bool Termino { get; private set; }
        public int Frame { get; private set; }
        public int TotalFrames { get { return frames != null ? frames.Length : 0; } }

        private void Awake() { sr = GetComponent<SpriteRenderer>(); }

        private void Start()
        {
            if (actual == null) Reproducir(inicial, direccion, true);
        }

        public E4Animacion Buscar(string nombre)
        {
            if (animaciones == null) return null;
            foreach (E4Animacion a in animaciones)
                if (a != null && a.nombre == nombre) return a;
            return null;
        }

        public bool Tiene(string nombre) { return Buscar(nombre) != null; }

        private static bool Hay(E4Animacion a, int d)
        {
            return a != null && a.dirs != null && d >= 0 && d < a.dirs.Length && a.dirs[d] != null
                   && a.dirs[d].frames != null && a.dirs[d].frames.Length > 0;
        }

        private static int DireccionDisponible(E4Animacion a, int d)
        {
            if (Hay(a, d)) return d;
            int mejor = -1, costoMejor = int.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                if (!Hay(a, k)) continue;
                int dist = Mathf.Abs(k - d);
                dist = Mathf.Min(dist, 8 - dist);
                int costo = dist * 10 + ((k == 2 || k == 6) ? 0 : 1);
                if (costo < costoMejor) { costoMejor = costo; mejor = k; }
            }
            return mejor;
        }

        public void Reproducir(string nombre, int dir, bool reiniciar = false)
        {
            E4Animacion a = Buscar(nombre);
            if (a == null) return;
            int dd = DireccionDisponible(a, dir);
            direccion = dir;
            if (!reiniciar && a == actual && dd == dirUsada) return;
            bool mismaAnimacion = a == actual;
            actual = a;
            dirUsada = dd;
            frames = dd >= 0 ? a.dirs[dd].frames : new Sprite[0];
            if (reiniciar || !mismaAnimacion)
            {
                t = 0f;
                Frame = 0;
                Termino = false;
            }
            else if (frames.Length > 0)
            {
                Frame = Mathf.Clamp(Frame, 0, frames.Length - 1);
            }
            Pintar();
        }

        private void Update()
        {
            if (actual == null || frames == null || frames.Length == 0 || Termino) return;
            t += Time.deltaTime * Mathf.Max(0.01f, actual.fps);
            while (t >= 1f)
            {
                t -= 1f;
                Frame++;
                if (Frame >= frames.Length)
                {
                    if (actual.bucle) Frame = 0;
                    else { Frame = frames.Length - 1; Termino = true; t = 0f; break; }
                }
            }
            Pintar();
        }

        private void Pintar()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null && frames != null && frames.Length > 0) sr.sprite = frames[Mathf.Clamp(Frame, 0, frames.Length - 1)];
        }
    }
}
