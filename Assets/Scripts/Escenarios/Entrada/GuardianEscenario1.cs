using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Al dar Play revisa el Escenario 1 antes que nada: paredes con colisión, sin restos del Piso 3 y Jason con
    /// su propio controlador. Escribe en la Consola "[Escenario 1] Paredes con colisión: 74/74 piezas".
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GuardianEscenario1 : MonoBehaviour
    {
        public TextAsset layout;

        void Awake()
        {
            string r = ReparacionEscenario1.Reparar(layout, false);
            if (r.Contains("Arreglado")) Debug.LogWarning(r); else Debug.Log(r);
        }
    }
}
