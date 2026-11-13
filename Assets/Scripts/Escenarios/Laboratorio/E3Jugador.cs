using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Jason Smith. WASD/flechas caminar, Espacio/J/clic disparar, Q/clic derecho escudo.
    /// Su colisión es una cápsula en los pies (perspectiva 3/4): el cuerpo puede tapar la cara de un muro
    /// o pasar por detrás de un objeto, pero los pies nunca entran en muros, objetos ni puertas.
    /// </summary>
    [RequireComponent(typeof(E3Movedor), typeof(E3Animador), typeof(E3Vida))]
    public class E3Jugador : MonoBehaviour
    {
        public float velocidad = 2.6f;
        [Tooltip("Segundos entre disparos.")]
        public float cadencia = 0.28f;
        public int danioBala = 1;
        public float duracionEscudo = 3f;
        public float recargaEscudo = 5f;
        public Sprite spriteBala;
        public Material materialSprites;

        public bool EscudoActivo { get { return Time.time < escudoHasta; } }
        public float RecargaRestante { get { return Mathf.Max(0f, escudoListo - Time.time); } }
        public bool Muerto { get { return muerto; } }

        private E3Movedor mov;
        private E3Animador anim;
        private E3Vida vida;
        private Collider2D[] propios;
        private int dir = 0;
        private float proxDisparo, finDisparo;
        private float escudoHasta = -1f, escudoListo = 0f;
        private bool muerto;

        private void Awake()
        {
            mov = GetComponent<E3Movedor>();
            anim = GetComponent<E3Animador>();
            vida = GetComponent<E3Vida>();
            propios = GetComponents<Collider2D>();
            vida.AlMorir += Morir;
        }

        private void Update()
        {
            if (muerto) return;
            bool escudo = EscudoActivo;
            vida.invulnerable = escudo;

            if (!escudo && Time.time >= escudoListo && E3Entrada.Escudo())
            {
                escudoHasta = Time.time + duracionEscudo;
                escudoListo = escudoHasta + recargaEscudo;
                vida.invulnerable = true;
                anim.Reproducir("Habilidad", E3Animador.Dir4(E3Animador.Vector(dir)), true);
                mov.Detener();
                return;
            }
            if (escudo)
            {
                // Mientras dura el escudo Jason no se mueve ni dispara.
                anim.Reproducir("Habilidad", E3Animador.Dir4(E3Animador.Vector(dir)));
                return;
            }

            Vector2 m = E3Entrada.Movimiento();
            if (m.sqrMagnitude > 1f) m.Normalize();
            bool camina = m.sqrMagnitude > 0.01f;
            if (camina)
            {
                dir = E3Animador.Dir8(m);
                mov.Mover(m * velocidad * Time.deltaTime);
            }
            else mov.Detener();

            if (Time.time >= proxDisparo && E3Entrada.Disparando()) Disparar();

            if (Time.time < finDisparo) anim.Reproducir("Disparo", dir);
            else if (camina) anim.Reproducir("Caminar", dir);
            else anim.Reproducir("Quieto", dir);
        }

        private void Disparar()
        {
            proxDisparo = Time.time + cadencia;
            finDisparo = Time.time + 0.3f;
            anim.Reproducir("Disparo", dir, true);
            Vector2 v = E3Animador.Vector(dir);
            GameObject b = new GameObject("E3_Bala");
            SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = spriteBala;
            if (materialSprites != null) sr.sharedMaterial = materialSprites;
            E3OrdenY o = b.AddComponent<E3OrdenY>();
            E3Bala bala = b.AddComponent<E3Bala>();
            o.desfase = Mathf.RoundToInt(bala.alturaVisual * 100f);
            bala.danio = danioBala;
            bala.Iniciar(mov.Posicion + new Vector2(0f, 0.08f) + v * 0.22f, v, propios);
        }

        private void Morir()
        {
            muerto = true;
            vida.invulnerable = true;
            anim.Reproducir("Muerte", E3Animador.Dir4(E3Animador.Vector(dir)), true);
            E3Hud.Mensaje("Jason ha caído. Pulsa R para reiniciar.", 999f);
        }
    }
}
