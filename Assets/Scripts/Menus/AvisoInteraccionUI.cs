using UnityEngine;
using UnityEngine.UIElements;

// Sólo UI: usa los contactos que habilitan F, sin crear otra zona de interacción.
public class AvisoInteraccionUI : MonoBehaviour
{
    private InteraccionJugador interaccion;
    private VisualElement panel;
    private Label texto;
    private PanelSettings ajustes;
    private GameObject documento;
    public string Mensaje { get; private set; } = "";
    public Object Objetivo { get; private set; }
    public bool Visible => panel != null && panel.style.display.value == DisplayStyle.Flex;
    public bool PuedeMostrarAvisoPuerta(PuertaNivel puerta) => panel != null && isActiveAndEnabled &&
        puerta != null && puerta.JugadorEnRango && LevelManager.Instance != null && LevelManager.Instance.JugadorActual == gameObject;
    private void Start()
    {
        interaccion = GetComponent<InteraccionJugador>();
        PanelSettings referencia = null;
        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            if (doc.gameObject.scene == gameObject.scene && doc.panelSettings != null) { referencia = doc.panelSettings; break; }
        if (referencia == null) return;
        ajustes = Instantiate(referencia); ajustes.name = "AvisoInteraccionPanel";
        ajustes.scaleMode = PanelScaleMode.ScaleWithScreenSize; ajustes.referenceResolution = new Vector2Int(1920, 1080);
        ajustes.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; ajustes.match = 0; ajustes.sortingOrder = 990;
        documento = new GameObject("AvisoInteraccion"); documento.transform.SetParent(transform, false);
        var ui = documento.AddComponent<UIDocument>(); ui.panelSettings = ajustes; ui.sortingOrder = 990;
        var root = ui.rootVisualElement; root.pickingMode = PickingMode.Ignore;
        root.style.position = Position.Absolute; root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
        panel = new VisualElement { name = "AvisoInteraccion" }; texto = new Label { name = "MensajeInteraccion" };
        panel.Add(texto); root.Add(panel); EstiloAvisoRecepcion.Aplicar(panel, texto);
        panel.style.bottom = 112; panel.style.display = DisplayStyle.None;
    }
    private void LateUpdate()
    {
        Mensaje = ""; Objetivo = null;
        if (panel == null) return;
        if (!DeathScreenController.GameplayBlocked && (UIManagerNotas.Instance == null || !UIManagerNotas.Instance.NotaActiva) &&
            LevelManager.Instance != null && LevelManager.Instance.JugadorActual == gameObject)
        {
            if (interaccion != null && interaccion.isActiveAndEnabled && interaccion.NotaCercana != null)
            { Objetivo = interaccion.NotaCercana; Mensaje = TextoInteraccion.Aviso("leer la nota"); }
            else if (interaccion != null && interaccion.isActiveAndEnabled && interaccion.BotiquinCercano != null)
            { Objetivo = interaccion.BotiquinCercano; Mensaje = TextoInteraccion.Aviso("abrir el botiquín"); }
            else
            {
                float distancia = float.MaxValue;
                foreach (var puerta in PuertaNivel.Activas)
                {
                    if (puerta == null || !puerta.isActiveAndEnabled || !puerta.JugadorEnRango || puerta.Cargando || puerta.gameObject.scene != gameObject.scene) continue;
                    float d = (puerta.transform.position - transform.position).sqrMagnitude;
                    if (d < distancia)
                    {
                        distancia = d; Objetivo = puerta;
                        Mensaje = puerta.AvisoVisible ? TextoInteraccion.Formatear(puerta.mensajeFaltaLlave) : TextoInteraccion.Aviso("interactuar con la puerta");
                    }
                }
            }
        }
        texto.text = Mensaje; panel.style.display = Objetivo != null ? DisplayStyle.Flex : DisplayStyle.None;
    }
    private void OnDisable() { if (panel != null) panel.style.display = DisplayStyle.None; Mensaje = ""; Objetivo = null; }
    private void OnDestroy() { if (documento != null) Destroy(documento); if (ajustes != null) Destroy(ajustes); }
}
