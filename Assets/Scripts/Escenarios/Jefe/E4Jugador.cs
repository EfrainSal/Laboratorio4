using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Jason Smith (jugable). WASD/flechas caminar · Espacio, J o clic izq. disparar ·
    /// Q, K o clic der. escudo (bloquea las bolas de poder).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(E4Anim))]
    [RequireComponent(typeof(E4Vida))]
    public class E4Jugador : MonoBehaviour
    {
        public float velocidad = 3.4f;
        public float cadencia = 0.26f;
        public int danioBala = 1;
        public float velocidadBala = 14f;
        public float alturaArma = 0.42f;
        public float duracionEscudo = 3f;
        public float recargaEscudo = 6f;
        public Sprite spriteBala;
        public Sprite spriteImpacto;
        public SpriteRenderer escudoVisual;
        [Tooltip("Si el jefe está casi en la dirección en la que miras, la bala va hacia él.")]
        public Transform objetivoAsistido;
        public float anguloAsistencia = 25f;

        private Rigidbody2D rb;
        private E4Anim anim;
        private E4Vida vida;
        private Vector2 mov;
        private int dir = 2;
        private float tDisparo, tEscudo, tRecarga, tPose;

        public float EscudoRestante { get { return Mathf.Max(0f, tEscudo); } }
        public float RecargaRestante { get { return Mathf.Max(0f, tRecarga); } }
        public bool Muerto { get { return vida != null && vida.Muerto; } }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<E4Anim>();
            vida = GetComponent<E4Vida>();
        }

        private void Start()
        {
            vida.AlMorir += Morir;
            if (escudoVisual != null) escudoVisual.enabled = false;
            anim.Reproducir("Quieto", dir, true);
        }

        private void Update()
        {
            if (vida.Muerto) { mov = Vector2.zero; return; }

            mov = E4Entrada.Movimiento();
            if (mov.sqrMagnitude > 1f) mov.Normalize();
            if (mov.sqrMagnitude > 0.01f) dir = E4Util.Direccion(mov);

            // ---- escudo
            if (tRecarga > 0f) tRecarga -= Time.deltaTime;
            if (tEscudo > 0f)
            {
                tEscudo -= Time.deltaTime;
                if (escudoVisual != null)
                {
                    float a = tEscudo < 0.8f ? (Mathf.Sin(Time.time * 30f) > 0f ? 1f : 0.25f) : 1f;   // parpadea al acabarse
                    escudoVisual.color = new Color(1f, 1f, 1f, a);
                }
                if (tEscudo <= 0f)
                {
                    vida.invulnerable = false;
                    if (escudoVisual != null) escudoVisual.enabled = false;
                }
            }
            if (E4Entrada.Escudo() && tEscudo <= 0f && tRecarga <= 0f)
            {
                tEscudo = duracionEscudo;
                tRecarga = duracionEscudo + recargaEscudo;
                vida.invulnerable = true;
                if (escudoVisual != null) escudoVisual.enabled = true;
                anim.Reproducir("Habilidad", dir, true);
                tPose = 0.5f;
            }

            // ---- disparo
            tDisparo -= Time.deltaTime;
            if (E4Entrada.Disparando() && tDisparo <= 0f)
            {
                tDisparo = cadencia;
                Disparar();
            }

            if (tPose > 0f) { tPose -= Time.deltaTime; return; }
            anim.Reproducir(mov.sqrMagnitude > 0.01f ? "Caminar" : "Quieto", dir);
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = vida.Muerto ? Vector2.zero : mov * velocidad;
        }

        private void Disparar()
        {
            Vector2 d = E4Util.Vector(dir);
            if (objetivoAsistido != null)
            {
                Vector2 hacia = (Vector2)objetivoAsistido.position - rb.position;
                if (hacia.magnitude < 15f && Vector2.Angle(d, hacia) < anguloAsistencia) d = hacia.normalized;
            }
            E4Util.CrearProyectil("E4_BalaJason", rb.position + d * 0.25f, d, E4Bando.Jugadores, danioBala,
                velocidadBala, 0.08f, 2f, spriteBala, spriteImpacto, alturaArma, transform, true);

            // Disparo solo existe hacia Sur, Este y Oeste; hacia el norte se queda con su pose normal.
            if (!E4Util.MiraAlNorte(dir) && anim.Tiene("Disparo"))
            {
                bool reiniciar = anim.Actual != "Disparo" || anim.Termino;
                anim.Reproducir("Disparo", dir, reiniciar);
                tPose = Mathf.Max(tPose, 0.3f);
            }
        }

        private void Morir()
        {
            mov = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            tEscudo = 0f;
            if (escudoVisual != null) escudoVisual.enabled = false;
            anim.Reproducir("Muerte", dir, true);
        }
    }
}
