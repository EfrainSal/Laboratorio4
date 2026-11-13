using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Profundidad 3/4: orden = -y * 100 + desfase (la Y de los pies; más abajo = más adelante).
    /// Es la misma fórmula que usan los otros escenarios.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class E4OrdenY : MonoBehaviour
    {
        public bool dinamico = true;
        public int desfase = 0;
        [Tooltip("Si se asigna, se usa la Y de este objeto (p. ej. los pies) en lugar de la propia.")]
        public Transform referencia;

        private SpriteRenderer sr;

        private void OnEnable() { Aplicar(); }

        private void LateUpdate()
        {
            if (dinamico || !Application.isPlaying) Aplicar();
        }

        public void Aplicar()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            float y = (referencia != null ? referencia : transform).position.y;
            sr.sortingOrder = Mathf.RoundToInt(-y * 100f) + desfase;
        }
    }
}
