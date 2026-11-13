using UnityEngine;

namespace Laboratorio4.Recepcion
{
    /// <summary>
    /// Perspectiva 3/4: ordena el sprite por la altura de sus pies (pivote abajo).
    /// Mientras más abajo en pantalla, más adelante se dibuja. Lo usan los personajes;
    /// los objetos fijos ya traen su orden calculado igual al construir la escena.
    /// </summary>
    [ExecuteAlways]
    public class RecepcionOrdenPorPies : MonoBehaviour
    {
        public int desplazamiento = 0;
        SpriteRenderer[] rs;

        void OnEnable() { rs = GetComponentsInChildren<SpriteRenderer>(true); }

        void LateUpdate()
        {
            if (rs == null) return;
            int orden = Calcular(transform.position.y) + desplazamiento;
            foreach (var r in rs) if (r) r.sortingOrder = orden;
        }

        public static int Calcular(float yMundo) => Mathf.RoundToInt(-yMundo * 100f);
    }
}
