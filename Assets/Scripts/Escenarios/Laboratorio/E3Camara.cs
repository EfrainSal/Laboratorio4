using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>Cámara ortográfica que sigue a Jason sin salirse del mapa.</summary>
    [RequireComponent(typeof(Camera))]
    public class E3Camara : MonoBehaviour
    {
        public Transform objetivo;
        [Tooltip("Raíz del nivel (E3_Nivel). Los límites son relativos a ella.")]
        public Transform referencia;
        public float suavizado = 0.12f;
        public Rect limites;

        private Camera cam;
        private Vector3 vel;

        private void Awake() { cam = GetComponent<Camera>(); }

        private void Start()
        {
            if (objetivo == null)
            {
                E3Jugador j = FindAnyObjectByType<E3Jugador>();
                if (j != null) objetivo = j.transform;
            }
            if (objetivo != null) transform.position = Destino();
        }

        private Vector3 Destino()
        {
            Vector3 d = new Vector3(objetivo.position.x, objetivo.position.y + 0.3f, transform.position.z);
            Vector2 off = referencia != null ? (Vector2)referencia.position : Vector2.zero;
            float hh = cam.orthographicSize, hw = hh * cam.aspect;
            float x0 = limites.xMin + off.x, x1 = limites.xMax + off.x, y0 = limites.yMin + off.y, y1 = limites.yMax + off.y;
            d.x = (x1 - x0 < hw * 2f) ? (x0 + x1) * 0.5f : Mathf.Clamp(d.x, x0 + hw, x1 - hw);
            d.y = (y1 - y0 < hh * 2f) ? (y0 + y1) * 0.5f : Mathf.Clamp(d.y, y0 + hh, y1 - hh);
            return d;
        }

        private void LateUpdate()
        {
            if (objetivo == null) return;
            transform.position = suavizado > 0f ? Vector3.SmoothDamp(transform.position, Destino(), ref vel, suavizado) : Destino();
        }
    }
}
