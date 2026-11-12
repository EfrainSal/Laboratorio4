using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Doctor Yiba (jefe final). Se mueve por su plataforma y alterna:
    ///  - Bola de poder: proyectil a distancia (3 en abanico con menos de la mitad de vida). Los muros de cobertura la detienen.
    ///  - Campo de energía: daño en área si te acercas demasiado.
    /// Al morir abre la puerta de salida.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(E4Anim))]
    [RequireComponent(typeof(E4Vida))]
    public class E4Jefe : MonoBehaviour
    {
        public Rect zona = new Rect(17.9f, -9.9f, 7.7f, 3.5f);
        public float velocidad = 1.7f;
        public float rangoActivacion = 15f;
        public Transform[] objetivos = new Transform[0];
        public Sprite spriteBola;
        public Sprite spriteImpactoBola;
        public float velocidadBola = 6.5f;
        public int danioBola = 2;
        public int danioCampo = 2;
        public float radioCampo = 2.3f;
        public float alturaBola = 0.75f;
        public E4PuertaSalida puerta;

        private enum Estado { Esperar, Mover, Bola, Campo, Muerto }
        private Estado estado = Estado.Esperar;
        private Rigidbody2D rb;
        private E4Anim anim;
        private E4Vida vida;
        private Vector2 destino;
        private float tEstado = 1f;
        private bool lanzo;
        private int dir = 0;
        private Transform objetivo;

        public bool Activo { get; private set; }
        public bool Muerto { get { return vida != null && vida.Muerto; } }
        public E4Vida Vida { get { return vida; } }
        private bool SegundaFase { get { return vida.actual * 2 <= vida.maximo; } }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<E4Anim>();
            vida = GetComponent<E4Vida>();
        }

        private void Start()
        {
            vida.AlMorir += Morir;
            anim.Reproducir("Quieto", dir, true);
        }

        private void Update()
        {
            if (estado == Estado.Muerto) return;
            if (!Activo)
            {
                foreach (Transform o in objetivos)
                    if (Vivo(o) && Vector2.Distance(o.position, transform.position) < rangoActivacion) Activo = true;
                if (!Activo) return;
                tEstado = 1f;
            }
            objetivo = ElegirObjetivo();

            switch (estado)
            {
                case Estado.Esperar:
                    if (objetivo != null) dir = E4Util.Direccion((Vector2)objetivo.position - rb.position);
                    anim.Reproducir("Quieto", dir);
                    tEstado -= Time.deltaTime;
                    if (tEstado <= 0f) Decidir();
                    break;
                case Estado.Mover:
                    tEstado -= Time.deltaTime;
                    Vector2 falta = destino - rb.position;
                    if (falta.magnitude < 0.1f || tEstado <= 0f) { Decidir(); break; }
                    dir = E4Util.Direccion(falta);
                    anim.Reproducir("Correr", dir);
                    break;
                case Estado.Bola:
                    if (!lanzo && anim.Frame >= 5) { lanzo = true; LanzarBola(); }
                    if (anim.Termino) { estado = Estado.Esperar; tEstado = SegundaFase ? 0.35f : 0.75f; }
                    break;
                case Estado.Campo:
                    if (!lanzo && anim.Frame >= 4) { lanzo = true; CampoEnergia(); }
                    if (anim.Termino) { estado = Estado.Esperar; tEstado = 0.6f; }
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (estado != Estado.Mover) return;
            Vector2 p = rb.position;
            Vector2 falta = destino - p;
            float paso = velocidad * (SegundaFase ? 1.35f : 1f) * Time.fixedDeltaTime;
            rb.MovePosition(falta.magnitude <= paso ? destino : p + falta.normalized * paso);
        }

        private static bool Vivo(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            E4Vida v = t.GetComponent<E4Vida>();
            return v == null || !v.Muerto;
        }

        private Transform ElegirObjetivo()
        {
            Transform mejor = null;
            float dMejor = float.MaxValue;
            for (int i = 0; i < objetivos.Length; i++)
            {
                if (!Vivo(objetivos[i])) continue;
                float d = Vector2.Distance(objetivos[i].position, transform.position) * (i == 0 ? 0.8f : 1f);   // prefiere a Jason
                if (d < dMejor) { dMejor = d; mejor = objetivos[i]; }
            }
            return mejor;
        }

        private void Decidir()
        {
            if (objetivo == null) { estado = Estado.Esperar; tEstado = 0.5f; return; }
            Vector2 hacia = (Vector2)objetivo.position - rb.position;
            if (hacia.magnitude < radioCampo * 0.95f)
            {
                estado = Estado.Campo; lanzo = false;
                dir = E4Util.Direccion(hacia);
                anim.Reproducir("CampoEnergia", dir, true);
                return;
            }
            if (Random.value < (SegundaFase ? 0.7f : 0.55f))
            {
                estado = Estado.Bola; lanzo = false;
                dir = E4Util.Direccion(hacia);
                anim.Reproducir("BolaDePoder", dir, true);
                return;
            }
            estado = Estado.Mover;
            tEstado = Random.Range(1.2f, 2.2f);
            destino = new Vector2(Random.Range(zona.xMin, zona.xMax), Random.Range(zona.yMin, zona.yMax));
        }

        private void LanzarBola()
        {
            if (objetivo == null) return;
            Vector2 o = rb.position;
            Vector2 d = ((Vector2)objetivo.position - o).normalized;
            int n = SegundaFase ? 3 : 1;
            for (int i = 0; i < n; i++)
            {
                float ang = (i - (n - 1) * 0.5f) * 14f;
                Vector2 di = Quaternion.Euler(0f, 0f, ang) * d;
                E4Proyectil p = E4Util.CrearProyectil("E4_BolaPoder", o + di * 0.45f, di, E4Bando.Jefe, danioBola,
                    velocidadBola, 0.2f, 6f, spriteBola, spriteImpactoBola, alturaBola, transform, false);
                p.pulsar = true;
            }
        }

        private void CampoEnergia()
        {
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = true;
            f.useLayerMask = false;
            List<Collider2D> cs = new List<Collider2D>();
            Physics2D.OverlapCircle(rb.position, radioCampo, f, cs);
            HashSet<E4Vida> golpeados = new HashSet<E4Vida>();
            foreach (Collider2D c in cs)
            {
                E4Hurtbox hb = c.GetComponent<E4Hurtbox>();
                E4Vida v = hb != null ? hb.vida : c.GetComponentInParent<E4Vida>();
                if (v == null || v.bando == E4Bando.Jefe || v.Muerto || !golpeados.Add(v)) continue;
                v.Recibir(danioCampo);
            }
        }

        private void Morir()
        {
            estado = Estado.Muerto;
            anim.Reproducir("Muerte", dir, true);
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            if (puerta != null) puerta.Desbloquear();
        }
    }
}
