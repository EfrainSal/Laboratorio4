using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Puerta de entrada o de salida. Tiene una colisión sólida en su base (Jason la topa, no la atraviesa).
    /// La de salida además muestra el aviso al tocarla y, si 'escenaSiguiente' está escrita y en Build Settings,
    /// carga ese nivel.
    /// </summary>
    public class E3Puerta : MonoBehaviour
    {
        public bool esSalida = false;
        [Tooltip("Nombre de la escena del siguiente nivel (debe estar en File > Build Settings). Vacío = solo aviso.")]
        public string escenaSiguiente = "";
        [Tooltip("Zona delante de la puerta donde se detecta a Jason (BoxCollider2D marcado como Trigger).")]
        public Collider2D zonaAviso;
        public float esperaAntesDeCargar = 1.2f;

        private E3Jugador jugador;
        private float cargarEn = -1f;

        private void Start() { jugador = FindAnyObjectByType<E3Jugador>(); }

        private void Update()
        {
            if (!esSalida || zonaAviso == null || jugador == null || jugador.Muerto) return;
            Vector2 pies = (Vector2)jugador.transform.position + new Vector2(0f, 0.05f);
            if (!zonaAviso.OverlapPoint(pies)) return;
            if (string.IsNullOrEmpty(escenaSiguiente))
            {
                E3Hud.Mensaje("Acceso al siguiente nivel", 0.5f);
                return;
            }
            E3Hud.Mensaje("Acceso al siguiente nivel...", 1.5f);
            if (cargarEn < 0f) cargarEn = Time.time + esperaAntesDeCargar;
            if (Time.time >= cargarEn)
            {
                if (Application.CanStreamedLevelBeLoaded(escenaSiguiente)) TransicionEntrePisos.CargarEscena(escenaSiguiente);
                else { E3Hud.Mensaje("La escena '" + escenaSiguiente + "' no está en Build Settings", 3f); cargarEn = float.MaxValue; }
            }
        }
    }
}
