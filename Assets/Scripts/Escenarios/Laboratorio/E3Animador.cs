using System;
using UnityEngine;

namespace Laboratorio4.Escenario3
{
    [Serializable] public class E3Direccion { public Sprite[] frames = new Sprite[0]; }

    [Serializable]
    public class E3Clip
    {
        public string nombre;
        public float fps = 10f;
        public bool bucle = true;
        // 0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste
        public E3Direccion[] dirs = new E3Direccion[8];
    }

    /// <summary>Animación por fotogramas en 8 direcciones (usa los sprites de tus carpetas de personajes).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class E3Animador : MonoBehaviour
    {
        public E3Clip[] clips = new E3Clip[0];
        public string actual = "Quieto";
        public int direccion = 0;

        public bool Terminado { get; private set; }
        public int Frame { get; private set; }

        private SpriteRenderer sr;
        private E3Clip clip;
        private Sprite[] frames;
        private float t;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            Reproducir(actual, direccion, true);
        }

        public bool Tiene(string nombre) { return Buscar(nombre) != null; }

        private E3Clip Buscar(string nombre)
        {
            if (clips == null) return null;
            foreach (E3Clip c in clips) if (c != null && c.nombre == nombre) return c;
            return null;
        }

        public void Reproducir(string nombre, int dir, bool reiniciar = false)
        {
            E3Clip c = Buscar(nombre);
            if (c == null) return;
            Sprite[] f = Frames(c, dir);
            if (f == null) return;
            bool mismo = c == clip;
            direccion = dir;
            if (mismo && f == frames && !reiniciar) return;
            clip = c; frames = f; actual = nombre;
            if (reiniciar || !mismo) { t = 0f; Frame = 0; Terminado = false; }
            Pintar();
        }

        /// <summary>Fotogramas de la dirección pedida o, si no existe, de la más cercana.</summary>
        public static Sprite[] Frames(E3Clip c, int dir)
        {
            if (c == null || c.dirs == null) return null;
            int[] cerca = { 0, 1, -1, 2, -2, 3, -3, 4 };
            foreach (int d in cerca)
            {
                int k = ((dir + d) % 8 + 8) % 8;
                if (k < c.dirs.Length && c.dirs[k] != null && c.dirs[k].frames != null && c.dirs[k].frames.Length > 0)
                    return c.dirs[k].frames;
            }
            return null;
        }

        private void Update()
        {
            if (clip == null || frames == null || frames.Length == 0) return;
            t += Time.deltaTime;
            int n = Mathf.FloorToInt(t * Mathf.Max(0.01f, clip.fps));
            if (clip.bucle) n %= frames.Length;
            else if (n >= frames.Length) { n = frames.Length - 1; Terminado = true; }
            Frame = n;
            Pintar();
        }

        private void Pintar()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null && frames != null && frames.Length > 0) sr.sprite = frames[Mathf.Clamp(Frame, 0, frames.Length - 1)];
        }

        /// <summary>Dirección de 8 a partir de un vector (0 = Sur, sentido antihorario).</summary>
        public static int Dir8(Vector2 v)
        {
            float a = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            return ((Mathf.RoundToInt((a + 90f) / 45f) % 8) + 8) % 8;
        }

        /// <summary>Dirección de 4 (0, 2, 4, 6) a partir de un vector.</summary>
        public static int Dir4(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x > 0f ? 2 : 6;
            return v.y > 0f ? 4 : 0;
        }

        public static Vector2 Vector(int dir)
        {
            float a = (dir * 45f - 90f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
    }
}
