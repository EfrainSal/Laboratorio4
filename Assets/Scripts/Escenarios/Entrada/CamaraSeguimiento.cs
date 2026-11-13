using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Cámara ortográfica que sigue al jugador sin salirse del mapa.
    /// Tamaño 5.4 = 1 píxel del fondo por píxel de pantalla en 1080p (nítido).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraSeguimiento : MonoBehaviour
    {
        public Transform objetivo;
        public float suavizado = 8f;
        public Rect limites = new Rect(0f, -28.23f, 50.16f, 28.23f);
        Camera cam;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (objetivo == null) return;
            Vector3 p = Vector3.Lerp(transform.position, objetivo.position + new Vector3(0f, 0.45f, 0f), 1f - Mathf.Exp(-suavizado * Time.deltaTime));
            float alto = cam.orthographicSize, ancho = alto * cam.aspect;
            p.x = limites.width > 2 * ancho ? Mathf.Clamp(p.x, limites.xMin + ancho, limites.xMax - ancho) : limites.center.x;
            p.y = limites.height > 2 * alto ? Mathf.Clamp(p.y, limites.yMin + alto, limites.yMax - alto) : limites.center.y;
            p.z = -10f;
            transform.position = p;
        }
    }
}
