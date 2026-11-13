using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UpgradePanelController : MonoBehaviour
{
    /// <summary>true mientras el panel está abierto (el tiempo NO se detiene).</summary>
    public static bool IsOpen { get; private set; }

    private static UpgradePanelController _instance;

    /// <summary>Cierra el panel si está abierto. Devuelve true si lo cerró.</summary>
    public static bool TryClose()
    {
        if (_instance == null || !IsOpen) return false;
        _instance.Close();
        return true;
    }

    private enum Skill { Vida = 0, Defensa = 1, Danio = 2 }

    private const string HiddenClass = "hidden";
    private const string FadeClass = "upg-fade";

    [Header("Valores de mejora por punto")]
    public float aumentoVida = 20f;
    public float aumentoDefensa = 2f;
    public float aumentoDanio = 5f;
    [Min(1)] public int costoPorMejora = 1;

    [Header("Jugador")]
    [Tooltip("Nombres posibles del campo de nivel en PlayerExperience. Añade el tuyo si es distinto.")]
    [SerializeField] private string[] _nombresCampoNivel = { "nivelActual", "nivel", "currentLevel", "level" };
    [SerializeField] private string[] _characterNames = { "JASON", "MILENA" };

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource _openSfx;
    [SerializeField] private AudioSource _upgradeSfx;

    // Referencias al jugador (se buscan al vuelo: LevelManager lo instancia en su Start)
    private PlayerExperience _exp;
    private PlayerCombat _combat;
    private PlayerHealth _health;
    private GameObject _appliedTo;

    // UI
    private VisualElement _overlay, _scanBand;
    private Label _statusRight, _subjectName, _playerLevel, _pointsValue;
    private Button _closeButton;

    private class SkillView
    {
        public VisualElement card, icon;
        public Label level, stat;
        public Button button;
        public System.Action onClick;
    }

    private SkillView[] _views;
    private readonly int[] _localLevels = new int[3];   // respaldo si no hay GameManager
    private readonly List<Texture2D> _textures = new List<Texture2D>();

    private IVisualElementScheduledItem _refreshLoop, _scanLoop, _hideItem;

    // Input
    private InputAction _toggleAction, _backAction;

    // ------------------------------------------------------------------
    void Awake()
    {
        IsOpen = false;

        // TAB en teclado, Select/View en mando (Xbox: View, PlayStation: Share/Create)
        _toggleAction = new InputAction("ToggleUpgrades", InputActionType.Button);
        _toggleAction.AddBinding("<Keyboard>/tab");
        _toggleAction.AddBinding("<Gamepad>/select");

        // B / Círculo: cerrar (solo activo mientras el panel está abierto)
        _backAction = new InputAction("UpgradesBack", InputActionType.Button, "<Gamepad>/buttonEast");
    }

    void OnEnable()
    {
        _instance = this;
        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay      = root.Q<VisualElement>("UpgradeOverlay");
        _scanBand     = root.Q<VisualElement>("ScanBand");
        _statusRight  = root.Q<Label>("StatusRight");
        _subjectName  = root.Q<Label>("SubjectName");
        _playerLevel  = root.Q<Label>("PlayerLevel");
        _pointsValue  = root.Q<Label>("PointsValue");
        _closeButton  = root.Q<Button>("CloseButton");

        _views = new SkillView[3];
        _views[(int)Skill.Vida]    = BuildView(root, "CardHealth",  "IconHealth",  "LevelHealth",  "StatHealth",  "UpgradeHealthButton",  Skill.Vida);
        _views[(int)Skill.Defensa] = BuildView(root, "CardDefense", "IconDefense", "LevelDefense", "StatDefense", "UpgradeDefenseButton", Skill.Defensa);
        _views[(int)Skill.Danio]   = BuildView(root, "CardDamage",  "IconDamage",  "LevelDamage",  "StatDamage",  "UpgradeDamageButton",  Skill.Danio);

        _closeButton.clicked += Close;
        BuildIcons();

        _toggleAction.performed += OnTogglePressed;
        _backAction.performed   += OnBackPressed;
        _toggleAction.Enable();

        // Bucles de actualización: solo corren mientras el panel está abierto
        _refreshLoop = _overlay.schedule.Execute(RefreshAll).Every(250);
        _scanLoop    = _overlay.schedule.Execute(ScanTick).Every(50);
        _refreshLoop.Pause();
        _scanLoop.Pause();
    }

    void Start()
    {
        StartCoroutine(ApplyStoredUpgradesWhenReady());
    }

    void OnDisable()
    {
        _toggleAction.performed -= OnTogglePressed;
        _backAction.performed   -= OnBackPressed;
        _toggleAction.Disable();
        _backAction.Disable();

        _refreshLoop?.Pause();
        _scanLoop?.Pause();
        _hideItem?.Pause();

        _closeButton.clicked -= Close;
        foreach (var view in _views) view.button.clicked -= view.onClick;

        foreach (var tex in _textures) if (tex != null) Destroy(tex);
        _textures.Clear();

        IsOpen = false;
        if (_instance == this) _instance = null;
    }

    void OnDestroy()
    {
        _toggleAction?.Dispose();
        _backAction?.Dispose();
    }

    private SkillView BuildView(VisualElement root, string card, string icon, string level, string stat,
                                string button, Skill skill)
    {
        var view = new SkillView
        {
            card   = root.Q<VisualElement>(card),
            icon   = root.Q<VisualElement>(icon),
            level  = root.Q<Label>(level),
            stat   = root.Q<Label>(stat),
            button = root.Q<Button>(button)
        };
        view.onClick = () => TryUpgrade(skill);
        view.button.clicked += view.onClick;
        return view;
    }

    // ------------------------------------------------------------------
    // Abrir / cerrar
    // ------------------------------------------------------------------
    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        if (IsOpen) Close();
        else Open();
    }

    private void OnBackPressed(InputAction.CallbackContext ctx)
    {
        if (IsOpen) Close();
    }

    public void Open()
    {
        if (IsOpen) return;
        if (PauseMenuController.IsPaused || DeathScreenController.IsShowing) return;

        ResolvePlayer();
        if (_health != null && _health.IsDead) return;

        IsOpen = true;
        RefreshAll();

        _hideItem?.Pause();
        _overlay.RemoveFromClassList(HiddenClass);
        _overlay.schedule.Execute(() => _overlay.RemoveFromClassList(FadeClass)).StartingIn(20);

        _refreshLoop.Resume();
        _scanLoop.Resume();
        _backAction.Enable();

        if (_openSfx != null) _openSfx.Play();

        FocusFirstAvailable();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        _refreshLoop.Pause();
        _scanLoop.Pause();
        _backAction.Disable();

        // Quita el foco para que un botón enfocado no reaccione al input del juego
        _overlay.panel?.focusController?.focusedElement?.Blur();

        _overlay.AddToClassList(FadeClass);
        _hideItem = _overlay.schedule.Execute(() => _overlay.AddToClassList(HiddenClass)).StartingIn(220);
    }

    private void FocusFirstAvailable()
    {
        _overlay.schedule.Execute(() =>
        {
            foreach (var view in _views)
            {
                if (view.button.enabledSelf) { view.button.Focus(); return; }
            }
            _closeButton.Focus();
        }).StartingIn(60);
    }

    // ------------------------------------------------------------------
    // Mejoras
    // ------------------------------------------------------------------
    private void TryUpgrade(Skill skill)
    {
        if (!ResolvePlayer()) return;
        if (GetPoints() < costoPorMejora || IsMaxed(skill)) return;

        _exp.puntosDeMejora -= costoPorMejora;

        switch (skill)
        {
            case Skill.Vida:
                _health.maxHealth += aumentoVida;
                _health.Heal(aumentoVida);
                break;
            case Skill.Defensa:
                _health.defensaPorcentual = Mathf.Min(_health.defensaPorcentual + aumentoDefensa, _health.defensaMaxima);
                break;
            case Skill.Danio:
                _combat.attackDamage += aumentoDanio;
                break;
        }

        SetLevel(skill, GetLevel(skill) + 1);
        RefreshAll();

        // Feedback: destello en la tarjeta y "pulso" en el contador
        var view = _views[(int)skill];
        view.card.AddToClassList("card-flash");
        view.card.schedule.Execute(() => view.card.RemoveFromClassList("card-flash")).StartingIn(250);
        _pointsValue.AddToClassList("points-pulse");
        _pointsValue.schedule.Execute(() => _pointsValue.RemoveFromClassList("points-pulse")).StartingIn(180);

        if (_upgradeSfx != null) _upgradeSfx.Play();

        // Si ya no quedan botones activos (sin puntos), el foco pasa a "Cerrar" para el mando
        bool anyEnabled = false;
        foreach (var v in _views) anyEnabled |= v.button.enabledSelf;
        if (!anyEnabled) _closeButton.Focus();
    }

    private bool IsMaxed(Skill skill)
    {
        if (skill != Skill.Defensa || _health == null) return false;
        return _health.defensaPorcentual >= _health.defensaMaxima - 0.001f;
    }

    // Reaplica las mejoras de la run al jugador recién instanciado (nuevo piso, "Continuar"...)
    private IEnumerator ApplyStoredUpgradesWhenReady()
    {
        for (int i = 0; i < 180 && !ResolvePlayer(); i++) yield return null;
        if (_health == null || _combat == null) yield break;

        yield return null;   // un frame extra: PlayerHealth y el LevelManager ya habrán fijado la vida inicial
        if (_appliedTo == _health.gameObject) yield break;
        _appliedTo = _health.gameObject;

        int lvVida = GetLevel(Skill.Vida), lvDef = GetLevel(Skill.Defensa), lvDanio = GetLevel(Skill.Danio);

        if (lvVida > 0)
        {
            // Solo curamos el bono si el jugador estaba con la vida llena:
            // así un jugador cargado con 70 de vida no se cura de más.
            bool estabaLleno = _health.currentHealth >= _health.maxHealth - 0.01f;

            float bonus = lvVida * aumentoVida;
            if (!_health.MaximoRestaurado) _health.maxHealth += bonus;
            if (estabaLleno && !_health.cargadoDesdeGuardado) 
                _health.Heal(bonus);
            else 
                _health.UpdateHealthUI();
        }
        if (lvDef > 0)
            _health.defensaPorcentual = Mathf.Min(_health.defensaPorcentual + lvDef * aumentoDefensa, _health.defensaMaxima);
        if (lvDanio > 0)
            _combat.attackDamage += lvDanio * aumentoDanio;
    }

    // ------------------------------------------------------------------
    // Datos
    // ------------------------------------------------------------------
    private bool ResolvePlayer()
    {
        if (_exp != null && _combat != null && _health != null) return true;

        GameObject player = LevelManager.Instance != null ? LevelManager.Instance.JugadorActual : null;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        _exp = player.GetComponent<PlayerExperience>();
        _combat = player.GetComponent<PlayerCombat>();
        _health = player.GetComponent<PlayerHealth>();
        return _exp != null && _combat != null && _health != null;
    }

    private int GetPoints() => _exp != null ? (int)_exp.puntosDeMejora : 0;

    private int GetLevel(Skill skill)
    {
        var gm = GameManager.Instance;
        if (gm == null) return _localLevels[(int)skill];
        switch (skill)
        {
            case Skill.Vida:    return gm.nivelMejoraVida;
            case Skill.Defensa: return gm.nivelMejoraDefensa;
            default:            return gm.nivelMejoraDanio;
        }
    }

    private void SetLevel(Skill skill, int value)
    {
        var gm = GameManager.Instance;
        if (gm == null) { _localLevels[(int)skill] = value; return; }
        switch (skill)
        {
            case Skill.Vida:    gm.nivelMejoraVida = value; break;
            case Skill.Defensa: gm.nivelMejoraDefensa = value; break;
            default:            gm.nivelMejoraDanio = value; break;
        }
    }

    // Lee el nivel del jugador por reflexión, probando los nombres de _nombresCampoNivel
    private int ReadPlayerLevel()
    {
        if (_exp == null) return -1;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var type = _exp.GetType();

        foreach (string name in _nombresCampoNivel)
        {
            var field = type.GetField(name, flags);
            if (field != null && IsNumeric(field.FieldType))
                return Mathf.FloorToInt(System.Convert.ToSingle(field.GetValue(_exp)));

            var prop = type.GetProperty(name, flags);
            if (prop != null && prop.CanRead && IsNumeric(prop.PropertyType))
                return Mathf.FloorToInt(System.Convert.ToSingle(prop.GetValue(_exp)));
        }
        return -1;
    }

    private static bool IsNumeric(System.Type t) => t == typeof(int) || t == typeof(float);

    // ------------------------------------------------------------------
    // Refresco de la interfaz
    // ------------------------------------------------------------------
    private void RefreshAll()
    {
        var gm = GameManager.Instance;
        bool hasPlayer = ResolvePlayer();

        _statusRight.text = gm != null ? $"PISO {gm.pisoActual} · {FormatTime(gm.tiempoRun)}" : "PISO 1 · 00:00";

        int charIdx = gm != null ? gm.personajeSeleccionado : 0;
        _subjectName.text = charIdx >= 0 && charIdx < _characterNames.Length ? _characterNames[charIdx] : "SUJETO";

        int playerLevel = ReadPlayerLevel();
        _playerLevel.text = playerLevel >= 0 ? $"NIVEL {playerLevel}" : "NIVEL --";

        int points = GetPoints();
        _pointsValue.text = points.ToString();
        _pointsValue.EnableInClassList("points-zero", points <= 0);

        UpdateCard(Skill.Vida, hasPlayer, points,
            hasPlayer ? $"{Mathf.RoundToInt(_health.maxHealth)} » {Mathf.RoundToInt(_health.maxHealth + aumentoVida)}" : "--");

        string defText = "--";
        if (hasPlayer)
        {
            float cur = _health.defensaPorcentual;
            defText = IsMaxed(Skill.Defensa)
                ? $"{cur:0.#}% (MÁX)"
                : $"{cur:0.#}% » {Mathf.Min(cur + aumentoDefensa, _health.defensaMaxima):0.#}%";
        }
        UpdateCard(Skill.Defensa, hasPlayer, points, defText);

        UpdateCard(Skill.Danio, hasPlayer, points,
            hasPlayer ? $"{_combat.attackDamage:0.#} » {_combat.attackDamage + aumentoDanio:0.#}" : "--");
    }

    private void UpdateCard(Skill skill, bool hasPlayer, int points, string statText)
    {
        var view = _views[(int)skill];
        view.level.text = $"NIV. {GetLevel(skill)}";
        view.stat.text = statText;

        bool maxed = hasPlayer && IsMaxed(skill);
        bool canBuy = hasPlayer && !maxed && points >= costoPorMejora;

        view.button.text = maxed ? "NIVEL MÁXIMO" : $"MEJORAR  ·  {costoPorMejora} PT";
        view.button.SetEnabled(canBuy);
    }

    private void ScanTick()
    {
        float t = Mathf.Repeat(Time.unscaledTime * 0.25f, 1f);
        _scanBand.style.top = Length.Percent(Mathf.Lerp(-15f, 100f, t));
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    // ------------------------------------------------------------------
    // Iconos pixel art generados por código (se tiñen desde el USS)
    // ------------------------------------------------------------------
    private void BuildIcons()
    {
        string[] heart =
        {
            ".###...###.",
            "#####.#####",
            "###########",
            "###########",
            "###########",
            ".#########.",
            "..#######..",
            "...#####...",
            "....###....",
            ".....#....."
        };

        string[] shield =
        {
            "###########",
            "#####.#####",
            "#####.#####",
            "###.....###",
            "#####.#####",
            ".####.####.",
            ".####.####.",
            "..#######..",
            "...#####...",
            "....###....",
            ".....#....."
        };

        string[] sword =
        {
            ".....#.....",
            "....###....",
            "....###....",
            "....###....",
            "....###....",
            "....###....",
            "....###....",
            "###########",
            "###########",
            "....###....",
            "...#####..."
        };

        _views[(int)Skill.Vida].icon.style.backgroundImage    = new StyleBackground(MakeIcon(heart));
        _views[(int)Skill.Defensa].icon.style.backgroundImage = new StyleBackground(MakeIcon(shield));
        _views[(int)Skill.Danio].icon.style.backgroundImage   = new StyleBackground(MakeIcon(sword));
    }

    private Texture2D MakeIcon(string[] rows)
    {
        int w = rows[0].Length, h = rows.Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var clear = new Color(1f, 1f, 1f, 0f);
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                tex.SetPixel(c, h - 1 - r, rows[r][c] == '#' ? Color.white : clear);
        tex.Apply();
        _textures.Add(tex);
        return tex;
    }
}
