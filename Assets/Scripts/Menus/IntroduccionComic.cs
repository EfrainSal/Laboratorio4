using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Reproductor visual: ninguna pista, AudioSource ni audio de navegación.
[DisallowMultipleComponent]
public class IntroduccionComic : MonoBehaviour
{
    public const string NombreEscena = "IntroduccionComic";
    public Sprite[] llegada = new Sprite[4];
    public Sprite[] rutaJason = new Sprite[4];
    public Sprite[] rutaMilena = new Sprite[4];
    public string escenaDestino = "Recepcion";
    public int Personaje { get; private set; }
    public int IndiceActual { get; private set; }
    public int TotalVinetas => llegada.Length + Rama.Length;
    public Sprite SpriteActual => imagen != null ? imagen.sprite : null;
    public static bool EnCurso => instancia != null && !instancia.finalizando;
    private Sprite[] Rama => Personaje == 1 ? rutaMilena : rutaJason;
    private static IntroduccionComic instancia;
    private Image imagen;
    private Texture2D degradado;
    private Sprite spriteDegradado;
    private bool finalizando, tiempoGuardado, audioGuardado, pausaAnterior, audioPausadoAnterior;
    private float tiempoAnterior, volumenAnterior;
    private int primerFotograma;

    private TextMeshProUGUI etiquetaSaltar, etiquetaSiguiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { instancia = null; }
    private void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        Personaje = GameManager.Instance != null ? GameManager.Instance.personajeSeleccionado : PlayerPrefs.GetInt("SelectedCharacter", 0);
        Personaje = Personaje == 1 ? 1 : 0;
        tiempoAnterior = Time.timeScale > 0f ? Time.timeScale : 1f;
        pausaAnterior = false;
        tiempoGuardado = true;
        Time.timeScale = 0; PauseMenuController.PauseBlocked = true;
        // Silenciar también la música persistente del menú, sin añadir ni modificar archivos de audio.
        volumenAnterior = AudioListener.volume; audioPausadoAnterior = AudioListener.pause; audioGuardado = true;
        AudioListener.pause = true; AudioListener.volume = 0;
        CrearInterfaz(); primerFotograma = Time.frameCount; Mostrar();
    }
    private void Update()
    {
        if (finalizando) return;
        GestorModoEntrada.ActualizarDeteccionAutomatica();
        ActualizarLeyendasControles();

        bool permitirTeclado = GestorModoEntrada.PermitirTecladoYRaton();
        bool permitirMando = GestorModoEntrada.PermitirMando;

        if (permitirTeclado && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        { Saltar(); return; }

        if (permitirMando && Gamepad.current != null &&
            (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame))
        { Saltar(); return; }

        // El clic o botón que confirmó el personaje no debe saltarse la primera viñeta.
        if (Time.frameCount <= primerFotograma + 1) return;

        bool avanzarRatonTeclado = permitirTeclado &&
            ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
             (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)));

        bool avanzarMando = permitirMando && Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame);

        if (avanzarRatonTeclado || avanzarMando) Avanzar();
    }
    public void Avanzar()
    {
        if (finalizando) return;
        if (IndiceActual + 1 >= TotalVinetas) { Finalizar(); return; }
        IndiceActual++; Mostrar();
    }
    public void Saltar() { Finalizar(); }
    private void Mostrar()
    {
        var sprite = IndiceActual < llegada.Length ? llegada[IndiceActual] : Rama[IndiceActual - llegada.Length];
        imagen.sprite = sprite;
        if (sprite == null) Debug.LogError("Falta la viñeta " + IndiceActual + " de la introducción del personaje " + Personaje);
        // La lectura está pausada: resolver el layout y la geometría de texto al cambiar imagen.
        Canvas.ForceUpdateCanvases();
    }
    private void Finalizar()
    {
        if (finalizando) return;
        if (!Application.CanStreamedLevelBeLoaded(escenaDestino))
        { Debug.LogError("La escena de recepción no está habilitada: " + escenaDestino); return; }
        finalizando = true;
        RestaurarTiempo();
        // El audio permanece silenciado hasta destruir la escena bajo el fundido negro.
        if (!TransicionEntrePisos.CargarEscena(escenaDestino, false))
        {
            finalizando = false;
            tiempoAnterior = Time.timeScale; pausaAnterior = PauseMenuController.PauseBlocked; tiempoGuardado = true;
            Time.timeScale = 0; PauseMenuController.PauseBlocked = true;
        }
    }
    private void CrearInterfaz()
    {
        var raiz = new GameObject("ComicCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        raiz.transform.SetParent(transform, false);
        var canvas = raiz.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2000;
        var escala = raiz.GetComponent<CanvasScaler>(); escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080); escala.matchWidthOrHeight = .5f;
        var fondo = CrearImagen("Fondo", raiz.transform); Estirar(fondo.rectTransform); fondo.color = Color.black;
        imagen = CrearImagen("Vineta", raiz.transform); Estirar(imagen.rectTransform);
        imagen.rectTransform.offsetMin = new Vector2(20, 112); imagen.rectTransform.offsetMax = new Vector2(-20, -16);
        imagen.preserveAspect = true;
        var pie = CrearImagen("DegradadoInferior", raiz.transform);
        pie.rectTransform.anchorMin = Vector2.zero; pie.rectTransform.anchorMax = Vector2.right;
        pie.rectTransform.pivot = new Vector2(.5f, 0); pie.rectTransform.anchoredPosition = Vector2.zero; pie.rectTransform.sizeDelta = new Vector2(0, 144);
        degradado = new Texture2D(2, 128, TextureFormat.RGBA32, false) { name = "ComicDegradadoTemporal", wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 128; y++)
        {
            var color = new Color(.025f, .045f, .055f, .96f * (1f - y / 127f));
            degradado.SetPixel(0, y, color); degradado.SetPixel(1, y, color);
        }
        degradado.Apply(); spriteDegradado = Sprite.Create(degradado, new Rect(0, 0, 2, 128), new Vector2(.5f, .5f)); pie.sprite = spriteDegradado;
        etiquetaSaltar = CrearTexto("Saltar", "Esc · Saltar cinemática", raiz.transform, false);
        etiquetaSiguiente = CrearTexto("Siguiente", "siguiente >>", raiz.transform, true);
        ActualizarLeyendasControles();
    }
    private void ActualizarLeyendasControles()
    {
        bool mando = GestorModoEntrada.UsandoMando;
        if (etiquetaSaltar != null)
            etiquetaSaltar.text = mando ? "Start / B · Saltar cinemática" : "Esc · Saltar cinemática";
        if (etiquetaSiguiente != null)
            etiquetaSiguiente.text = mando ? "A · siguiente >>" : "siguiente >>";
    }
    private static Image CrearImagen(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(padre, false);
        var image = go.GetComponent<Image>(); image.raycastTarget = false; return image;
    }
    private static void Estirar(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static TextMeshProUGUI CrearTexto(string nombre, string contenido, Transform padre, bool derecha)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(padre, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(derecha ? .52f : 0, 0); rect.anchorMax = new Vector2(derecha ? 1 : .48f, 0);
        rect.pivot = new Vector2(.5f, 0); rect.offsetMin = new Vector2(derecha ? 0 : 48, 24); rect.offsetMax = new Vector2(derecha ? -48 : 0, 88);
        var label = go.GetComponent<TextMeshProUGUI>(); label.text = contenido; label.raycastTarget = false;
        label.font = TMP_Settings.defaultFontAsset; label.fontStyle = FontStyles.Bold; label.color = new Color(.94f, .96f, .94f);
        label.alignment = derecha ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = false; label.fontSize = derecha ? 32 : 26;
        label.ForceMeshUpdate();
        return label;
    }
    private void RestaurarTiempo()
    {
        if (!tiempoGuardado) return; tiempoGuardado = false;
        Time.timeScale = tiempoAnterior; PauseMenuController.PauseBlocked = pausaAnterior;
    }
    private void OnDisable()
    {
        if (instancia != this) return;
        RestaurarTiempo();
        if (audioGuardado) { audioGuardado = false; AudioListener.pause = audioPausadoAnterior; AudioListener.volume = volumenAnterior; }
    }
    private void OnDestroy()
    {
        if (instancia != this) return;
        OnDisable(); instancia = null;
        if (spriteDegradado != null) Destroy(spriteDegradado);
        if (degradado != null) Destroy(degradado);
    }
}
