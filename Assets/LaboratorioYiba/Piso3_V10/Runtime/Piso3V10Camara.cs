using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>Cámara ortográfica que sigue al jugador sin salirse del mapa.</summary>
    [RequireComponent(typeof(Camera))]
    public class Piso3V10Camara : MonoBehaviour
    {
        public Transform objetivo;
        [Tooltip("0 = instantáneo. Valores típicos 0.08 - 0.2")]
        public float suavizado = 0.12f;
        [Tooltip("Límites del mapa en unidades de mundo (x, y, ancho, alto).")]
        public Rect limites = new Rect(0f, -18.72f, 38.88f, 18.72f);

        private Camera cam;
        private Vector3 velocidadRef;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (objetivo == null) return;
            Vector3 destino = new Vector3(objetivo.position.x, objetivo.position.y + 0.3f, transform.position.z);
            Vector3 pos = suavizado > 0f
                ? Vector3.SmoothDamp(transform.position, destino, ref velocidadRef, suavizado)
                : destino;

            float altoMedio = cam.orthographicSize;
            float anchoMedio = altoMedio * cam.aspect;
            if (limites.width > anchoMedio * 2f)
                pos.x = Mathf.Clamp(pos.x, limites.xMin + anchoMedio, limites.xMax - anchoMedio);
            else
                pos.x = limites.center.x;
            if (limites.height > altoMedio * 2f)
                pos.y = Mathf.Clamp(pos.y, limites.yMin + altoMedio, limites.yMax - altoMedio);
            else
                pos.y = limites.center.y;

            transform.position = pos;
        }
    }
}
