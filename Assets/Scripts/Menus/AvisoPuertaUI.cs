using UnityEngine;
using UnityEngine.UIElements;

// Misma presentación para el objetivo de recepción y los avisos de acceso de los mapas.
public static class EstiloAvisoRecepcion
{
    public static void Aplicar(VisualElement panel, Label texto)
    {
        panel.pickingMode = PickingMode.Ignore;
        panel.style.position = Position.Absolute;
        panel.style.bottom = 27;
        panel.style.alignSelf = Align.Center;
        panel.style.maxWidth = Length.Percent(94);
        panel.style.paddingTop = panel.style.paddingBottom = 13;
        panel.style.paddingLeft = panel.style.paddingRight = 24;
        panel.style.backgroundColor = (Color)new Color32(7, 19, 23, 240);
        panel.style.borderLeftWidth = 3;
        panel.style.borderLeftColor = (Color)new Color32(111, 179, 151, 255);
        texto.pickingMode = PickingMode.Ignore;
        texto.style.color = (Color)new Color32(223, 236, 226, 255);
        texto.style.fontSize = 18;
        texto.style.unityFontStyleAndWeight = FontStyle.Bold;
        texto.style.whiteSpace = WhiteSpace.Normal;
    }
}

[RequireComponent(typeof(UIDocument))]
public class AvisoPuertaUI : MonoBehaviour
{
    private VisualElement panel;
    private Label texto;
    private PanelSettings ajustes;
    private float fin;
    public bool Visible => panel != null && Time.unscaledTime < fin && !DeathScreenController.GameplayBlocked;
    public string Mensaje => texto != null ? texto.text : "";

    public static bool Mostrar(PuertaNivel puerta, string mensaje, float duracion)
    {
        var recepcion = FindFirstObjectByType<PantallaControlesInicio>();
        if (recepcion != null && recepcion.MostrarAvisoPuerta(mensaje, duracion)) return true;
        AvisoPuertaUI aviso = null;
        foreach (var actual in FindObjectsByType<AvisoPuertaUI>(FindObjectsSortMode.None))
            if (actual.gameObject.scene == puerta.gameObject.scene) { aviso = actual; break; }
        if (aviso == null)
        {
            PanelSettings referencia = null;
            foreach (var documento in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                if (documento.gameObject.scene == puerta.gameObject.scene && documento.panelSettings != null)
                { referencia = documento.panelSettings; break; }
            if (referencia == null) return false;
            var go = new GameObject("AvisosPuerta");
            var canvas = puerta.textoMensajeUI != null ? puerta.textoMensajeUI.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas != null) go.transform.SetParent(canvas.transform, false);
            else UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, puerta.gameObject.scene);
            var ui = go.AddComponent<UIDocument>();
            // Copiar los ajustes conserva fuentes/tema sin alterar el asset que comparten otras UI.
            var copia = Instantiate(referencia);
            copia.name = "AvisosPuertaPanel"; copia.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            copia.referenceResolution = new Vector2Int(1920, 1080);
            copia.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; copia.match = 0;
            copia.sortingOrder = 1000;
            ui.panelSettings = copia; ui.sortingOrder = 1000;
            aviso = go.AddComponent<AvisoPuertaUI>(); aviso.ajustes = copia;
            var root = ui.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            aviso.panel = new VisualElement { name = "AvisoAccesoPuerta" };
            aviso.panel.AddToClassList("recepcion-objetivo");
            aviso.texto = new Label { name = "MensajeAccesoPuerta" };
            aviso.texto.AddToClassList("recepcion-objetivo-texto");
            aviso.panel.Add(aviso.texto); root.Add(aviso.panel);
            EstiloAvisoRecepcion.Aplicar(aviso.panel, aviso.texto);
        }
        aviso.texto.text = mensaje; aviso.fin = Time.unscaledTime + Mathf.Max(.1f, duracion);
        aviso.panel.style.display = DisplayStyle.Flex;
        return true;
    }
    private void Update()
    {
        if (panel != null) panel.style.display = Visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    private void OnDestroy() { if (ajustes != null) Destroy(ajustes); }
}
