using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Laboratorio4.Recepcion
{
    /// <summary>
    /// Movimiento de Jason en la Recepción (WASD o flechas). La animación la hace RecepcionAnimador.
    /// Choca con muros y objetos solo en la base (los pies), así puede pasar por detrás de ellos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RecepcionJugador : MonoBehaviour
    {
        public float velocidad = 3.5f;

        Rigidbody2D rb;
        Vector2 mov;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
            }
#else
            v.x = Input.GetAxisRaw("Horizontal");
            v.y = Input.GetAxisRaw("Vertical");
#endif
            mov = v.sqrMagnitude > 1f ? v.normalized : v;
        }

        void FixedUpdate()
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = mov * velocidad;
#else
            rb.velocity = mov * velocidad;
#endif
        }
    }
}
