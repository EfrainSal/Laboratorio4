using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Se crea una vez por entrada a un piso. No pausa el juego ni captura botones del HUD.
public class AnuncioPisoUI : MonoBehaviour
{
    public float duracion = 3.2f;
    public float duracionFundido = .4f;
    public string Titulo { get; private set; }
    public bool Visible => grupo != null && grupo.alpha > .01f;
    public float Opacidad => grupo != null ? grupo.alpha : 0;
    private CanvasGroup grupo;
    private RectTransform cartel;
    private float transcurrido;
    private readonly Color celeste = new Color32(110, 190, 193, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Registrar()
    { SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar; }
    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (NumeroPiso(escena.name) == 0) return;
        foreach (var root in escena.GetRootGameObjects()) if (root.GetComponent<AnuncioPisoUI>() != null) return;
        var go = new GameObject("AnuncioProgresionPiso"); SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<AnuncioPisoUI>().Crear(NumeroPiso(escena.name));
    }
    public static int NumeroPiso(string escena)
    {
        switch (escena)
        {
            case "Escenario1_Entrada": return 1;
            case "Piso3_V10": return 2;
            case "E3_Laboratorio": return 3;
            case "Escenario4_Jefe": return 4;
            default: return 0;
        }
    }
    private void Crear(int piso)
    {
        Titulo = new[] { "", "PRIMER PISO", "SEGUNDO PISO", "TERCER PISO", "CUARTO PISO" }[piso];
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 930;
        var escala = gameObject.AddComponent<CanvasScaler>(); escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080); escala.matchWidthOrHeight = 0;
        grupo = gameObject.AddComponent<CanvasGroup>(); grupo.alpha = 0; grupo.interactable = grupo.blocksRaycasts = false;
        cartel = Rectangulo("Cartel", transform, new Color32(9, 26, 33, 241));
        cartel.anchorMin = cartel.anchorMax = new Vector2(.5f, 1); cartel.pivot = new Vector2(.5f, 1);
        cartel.sizeDelta = new Vector2(720, 112); cartel.anchoredPosition = new Vector2(0, -180);
        Borde("BordeSuperior", 0, 109, 720, 3, celeste); Borde("BordeInferior", 0, 0, 720, 2, celeste);
        Borde("MarcaIzquierda", 0, 0, 5, 112, celeste); Borde("MarcaDerecha", 715, 0, 5, 112, celeste);
        for (int i = 0; i < 4; i++) Borde("Segmento" + i, 22 + i * 17, 18, 11, 5, i < piso ? celeste : new Color32(37, 64, 71, 255));
        Texto("HAS ASCENDIDO AL", 17, new Vector2(110, 78), celeste);
        Texto(Titulo, 32, new Vector2(110, 36), new Color32(233, 244, 245, 255)).fontStyle = FontStyles.Bold;
        Texto(piso.ToString("00") + " / 04", 19, new Vector2(585, 18), celeste);
    }
    private static RectTransform Rectangulo(string nombre, Transform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(padre, false);
        var imagen = go.GetComponent<Image>(); imagen.color = color; imagen.raycastTarget = false; return go.GetComponent<RectTransform>();
    }
    private void Borde(string nombre, float x, float y, float ancho, float alto, Color color)
    {
        var r = Rectangulo(nombre, cartel, color); r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(ancho, alto);
    }
    private TextMeshProUGUI Texto(string texto, float tamano, Vector2 posicion, Color color)
    {
        var go = new GameObject(texto, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(cartel, false);
        var t = go.GetComponent<TextMeshProUGUI>(); t.font = TMP_Settings.defaultFontAsset; t.text = texto; t.fontSize = tamano;
        t.color = color; t.raycastTarget = false; t.alignment = TextAlignmentOptions.Left;
        var r = t.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        r.anchoredPosition = posicion; r.sizeDelta = new Vector2(posicion.x > 500 ? 110 : 470, 36); return t;
    }
    private void Update()
    {
        if (grupo == null) return;
        if (DeathScreenController.GameplayBlocked || (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva))
        { grupo.alpha = 0; return; }
        transcurrido += Time.unscaledDeltaTime;
        float entrada = Mathf.SmoothStep(0, 1, transcurrido / Mathf.Max(.05f, duracionFundido));
        float salida = Mathf.SmoothStep(0, 1, (duracion - transcurrido) / Mathf.Max(.05f, duracionFundido));
        grupo.alpha = Mathf.Min(entrada, salida); cartel.anchoredPosition = new Vector2(0, Mathf.Lerp(-155, -180, entrada));
        if (transcurrido >= duracion) Destroy(gameObject);
    }
}
