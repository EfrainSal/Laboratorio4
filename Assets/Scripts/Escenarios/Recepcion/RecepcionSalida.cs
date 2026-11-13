using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Recepcion
{
    /// <summary>Zona frente a la puerta norte: al tocarla Jason pasa al Escenario 1.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class RecepcionSalida : MonoBehaviour
    {
        [Tooltip("Nombre exacto de la escena del Escenario 1 (debe estar en Build Settings).")]
        public string escenaDestino = "Escenario1_Entrada";
        bool usada;

        void Reset() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D otro)
        {
            if (usada || otro.GetComponentInParent<RecepcionJugador>() == null) return;
            if (Application.CanStreamedLevelBeLoaded(escenaDestino))
            {
                usada = true;
                TransicionEntrePisos.CargarEscena(escenaDestino);
            }
            else
                Debug.LogWarning("[Recepción] La escena '" + escenaDestino + "' no está en Build Settings. Agrégala o cambia 'Escena Destino' en Salida_Escenario1.");
        }
    }
}
