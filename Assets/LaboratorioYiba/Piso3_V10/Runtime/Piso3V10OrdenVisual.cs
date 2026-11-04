using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Orden de dibujo por la posición Y del pivote (los sprites usan pivote abajo-centro, en los "pies").
    /// Más abajo en pantalla = se dibuja delante. Solo afecta al dibujo; las colisiones no cambian.
    /// Rango usado en la escena: Fondo -3000, decals -2000, sombras -1500,
    /// objetos/jugador 0..1872, MurosFrente (cornisa sur) 20000.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Piso3V10OrdenVisual : MonoBehaviour
    {
        [Tooltip("Actívalo en objetos que se mueven (jugador, enemigos).")]
        public bool dinamico = false;

        [Tooltip("Ajuste fino si dos objetos comparten la misma Y.")]
        public int desfase = 0;

        private SpriteRenderer sr;

        private void OnEnable()
        {
            sr = GetComponent<SpriteRenderer>();
            Aplicar();
        }

        private void OnValidate()
        {
            sr = GetComponent<SpriteRenderer>();
            Aplicar();
        }

        private void LateUpdate()
        {
            if (dinamico || !Application.isPlaying || transform.hasChanged)
            {
                Aplicar();
                transform.hasChanged = false;
            }
        }

        public void Aplicar()
        {
            if (sr == null) return;
            int orden = Mathf.RoundToInt(-transform.position.y * 100f) + desfase;
            sr.sortingOrder = Mathf.Clamp(orden, -1000, 15000);
        }
    }
}
