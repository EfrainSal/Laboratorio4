using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Sorbo (normal o acorazado). Espera quieto; si ve a Jason (sin muro ni objeto en medio) o lo oye muy cerca,
    /// lo persigue. Con vía libre va directo; si hay obstáculos sigue una ruta por la rejilla. Usa el mismo
    /// movimiento por barrido que Jason, así que nunca atraviesa muros ni objetos. Al alcanzarlo ataca y
    /// le quita vida en el golpe (el escudo lo bloquea).
    /// </summary>
    [RequireComponent(typeof(E3Movedor), typeof(E3Animador), typeof(E3Vida))]
    public class E3Sorbo : MonoBehaviour
    {
        public bool acorazado = false;
        public float velocidad = 1.6f;
        public float radioVision = 5f;
        public float radioOido = 1.8f;
        public float distanciaAtaque = 0.55f;
        public float alcanceGolpe = 0.85f;
        public int danio = 1;
        public float pausaEntreAtaques = 0.6f;
        [Tooltip("Fotograma de la animación de ataque en el que se aplica el golpe.")]
        public int frameGolpe = 5;

        private enum Estado { Quieto, Persiguiendo, Atacando, Muerto }
        private Estado estado = Estado.Quieto;
        private E3Movedor mov;
        private E3Animador anim;
        private E3Vida vida;
        private Transform jugador;
        private E3Vida vidaJugador;
        private Collider2D[] propios;
        private int dir;
        private string clipMover = "Caminar";
        private readonly List<Vector2> ruta = new List<Vector2>();
        private int idx;
        private float proxRuta, olvido, proxAtaque;
        private Vector2 ultimoVisto;
        private bool golpeDado;
        private ContactFilter2D filtro;
        private readonly RaycastHit2D[] impactos = new RaycastHit2D[12];

        private void Awake()
        {
            mov = GetComponent<E3Movedor>();
            anim = GetComponent<E3Animador>();
            vida = GetComponent<E3Vida>();
            propios = GetComponents<Collider2D>();
            filtro = new ContactFilter2D();
            filtro.useTriggers = false;
        }

        private void Start()
        {
            E3Jugador j = FindAnyObjectByType<E3Jugador>();
            if (j != null) { jugador = j.transform; vidaJugador = j.GetComponent<E3Vida>(); }
            clipMover = anim.Tiene("Caminar") ? "Caminar" : "Correr";
            vida.AlMorir += Morir;
            vida.AlRecibir += Herido;
            dir = anim.direccion;
            anim.Reproducir("Quieto", dir);
            proxRuta = Time.time + Random.value * 0.5f;
        }

        private void Herido(int restante)
        {
            if (jugador == null || estado == Estado.Muerto) return;
            ultimoVisto = jugador.position;
            olvido = 8f;
            if (estado == Estado.Quieto) estado = Estado.Persiguiendo;
        }

        private void Update()
        {
            if (estado == Estado.Muerto || jugador == null) return;
            Vector2 p = mov.Posicion;
            Vector2 pj = jugador.position;
            float d = Vector2.Distance(p, pj);
            bool vivo = vidaJugador != null && !vidaJugador.Muerto;
            bool ve = vivo && d < radioVision && Libre(p, pj, 0f);
            if (vivo && (ve || d < radioOido))
            {
                ultimoVisto = pj;
                olvido = 8f;
                if (estado == Estado.Quieto) estado = Estado.Persiguiendo;
            }

            if (estado == Estado.Atacando) { Atacar(d); return; }
            if (estado != Estado.Persiguiendo) return;

            if (!vivo) { Parar(); return; }
            olvido -= Time.deltaTime;
            if (olvido <= 0f) { Parar(); return; }

            if (d <= distanciaAtaque)
            {
                dir = E3Animador.Dir4(pj - p);
                if (Time.time >= proxAtaque)
                {
                    estado = Estado.Atacando;
                    golpeDado = false;
                    anim.Reproducir("Ataque", dir, true);
                }
                else anim.Reproducir("Quieto", dir);
                mov.Detener();
                return;
            }

            Vector2 objetivo = ve ? pj : ultimoVisto;
            Vector2 paso;
            if (ve && Libre(p, pj, 0.2f))
            {
                paso = pj - p;
                ruta.Clear();
            }
            else
            {
                if (Time.time >= proxRuta || idx >= ruta.Count)
                {
                    proxRuta = Time.time + 0.5f;
                    E3Rejilla.Ruta(p, objetivo, ruta);
                    idx = 0;
                }
                while (idx < ruta.Count && Vector2.Distance(p, ruta[idx]) < 0.1f) idx++;
                paso = idx < ruta.Count ? ruta[idx] - p : Vector2.zero;
                if (!ve && idx >= ruta.Count && Vector2.Distance(p, ultimoVisto) < 0.3f) olvido = Mathf.Min(olvido, 1.5f);
            }

            if (paso.sqrMagnitude > 0.0004f)
            {
                Vector2 v = paso.normalized;
                dir = E3Animador.Dir8(v);
                mov.Mover(v * Mathf.Min(velocidad * Time.deltaTime, paso.magnitude));
                anim.Reproducir(clipMover, dir);
            }
            else anim.Reproducir("Quieto", dir);
        }

        private void Parar()
        {
            estado = Estado.Quieto;
            ruta.Clear();
            anim.Reproducir("Quieto", dir);
        }

        private void Atacar(float d)
        {
            mov.Detener();
            if (!golpeDado && anim.Frame >= frameGolpe)
            {
                golpeDado = true;
                if (d <= alcanceGolpe && vidaJugador != null) vidaJugador.Danio(danio);
            }
            if (anim.Terminado)
            {
                proxAtaque = Time.time + pausaEntreAtaques;
                estado = Estado.Persiguiendo;
                anim.Reproducir("Quieto", dir);
            }
        }

        /// <summary>Línea (radio 0) o paso del cuerpo (radio > 0) sin muros ni objetos entre a y b.</summary>
        private bool Libre(Vector2 a, Vector2 b, float radio)
        {
            Vector2 o = a + new Vector2(0f, 0.08f);
            Vector2 dvec = (b + new Vector2(0f, 0.08f)) - o;
            float dist = dvec.magnitude;
            if (dist < 0.001f) return true;
            int n = radio > 0f
                ? Physics2D.CircleCast(o, radio, dvec / dist, filtro, impactos, dist)
                : Physics2D.Raycast(o, dvec / dist, filtro, impactos, dist);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = impactos[i].collider;
                if (c == null) continue;
                bool propio = false;
                for (int k = 0; k < propios.Length; k++) if (propios[k] == c) propio = true;
                if (propio) continue;
                if (c.GetComponentInParent<E3Movedor>() != null) continue;   // personajes no tapan la vista
                return false;
            }
            return true;
        }

        private void Morir()
        {
            estado = Estado.Muerto;
            anim.Reproducir("Muerte", E3Animador.Dir4(E3Animador.Vector(dir)), true);
            foreach (Collider2D c in propios) if (c != null) c.enabled = false;
            E3OrdenY o = GetComponent<E3OrdenY>();
            if (o != null) { o.fijo = true; o.ordenFijo = -1400; o.Aplicar(); }
        }
    }
}
