using System.Collections.Generic;
using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Zorbo / Zorbo2 con IA (V14):
    ///  - Te detecta si te ve (radioDeteccion, sin muro/objeto en medio) o si estás muy cerca (radioOido).
    ///  - Te persigue: en línea recta si te ve; si no, o si se atora, sigue una ruta (Piso3V10Navegacion)
    ///    que rodea paredes y muebles. Nunca atraviesa nada (barrido con colisiones).
    ///  - Al alcanzarte ataca y te quita vida en el fotograma del golpe.
    ///  - Las balas le quitan vida; a 0 reproduce Muerte y desaparece.
    /// </summary>
    [RequireComponent(typeof(Piso3V10Personaje8Dir))]
    public class Piso3V10Zorbo : MonoBehaviour
    {
        [Range(0, 7)]
        [Tooltip("0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste")]
        public int orientacion = 0;

        [Header("Comportamiento")]
        public bool perseguir = true;
        public float radioDeteccion = 4.5f;
        [Tooltip("Te nota aunque no te vea si estás así de cerca.")]
        public float radioOido = 2f;
        [Tooltip("Una vez que te persigue, te pierde si te alejas más de esto.")]
        public float radioOlvido = 8f;
        public float radioAtaque = 0.66f;
        public float velocidad = 1.7f;
        public float tiempoEntreAtaques = 1.1f;
        [Tooltip("Fotograma del ataque en que conecta el golpe (0 = primero).")]
        public int fotogramaGolpe = 5;
        public int danio = 1;

        [Header("Vida")]
        public int vida = 3;
        public bool muerto = false;

        private Piso3V10Personaje8Dir anim;
        private SpriteRenderer sr;
        private Rigidbody2D cuerpo;
        private CapsuleCollider2D capsula;
        private ContactFilter2D filtro;
        private Collider2D[] propios;
        private Transform objetivo;
        private Piso3V10JasonAnimacion jason;
        private float parpadeo, esperaAtaque, tiempoMuerto;
        private bool atacando, golpeDado, persiguiendo, tieneUltimoVisto;
        private Vector2 ultimoVisto, deseo;
        // ruta
        private readonly List<Vector2> ruta = new List<Vector2>();
        private int indiceRuta;
        private float recalcular;
        private Vector2 destinoRuta;
        // atasco
        private float tiempoLento, modoRuta;

        private void Awake()
        {
            anim = GetComponent<Piso3V10Personaje8Dir>();
            sr = GetComponent<SpriteRenderer>();
            cuerpo = GetComponent<Rigidbody2D>();
            capsula = GetComponent<CapsuleCollider2D>();
            if (cuerpo != null)
            {
                cuerpo.bodyType = RigidbodyType2D.Kinematic;
                cuerpo.gravityScale = 0f;
                cuerpo.freezeRotation = true;
                cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            filtro = Piso3V10Barrido.Filtro(gameObject);
            propios = GetComponentsInChildren<Collider2D>();
            Aplicar();
        }

        private void Start()
        {
            jason = FindFirstObjectByType<Piso3V10JasonAnimacion>();
            if (jason != null) objetivo = jason.transform;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) objetivo = p.transform;
            }
            if (capsula != null)
            {
                Vector2 esc = transform.lossyScale;
                Vector2 tam = new Vector2(capsula.size.x * Mathf.Abs(esc.x), capsula.size.y * Mathf.Abs(esc.y));
                Vector2 des = new Vector2(capsula.offset.x * esc.x, capsula.offset.y * esc.y);
                Piso3V10Navegacion.Asegurar(tam + new Vector2(0.04f, 0.02f), des);
            }
        }

        private void OnValidate()
        {
            Piso3V10Personaje8Dir a = GetComponent<Piso3V10Personaje8Dir>();
            if (a != null) { a.direccion = orientacion; a.animacionInicial = "Quieto"; }
        }

        public void Aplicar()
        {
            if (anim == null) anim = GetComponent<Piso3V10Personaje8Dir>();
            if (anim != null) anim.Reproducir("Quieto", orientacion, true);
        }

        public void Quieto() { if (!muerto) anim.Reproducir("Quieto", orientacion); }

        public void Caminar(int dir8)
        {
            if (muerto) return;
            orientacion = dir8;
            anim.Reproducir(anim.Tiene("Caminar") ? "Caminar" : "Correr", dir8);
        }

        public void Atacar(int dir8)
        {
            if (muerto) return;
            orientacion = dir8;
            atacando = true;
            golpeDado = false;
            anim.Reproducir("Ataque", Direccion4(dir8), true);
        }

        public void Morir()
        {
            if (muerto) return;
            muerto = true;
            atacando = false;
            deseo = Vector2.zero;
            anim.Reproducir("Muerte", Direccion4(orientacion), true);
            foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;
        }

        /// <summary>Lo llama la bala al impactar.</summary>
        public void RecibirImpacto()
        {
            if (muerto) return;
            parpadeo = 0.1f;
            vida--;
            persiguiendo = true;
            if (objetivo != null) { ultimoVisto = objetivo.position; tieneUltimoVisto = true; }
            if (vida <= 0) Morir();
        }

        private void Update()
        {
            ActualizarParpadeo();
            deseo = Vector2.zero;
            if (anim == null) return;

            if (muerto)
            {
                tiempoMuerto += Time.deltaTime;
                if (anim.Terminada && tiempoMuerto > 1.2f)
                {
                    if (sr != null) sr.enabled = false;
                    Transform s = transform.Find("Sombra");
                    if (s != null) s.gameObject.SetActive(false);
                    enabled = false;
                }
                return;
            }

            if (esperaAtaque > 0f) esperaAtaque -= Time.deltaTime;
            if (modoRuta > 0f) modoRuta -= Time.deltaTime;
            if (recalcular > 0f) recalcular -= Time.deltaTime;

            if (atacando)
            {
                if (!golpeDado && anim.FotogramaActual >= Mathf.Min(fotogramaGolpe, Mathf.Max(0, anim.TotalFotogramas - 1)))
                {
                    golpeDado = true;
                    if (jason != null && objetivo != null && Vector2.Distance(Pies(), objetivo.position) <= radioAtaque + 0.25f)
                        jason.RecibirDanio(danio);
                }
                if (!anim.Terminada) return;
                atacando = false;
                esperaAtaque = tiempoEntreAtaques;
            }

            if (!perseguir || objetivo == null || (jason != null && jason.muerto)) { Quieto(); return; }

            Vector2 yo = Pies();
            Vector2 el = objetivo.position;
            float dist = Vector2.Distance(yo, el);
            bool veo = dist <= (persiguiendo ? radioOlvido : radioDeteccion) &&
                       Piso3V10Barrido.LineaDeVision(yo + Vector2.up * 0.1f, el + Vector2.up * 0.1f, propios, objetivo);
            bool oigo = dist <= radioOido;
            if (veo || oigo) { persiguiendo = true; ultimoVisto = el; tieneUltimoVisto = true; }
            else if (dist > radioOlvido) persiguiendo = false;

            if (veo && dist <= radioAtaque)
            {
                int d8 = Direccion8(el - yo);
                if (esperaAtaque <= 0f) Atacar(d8);
                else { orientacion = d8; Quieto(); }
                return;
            }

            if (!persiguiendo || !tieneUltimoVisto) { Quieto(); return; }

            Vector2 destino = (veo || oigo) ? el : ultimoVisto;
            if (!veo && !oigo && Vector2.Distance(yo, destino) < 0.2f) { tieneUltimoVisto = false; persiguiendo = false; Quieto(); return; }

            Vector2 dir;
            if (veo && modoRuta <= 0f)
            {
                dir = destino - yo;
                ruta.Clear();
            }
            else
            {
                if (recalcular <= 0f || ruta.Count == 0 || Vector2.Distance(destino, destinoRuta) > 0.5f)
                {
                    Piso3V10Navegacion.Ruta(yo, destino, ruta);
                    indiceRuta = 0;
                    destinoRuta = destino;
                    recalcular = 0.5f;
                }
                while (indiceRuta < ruta.Count - 1 && Vector2.Distance(yo, ruta[indiceRuta]) < 0.14f) indiceRuta++;
                if (ruta.Count == 0) { Quieto(); return; }
                dir = ruta[indiceRuta] - yo;
            }
            if (dir.sqrMagnitude < 0.0004f) { Quieto(); return; }
            deseo = dir.normalized;
            Caminar(Direccion8(deseo));
        }

        private void FixedUpdate()
        {
            if (cuerpo == null || capsula == null || muerto || atacando) return;
            Vector2 delta = deseo * velocidad * Time.fixedDeltaTime;
            if (delta.sqrMagnitude <= 0f) { tiempoLento = 0f; return; }
            Vector2 antes = cuerpo.position;
            Vector2 nueva = Piso3V10Barrido.Mover(cuerpo, capsula, filtro, delta, 0.03f);
            cuerpo.MovePosition(nueva);
            // atasco: avanza menos del 30 % de lo pedido durante 0.35 s -> sigue la ruta un rato
            if ((nueva - antes).magnitude < delta.magnitude * 0.3f) tiempoLento += Time.fixedDeltaTime;
            else tiempoLento = 0f;
            if (tiempoLento > 0.35f) { modoRuta = 2f; recalcular = 0f; tiempoLento = 0f; }
        }

        private Vector2 Pies() { return cuerpo != null ? cuerpo.position : (Vector2)transform.position; }

        private void ActualizarParpadeo()
        {
            if (sr == null || parpadeo <= 0f) return;
            parpadeo -= Time.deltaTime;
            sr.color = parpadeo > 0f ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
        }

        private static int Direccion8(Vector2 d)
        {
            float a = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
            int i = Mathf.RoundToInt(a / 45f);
            return ((i % 8) + 8) % 8;
        }

        private static int Direccion4(int d8)
        {
            switch (d8)
            {
                case 1: case 2: case 3: return 2;
                case 5: case 6: case 7: return 6;
                case 4: return 4;
                default: return 0;
            }
        }
    }
}
