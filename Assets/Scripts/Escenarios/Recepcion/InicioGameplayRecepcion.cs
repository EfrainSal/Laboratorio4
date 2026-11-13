using UnityEngine;

namespace Laboratorio4.Recepcion
{
    [DefaultExecutionOrder(-1100)]
    public class InicioGameplayRecepcion : MonoBehaviour
    {
        public GameObject personajesPrototipo;
        public Transform puntoDeInicio;
        public RecepcionCamara camara;
        public LevelManager gestorNivel;
        public GameObject prefabJason;
        public PantallaControlesInicio pantallaControles;

        private void Awake()
        {
            if (personajesPrototipo != null) personajesPrototipo.SetActive(false);
            var prefab = GameManager.Instance != null ? GameManager.Instance.ObtenerPrefabJugador() : prefabJason;
            if (prefab == null || puntoDeInicio == null) return;
            var jugador = Instantiate(prefab, puntoDeInicio.position, Quaternion.identity);
            if (camara != null) camara.objetivo = jugador.transform;
            if (gestorNivel != null) gestorNivel.UsarJugadorExistente(jugador);
            if (pantallaControles != null) pantallaControles.jugador = jugador;
        }
    }
}
