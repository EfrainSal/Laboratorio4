using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Movimiento top-down del jugador provisional (WASD / flechas).
    /// La física la resuelve Rigidbody2D + CapsuleCollider2D en los pies,
    /// por eso el cuerpo puede dibujarse delante de una pared frontal sin atravesarla.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Piso3V10Movimiento : MonoBehaviour
    {
        [Tooltip("Unidades por segundo (100 px = 1 unidad).")]
        public float velocidad = 3.2f;

        [Tooltip("Lo activa la animación de disparo: el personaje se queda quieto mientras dispara.")]
        public bool bloqueado = false;

        /// <summary>Dirección pedida por el teclado en este frame (la usa la animación).</summary>
        public Vector2 DireccionEntrada { get { return direccion; } }

        private Rigidbody2D cuerpo;
        private Vector2 direccion;
        private Vector3 posicionInicial;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody2D>();
            cuerpo.gravityScale = 0f;
            cuerpo.freezeRotation = true;
            cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            posicionInicial = transform.position;
        }

        private void Update()
        {
            direccion = Piso3V10Entrada.Direccion();
        }

        private void FixedUpdate()
        {
            cuerpo.linearVelocity = bloqueado ? Vector2.zero : direccion * velocidad;
        }

        public void Reiniciar()
        {
            cuerpo.linearVelocity = Vector2.zero;
            cuerpo.position = posicionInicial;
            transform.position = posicionInicial;
        }
    }
}
