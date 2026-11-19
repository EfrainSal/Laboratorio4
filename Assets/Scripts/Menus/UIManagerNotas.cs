using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManagerNotas : MonoBehaviour
{
    public static UIManagerNotas Instance { get; private set; }
    public static event System.Action<NotaInteractable> AlCerrarNota;
    public NotaInteractable NotaMostrada { get; private set; }
    private GameObject documentoTexto;
    private TextMeshProUGUI titulo, contenido, avisoCerrar;
    private string contenidoOriginal;

    [Header("Referencias UI")]
    public GameObject panelNota; // Asigna aquí el PanelNotaUI
    public Image imagenNotaMostrada; // Asigna aquí la ImagenNotaMostrada

    public bool NotaActiva => panelNota != null && panelNota.activeSelf;

    private void Awake()
    {
        // Singleton para poder llamarlo fácilmente desde cualquier lado
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MostrarNota(Sprite spriteDeLaNota)
    {
        if (spriteDeLaNota != null)
        {
            imagenNotaMostrada.sprite = spriteDeLaNota;
            imagenNotaMostrada.enabled = true;
            if (documentoTexto != null) documentoTexto.SetActive(false);
            panelNota.SetActive(true);
            AsegurarAvisoCerrar();
            ColocarAvisoCerrar(panelNota.transform);
        }
    }
    public bool MostrarNota(NotaInteractable nota)
    {
        if (nota == null || panelNota == null) return false;
        if (string.IsNullOrWhiteSpace(nota.textoNota))
        {
            if (nota.spriteNotaAltaResolucion == null || imagenNotaMostrada == null) return false;
            MostrarNota(nota.spriteNotaAltaResolucion);
        }
        else
        {
            if (imagenNotaMostrada != null) imagenNotaMostrada.enabled = false;
            if (documentoTexto == null) CrearDocumentoTexto();
            documentoTexto.SetActive(true); titulo.text = nota.tituloNota;
            contenidoOriginal = nota.textoNota; panelNota.SetActive(true); AsegurarAvisoCerrar();
            ColocarAvisoCerrar(documentoTexto.transform);
        }
        NotaMostrada = nota; RefrescarTexto(); return NotaActiva;
    }
    private void CrearDocumentoTexto()
    {
        documentoTexto = new GameObject("DocumentoTutorial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        documentoTexto.transform.SetParent(panelNota.transform, false);
        var rect = documentoTexto.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.17f, .16f); rect.anchorMax = new Vector2(.83f, .86f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        documentoTexto.GetComponent<Image>().color = new Color32(12, 34, 42, 255);
        titulo = CrearTexto("Titulo", documentoTexto.transform, new Vector2(38, -112), new Vector2(-38, -30), 36);
        titulo.fontStyle = FontStyles.Bold; titulo.color = new Color32(110, 190, 193, 255);
        contenido = CrearTexto("Contenido", documentoTexto.transform, new Vector2(38, -130), new Vector2(-38, -30), 29);
        contenido.enableAutoSizing = true; contenido.fontSizeMin = 20; contenido.fontSizeMax = 29;
        var r = contenido.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(38, 85); r.offsetMax = new Vector2(-38, -130);
    }
    private static TextMeshProUGUI CrearTexto(string nombre, Transform padre, Vector2 min, Vector2 max, float tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(padre, false);
        var t = go.GetComponent<TextMeshProUGUI>(); t.font = TMP_Settings.defaultFontAsset; t.fontSize = tamano;
        t.color = new Color32(231, 242, 242, 255); t.raycastTarget = false; t.alignment = TextAlignmentOptions.TopLeft;
        var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
        r.anchorMax = Vector2.one; r.offsetMin = min; r.offsetMax = max;
        return t;
    }
    private void AsegurarAvisoCerrar()
    {
        if (avisoCerrar != null) return;
        avisoCerrar = CrearTexto("CerrarConEntrada", panelNota.transform, new Vector2(30, 0), new Vector2(-30, 52), 25);
        var r = avisoCerrar.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.right; r.offsetMin = new Vector2(30, 20); r.offsetMax = new Vector2(-30, 68);
        avisoCerrar.alignment = TextAlignmentOptions.Center; avisoCerrar.color = new Color32(110, 190, 193, 255);
    }
    private void ColocarAvisoCerrar(Transform padre)
    {
        avisoCerrar.transform.SetParent(padre, false);
        var r = avisoCerrar.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.right;
        r.offsetMin = new Vector2(30, 20); r.offsetMax = new Vector2(-30, 68);
    }
    private void Update()
    {
        if (NotaActiva) RefrescarTexto();
        else if (NotaMostrada != null) { var nota = NotaMostrada; NotaMostrada = null; AlCerrarNota?.Invoke(nota); }
    }
    private void RefrescarTexto()
    {
        if (contenido != null && documentoTexto.activeSelf) contenido.text = TextoInteraccion.Formatear(contenidoOriginal);
        if (avisoCerrar != null) avisoCerrar.text = TextoInteraccion.Aviso("cerrar la nota");
    }

    public void OcultarNota()
    {
        if (panelNota == null || !panelNota.activeSelf) return;
        var nota = NotaMostrada; NotaMostrada = null;
        panelNota.SetActive(false);
        if (imagenNotaMostrada != null) imagenNotaMostrada.sprite = null;
        AlCerrarNota?.Invoke(nota);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
