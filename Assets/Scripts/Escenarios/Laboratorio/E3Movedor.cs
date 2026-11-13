using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Movimiento cinemático por barrido (Jason y Sorbos). Antes de moverse lanza su colisión en la dirección
    /// del movimiento y se detiene justo antes del obstáculo: nunca entra en un muro, objeto, puerta ni personaje.
    /// Se mueve por ejes (X y luego Y), así se desliza por las paredes y no se engancha en las juntas.
    /// Si alguna vez quedara encimado a algo, se separa solo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class E3Movedor : MonoBehaviour
    {
        [Tooltip("Separación mínima que deja con los obstáculos (unidades).")]
        public float piel = 0.015f;

        private Rigidbody2D rb;
        private Collider2D[] propios;
        private ContactFilter2D filtro;
        private readonly RaycastHit2D[] impactos = new RaycastHit2D[16];
        private readonly Collider2D[] solapes = new Collider2D[16];

        public Vector2 Posicion { get { return rb != null ? rb.position : (Vector2)transform.position; } }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.freezeRotation = true;
            propios = GetComponents<Collider2D>();
            filtro = new ContactFilter2D();
            filtro.useTriggers = false;
            filtro.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        }

        private bool EsPropio(Collider2D c)
        {
            if (c == null) return true;
            for (int i = 0; i < propios.Length; i++) if (propios[i] == c) return true;
            return false;
        }

        public void Mover(Vector2 desplazamiento)
        {
            if (rb == null) return;
            Separar();
            if (Mathf.Abs(desplazamiento.x) > 0.000001f) Eje(new Vector2(desplazamiento.x, 0f));
            if (Mathf.Abs(desplazamiento.y) > 0.000001f) Eje(new Vector2(0f, desplazamiento.y));
            Sincronizar();
        }

        public void Detener() { Separar(); Sincronizar(); }

        private void Sincronizar()
        {
            transform.position = new Vector3(rb.position.x, rb.position.y, transform.position.z);
        }

        private void Eje(Vector2 d)
        {
            float dist = d.magnitude;
            Vector2 dir = d / dist;
            float libre = dist;
            int n = rb.Cast(dir, filtro, impactos, dist + piel);
            for (int i = 0; i < n; i++)
            {
                RaycastHit2D h = impactos[i];
                if (EsPropio(h.collider)) continue;
                if (Vector2.Dot(h.normal, dir) >= 0f) continue;   // ya se aleja de ese obstáculo
                float l = h.distance - piel;
                if (l < libre) libre = l;
            }
            if (libre > 0f) rb.position = rb.position + dir * libre;
        }

        /// <summary>Si está encimado a algún collider sólido, lo saca por el camino más corto.</summary>
        private void Separar()
        {
            for (int iter = 0; iter < 3; iter++)
            {
                bool movido = false;
                foreach (Collider2D propio in propios)
                {
                    if (propio == null || !propio.enabled || propio.isTrigger) continue;
                    int n = propio.Overlap(filtro, solapes);
                    for (int i = 0; i < n; i++)
                    {
                        Collider2D otro = solapes[i];
                        if (EsPropio(otro)) continue;
                        ColliderDistance2D cd = otro.Distance(propio);
                        if (cd.isValid && cd.isOverlapped)
                        {
                            rb.position = rb.position + (cd.pointA - cd.pointB);
                            movido = true;
                        }
                    }
                }
                if (!movido) break;
            }
        }
    }
}
