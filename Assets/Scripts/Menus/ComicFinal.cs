using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Sólo existe en el mapa del jefe. Las imágenes aprobadas se referencian directamente, fuera de Resources.
[DefaultExecutionOrder(-800), DisallowMultipleComponent]
public class ComicFinal : MonoBehaviour
{
    public BossHealth jefe;
    public Sprite reencuentroJason, reencuentroMilena, ultimasPalabras, escape;
    [Header("Compañero junto al punto de aparición")]
    public Vector2 desplazamientoAyudante = new Vector2(1.8f, -.6f);
    [Min(.2f)] public float radioProximidad = 1f;
    public Vector2 posicionAyudante { get; private set; }
    public Rect zonaReencuentro => new Rect(posicionAyudante - Vector2.one * radioProximidad, Vector2.one * radioProximidad * 2);
    public CircleCollider2D TriggerProximidad { get; private set; }
    public bool EnTransicion => transicion;
    public bool ReencuentroTerminado { get; private set; }
    public bool EsDesenlace { get; private set; }
    public int IndiceActual { get; private set; }
    public Sprite SpriteActual => imagen != null ? imagen.sprite : null;
    public static bool EnCurso => instancia != null && (instancia.leyendo || Time.frameCount <= instancia.bloquearHasta);
    private static ComicFinal instancia;
    private bool leyendo, cerrando, transicion, omitirPendiente, estadoGuardado, pausaAnterior, audioPausaAnterior;
    private float tiempoAnterior, volumenAnterior;
    private int primerFotograma, bloquearHasta = -1;
    private Action alTerminar;
    private bool ayudantePreparado;
    private GameObject interfaz, pie;
    private Image imagen;
    private Texture2D texturaDegradado;
    private Sprite spriteDegradado;
    private TextMeshProUGUI etiquetaSaltar, etiquetaSiguiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { instancia = null; }
    public static bool EsperaCombate(Component actor) => instancia != null && instancia.isActiveAndEnabled &&
        actor != null && actor.gameObject.scene == instancia.gameObject.scene && !instancia.ReencuentroTerminado;

    private void Awake()
    {
        if (gameObject.scene.name != "Escenario4_Jefe" || (instancia != null && instancia != this)) { enabled = false; return; }
        instancia = this;
    }
    private void Update()
    {
        if (!leyendo)
        {
            PrepararAyudante();
            return;
        }
        if (cerrando) return;
        GestorModoEntrada.ActualizarDeteccionAutomatica();
        ActualizarLeyendasControles();

        bool permitirTeclado = GestorModoEntrada.PermitirTecladoYRaton();
        bool permitirMando = GestorModoEntrada.PermitirMando;

        if ((permitirTeclado && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (permitirMando && Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame)))
        { if (transicion) omitirPendiente = true; else Saltar(); return; }
        if (transicion) return;
        if (Time.frameCount <= primerFotograma + 1) return;

        bool avanzarRatonTeclado = permitirTeclado &&
            ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
             (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)));
        bool avanzarMando = permitirMando && Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame);

        if (avanzarRatonTeclado || avanzarMando) Avanzar();
    }
    private void PrepararAyudante()
    {
        if (ayudantePreparado || ReencuentroTerminado || LevelManager.Instance == null) return;
        var ayudante = LevelManager.Instance.AyudanteActual;
        if (ayudante == null || ayudante.scene != gameObject.scene) return;
        var inicio = LevelManager.Instance.puntoDeInicio;
        posicionAyudante = (Vector2)(inicio != null ? inicio.position : LevelManager.Instance.JugadorActual.transform.position) + desplazamientoAyudante;
        ayudante.transform.position = posicionAyudante;
        if (ayudante.TryGetComponent<Rigidbody2D>(out var rb)) { rb.position = posicionAyudante; rb.linearVelocity = Vector2.zero; }
        var zona = new GameObject("TriggerReencuentroCompanero"); zona.transform.SetParent(transform, false); zona.transform.position = posicionAyudante;
        TriggerProximidad = zona.AddComponent<CircleCollider2D>(); TriggerProximidad.isTrigger = true; TriggerProximidad.radius = radioProximidad;
        zona.AddComponent<ProximidadCompaneroFinal>().comic = this;
        ayudantePreparado = true;
    }
    public void NotificarProximidad()
    {
        if (!ayudantePreparado || ReencuentroTerminado || leyendo || jefe == null || jefe.IsDead ||
            TransicionEntrePisos.EnCurso || DeathScreenController.GameplayBlocked) return;
        IniciarReencuentro();
    }
    private void IniciarReencuentro()
    {
        if (reencuentroJason == null || reencuentroMilena == null) return;
        EsDesenlace = false;
        if (TriggerProximidad != null) TriggerProximidad.enabled = false;
        Abrir(() => {
            ReencuentroTerminado = true;
            if (jefe != null) jefe.GetComponent<BossAI>()?.IniciarCombateDramatico();
        });
    }
    // El ranking y RunSummary ya se cerraron al derrotar al jefe; sólo diferimos su presentación.
    public static bool ReproducirDesenlace(Action mostrarVictoria)
    {
        if (instancia == null || !instancia.isActiveAndEnabled || instancia.leyendo ||
            instancia.ultimasPalabras == null || instancia.escape == null) return false;
        instancia.EsDesenlace = true;
        instancia.Abrir(mostrarVictoria);
        return true;
    }
    private void Abrir(Action continuar)
    {
        alTerminar = continuar; IndiceActual = 0; leyendo = true; cerrando = false; omitirPendiente = false;
        tiempoAnterior = Time.timeScale; pausaAnterior = PauseMenuController.PauseBlocked;
        audioPausaAnterior = AudioListener.pause; volumenAnterior = AudioListener.volume; estadoGuardado = true;
        Time.timeScale = 0; PauseMenuController.PauseBlocked = true;
        AudioListener.pause = true; AudioListener.volume = 0;
        StartCoroutine(FundirCambio(() => {
            if (interfaz == null) CrearInterfaz();
            interfaz.SetActive(true); pie.SetActive(true); imagen.enabled = true; Mostrar();
        }, null));
    }
    private void Mostrar()
    {
        int personaje = GameManager.Instance != null ? GameManager.Instance.personajeSeleccionado : PlayerPrefs.GetInt("SelectedCharacter", 0);
        imagen.sprite = EsDesenlace ? (IndiceActual == 0 ? ultimasPalabras : escape) :
            (personaje == 1 ? reencuentroMilena : reencuentroJason);
        Canvas.ForceUpdateCanvases();
    }
    public void Avanzar()
    {
        if (!leyendo || cerrando || transicion) return;
        if (EsDesenlace && IndiceActual == 0)
            StartCoroutine(FundirCambio(() => { IndiceActual = 1; Mostrar(); }, null));
        else Saltar();
    }
    public void Saltar()
    {
        if (!leyendo || cerrando) return;
        if (transicion) { omitirPendiente = true; return; }
        cerrando = true;
        StartCoroutine(Cerrar());
    }
    private IEnumerator Cerrar()
    {
        var continuar = alTerminar; alTerminar = null;
        yield return FundirCambio(() => {
            interfaz.SetActive(false);
            // La victoria se prepara bajo negro, con sus estadísticas originales.
            if (EsDesenlace) { RestaurarEstado(); continuar?.Invoke(); }
        }, () => {
            if (!EsDesenlace) { RestaurarEstado(); continuar?.Invoke(); }
            leyendo = false; bloquearHasta = Time.frameCount; cerrando = false;
        });
    }
    private IEnumerator FundirCambio(Action bajoNegro, Action completo)
    {
        transicion = true;
        while (TransicionEntrePisos.EnCurso) yield return null;
        Time.timeScale = 0; PauseMenuController.PauseBlocked = true;
        TransicionEntrePisos.FundirPantalla(bajoNegro, () => {
            transicion = false; primerFotograma = Time.frameCount; completo?.Invoke();
            if (omitirPendiente && leyendo && !cerrando) { omitirPendiente = false; Saltar(); }
        }, this);
        while (transicion) yield return null;
    }
    private void RestaurarEstado()
    {
        if (!estadoGuardado) return;
        estadoGuardado = false;
        Time.timeScale = tiempoAnterior; PauseMenuController.PauseBlocked = pausaAnterior;
        AudioListener.pause = audioPausaAnterior; AudioListener.volume = volumenAnterior;
    }
    private void CrearInterfaz()
    {
        interfaz = new GameObject("ComicFinalCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        interfaz.transform.SetParent(transform, false);
        var canvas = interfaz.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
        var escala = interfaz.GetComponent<CanvasScaler>(); escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080); escala.matchWidthOrHeight = .5f;
        var fondo = NuevaImagen("FondoNegro", interfaz.transform); Estirar(fondo.rectTransform); fondo.color = Color.black;
        imagen = NuevaImagen("Vineta", interfaz.transform); Estirar(imagen.rectTransform);
        imagen.rectTransform.offsetMin = new Vector2(20, 112); imagen.rectTransform.offsetMax = new Vector2(-20, -16); imagen.preserveAspect = true;
        pie = new GameObject("Navegacion", typeof(RectTransform)); pie.transform.SetParent(interfaz.transform, false); Estirar((RectTransform)pie.transform);
        var degradado = NuevaImagen("DegradadoInferior", pie.transform);
        degradado.rectTransform.anchorMin = Vector2.zero; degradado.rectTransform.anchorMax = Vector2.right;
        degradado.rectTransform.pivot = new Vector2(.5f, 0); degradado.rectTransform.anchoredPosition = Vector2.zero; degradado.rectTransform.sizeDelta = new Vector2(0, 144);
        texturaDegradado = new Texture2D(2, 128, TextureFormat.RGBA32, false) { name = "ComicFinalDegradado", wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 128; y++)
        {
            var color = new Color(.025f, .045f, .055f, .96f * (1f - y / 127f));
            texturaDegradado.SetPixel(0, y, color); texturaDegradado.SetPixel(1, y, color);
        }
        texturaDegradado.Apply(); spriteDegradado = Sprite.Create(texturaDegradado, new Rect(0, 0, 2, 128), new Vector2(.5f, .5f)); degradado.sprite = spriteDegradado;
        etiquetaSaltar = Texto("Esc · Saltar cinemática", false); etiquetaSiguiente = Texto("siguiente >>", true);
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
    private static Image NuevaImagen(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(padre, false);
        var image = go.GetComponent<Image>(); image.raycastTarget = false; return image;
    }
    private static void Estirar(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private TextMeshProUGUI Texto(string contenido, bool derecha)
    {
        var go = new GameObject(derecha ? "Siguiente" : "Saltar", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(pie.transform, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(derecha ? .52f : 0, 0); rect.anchorMax = new Vector2(derecha ? 1 : .48f, 0);
        rect.pivot = new Vector2(.5f, 0); rect.offsetMin = new Vector2(derecha ? 0 : 48, 24); rect.offsetMax = new Vector2(derecha ? -48 : 0, 88);
        var label = go.GetComponent<TextMeshProUGUI>(); label.text = contenido; label.raycastTarget = false;
        label.font = TMP_Settings.defaultFontAsset; label.fontStyle = FontStyles.Bold; label.color = new Color(.94f, .96f, .94f);
        label.alignment = derecha ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft; label.fontSize = derecha ? 32 : 26;
        return label;
    }
    private void OnDisable()
    {
        if (instancia != this) return;
        TransicionEntrePisos.CancelarFundidoLocal(this);
        StopAllCoroutines(); alTerminar = null; leyendo = false; cerrando = false; transicion = false; bloquearHasta = -1;
        RestaurarEstado(); if (interfaz != null) interfaz.SetActive(false);
    }
    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
        if (spriteDegradado != null) Destroy(spriteDegradado);
        if (texturaDegradado != null) Destroy(texturaDegradado);
    }
    private void OnDrawGizmosSelected()
    { Gizmos.color = Color.cyan; Gizmos.DrawWireCube(zonaReencuentro.center, zonaReencuentro.size); }
}
