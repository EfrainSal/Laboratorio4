using UnityEngine;

namespace Laboratorio4.Animacion
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimadorSprites8Direcciones : MonoBehaviour
    {
        public BibliotecaPersonaje biblioteca;
        public ClipPersonaje[] Animaciones => biblioteca != null ? biblioteca.animaciones : System.Array.Empty<ClipPersonaje>();
        public string AnimacionActual => actual != null ? actual.nombre : "";
        public bool Terminada { get; private set; }
        public int FotogramaActual { get; private set; }
        public int TotalFotogramas => Frames(actual, direccion)?.Length ?? 0;
        private ClipPersonaje actual;
        private int direccion;
        private float tiempo;
        private SpriteRenderer render;

        private void Awake() { render = GetComponent<SpriteRenderer>(); Reproducir("Quieto", 0); }
        public bool Tiene(string nombre) => Buscar(nombre) != null;
        private ClipPersonaje Buscar(string nombre)
        {
            foreach (var a in Animaciones) if (a != null && a.nombre == nombre && Frames(a, 0) != null) return a;
            return null;
        }
        public void Reproducir(string nombre, int dir, bool reiniciar = false)
        {
            var siguiente = Buscar(nombre);
            if (siguiente == null) return;
            if (actual != siguiente || reiniciar) { tiempo = 0f; Terminada = false; }
            actual = siguiente;
            direccion = ((dir % 8) + 8) % 8;
            Mostrar();
        }
        private void Update()
        {
            if (actual == null) return;
            tiempo += actual.nombre == "Muerte" ? Time.unscaledDeltaTime : Time.deltaTime;
            Mostrar();
        }
        private void Mostrar()
        {
            if (render == null) render = GetComponent<SpriteRenderer>();
            var frames = Frames(actual, direccion);
            if (frames == null) return;
            int fotograma = Mathf.FloorToInt(tiempo * Mathf.Max(1f, actual.fps));
            Terminada = !actual.bucle && fotograma >= frames.Length;
            FotogramaActual = actual.bucle ? fotograma % frames.Length : Mathf.Min(fotograma, frames.Length - 1);
            render.sprite = frames[FotogramaActual];
            render.flipX = false;
        }
        public static Sprite[] Frames(ClipPersonaje clip, int dir)
        {
            if (clip?.direcciones == null) return null;
            // Las diagonales priorizan la vista lateral realmente dibujada.
            int signo = dir == 1 || dir == 5 ? 1 : -1;
            int[] orden = { 0, signo, -signo, 2, -2, 3, -3, 4 };
            foreach (int desplazamiento in orden)
            {
                int i = (dir + desplazamiento + 8) % 8;
                if (i >= clip.direcciones.Length) continue;
                var frames = clip.direcciones[i]?.frames;
                if (frames != null && frames.Length > 0) return frames;
            }
            return null;
        }
    }
}
