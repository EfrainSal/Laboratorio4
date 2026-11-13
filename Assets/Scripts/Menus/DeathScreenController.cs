using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DeathScreenController : MonoBehaviour
{
    [System.Serializable]
    public class CauseMessages
    {
        public DeathCause cause;
        [TextArea(1, 2)] public string[] messages;
    }

    /// <summary>true mientras la pantalla de muerte está activa.</summary>
    public static bool IsShowing { get; private set; }

    /// <summary>Usa esto en tus scripts de gameplay: bloquea el input en pausa, muerte o panel de mejoras.</summary>
    public static bool GameplayBlocked =>
        IsShowing || VictoryScreenController.IsShowing || PauseMenuController.IsPaused || UpgradePanelController.IsOpen
        || PantallaControlesInicio.BloqueaJuego || TransicionEntrePisos.EnCurso;

    private const string HiddenClass = "hidden";
    private const string VisibleClass = "visible";

    [Header("Escenas")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("Secuencia de muerte")]
    [SerializeField, Range(0.05f, 1f)] private float _slowMotionScale = 0.25f;
    [SerializeField] private float _slowMotionSeconds = 0.9f;
    [Tooltip("Intensidad del desenfoque: 2 suave, 4 medio, 6 muy fuerte")]
    [SerializeField, Range(2, 6)] private int _blurSteps = 4;
    [Tooltip("Objetos a ocultar tras la captura (p. ej. el Canvas del HUD)")]
    [SerializeField] private GameObject[] _hideOnDeath;

    [Header("Audio")]
    [SerializeField] private AudioSource _deathSting;          // Opcional: sonido al morir
    [SerializeField] private AudioMixer _mixer;                // Opcional: baja la música al morir
    [SerializeField] private float _musicDuckDb = -15f;

    [Header("Mensajes de causa de muerte (2-3 por tipo)")]
    [SerializeField]
    private List<CauseMessages> _causeMessages = new List<CauseMessages>
    {
        new CauseMessages { cause = DeathCause.Mutante, messages = new[] {
            "ESPÉCIMEN HOSTIL: CONTACTO FATAL",
            "LOS MUTANTES TE ALCANZARON",
            "TEJIDO ORGÁNICO DESGARRADO POR GARRAS" } },
        new CauseMessages { cause = DeathCause.Disparo, messages = new[] {
            "IMPACTO DE PROYECTIL BIOLÓGICO",
            "NO ESQUIVASTE A TIEMPO",
            "EL ESPÉCIMEN APUNTÓ MEJOR QUE TÚ" } },
        new CauseMessages { cause = DeathCause.Jefe, messages = new[] {
            "EL GUARDIÁN DEL SECTOR 4 TE DETUVO",
            "A UN PASO DE LA SALIDA",
            "EL JEFE NO SE DETIENE PARA NADIE" } },
        new CauseMessages { cause = DeathCause.Entorno, messages = new[] {
            "RIESGO AMBIENTAL NO CONTENIDO",
            "EL LABORATORIO TAMBIÉN MATA",
            "FALLO ESTRUCTURAL DEL SECTOR" } },
        new CauseMessages { cause = DeathCause.Desconocida, messages = new[] {
            "SISTEMA DE CONTENCIÓN COMPROMETIDO",
            "SEÑALES VITALES PERDIDAS",
            "CAUSA DESCONOCIDA: ARCHIVO CORRUPTO" } },
    };

    // Último mensaje usado por causa (para no repetir dos veces seguidas, incluso al reintentar)
    private static readonly Dictionary<DeathCause, int> LastMessageIndex = new Dictionary<DeathCause, int>();

    // UI
    private VisualElement _overlay, _backdrop, _vignette, _scanBar, _glitchTitle, _skull;
    private Label _floorLabel, _ghostA, _ghostB, _titleMain;
    private Label _timeValue, _killsValue, _scoreValue, _bestValue, _recordBadge, _causeText;
    private Button _retryButton, _menuButton;
    private List<VisualElement> _reveals;

    private readonly List<IVisualElementScheduledItem> _loops = new List<IVisualElementScheduledItem>();
    private RenderTexture _blurRT;
    private Texture2D _skullTex, _vignetteTex;

    private RunSummary _summary;
    private string _causeFullText = "";
    private bool _shown;
    private bool _retryRequested;
    private float _burstEnd, _nextBurstAt;

    // ------------------------------------------------------------------
    void Awake()
    {
        IsShowing = false;
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay      = root.Q<VisualElement>("DeathOverlay");
        _backdrop     = root.Q<VisualElement>("DeathBackdrop");
        _vignette     = root.Q<VisualElement>("DeathVignette");
        _scanBar      = root.Q<VisualElement>("ScanBar");
        _glitchTitle  = root.Q<VisualElement>("GlitchTitle");
        _skull        = root.Q<VisualElement>("DeathSkull");

        _floorLabel   = root.Q<Label>("FloorLabel");
        _ghostA       = root.Q<Label>("TitleGhostA");
        _ghostB       = root.Q<Label>("TitleGhostB");
        _titleMain    = root.Q<Label>("TitleMain");
        _timeValue    = root.Q<Label>("TimeValue");
        _killsValue   = root.Q<Label>("KillsValue");
        _scoreValue   = root.Q<Label>("ScoreValue");
        _bestValue    = root.Q<Label>("BestValue");
        _recordBadge  = root.Q<Label>("RecordBadge");
        _causeText    = root.Q<Label>("CauseText");

        _retryButton  = root.Q<Button>("RetryButton");
        _menuButton   = root.Q<Button>("MenuButton");
        _reveals      = root.Query<VisualElement>(className: "reveal").ToList();

        _retryButton.clicked += OnRetry;
        _menuButton.clicked  += OnMenu;
        _retryButton.SetEnabled(false);
        _menuButton.SetEnabled(false);

        BuildProceduralTextures();

        GameManager.OnRunTerminada += OnRunTerminada;
    }

    void OnDisable()
    {
        GameManager.OnRunTerminada -= OnRunTerminada;

        _retryButton.clicked -= OnRetry;
        _menuButton.clicked  -= OnMenu;

        foreach (var loop in _loops) loop.Pause();
        _loops.Clear();

        BackdropBlur.Release(_blurRT);
        _blurRT = null;
        if (_skullTex != null) Destroy(_skullTex);
        if (_vignetteTex != null) Destroy(_vignetteTex);

        if (IsShowing) RestoreTime();
    }

    // ------------------------------------------------------------------
    // Entrada
    // ------------------------------------------------------------------
    private void OnRunTerminada(RunSummary summary)
    {
        if (_shown) return;      // evita doble disparo si PlayerHealth llama dos veces
        _shown = true;
        _summary = summary;
        StartCoroutine(ShowRoutine());
    }

    [ContextMenu("Probar pantalla de muerte")]
    private void TestShow()
    {
        if (!Application.isPlaying) return;
        _shown = false;
        OnRunTerminada(new RunSummary
        {
            tiempo = 290f, piso = 2, enemigos = 37, puntuacion = 4820,
            mejorPuntuacion = 4820, nuevoRecord = true,
            causa = (DeathCause)Random.Range(0, 5)
        });
    }

    // ------------------------------------------------------------------
    // Secuencia
    // ------------------------------------------------------------------
    private IEnumerator ShowRoutine()
    {
        IsShowing = true;
        PauseMenuController.PauseBlocked = true;
        UpgradePanelController.TryClose();                       // CAMBIO: cierra el panel de mejoras si estaba abierto

        if (_mixer != null) _mixer.SetFloat("MusicVolume", _musicDuckDb);
        if (_deathSting != null) _deathSting.Play();

        // 1. Cámara lenta dramática
        Time.timeScale = _slowMotionScale;
        yield return new WaitForSecondsRealtime(_slowMotionSeconds);

        // 2. Captura + desenfoque con la escena aún visible
        RenderTexture blurred = null;
        yield return StartCoroutine(BackdropBlur.Capture(rt => blurred = rt, _blurSteps));

        // 3. Congelar y limpiar
        Time.timeScale = 0f;
        foreach (var go in _hideOnDeath)
            if (go != null) go.SetActive(false);

        _blurRT = blurred;
        if (_blurRT != null) _backdrop.style.backgroundImage = Background.FromRenderTexture(_blurRT);

        PopulateUI();

        // 4. Mostrar y animar
        _overlay.RemoveFromClassList(HiddenClass);
        _overlay.schedule.Execute(() => _overlay.AddToClassList("shown")).StartingIn(30);

        _nextBurstAt = Time.unscaledTime + 2.5f;
        Track(_overlay.schedule.Execute(GlitchTick).Every(50));

        for (int i = 0; i < _reveals.Count; i++)
        {
            var element = _reveals[i];
            long delay = 250 + i * 380;
            _overlay.schedule.Execute(() =>
            {
                element.AddToClassList(VisibleClass);
                OnRevealed(element);
            }).StartingIn(delay);
        }
    }

    private void OnRevealed(VisualElement element)
    {
        switch (element.name)
        {
            case "GlitchTitle":
                TriggerBurst(0.7f);
                break;

            case "StatsPanel":
                AnimateNumber(_killsValue, _summary.enemigos, 1.1f);
                AnimateNumber(_scoreValue, _summary.puntuacion, 1.4f);
                break;

            case "CauseRow":
                Typewrite(_causeText, _causeFullText, 28);
                break;

            case "DeathActions":
                _retryButton.SetEnabled(true);
                _menuButton.SetEnabled(true);
                _overlay.schedule.Execute(() => _retryButton.Focus()).StartingIn(60);
                Track(_retryButton.schedule.Execute(() => _retryButton.ToggleInClassList("pulse")).Every(700));
                break;
        }
    }

    private void PopulateUI()
    {
        _floorLabel.text = $"HAS CAÍDO EN EL PISO {_summary.piso}";
        _timeValue.text  = FormatTime(_summary.tiempo);
        _killsValue.text = "0";
        _scoreValue.text = "0";
        _bestValue.text  = _summary.mejorPuntuacion.ToString("N0");

        _recordBadge.EnableInClassList(HiddenClass, !_summary.nuevoRecord);
        if (_summary.nuevoRecord)
            Track(_recordBadge.schedule.Execute(() => _recordBadge.ToggleInClassList("blink")).Every(450));

        _causeFullText = "[" + PickMessage(_summary.causa) + "]";
        _causeText.text = "";
    }

    // ------------------------------------------------------------------
    // Mensajes de causa de muerte
    // ------------------------------------------------------------------
    private string PickMessage(DeathCause cause)
    {
        string[] pool = FindPool(cause) ?? FindPool(DeathCause.Desconocida);
        if (pool == null) return "SISTEMA DE CONTENCIÓN COMPROMETIDO";

        int index = Random.Range(0, pool.Length);
        if (pool.Length > 1 && LastMessageIndex.TryGetValue(cause, out int last) && index == last)
            index = (index + 1) % pool.Length;

        LastMessageIndex[cause] = index;
        return pool[index];
    }

    private string[] FindPool(DeathCause cause)
    {
        foreach (var entry in _causeMessages)
            if (entry.cause == cause && entry.messages != null && entry.messages.Length > 0)
                return entry.messages;
        return null;
    }

    // ------------------------------------------------------------------
    // Efectos
    // ------------------------------------------------------------------
    private void TriggerBurst(float seconds) => _burstEnd = Time.unscaledTime + seconds;

    private void GlitchTick()
    {
        float now = Time.unscaledTime;

        // Ráfagas de glitch aleatorias
        if (now >= _burstEnd && now >= _nextBurstAt)
        {
            TriggerBurst(Random.Range(0.15f, 0.35f));
            _nextBurstAt = _burstEnd + Random.Range(1.4f, 3.4f);
        }

        bool burst = now < _burstEnd;
        float amp = burst ? 16f : 2.5f;
        float ampY = burst ? 4f : 0f;

        _ghostA.style.translate = new Translate(Random.Range(-amp, amp), Random.Range(-ampY, ampY));
        _ghostB.style.translate = new Translate(Random.Range(-amp, amp), Random.Range(-ampY, ampY));
        _titleMain.style.translate = burst
            ? new Translate(Random.Range(-4f, 4f), 0f)
            : new Translate(0f, 0f);
        _titleMain.style.opacity = burst ? Random.Range(0.65f, 1f) : 1f;

        // Barra de escaneo
        float scan = Mathf.Repeat(now * 0.35f, 1f);
        _scanBar.style.top = Length.Percent(Mathf.Lerp(-15f, 100f, scan));
    }

    private void AnimateNumber(Label label, int target, float seconds)
    {
        float start = Time.unscaledTime;
        IVisualElementScheduledItem item = null;
        item = label.schedule.Execute(() =>
        {
            float t = Mathf.Clamp01((Time.unscaledTime - start) / seconds);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            label.text = Mathf.RoundToInt(target * eased).ToString("N0");
            if (t >= 1f) item.Pause();
        }).Every(16);
        Track(item);
    }

    private void Typewrite(Label label, string text, long msPerChar)
    {
        int count = 0;
        IVisualElementScheduledItem item = null;
        item = label.schedule.Execute(() =>
        {
            count++;
            label.text = text.Substring(0, Mathf.Min(count, text.Length));
            if (count >= text.Length) item.Pause();
        }).Every(msPerChar);
        Track(item);
    }

    private void Track(IVisualElementScheduledItem item) => _loops.Add(item);

    // ------------------------------------------------------------------
    // Texturas generadas por código (calavera pixel art + viñeta)
    // ------------------------------------------------------------------
    private void BuildProceduralTextures()
    {
        string[] rows =
        {
            "..#######..",
            ".#########.",
            "###########",
            "##..###..##",
            "##..###..##",
            "###########",
            "####...####",
            ".#########.",
            "..#.#.#.#..",
            "..#.#.#.#.."
        };

        int w = rows[0].Length, h = rows.Length;
        _skullTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var clear = new Color(0f, 0f, 0f, 0f);
        var red = new Color(0.82f, 0.07f, 0.11f, 1f);
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                _skullTex.SetPixel(c, h - 1 - r, rows[r][c] == '#' ? red : clear);
        _skullTex.Apply();
        _skull.style.backgroundImage = new StyleBackground(_skullTex);

        const int n = 128;
        _vignetteTex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                var uv = new Vector2(x / (n - 1f), y / (n - 1f)) * 2f - Vector2.one;
                float alpha = Mathf.SmoothStep(0.3f, 1.2f, uv.magnitude) * 0.9f;
                _vignetteTex.SetPixel(x, y, new Color(0.08f, 0f, 0f, alpha));
            }
        }
        _vignetteTex.Apply();
        _vignette.style.backgroundImage = new StyleBackground(_vignetteTex);
    }

    // ------------------------------------------------------------------
    // Botones
    // ------------------------------------------------------------------
    private void OnRetry()
    {
        if (_retryRequested || !IsShowing || !_retryButton.enabledSelf || TransicionEntrePisos.EnCurso) return;
        if (!Application.CanStreamedLevelBeLoaded("Recepcion"))
        {
            Debug.LogError("La escena Recepcion debe estar habilitada en Build Settings para reintentar.");
            return;
        }
        _retryRequested = true;
        _retryButton.SetEnabled(false);
        _menuButton.SetEnabled(false);
        foreach (var loop in _loops) loop.Pause();
        var gm = GameManager.Instance;
        int character = gm != null ? gm.personajeSeleccionado : 0;

        RestoreTime();
        if (gm != null) gm.IniciarNuevaRun(character);     // misma elección de personaje
        TransicionEntrePisos.CargarEscena("Recepcion", false);
    }

    private void OnMenu()
    {
        if (_retryRequested || TransicionEntrePisos.EnCurso) return;
        RestoreTime();
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    private void RestoreTime()
    {
        IsShowing = false;
        Time.timeScale = 1f;
        PauseMenuController.PauseBlocked = false;
        AudioListener.pause = false;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }
}
