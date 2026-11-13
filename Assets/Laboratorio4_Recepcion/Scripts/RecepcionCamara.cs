using UnityEngine;

namespace Laboratorio4.Recepcion
{
    /// <summary>Cámara que sigue a Jason sin salirse del mapa de la Recepción.</summary>
    public class RecepcionCamara : MonoBehaviour
    {
        public Transform objetivo;
        public Rect limites;          // en unidades de mundo
        public float suavizado = 8f;
        Camera cam;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (objetivo == null || cam == null) return;
            Vector3 p = transform.position;
            Vector3 meta = new Vector3(objetivo.position.x, objetivo.position.y + 0.4f, p.z);
            p = Vector3.Lerp(p, meta, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
            float h = cam.orthographicSize, w = h * cam.aspect;
            p.x = limites.width > 2 * w ? Mathf.Clamp(p.x, limites.xMin + w, limites.xMax - w) : limites.center.x;
            p.y = limites.height > 2 * h ? Mathf.Clamp(p.y, limites.yMin + h, limites.yMax - h) : limites.center.y;
            transform.position = p;
        }
    }
}
