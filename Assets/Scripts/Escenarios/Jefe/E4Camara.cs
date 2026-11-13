using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>Cámara ortográfica que sigue a Jason sin salirse del mapa.</summary>
    [RequireComponent(typeof(Camera))]
    public class E4Camara : MonoBehaviour
    {
        public Transform objetivo;
        public float suavizado = 8f;
        public Vector2 desfase = new Vector2(0f, 0.5f);
        [Tooltip("Límites del mapa en unidades (x min, y min, ancho, alto).")]
        public Rect limites;

        private Camera cam;

        private void Awake() { cam = GetComponent<Camera>(); }

        private void LateUpdate()
        {
            if (objetivo == null) return;
            Vector3 meta = objetivo.position + (Vector3)desfase;
            Vector3 p = Vector3.Lerp(transform.position, meta, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
            float alto = cam.orthographicSize, ancho = alto * cam.aspect;
            p.x = limites.width > 2f * ancho ? Mathf.Clamp(p.x, limites.xMin + ancho, limites.xMax - ancho) : limites.center.x;
            p.y = limites.height > 2f * alto ? Mathf.Clamp(p.y, limites.yMin + alto, limites.yMax - alto) : limites.center.y;
            p.z = -10f;
            transform.position = p;
        }
    }
}
