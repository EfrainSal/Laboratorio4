using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Control de animación de Jason (V12). Usa Piso3V10Personaje8Dir con las carpetas de
    /// Art/Personajes/Jason:  Quieto (8 dir) · Caminar (S, E, N, O) · Disparo (S, E, O) ·
    /// Habilidad (S, E, N, O) · Muerte (S, E, N, O).
    /// Controles: WASD/flechas caminar · Espacio/J/clic izq. disparar · Q/K/clic der. habilidad ·
    /// M probar muerte · R reiniciar (revive).
    /// La bala (Piso3V10Bala) sale en el fotograma del fogonazo y choca con muros, objetos, puertas y Zorbos.
    /// V13: vida y daño (los Zorbos le pegan); si le falta el movimiento lo agrega y apaga otros scripts
    /// de movimiento; si le faltan las animaciones avisa en vez de quedarse congelado con errores.
    /// </summary>
    [RequireComponent(typeof(Piso3V10Personaje8Dir))]
    public class Piso3V10JasonAnimacion : MonoBehaviour
    {
        [Header("Bala")]
        public Sprite spriteBala;
        public Sprite spriteImpacto;
        public float velocidadBala = 14f;
        [Tooltip("Fotograma (0 = primero) en que sale la bala: el del fogonazo.")]
        public int fotogramaDisparoLateral = 4;
        public int fotogramaDisparoFrente = 2;
        [Tooltip("Boca del arma: x,y = desplazamiento sobre el suelo desde los pies; z = altura de dibujo.")]
        public Vector3 bocaDerecha = new Vector3(0.42f, 0f, 0.33f);
        public Vector3 bocaIzquierda = new Vector3(-0.45f, 0f, 0.30f);
        public Vector3 bocaFrente = new Vector3(0.09f, -0.02f, 0.42f);

        [Header("Comportamiento")]
        [Tooltip("Inmoviliza a Jason mientras dispara o usa la habilidad.")]
        public bool quietoDuranteAcciones = true;
        public bool muerto = false;

        [Header("Vida")]
        public int vidaMaxima = 6;
        public int vida = 6;
        [Tooltip("Segundos de invulnerabilidad tras recibir un golpe.")]
        public float invulnerable = 0.6f;

        private Piso3V10Personaje8Dir anim;
        private Piso3V10Movimiento movimiento;
        private Collider2D[] propios;
        private int direccion8 = 0;            // 0 = Sur
        private bool ultimoLadoDerecho = true;
        private string accion;                 // "Disparo", "Habilidad" o null
        private int dirAccion;
        private bool balaLanzada;
        private SpriteRenderer sr;
        private float parpadeo;
        private float tiempoInvulnerable;
        private bool listo;

        private void Awake()
        {
            anim = GetComponent<Piso3V10Personaje8Dir>();
            sr = GetComponent<SpriteRenderer>();
            movimiento = GetComponent<Piso3V10Movimiento>();
            if (movimiento == null) movimiento = gameObject.AddComponent<Piso3V10Movimiento>();   // también apaga PlayerMovement
            propios = GetComponentsInChildren<Collider2D>();
            vida = vidaMaxima;
            listo = anim != null && anim.Tiene("Quieto") && anim.Tiene("Caminar");
            if (!listo)
                Debug.LogError("[Piso3] Jason no tiene sus animaciones cargadas. En el editor se arregla solo al darle Play " +
                               "(o usa Laboratorio Yiba > Piso 3 V10 > 4 - Integrar Jason y Zorbos).", this);
        }

        private void Update()
        {
            if (!listo) return;
            ActualizarParpadeo();
            if (Piso3V10Entrada.PulsoReinicio() && muerto) Revivir();
            if (muerto) return;
            if (Piso3V10Entrada.PulsoPruebaMuerte()) { Morir(); return; }

            Vector2 dir = movimiento != null ? movimiento.DireccionEntrada : Piso3V10Entrada.Direccion();

            if (accion == null)
            {
                if (Piso3V10Entrada.PulsoDisparo()) Empezar("Disparo", DireccionDisparo());
                else if (Piso3V10Entrada.PulsoHabilidad()) Empezar("Habilidad", Direccion4Desde8(direccion8));
            }

            if (accion != null)
            {
                if (accion == "Disparo" && !balaLanzada)
                {
                    int salida = dirAccion == 0 ? fotogramaDisparoFrente : fotogramaDisparoLateral;
                    if (anim.FotogramaActual >= Mathf.Min(salida, Mathf.Max(0, anim.TotalFotogramas - 1)))
                    {
                        balaLanzada = true;
                        LanzarBala();
                    }
                }
                if (!anim.Terminada) return;
                accion = null;
                if (movimiento != null) movimiento.bloqueado = false;
            }

            if (dir.sqrMagnitude > 0.01f)
            {
                direccion8 = Direccion8(dir);
                if (Mathf.Abs(dir.x) > 0.01f) ultimoLadoDerecho = dir.x > 0f;
                // V15.2: empujando contra una pared se queda quieto mirando hacia allá (no "camina en el sitio").
                if (movimiento == null || movimiento.Avanzando) anim.Reproducir("Caminar", Direccion4(dir));
                else anim.Reproducir("Quieto", direccion8);
            }
            else
            {
                anim.Reproducir("Quieto", direccion8);
            }
        }

        private void Empezar(string nombre, int dir8)
        {
            accion = nombre;
            dirAccion = dir8;
            balaLanzada = false;
            anim.Reproducir(nombre, dir8, true);
            if (movimiento != null) movimiento.bloqueado = quietoDuranteAcciones;
        }

        public void Morir()
        {
            muerto = true;
            accion = null;
            if (movimiento != null) movimiento.bloqueado = true;
            anim.Reproducir("Muerte", Direccion4Desde8(direccion8), true);
        }

        public void RecibirDanio(int cantidad)
        {
            if (muerto || tiempoInvulnerable > 0f) return;
            vida = Mathf.Max(0, vida - cantidad);
            tiempoInvulnerable = invulnerable;
            parpadeo = 0.15f;
            if (vida <= 0) Morir();
        }

        private void ActualizarParpadeo()
        {
            if (tiempoInvulnerable > 0f) tiempoInvulnerable -= Time.deltaTime;
            if (sr == null) return;
            if (parpadeo > 0f)
            {
                parpadeo -= Time.deltaTime;
                sr.color = parpadeo > 0f ? new Color(1f, 0.4f, 0.4f, 1f) : Color.white;
            }
        }

        public void Revivir()
        {
            muerto = false;
            vida = vidaMaxima;
            if (sr != null) sr.color = Color.white;
            if (movimiento != null) { movimiento.bloqueado = false; movimiento.Reiniciar(); }
            direccion8 = 0;
            anim.Reproducir("Quieto", 0, true);
        }

        /// <summary>Disparo solo existe al frente (S), derecha (E) e izquierda (O).</summary>
        private int DireccionDisparo()
        {
            switch (direccion8)
            {
                case 1: case 2: case 3: return 2;
                case 5: case 6: case 7: return 6;
                case 0: return 0;
                default: return ultimoLadoDerecho ? 2 : 6;   // mirando al norte
            }
        }

        private void LanzarBala()
        {
            if (spriteBala == null) return;
            Vector3 boca; Vector2 d;
            if (dirAccion == 0) { boca = bocaFrente; d = Vector2.down; }
            else if (dirAccion == 2) { boca = bocaDerecha; d = Vector2.right; }
            else { boca = bocaIzquierda; d = Vector2.left; }
            Vector2 pies = transform.position;
            Piso3V10Bala.Disparar(pies, pies + new Vector2(boca.x, boca.y), boca.z, d,
                                  spriteBala, spriteImpacto, velocidadBala, propios);
        }

        /// <summary>0 Sur, 1 Sureste, 2 Este, 3 Noreste, 4 Norte, 5 Noroeste, 6 Oeste, 7 Suroeste.</summary>
        private static int Direccion8(Vector2 d)
        {
            float a = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
            int i = Mathf.RoundToInt(a / 45f);
            return ((i % 8) + 8) % 8;
        }

        /// <summary>Caminar tiene 4 direcciones: en diagonal manda el eje dominante (igual que antes).</summary>
        private static int Direccion4(Vector2 d)
        {
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return d.x > 0f ? 2 : 6;
            return d.y > 0f ? 4 : 0;
        }

        private static int Direccion4Desde8(int d8)
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
