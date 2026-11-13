using UnityEngine;

namespace Laboratorio4.Recepcion
{
    /// <summary>Milena sigue a Jason a una distancia corta. Si se queda atorada lejos, aparece junto a él.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RecepcionSeguidor : MonoBehaviour
    {
        public Transform objetivo;
        public float distancia = 0.9f;
        public float velocidad = 3.3f;
        public float distanciaTeletransporte = 5f;
        Rigidbody2D rb;
        Vector2 mov;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void Start()
        {
            if (objetivo == null) return;
            var a = GetComponent<Collider2D>(); var b = objetivo.GetComponent<Collider2D>();
            if (a && b) Physics2D.IgnoreCollision(a, b);
        }

        void Update()
        {
            mov = Vector2.zero;
            if (objetivo == null) return;
            Vector2 d = objetivo.position - transform.position;
            if (d.magnitude > distanciaTeletransporte)
            {
                rb.position = (Vector2)objetivo.position - d.normalized * distancia;
                return;
            }
            if (d.magnitude > distancia)
                mov = d.normalized * Mathf.Clamp01((d.magnitude - distancia) * 2f + 0.4f);
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
