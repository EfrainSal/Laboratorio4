using UnityEngine;

namespace Laboratorio4
{
    /// <summary>Marca de un vano/puerta (umbral de franjas pintado en el suelo). Solo informativo.</summary>
    public class PuertaInfo : MonoBehaviour
    {
        public string orientacion = "V";
        [Tooltip("Ancho libre de paso en píxeles del fondo (100 px = 1 unidad)")]
        public int aberturaPx = 168;
        public Vector2 tamano = new Vector2(0.2f, 1.6f);

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(tamano.x, tamano.y, 0f));
        }
    }
}
