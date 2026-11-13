using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario4
{
    /// <summary>
    /// Puerta de salida: bloqueada (luz roja) hasta que muere el Doctor Yiba. Luego luz verde y, al llegar
    /// Jason frente a ella, carga 'escenaDestino' (si está en Build Settings) o muestra el mensaje final.
    /// </summary>
    public class E4PuertaSalida : MonoBehaviour
    {
        public bool bloqueada = true;
        public string escenaDestino = "";
        public string mensajeFinal = "¡Escapaste del Laboratorio Yiba!";
        public Transform jugador;
        [Tooltip("Zona frente a la puerta (unidades del mundo).")]
        public Rect zonaSalida;
        public SpriteRenderer luz;
        public Sprite luzRoja;
        public Sprite luzVerde;

        public bool Escapo { get; private set; }

        private void Start() { ActualizarLuz(); }

        public void Desbloquear()
        {
            bloqueada = false;
            ActualizarLuz();
        }

        private void ActualizarLuz()
        {
            if (luz != null) luz.sprite = bloqueada ? luzRoja : luzVerde;
        }

        private void Update()
        {
            if (luz != null) luz.color = new Color(1f, 1f, 1f, 0.75f + Mathf.Sin(Time.time * (bloqueada ? 6f : 2f)) * 0.25f);
            if (bloqueada || Escapo || jugador == null) return;
            if (!zonaSalida.Contains((Vector2)jugador.position)) return;
            Escapo = true;
            if (!string.IsNullOrEmpty(escenaDestino) && Application.CanStreamedLevelBeLoaded(escenaDestino))
                TransicionEntrePisos.CargarEscena(escenaDestino);
        }
    }
}
