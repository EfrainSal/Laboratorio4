using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Milena Rhea (compañera controlada por el juego): sigue a Jason y le dispara al Doctor
    /// cuando lo tiene a tiro y sin muros de por medio.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(E4Anim))]
    [RequireComponent(typeof(E4Vida))]
    public class E4Companera : MonoBehaviour
    {
        public Transform lider;
        public E4Jefe jefe;
        public float distanciaSeguir = 1.8f;
        public float velocidad = 3.2f;
        public float rangoDisparo = 10f;
        public float cadencia = 0.7f;
        public int danioBala = 1;
        public float alturaArma = 0.42f;
        public Sprite spriteBala;
        public Sprite spriteImpacto;

        private Rigidbody2D rb;
        private E4Anim anim;
        private E4Vida vida;
        private Vector2 mov;
        private int dir = 2;
        private float tDisparo = 1f, tPose, tLejos;
        private static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<E4Anim>();
            vida = GetComponent<E4Vida>();
        }

        private void Start()
        {
            vida.AlMorir += () => { mov = Vector2.zero; anim.Reproducir("Muerte", dir, true); };
            anim.Reproducir("Quieto", dir, true);
        }

        private void Update()
        {
            if (vida.Muerto) return;
            mov = Vector2.zero;
            if (lider != null)
            {
                Vector2 hacia = (Vector2)lider.position - rb.position;
                float d = hacia.magnitude;
                if (d > distanciaSeguir) mov = hacia / d * Mathf.Clamp01((d - distanciaSeguir) * 2f);
                // si se queda atorada lejos (detrás de un muro) aparece junto a Jason
                tLejos = d > 7f ? tLejos + Time.deltaTime : 0f;
                if (tLejos > 2.5f)
                {
                    rb.position = (Vector2)lider.position + new Vector2(-0.6f, -0.4f);
                    tLejos = 0f;
                }
            }
            if (mov.sqrMagnitude > 0.01f) dir = E4Util.Direccion(mov);

            tDisparo -= Time.deltaTime;
            if (jefe != null && jefe.Activo && !jefe.Muerto)
            {
                Vector2 aJefe = (Vector2)jefe.transform.position - rb.position;
                if (aJefe.magnitude < rangoDisparo && LineaLibre(aJefe))
                {
                    if (mov.sqrMagnitude < 0.01f) dir = E4Util.Direccion(aJefe);
                    if (tDisparo <= 0f)
                    {
                        tDisparo = cadencia;
                        Vector2 dd = aJefe.normalized;
                        E4Util.CrearProyectil("E4_BalaMilena", rb.position + dd * 0.25f, dd, E4Bando.Jugadores, danioBala,
                            14f, 0.08f, 2f, spriteBala, spriteImpacto, alturaArma, transform, true);
                        anim.Reproducir("Disparo", E4Util.Direccion(aJefe), anim.Actual != "Disparo" || anim.Termino);
                        tPose = 0.35f;
                    }
                }
            }

            if (tPose > 0f) { tPose -= Time.deltaTime; return; }
            anim.Reproducir(mov.sqrMagnitude > 0.01f ? "CorrerConArma" : "Quieto", dir);
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = vida.Muerto ? Vector2.zero : mov * velocidad;
        }

        private bool LineaLibre(Vector2 hacia)
        {
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = false;
            f.useLayerMask = false;
            hits.Clear();
            Physics2D.CircleCast(rb.position, 0.08f, hacia.normalized, f, hits, hacia.magnitude);
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit2D h in hits)
            {
                if (h.collider == null) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;
                E4Vida v = h.collider.GetComponentInParent<E4Vida>();
                if (v != null && v.bando == E4Bando.Jugadores) continue;
                return v != null && v.bando == E4Bando.Jefe;
            }
            return true;
        }
    }
}
