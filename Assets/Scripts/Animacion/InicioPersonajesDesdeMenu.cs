using UnityEngine;
using LaboratorioYiba.Piso3V10;

namespace Laboratorio4.Animacion
{
    // Conecta la escena de escenario con los prefabs elegidos en el MainMenu.
    [DefaultExecutionOrder(-1000)]
    public class InicioPersonajesDesdeMenu : MonoBehaviour
    {
        public Transform jugadorOriginal;
        public Piso3V10Zorbo[] sorbosOriginales;
        public GameObject prefabJason, prefabMilena, sorboA, sorboB;
        public Piso3V10Camara camara;
        public Material materialEscenario;
        public LevelManager gestorNivel;

        private void Awake()
        {
            Vector3 posicion = jugadorOriginal != null ? jugadorOriginal.position : Vector3.zero;
            if (jugadorOriginal != null) jugadorOriginal.gameObject.SetActive(false);
            foreach (var original in sorbosOriginales) if (original != null) original.gameObject.SetActive(false);
            var prefab = GameManager.Instance != null ? GameManager.Instance.ObtenerPrefabJugador() : prefabJason;
            if (prefab == null) return;
            var jugador = Instantiate(prefab, posicion, Quaternion.identity);
            if (materialEscenario != null) jugador.GetComponent<SpriteRenderer>().sharedMaterial = materialEscenario;
            if (camara != null) camara.objetivo = jugador.transform;
            if (gestorNivel != null) gestorNivel.UsarJugadorExistente(jugador);
        }
    }
}
