using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Un único fundido persiste entre escenas y cubre tanto el mundo como las interfaces.
[DefaultExecutionOrder(10000)]
public class TransicionEntrePisos : MonoBehaviour
{
    public static bool EnCurso { get; private set; }
    public static float EscalaTiempoAnterior { get; private set; } = 1f;
    public static bool BloqueoPausaAnterior { get; private set; }
    public static float Opacidad => instancia != null && instancia.grupo != null ? instancia.grupo.alpha : 0;
    public static string EscenaDestino { get; private set; }
    private static TransicionEntrePisos instancia;
    [Min(.05f)] public float duracionOscurecer = .35f;
    [Min(.05f)] public float duracionIluminar = .4f;
    private CanvasGroup grupo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstado()
    {
        instancia = null; EnCurso = false; EscenaDestino = null;
        EscalaTiempoAnterior = 1; BloqueoPausaAnterior = false;
    }
    public static bool CargarEscena(string escena)
    {
        if (EnCurso || string.IsNullOrWhiteSpace(escena) || !Application.CanStreamedLevelBeLoaded(escena)) return false;
        if (instancia == null)
        {
            var go = new GameObject("TransicionEntrePisos");
            instancia = go.AddComponent<TransicionEntrePisos>();
            DontDestroyOnLoad(go);
            instancia.CrearCortina();
        }
        EscalaTiempoAnterior = Time.timeScale;
        BloqueoPausaAnterior = PauseMenuController.PauseBlocked;
        EscenaDestino = escena; EnCurso = true;
        instancia.StartCoroutine(instancia.CambiarEscena(escena));
        return true;
    }
    private void CrearCortina()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<GraphicRaycaster>();
        grupo = gameObject.AddComponent<CanvasGroup>(); grupo.alpha = 0; grupo.blocksRaycasts = false;
        var cortina = new GameObject("Negro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cortina.transform.SetParent(transform, false);
        var rect = cortina.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var imagen = cortina.GetComponent<Image>(); imagen.color = Color.black; imagen.raycastTarget = true;
    }
    private IEnumerator CambiarEscena(string escena)
    {
        Time.timeScale = 0; PauseMenuController.PauseBlocked = true; grupo.blocksRaycasts = true;
        try
        {
            yield return Fundir(0, 1, duracionOscurecer);
            // Dar al render un cuadro completamente negro antes de activar el siguiente mapa.
            yield return null;
            var carga = SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
            while (!carga.isDone) yield return null;
            // Awake, Start y las referencias de jugador/HUD se inicializan bajo la cortina.
            yield return null; yield return null;
            Time.timeScale = 0; PauseMenuController.PauseBlocked = true;
            yield return Fundir(1, 0, duracionIluminar);
        }
        finally { Finalizar(); }
    }
    private IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        float inicio = Time.unscaledTime;
        grupo.alpha = desde;
        float t;
        do
        {
            t = Mathf.Clamp01((Time.unscaledTime - inicio) / Mathf.Max(.05f, duracion));
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0, 1, t));
            yield return null;
        } while (t < 1);
        grupo.alpha = hasta;
    }
    private void Update()
    {
        if (EnCurso) { Time.timeScale = 0; PauseMenuController.PauseBlocked = true; }
    }
    private void OnGUI()
    {
        // Los HUD de los mapas del prototipo usan IMGUI: también deben quedar bajo negro.
        if (!EnCurso || grupo == null || grupo.alpha <= 0) return;
        int profundidad = GUI.depth; Color color = GUI.color;
        GUI.depth = int.MinValue; GUI.color = new Color(1, 1, 1, grupo.alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
        GUI.color = color; GUI.depth = profundidad;
    }
    private void Finalizar()
    {
        if (!EnCurso) return;
        if (grupo != null) { grupo.alpha = 0; grupo.blocksRaycasts = false; }
        EnCurso = false; EscenaDestino = null;
        // Una ayuda de controles nueva conserva su propia pausa hasta que el usuario la cierre.
        if (!PantallaControlesInicio.IsShowing)
        {
            Time.timeScale = EscalaTiempoAnterior;
            PauseMenuController.PauseBlocked = BloqueoPausaAnterior;
        }
    }
    private void OnDestroy()
    {
        if (instancia != this) return;
        Finalizar(); instancia = null;
    }
}
