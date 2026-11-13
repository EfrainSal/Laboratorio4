using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    /// <summary>Otros scripts (movimiento, disparo...) deben ignorar su input si esto es true.</summary>
    public static bool IsPaused { get; private set; }

    /// <summary>Ponlo en true desde la muerte del jugador o cutscenes para impedir pausar.</summary>
    public static bool PauseBlocked { get; set; }

    private enum View { Main, Options, Confirm }

    private const string HiddenClass = "hidden";
    private const string FadeClass = "pause-fade";
    private const string PipDoneClass = "pip-done";

    [Header("Escenas")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("Partida")]
    [SerializeField] private int _totalFloors = 4;

    [Header("Fondo desenfocado")]
    [Tooltip("Intensidad del desenfoque: 2 suave, 3 medio, 5 muy fuerte")]
    [SerializeField, Range(2, 6)] private int _blurSteps = 3;

    [Header("Audio")]
    [SerializeField] private AudioMixer _mixer;                       // MainMixer
    [SerializeField] private bool _pauseGameAudio = true;
    [SerializeField] private AudioSource[] _keepPlayingWhilePaused;   // p. ej. la música de fondo

    [Header("Personajes (0 = Jason, 1 = Milena)")]
    [SerializeField] private string[] _characterNames = { "JASON", "MILENA" };
    [SerializeField, TextArea(2, 3)] private string[] _characterDescriptions =
    {
        "Descripción corta de Jason.",
        "Descripción corta de Milena."
    };
    [Tooltip("Opcional. Si se deja vacío, se usa el sprite del prefab del jugador.")]
    [SerializeField] private Sprite[] _characterPortraits;

    // UI
    private VisualElement _overlay, _backdrop, _mainView, _optionsView, _confirmView, _characterPortrait;
    private Button _resumeButton, _saveQuitButton, _optionsButton, _abandonButton;
    private Button _optionsBackButton, _confirmAbandonButton, _cancelAbandonButton;
    private Label _timeValue, _floorValue, _killsValue, _scoreValue, _characterName, _characterDesc;
    private List<VisualElement> _floorPips;
    private VolumeChannel _master, _music, _sfx;
    private IVisualElementScheduledItem _hideItem;
    private RenderTexture _blurRT;

    // Input
    private InputAction _pauseAction, _backAction;

    private View _currentView = View.Main;
    private bool _isPaused;

    // ------------------------------------------------------------------
    void Awake()
    {
        IsPaused = false;
        PauseBlocked = false;

        // ESC en teclado y Start/Menú en mandos (Xbox, PlayStation Options, etc.)
        _pauseAction = new InputAction("Pause", InputActionType.Button);
        _pauseAction.AddBinding("<Keyboard>/escape");
        _pauseAction.AddBinding("<Gamepad>/start");

        // B (Xbox) / Círculo (PlayStation): volver un nivel
        _backAction = new InputAction("Back", InputActionType.Button, "<Gamepad>/buttonEast");

        foreach (var source in _keepPlayingWhilePaused)
            if (source != null) source.ignoreListenerPause = true;
    }

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _overlay      = root.Q<VisualElement>("PauseOverlay");
        _backdrop     = root.Q<VisualElement>("PauseBackdrop");   // opcional (puede ser null)
        _mainView     = root.Q<VisualElement>("MainView");
        _optionsView  = root.Q<VisualElement>("OptionsView");
        _confirmView  = root.Q<VisualElement>("ConfirmView");

        _resumeButton         = root.Q<Button>("ResumeButton");
        _saveQuitButton       = root.Q<Button>("SaveQuitButton");
        _optionsButton        = root.Q<Button>("OptionsButton");
        _abandonButton        = root.Q<Button>("AbandonButton");
        _optionsBackButton    = root.Q<Button>("OptionsBackButton");
        _confirmAbandonButton = root.Q<Button>("ConfirmAbandonButton");
        _cancelAbandonButton  = root.Q<Button>("CancelAbandonButton");

        _timeValue = root.Q<Label>("TimeValue");
        _floorValue = root.Q<Label>("FloorValue");
        _killsValue = root.Q<Label>("KillsValue");
        _scoreValue = root.Q<Label>("ScoreValue");
        _characterPortrait = root.Q<VisualElement>("CharacterPortrait");
        _characterName = root.Q<Label>("CharacterName");
        _characterDesc = root.Q<Label>("CharacterDesc");
        _floorPips = root.Query<VisualElement>(className: "floor-pip").ToList();

        _resumeButton.clicked         += Resume;
        _saveQuitButton.clicked       += OnSaveAndQuit;
        _optionsButton.clicked        += OnOpenOptions;
        _abandonButton.clicked        += OnAskAbandon;
        _optionsBackButton.clicked    += OnCloseOptions;
        _confirmAbandonButton.clicked += OnConfirmAbandon;
        _cancelAbandonButton.clicked  += OnCancelAbandon;

        _master = new VolumeChannel(_mixer, "MasterVolume",
            root.Q<Slider>("MasterSlider"), root.Q<Label>("MasterValueLabel"), 0.8f);
        _music = new VolumeChannel(_mixer, "MusicVolume",
            root.Q<Slider>("MusicSlider"), root.Q<Label>("MusicValueLabel"), 0.8f);
        _sfx = new VolumeChannel(_mixer, "SFXVolume",
            root.Q<Slider>("SfxSlider"), root.Q<Label>("SfxValueLabel"), 0.8f);

        _pauseAction.performed += OnPausePressed;
        _backAction.performed  += OnBackPressed;
        _pauseAction.Enable();
    }

    void Start()
    {
        // Aplica los volúmenes guardados al mixer en esta escena
        _master.ApplyCurrent();
        _music.ApplyCurrent();
        _sfx.ApplyCurrent();
    }

    void OnDisable()
    {
        _pauseAction.performed -= OnPausePressed;
        _backAction.performed  -= OnBackPressed;
        _pauseAction.Disable();
        _backAction.Disable();
        _hideItem?.Pause();

        _resumeButton.clicked         -= Resume;
        _saveQuitButton.clicked       -= OnSaveAndQuit;
        _optionsButton.clicked        -= OnOpenOptions;
        _abandonButton.clicked        -= OnAskAbandon;
        _optionsBackButton.clicked    -= OnCloseOptions;
        _confirmAbandonButton.clicked -= OnConfirmAbandon;
        _cancelAbandonButton.clicked  -= OnCancelAbandon;

        _master.Dispose();
        _music.Dispose();
        _sfx.Dispose();

        BackdropBlur.Release(_blurRT);
        _blurRT = null;

        // Seguridad: nunca dejar el juego congelado si el objeto se desactiva en pausa
        if (_isPaused) RestoreTime();
    }

    void OnDestroy()
    {
        _pauseAction?.Dispose();
        _backAction?.Dispose();
    }

    // ------------------------------------------------------------------
    // Entradas
    // ------------------------------------------------------------------
    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        if (TransicionEntrePisos.EnCurso) return;
        if (!_isPaused)
        {
            if (UpgradePanelController.TryClose()) return;   // primero se cierra el panel de mejoras
            if (!PauseBlocked) Pause();
        }
        else
        {
            GoBack();      // ESC/Start: cierra la vista actual o reanuda si estás en la principal
        }
    }

    private void OnBackPressed(InputAction.CallbackContext ctx)
    {
        if (_isPaused) GoBack();
    }

    private void GoBack()
    {
        if (_currentView == View.Main)
        {
            Resume();
        }
        else
        {
            PlayerPrefs.Save();
            ShowView(View.Main);
        }
    }

    // ------------------------------------------------------------------
    // Pausar / reanudar
    // ------------------------------------------------------------------
    public void TogglePause()
    {
        if (_isPaused) Resume();
        else if (!PauseBlocked) Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;
        IsPaused = true;

        Time.timeScale = 0f;                 // también detiene el tiempoRun del GameManager
        if (_pauseGameAudio) AudioListener.pause = true;

        RefreshStats();
        ShowView(View.Main);
        _backAction.Enable();

        StartCoroutine(ShowOverlayRoutine());
    }

    private IEnumerator ShowOverlayRoutine()
    {
        // Con el elemento PauseBackdrop en el UXML: captura y desenfoca la escena antes de mostrar el menú
        if (_backdrop != null)
        {
            RenderTexture blurred = null;
            yield return StartCoroutine(BackdropBlur.Capture(rt => blurred = rt, _blurSteps));

            // Si el jugador reanudó mientras se capturaba, descartamos el resultado
            if (!_isPaused)
            {
                BackdropBlur.Release(blurred);
                yield break;
            }

            BackdropBlur.Release(_blurRT);
            _blurRT = blurred;
            if (_blurRT != null) _backdrop.style.backgroundImage = Background.FromRenderTexture(_blurRT);
        }

        _hideItem?.Pause();
        _overlay.RemoveFromClassList(HiddenClass);
        _overlay.schedule.Execute(() => _overlay.RemoveFromClassList(FadeClass)).StartingIn(20);
    }

    public void Resume()
    {
        if (!_isPaused) return;

        RestoreTime();
        PlayerPrefs.Save();
        _backAction.Disable();

        // Quita el foco para que un botón enfocado no reaccione al input del juego
        _overlay.panel?.focusController?.focusedElement?.Blur();

        _overlay.AddToClassList(FadeClass);
        _hideItem = _overlay.schedule.Execute(() => _overlay.AddToClassList(HiddenClass)).StartingIn(180);
    }

    private void RestoreTime()
    {
        _isPaused = false;
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // ------------------------------------------------------------------
    // Vistas
    // ------------------------------------------------------------------
    private void ShowView(View view)
    {
        _currentView = view;
        _mainView.EnableInClassList(HiddenClass, view != View.Main);
        _optionsView.EnableInClassList(HiddenClass, view != View.Options);
        _confirmView.EnableInClassList(HiddenClass, view != View.Confirm);

        Focusable target = view switch
        {
            View.Main    => _resumeButton,
            View.Options => _master.Slider,
            _            => _cancelAbandonButton   // por seguridad, el foco empieza en "Cancelar"
        };

        // Un instante después, cuando el layout ya está calculado
        _overlay.schedule.Execute(() => target.Focus()).StartingIn(40);
    }

    private void OnOpenOptions() => ShowView(View.Options);

    private void OnCloseOptions()
    {
        PlayerPrefs.Save();
        ShowView(View.Main);
    }

    private void OnAskAbandon() => ShowView(View.Confirm);
    private void OnCancelAbandon() => ShowView(View.Main);

    // ------------------------------------------------------------------
    // Datos de la run
    // ------------------------------------------------------------------
    private void RefreshStats()
    {
        var gm = GameManager.Instance;

        float time  = gm != null ? gm.tiempoRun : 0f;
        int floor   = gm != null ? gm.pisoActual : 1;
        int kills   = gm != null ? gm.enemigosDerrotadosTotal : 0;
        int score   = gm != null ? gm.puntuacionTotal : 0;
        int charIdx = gm != null ? gm.personajeSeleccionado : 0;

        _timeValue.text  = FormatTime(time);
        _floorValue.text = $"{floor} / {_totalFloors}";
        _killsValue.text = kills.ToString("N0");
        _scoreValue.text = score.ToString("N0");

        for (int i = 0; i < _floorPips.Count; i++)
            _floorPips[i].EnableInClassList(PipDoneClass, i < floor);

        // Personaje elegido en esta run
        _characterName.text = charIdx >= 0 && charIdx < _characterNames.Length
            ? _characterNames[charIdx] : "SUJETO";
        _characterDesc.text = charIdx >= 0 && charIdx < _characterDescriptions.Length
            ? _characterDescriptions[charIdx] : "";

        Sprite sprite = null;
        if (_characterPortraits != null && charIdx >= 0 && charIdx < _characterPortraits.Length)
            sprite = _characterPortraits[charIdx];

        if (sprite == null && gm != null)
        {
            var prefab = gm.ObtenerPrefabJugador();
            var renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (renderer != null) sprite = renderer.sprite;
        }

        _characterPortrait.style.backgroundImage = sprite != null
            ? new StyleBackground(sprite)
            : new StyleBackground(StyleKeyword.None);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    // ------------------------------------------------------------------
    // Salir de la partida
    // ------------------------------------------------------------------
    private void OnSaveAndQuit()
    {
        // Si el guardado falla, NO salimos: así no se pierde la run sin querer
        if (GameManager.Instance != null && !GameManager.Instance.GuardarRun())
        {
            Debug.LogError("No se pudo guardar la partida. Se cancela la salida.");
            return;
        }
        GoToMainMenu();
    }

    private void OnConfirmAbandon()
    {
        if (GameManager.Instance != null) GameManager.Instance.AbandonarRun();
        GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        PlayerPrefs.Save();
        RestoreTime();
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
