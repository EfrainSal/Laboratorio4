using UnityEngine;
using Laboratorio4;
using Laboratorio4.Escenario3;

namespace Laboratorio4.Animacion
{
    [DefaultExecutionOrder(-1100)]
    public class InicioGameplayEscenario1 : MonoBehaviour
    {
        public GameObject personajesPrototipo;
        public GameObject sorbosPrototipo;
        public GameObject selectorPrototipo;
        public Transform puntoDeInicio;
        public CamaraSeguimiento camara;
        public E3Camara camaraE3;
        public LevelManager gestorNivel;
        public GameObject prefabJason;

        private void Awake()
        {
            if (selectorPrototipo != null) selectorPrototipo.SetActive(false);
            if (personajesPrototipo != null) personajesPrototipo.SetActive(false);
            if (sorbosPrototipo != null) sorbosPrototipo.SetActive(false);
            var prefab = GameManager.Instance != null ? GameManager.Instance.ObtenerPrefabJugador() : prefabJason;
            if (prefab == null || puntoDeInicio == null) return;
            var jugador = Instantiate(prefab, puntoDeInicio.position, Quaternion.identity);
            if (camara != null) camara.objetivo = jugador.transform;
            if (camaraE3 == null) camaraE3 = FindFirstObjectByType<E3Camara>();
            if (camaraE3 != null) camaraE3.objetivo = jugador.transform;
            if (gestorNivel != null) gestorNivel.UsarJugadorExistente(jugador);
        }
    }
}
