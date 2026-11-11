using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Profundidad 3/4: se dibuja delante lo que tiene los pies más abajo.
    /// Misma fórmula que Piso3V10OrdenVisual (los prefabs), para que todo se ordene igual.
    /// Los objetos estáticos se calculan una vez; los móviles cada frame.
    /// </summary>
    public class OrdenPorY : MonoBehaviour
    {
        public bool dinamico = false;
        public int desplazamiento = 0;
        SpriteRenderer sr;

        void Start() { sr = GetComponent<SpriteRenderer>(); Aplicar(); }
        void LateUpdate() { if (dinamico) Aplicar(); }

        public void Aplicar()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + desplazamiento;
        }
    }
}
