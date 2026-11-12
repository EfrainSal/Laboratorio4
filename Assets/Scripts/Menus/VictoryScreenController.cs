using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VictoryScreenController : MonoBehaviour
{
    /// <summary>true mientras la pantalla de victoria está activa.</summary>
    public static bool IsShowing { get; private set; }

    private const string HiddenClass = "hidden";
    private const string VisibleClass = "visible";

    [Header("Continuar (cinemática final)")]
    [Tooltip("Escena de la cinemática final. Si lo dejas vacío solo se dispara el evento.")]
    [SerializeField] private string _cinematicSceneName = "FinalCinematic";
    [Tooltip("Evento opcional (p. ej. iniciar un Timeline en la misma escena)")]
    [SerializeField] private UnityEvent _onContinue;

    [Header("Secuencia de victoria")]
    [SerializeField, Range(0.05f, 1f)] private float _slowMotionScale = 0.4f;
    [SerializeField] private float _slowMotionSeconds = 0.6f;
    [SerializeField, Range(2, 6)] private int _blurSteps = 3;
    [SerializeField] private GameObject[] _hideOnVictory;

    [Header("Audio")]
    [SerializeField] private AudioSource _victorySting;
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private float _musicDuckDb = -10f;

    [Header("Mensajes finales")]
    [SerializeField, TextArea(1, 2)] private string[] _messages =
    {
        "SECTOR 4 NEUTRALIZADO",
        "CONTENCIÓN RESTABLECIDA",
        "EL LABORATORIO YA NO ES UNA AMENAZA",
        "LA SALIDA ESTÁ ABIERTA"
    };

    // UI
    private VisualElement _overlay, _backdrop, _vignette, _scanBar, _trophy;
    private Label _ghostA, _ghostB, _titleMain;
    private Label _timeValue, _killsValue, _scoreValue, _bestValue, _recordBadge, _messageText;
    private Button _continueButton;
    private List<VisualElement> _reveals;

    private readonly List<IVisualElementScheduledItem> _loops = new List<IVisualElementScheduledItem>();
    private RenderTexture _blurRT;
    private Texture2D _trophyTex, _vignetteTex;

    private RunSummary _summary;
    private string _messageFull = "";
    private bool _shown;
    private float _burstEnd, _nextBurstAt;

    void Awake()
    {
        IsShowing = false;
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay     = root.Q<VisualElement>("DeathOverlay");
        _backdrop    = root.Q<VisualElement>("DeathBackdrop");
        _vignette    = root.Q<VisualElement>("DeathVignette");
        _scanBar     = root.Q<VisualElement>("ScanBar");
        _trophy      = root.Q<VisualElement>("DeathSkull");

        _ghostA      = root.Q<Label>("TitleGhostA");
        _ghostB      = root.Q<Label>("TitleGhostB");
        _titleMain   = root.Q<Label>("TitleMain");
        _timeValue   = root.Q<Label>("TimeValue");
        _killsValue  = root.Q<Label>("KillsValue");
        _scoreValue  = root.Q<Label>("ScoreValue");
        _bestValue   = root.Q<Label>("BestValue");
        _recordBadge = root.Q<Label>("RecordBadge");
        _messageText = root.Q<Label>("CauseText");

        _continueButton = root.Q<Button>("ContinueButton");
        _reveals = root.Query<VisualElement>(className: "reveal").ToList();

        _continueButton.clicked += OnContinue;
        _continueButton.SetEnabled(false);

        BuildProceduralTextures();

        GameManager.OnRunVictoria += OnRunVictoria;
    }

    void OnDisable()
    {
        GameManager.OnRunVictoria -= OnRunVictoria;
        _continueButton.clicked -= OnContinue;

        foreach (var loop in _loops) loop.Pause();
        _loops.Clear();

        BackdropBlur.Release(_blurRT);
        _blurRT = null;
        if (_trophyTex != null) Destroy(_trophyTex);
        if (_vignetteTex != null) Destroy(_vignetteTex);

        if (IsShowing) RestoreTime();
    }

    // ------------------------------------------------------------------
    private void OnRunVictoria(RunSummary summary)
    {
        if (_shown) return;
        _shown = true;
        _summary = summary;
        StartCoroutine(ShowRoutine());
    }

    [ContextMenu("Probar pantalla de victoria")]
    private void TestShow()
    {
        if (!Application.isPlaying) return;
        _shown = false;
        OnRunVictoria(new RunSummary
        {
            tiempo = 1290f, piso = 4, enemigos = 112, puntuacion = 15820,
            mejorPuntuacion = 15820, nuevoRecord = true
        });
    }

    private IEnumerator ShowRoutine()
    {
        IsShowing = true;
        PauseMenuController.PauseBlocked = true;
        UpgradePanelController.TryClose();

        if (_mixer != null) _mixer.SetFloat("MusicVolume", _musicDuckDb);
        if (_victorySting != null) _victorySting.Play();

        Time.timeScale = _slowMotionScale;
        yield return new WaitForSecondsRealtime(_slowMotionSeconds);

        RenderTexture blurred = null;
        yield return StartCoroutine(BackdropBlur.Capture(rt => blurred = rt, _blurSteps));

        Time.timeScale = 0f;
        foreach (var go in _hideOnVictory)
            if (go != null) go.SetActive(false);

        _blurRT = blurred;
        if (_blurRT != null) _backdrop.style.backgroundImage = Background.FromRenderTexture(_blurRT);

        PopulateUI();

        _overlay.RemoveFromClassList(HiddenClass);
        _overlay.schedule.Execute(() => _overlay.AddToClassList("shown")).StartingIn(30);

        _nextBurstAt = Time.unscaledTime + 3f;
        Track(_overlay.schedule.Execute(ShimmerTick).Every(50));

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
                TriggerBurst(0.5f);
                break;

            case "StatsPanel":
                AnimateNumber(_killsValue, _summary.enemigos, 1.1f);
                AnimateNumber(_scoreValue, _summary.puntuacion, 1.6f);
                break;

            case "CauseRow":
                Typewrite(_messageText, _messageFull, 28);
                break;

            case "DeathActions":
                _continueButton.SetEnabled(true);
                _overlay.schedule.Execute(() => _continueButton.Focus()).StartingIn(60);
                Track(_continueButton.schedule.Execute(() => _continueButton.ToggleInClassList("pulse")).Every(700));
                break;
        }
    }

    private void PopulateUI()
    {
        _timeValue.text  = FormatTime(_summary.tiempo);
        _killsValue.text = "0";
        _scoreValue.text = "0";
        _bestValue.text  = _summary.mejorPuntuacion.ToString("N0");

        _recordBadge.EnableInClassList(HiddenClass, !_summary.nuevoRecord);
        if (_summary.nuevoRecord)
            Track(_recordBadge.schedule.Execute(() => _recordBadge.ToggleInClassList("blink")).Every(450));

        string msg = _messages != null && _messages.Length > 0
            ? _messages[Random.Range(0, _messages.Length)]
            : "SECTOR 4 NEUTRALIZADO";
        _messageFull = "[" + msg + "]";
        _messageText.text = "";
    }

    // ------------------------------------------------------------------
    // Efectos (más suaves que en la pantalla de muerte)
    // ------------------------------------------------------------------
    private void TriggerBurst(float seconds) => _burstEnd = Time.unscaledTime + seconds;

    private void ShimmerTick()
    {
        float now = Time.unscaledTime;

        if (now >= _burstEnd && now >= _nextBurstAt)
        {
            TriggerBurst(0.15f);
            _nextBurstAt = _burstEnd + Random.Range(2.5f, 4.5f);
        }

        bool burst = now < _burstEnd;
        float amp = burst ? 8f : 1.5f;

        _ghostA.style.translate = new Translate(Random.Range(-amp, amp), 0f);
        _ghostB.style.translate = new Translate(Random.Range(-amp, amp), 0f);
        _titleMain.style.opacity = burst ? Random.Range(0.8f, 1f) : 1f;

        float scan = Mathf.Repeat(now * 0.25f, 1f);
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
    // Texturas por código (trofeo pixel art + viñeta verdosa)
    // ------------------------------------------------------------------
    private void BuildProceduralTextures()
    {
        string[] rows =
        {
            "#.#######.#",
            "#.#######.#",
            "#..#####..#",
            ".#.#####.#.",
            "...#####...",
            "....###....",
            ".....#.....",
            ".....#.....",
            "...#####...",
            "..#######.."
        };

        int w = rows[0].Length, h = rows.Length;
        _trophyTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var clear = new Color(0f, 0f, 0f, 0f);
        var gold = new Color(1f, 0.84f, 0.25f, 1f);
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                _trophyTex.SetPixel(c, h - 1 - r, rows[r][c] == '#' ? gold : clear);
        _trophyTex.Apply();
        _trophy.style.backgroundImage = new StyleBackground(_trophyTex);

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
                float alpha = Mathf.SmoothStep(0.3f, 1.2f, uv.magnitude) * 0.85f;
                _vignetteTex.SetPixel(x, y, new Color(0f, 0.07f, 0.04f, alpha));
            }
        }
        _vignetteTex.Apply();
        _vignette.style.backgroundImage = new StyleBackground(_vignetteTex);
    }

    // ------------------------------------------------------------------
    // Continuar → cinemática final
    // ------------------------------------------------------------------
    private void OnContinue()
    {
        _continueButton.SetEnabled(false);   // evita doble clic
        RestoreTime();

        _onContinue?.Invoke();

        if (!string.IsNullOrEmpty(_cinematicSceneName))
            SceneManager.LoadScene(_cinematicSceneName);
    }

    private void RestoreTime()
    {
        IsShowing = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }
}