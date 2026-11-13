using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Movimiento top-down de Jason (WASD / flechas) con barrido y deslizamiento (Piso3V10Barrido).
    ///
    /// V13: si el mismo objeto tiene otro script de movimiento (p. ej. "PlayerMovement" del proyecto
    /// Laboratorio4, que usa transform.Translate y por eso atravesaba paredes, objetos y Zorbos),
    /// se desactiva automáticamente al iniciar y se avisa en la consola.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class Piso3V10Movimiento : MonoBehaviour
    {
        [Tooltip("Unidades por segundo (100 px = 1 unidad).")]
        public float velocidad = 3.2f;

        [Tooltip("Lo activan las animaciones de disparo / habilidad / muerte.")]
        public bool bloqueado = false;

        [Tooltip("Distancia mínima que se deja con muros y objetos (unidades).")]
        public float piel = 0.03f;

        /// <summary>Dirección pedida por el teclado en este frame (la usa la animación).</summary>
        public Vector2 DireccionEntrada { get { return direccion; } }

        /// <summary>V15.2: true si de verdad avanza (no está empujando contra una pared). Lo usa la animación.</summary>
        public bool Avanzando { get { return framesSinAvanzar < 4; } }
        private int framesSinAvanzar = 99;

        [Tooltip("V16: si Jason se queda atorado (empuja y no avanza medio segundo), escribe en la Consola qué collider lo frena.")]
        public bool avisarAtascos = true;
        private float tiempoAtascado;
        private float ultimoAviso = -10f;

        private Rigidbody2D cuerpo;
        private CapsuleCollider2D capsula;
        private Vector2 direccion;
        private Vector3 posicionInicial;
        private ContactFilter2D filtro;

        private void Awake()
        {
            DesactivarOtrosMovimientos();
            cuerpo = GetComponent<Rigidbody2D>();
            capsula = GetComponent<CapsuleCollider2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
            cuerpo.gravityScale = 0f;
            cuerpo.freezeRotation = true;
            cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (capsula.size.x < 0.05f || capsula.size.y < 0.05f)
            {
                capsula.direction = CapsuleDirection2D.Horizontal;
                capsula.size = new Vector2(0.34f, 0.16f);
                capsula.offset = new Vector2(0f, 0.08f);
            }
            filtro = Piso3V10Barrido.Filtro(gameObject);
            posicionInicial = transform.position;
        }

        /// <summary>Apaga cualquier otro script que mueva este objeto (PlayerMovement, etc.).</summary>
        private void DesactivarOtrosMovimientos()
        {
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb == this || !mb.enabled) continue;
                string n = mb.GetType().Name;
                if (EsMovimientoAjeno(mb))
                {
                    mb.enabled = false;
                    Debug.LogWarning("[Piso3] Se desactivó '" + n + "' en " + name +
                                     ": movía al personaje sin física (atravesaba paredes). Jason usa Piso3V10Movimiento.", this);
                }
            }
        }

        /// <summary>
        /// Scripts de otros que mueven al personaje sin física: PlayerMovement (Laboratorio4),
        /// Piso3TestMovement / Piso3TestPlayer (escena modular), PlayerController, etc.
        /// </summary>
        public static bool EsMovimientoAjeno(MonoBehaviour mb)
        {
            if (mb == null) return false;
            string ns = mb.GetType().Namespace ?? "";
            if (ns.StartsWith("LaboratorioYiba")) return false;
            string n = mb.GetType().Name;
            return n.Contains("Movement") || n.Contains("Movimiento") || n.Contains("Mover") || n == "PlayerController"
                   || n.Contains("TestPlayer") || n.Contains("JugadorPrueba") || n.EndsWith("PlayerMove");
        }

        private void Update()
        {
            direccion = Piso3V10Entrada.Direccion();
        }

        private void FixedUpdate()
        {
            Vector2 delta = bloqueado ? Vector2.zero : direccion * velocidad * Time.fixedDeltaTime;
            Vector2 antes = cuerpo.position;
            Vector2 nueva = Piso3V10Barrido.Mover(cuerpo, capsula, filtro, delta, piel);
            cuerpo.MovePosition(nueva);
            if (delta.sqrMagnitude > 0f && (nueva - antes).magnitude >= delta.magnitude * 0.15f) framesSinAvanzar = 0;
            else if (framesSinAvanzar < 99) framesSinAvanzar++;

            if (avisarAtascos && delta.sqrMagnitude > 0f && (nueva - antes).magnitude < delta.magnitude * 0.1f)
            {
                tiempoAtascado += Time.fixedDeltaTime;
                if (tiempoAtascado > 0.5f && Time.time - ultimoAviso > 2f)
                {
                    ultimoAviso = Time.time;
                    Piso3V10Barrido.Diagnosticar(name, cuerpo, capsula, filtro, antes, delta.normalized);
                }
            }
            else tiempoAtascado = 0f;
        }

        public void Reiniciar()
        {
            cuerpo.position = posicionInicial;
            transform.position = posicionInicial;
        }
    }
}
