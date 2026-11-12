using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Personaje jugable (Jason Smith) con la configuración del Piso 3:
    /// - Rigidbody2D Kinematic y movimiento por barrido: lanza su cápsula, se detiene antes de tocar y
    ///   desliza. Ignora las juntas entre piezas de pared (no se "engancha") y, si alguna vez queda
    ///   encimado a algo, se separa solo (no se queda pegado).
    /// - Animación fotograma a fotograma: quieto 8 direcciones; caminar norte/este/sur/oeste;
    ///   disparo frente/derecha/izquierda (Espacio, J o clic izquierdo).
    /// - Habilidad: ESCUDO con Q o clic derecho. Dura 'duracionEscudo' segundos, luego recarga.
    ///   Mientras está activo Jason no se mueve ni dispara y EscudoActivo = true (para el daño futuro).
    /// - Muerte: Morir() reproduce la animación (preparado para cuando exista el sistema de vida).
    /// Direcciones: 0 Norte, 1 Noreste, 2 Este, 3 Sureste, 4 Sur, 5 Suroeste, 6 Oeste, 7 Noroeste.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class ControladorJugador : MonoBehaviour
    {
        public string nombrePersonaje = "Jason Smith";
        public float velocidad = 3.0f;
        public SpriteRenderer render;

        [Header("Quieto (N, NE, E, SE, S, SO, O, NO)")]
        public Sprite[] spritesDireccion = new Sprite[8];

        [Header("Caminar (fotograma a fotograma)")]
        public Sprite[] caminarNorte;
        public Sprite[] caminarEste;
        public Sprite[] caminarOeste;
        public Sprite[] caminarSur;
        public float fpsCaminar = 10f;

        [Header("Disparo")]
        public Sprite[] disparoFrente;
        public Sprite[] disparoDerecha;
        public Sprite[] disparoIzquierda;
        public float fpsDisparo = 14f;
        public int fogonazoFrente = 2, fogonazoDerecha = 4, fogonazoIzquierda = 4;
        public Vector2 bocaFrente = new Vector2(0.21f, 0.39f);
        public Vector2 bocaDerecha = new Vector2(0.45f, 0.33f);
        public Vector2 bocaIzquierda = new Vector2(-0.45f, 0.33f);
        public Sprite spriteBala;
        public Sprite spriteImpacto;
        public float velocidadBala = 16f;

        [Header("Escudo (Q / clic derecho)")]
        public Sprite[] escudoNorte;
        public Sprite[] escudoEste;
        public Sprite[] escudoSur;
        public Sprite[] escudoOeste;
        public float fpsEscudo = 12f;
        [Tooltip("Fotogramas de entrada antes del bucle del escudo.")]
        public int inicioBucleEscudo = 2;
        public float duracionEscudo = 3f;
        public float recargaEscudo = 5f;

        [Header("Muerte")]
        public Sprite[] muerteNorte;
        public Sprite[] muerteEste;
        public Sprite[] muerteSur;
        public Sprite[] muerteOeste;
        public float fpsMuerte = 10f;

        public bool EscudoActivo { get { return escudo != null; } }
        public float RecargaRestante { get { return Mathf.Max(0f, siguienteEscudo - Time.time); } }
        public bool Muerto { get { return muerte != null; } }

        const float Piel = 0.015f;
        Rigidbody2D rb;
        CapsuleCollider2D capsula;
        ContactFilter2D filtro;
        readonly RaycastHit2D[] golpes = new RaycastHit2D[16];
        readonly Collider2D[] encimados = new Collider2D[8];
        Vector2 entrada;
        int direccion = 2;
        float reloj;
        Sprite[] animDisparo; int fogonazo; Vector2 boca, dirBala; bool balaLanzada; float relojDisparo;
        Sprite[] escudo; float relojEscudo; float siguienteEscudo;
        Sprite[] muerte; float relojMuerte;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            capsula = GetComponent<CapsuleCollider2D>();
            if (render == null) render = GetComponent<SpriteRenderer>();
            filtro = new ContactFilter2D();
            filtro.useTriggers = false;
            filtro.useLayerMask = false;
        }

        void Update()
        {
            if (muerte != null) { AnimarMuerte(); return; }
            bool ocupado = animDisparo != null || escudo != null;
            entrada = ocupado ? Vector2.zero : Entrada.Direccion();
            if (entrada.sqrMagnitude > 1f) entrada.Normalize();
            if (!ocupado && entrada.sqrMagnitude > 0.01f) direccion = Direccion8(entrada);

            if (!ocupado && Entrada.Escudo() && Time.time >= siguienteEscudo) EmpezarEscudo();
            else if (!ocupado && Entrada.Disparo()) EmpezarDisparo();
            ActualizarSprite();
        }

        void FixedUpdate()
        {
            Vector2 pos = Separar(rb.position);
            Vector2 paso = entrada * velocidad * Time.fixedDeltaTime;
            pos = Barrer(pos, new Vector2(paso.x, 0f));
            pos = Barrer(pos, new Vector2(0f, paso.y));
            rb.MovePosition(pos);
        }

        /// <summary>Si la cápsula quedó encimada a algo sólido, la empuja hacia fuera.</summary>
        Vector2 Separar(Vector2 pos)
        {
            int n = capsula.Overlap(filtro, encimados);
            for (int i = 0; i < n; i++)
            {
                var c = encimados[i];
                if (c == null || c.isTrigger || c.attachedRigidbody == rb) continue;
                ColliderDistance2D d = capsula.Distance(c);
                if (d.isOverlapped && d.isValid) pos += d.normal * (d.distance - Piel);
            }
            return pos;
        }

        /// <summary>Mueve la cápsula a lo largo de 'paso' deteniéndose antes de tocar algo sólido.</summary>
        Vector2 Barrer(Vector2 pos, Vector2 paso)
        {
            float dist = paso.magnitude;
            if (dist < 1e-6f) return pos;
            Vector2 dir = paso / dist;
            Vector2 escala = transform.lossyScale;
            Vector2 centro = pos + Vector2.Scale(capsula.offset, escala);
            Vector2 tam = Vector2.Scale(capsula.size, new Vector2(Mathf.Abs(escala.x), Mathf.Abs(escala.y)));
            int n = Physics2D.CapsuleCast(centro, tam, capsula.direction, 0f, dir, filtro, golpes, dist + Piel);
            float permitido = dist;
            for (int i = 0; i < n; i++)
            {
                var g = golpes[i];
                if (g.collider == null || g.collider == capsula || g.collider.isTrigger) continue;
                if (g.collider.attachedRigidbody == rb) continue;
                // solo frena lo que está DELANTE: juntas laterales entre piezas de pared se ignoran
                if (Vector2.Dot(g.normal, dir) > -0.05f) continue;
                permitido = Mathf.Min(permitido, Mathf.Max(0f, g.distance - Piel));
            }
            return pos + dir * permitido;
        }

        Sprite[] Por4(Sprite[] n, Sprite[] e, Sprite[] s, Sprite[] o)
        {
            if (direccion >= 1 && direccion <= 3) return e;
            if (direccion >= 5 && direccion <= 7) return o;
            return direccion == 4 ? s : n;
        }

        void EmpezarEscudo()
        {
            escudo = Por4(escudoNorte, escudoEste, escudoSur, escudoOeste);
            if (escudo == null || escudo.Length == 0) { escudo = null; return; }
            relojEscudo = 0f;
        }

        void EmpezarDisparo()
        {
            if (direccion >= 1 && direccion <= 3) { animDisparo = disparoDerecha; fogonazo = fogonazoDerecha; boca = bocaDerecha; dirBala = Vector2.right; }
            else if (direccion >= 5 && direccion <= 7) { animDisparo = disparoIzquierda; fogonazo = fogonazoIzquierda; boca = bocaIzquierda; dirBala = Vector2.left; }
            else if (direccion == 4) { animDisparo = disparoFrente; fogonazo = fogonazoFrente; boca = bocaFrente; dirBala = Vector2.down; }
            else { animDisparo = disparoDerecha; fogonazo = fogonazoDerecha; boca = bocaDerecha; dirBala = Vector2.right; }
            if (animDisparo == null || animDisparo.Length == 0) { animDisparo = null; return; }
            relojDisparo = 0f;
            balaLanzada = false;
        }

        /// <summary>Reproduce la animación de muerte y bloquea el control.</summary>
        public void Morir()
        {
            if (muerte != null) return;
            muerte = Por4(muerteNorte, muerteEste, muerteSur, muerteOeste);
            if (muerte == null || muerte.Length == 0) { muerte = null; return; }
            animDisparo = null; escudo = null; entrada = Vector2.zero; relojMuerte = 0f;
        }

        void AnimarMuerte()
        {
            entrada = Vector2.zero;
            relojMuerte += Time.deltaTime;
            int f = Mathf.Min(Mathf.FloorToInt(relojMuerte * fpsMuerte), muerte.Length - 1);
            render.sprite = muerte[f];
        }

        void ActualizarSprite()
        {
            if (render == null) return;
            if (escudo != null)
            {
                relojEscudo += Time.deltaTime;
                if (relojEscudo >= duracionEscudo) { escudo = null; siguienteEscudo = Time.time + recargaEscudo; }
                else
                {
                    int f = Mathf.FloorToInt(relojEscudo * fpsEscudo);
                    int bucle = Mathf.Clamp(inicioBucleEscudo, 0, escudo.Length - 1);
                    if (f >= escudo.Length) f = bucle + (f - escudo.Length) % (escudo.Length - bucle);
                    render.sprite = escudo[f];
                    return;
                }
            }
            if (animDisparo != null)
            {
                relojDisparo += Time.deltaTime;
                int f = Mathf.FloorToInt(relojDisparo * fpsDisparo);
                if (!balaLanzada && f >= fogonazo)
                {
                    balaLanzada = true;
                    Bala.Disparar(rb.position, boca, dirBala, velocidadBala, spriteBala, spriteImpacto, capsula);
                }
                if (f >= animDisparo.Length) animDisparo = null;
                else { render.sprite = animDisparo[f]; return; }
            }

            Sprite[] anim = null;
            if (entrada.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(entrada.x) >= Mathf.Abs(entrada.y)) anim = entrada.x > 0f ? caminarEste : caminarOeste;
                else anim = entrada.y > 0f ? caminarNorte : caminarSur;
            }
            if (anim != null && anim.Length > 0)
            {
                reloj += Time.deltaTime;
                render.sprite = anim[Mathf.FloorToInt(reloj * fpsCaminar) % anim.Length];
            }
            else
            {
                reloj = 0f;
                if (spritesDireccion != null && spritesDireccion.Length == 8 && spritesDireccion[direccion] != null)
                    render.sprite = spritesDireccion[direccion];
            }
        }

        static int Direccion8(Vector2 v)
        {
            float ang = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            return Mathf.RoundToInt(ang / 45f) % 8;
        }
    }
}
