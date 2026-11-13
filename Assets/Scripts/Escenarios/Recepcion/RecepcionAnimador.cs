using UnityEngine;

namespace Laboratorio4.Recepcion
{
    /// <summary>
    /// Anima a un personaje con sus cuadros de caminar (sacados de los GIF) y lo deja
    /// con su sprite quieto al detenerse. Detecta el movimiento por el cambio de posición,
    /// así funciona igual para Jason (teclado) y para Milena (lo sigue).
    /// Orden de direcciones: Norte, Noreste, Este, Sureste, Sur, Suroeste, Oeste, Noroeste.
    /// </summary>
    public class RecepcionAnimador : MonoBehaviour
    {
        [System.Serializable] public class Cuadros { public Sprite[] cuadros; }

        public Sprite[] quieto = new Sprite[8];
        public Cuadros[] caminar = new Cuadros[8];
        public float cuadrosPorSegundo = 10f;
        public float umbral = 0.15f;   // unidades/segundo para considerar que camina

        SpriteRenderer sr;
        Vector3 ultima;
        int dir = 0;
        float t;
        float vel;

        void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); ultima = transform.position; }

        void LateUpdate()
        {
            if (sr == null || Time.deltaTime <= 0f) return;
            Vector2 d = (Vector2)(transform.position - ultima) / Time.deltaTime;
            ultima = transform.position;
            vel = Mathf.Lerp(vel, d.magnitude, 0.5f);
            bool camina = vel > umbral;
            if (d.sqrMagnitude > umbral * umbral)
            {
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                dir = Mathf.RoundToInt(Mathf.Repeat(90f - ang, 360f) / 45f) % 8;
            }
            if (camina && caminar != null && dir < caminar.Length && caminar[dir] != null &&
                caminar[dir].cuadros != null && caminar[dir].cuadros.Length > 0)
            {
                t += Time.deltaTime * cuadrosPorSegundo;
                var c = caminar[dir].cuadros;
                sr.sprite = c[(int)t % c.Length];
            }
            else
            {
                t = 0f;
                if (quieto != null && dir < quieto.Length && quieto[dir] != null) sr.sprite = quieto[dir];
            }
        }

        public void Mirar(int direccion) { dir = Mathf.Clamp(direccion, 0, 7); }
    }
}
