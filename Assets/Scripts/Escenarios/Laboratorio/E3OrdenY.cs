using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Orden de dibujo por la Y de los pies, con la MISMA fórmula que tus objetos (Piso3V10OrdenVisual):
    /// orden = -y * 100 + desfase. Más abajo en pantalla = se dibuja delante.
    /// La cornisa de cada muro usa el desfase para ordenarse por el borde del muro.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class E3OrdenY : MonoBehaviour
    {
        public bool dinamico = true;
        public int desfase = 0;
        [Tooltip("Si está activo se usa 'ordenFijo' en lugar de la Y.")]
        public bool fijo = false;
        public int ordenFijo = 0;

        private SpriteRenderer sr;

        private void OnEnable() { Aplicar(); }
        private void OnValidate() { Aplicar(); }

        private void LateUpdate()
        {
            if (dinamico || !Application.isPlaying) Aplicar();
        }

        public void Aplicar()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            int o = fijo ? ordenFijo : Mathf.RoundToInt(-transform.position.y * 100f) + desfase;
            sr.sortingOrder = Mathf.Clamp(o, -32000, 32000);
        }
    }
}
