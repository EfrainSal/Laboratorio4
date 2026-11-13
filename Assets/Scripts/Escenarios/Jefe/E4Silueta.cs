using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Silueta translúcida del personaje cuando está detrás de un muro de cobertura,
    /// para que el jugador no lo pierda de vista.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class E4Silueta : MonoBehaviour
    {
        public SpriteRenderer origen;
        public Color color = new Color(0.55f, 0.85f, 1f, 0.45f);

        private SpriteRenderer sr;

        private void Awake() { sr = GetComponent<SpriteRenderer>(); }

        private void LateUpdate()
        {
            if (origen == null || sr == null) return;
            sr.sprite = origen.sprite;
            sr.flipX = origen.flipX;
            sr.color = color;
            sr.enabled = origen.enabled && Tapado();
        }

        private bool Tapado()
        {
            Bounds b = origen.bounds;
            float pies = origen.transform.position.y;
            foreach (E4Columna c in E4Columna.Todas)
            {
                if (c == null) continue;
                Bounds cb = c.Limites;
                if (pies > cb.min.y && b.Intersects(cb)) return true;
            }
            return false;
        }
    }
}
