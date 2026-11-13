using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Personaje jugable de la escena. Por defecto: Jason Smith (tiene caminar y disparo completos).
    /// Milena Rhea queda en la escena desactivada; cambia "Jugable" en el Inspector para usarla.
    /// </summary>
    public class SeleccionPersonaje : MonoBehaviour
    {
        public enum Personaje { JasonSmith, MilenaRhea }
        public Personaje jugable = Personaje.JasonSmith;
        public GameObject jasonSmith;
        public GameObject milenaRhea;
        public CamaraSeguimiento camara;

        void Awake()
        {
            bool jason = jugable == Personaje.JasonSmith;
            if (jasonSmith != null) jasonSmith.SetActive(jason);
            if (milenaRhea != null) milenaRhea.SetActive(!jason);
            var obj = jason ? jasonSmith : milenaRhea;
            if (camara != null && obj != null) camara.objetivo = obj.transform;
        }
    }
}
