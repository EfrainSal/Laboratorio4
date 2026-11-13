using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4
{
    /// <summary>
    /// Puerta de acceso al piso 2. Muestra un aviso al entrar el jugador y, si la escena
    /// indicada está en Build Settings, la carga.
    /// </summary>
    public class PuertaSalida : MonoBehaviour
    {
        public string escenaDestino = "Escenario2";
        public string mensaje = "Acceso al piso 2";
        bool mostrando;

        void OnTriggerEnter2D(Collider2D otro)
        {
            if (!otro.CompareTag("Player")) return;
            mostrando = true;
            Debug.Log("[Laboratorio 4] " + mensaje);
            if (!string.IsNullOrEmpty(escenaDestino) && Application.CanStreamedLevelBeLoaded(escenaDestino))
                TransicionEntrePisos.CargarEscena(escenaDestino);
        }

        void OnTriggerExit2D(Collider2D otro) { if (otro.CompareTag("Player")) mostrando = false; }

        void OnGUI()
        {
            if (!mostrando) return;
            var estilo = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width / 2 - 160, Screen.height - 90, 320, 50), mensaje, estilo);
        }
    }
}
