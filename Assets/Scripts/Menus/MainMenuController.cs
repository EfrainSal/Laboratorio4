using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private const string SaveKey = "HasSave";                 // placeholder: reemplázalo por tu sistema de guardado
    private const string SelectedCharacterKey = "SelectedCharacter";

    private const string HiddenClass = "hidden";
    private const string PanelFadeClass = "panel-fade";
    private const string PanelWideClass = "panel-wide";
    private const string MenuHiddenClass = "menu-hidden";
    private const string MenuButtonClass = "btn-menu";
    private const string LogoDimClass = "logo-dim";
    private const string ActiveClass = "btn-active";
    private const string SelectedCardClass = "card-selected";

    [Header("Fondo de video")]
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RenderTexture _videoTexture;
    [SerializeField, Range(0.1f, 2f)] private float _videoSpeed = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioMixer _mixer;               // MainMixer
    [SerializeField] private AudioSource _sfxPreview;         // Output = grupo SFX

    [Header("Escenas")]
    [SerializeField] private string _gameSceneName = "SampleScene";

    private VisualElement _root, _menuContainer, _logo, _videoBackground, _leftShade;
    private VisualElement _sidePanel, _pagePlay, _pageSettings;
    private Button _playButton, _continueButton, _settingsButton, _quitButton;
    private Button _playBackButton, _confirmButton, _settingsBackButton;
    private VisualElement[] _cards;
    private EventCallback<ClickEvent>[] _cardCallbacks;
    private VolumeChannel _master, _music, _sfx;

    private IVisualElementScheduledItem _flickerLoop, _hidePanelItem, _sfxPreviewItem;
    private Texture2D _leftShadeTexture, _buttonGradientTexture;
    private int _selectedCharacter = -1;
    private bool _panelOpen;

    // ------------------------------------------------------------------
    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _videoBackground = _root.Q<VisualElement>("VideoBackground");
        _leftShade       = _root.Q<VisualElement>("LeftShade");
        _menuContainer   = _root.Q<VisualElement>("MenuContainer");
        _logo            = _root.Q<VisualElement>("Logo");
        _sidePanel       = _root.Q<VisualElement>("SidePanel");
        _pagePlay        = _root.Q<VisualElement>("PagePlay");
        _pageSettings    = _root.Q<VisualElement>("PageSettings");

        _continueButton     = _root.Q<Button>("ContinueButton");
        _playButton         = _root.Q<Button>("PlayButton");
        _settingsButton     = _root.Q<Button>("SettingsButton");
        _quitButton         = _root.Q<Button>("QuitButton");
        _playBackButton     = _root.Q<Button>("PlayBackButton");
        _confirmButton      = _root.Q<Button>("ConfirmButton");
        _settingsBackButton = _root.Q<Button>("BackButton");

        _continueButton.clicked     += OnContinueClicked;
        _playButton.clicked         += OpenPlayPage;
        _settingsButton.clicked     += OpenSettingsPage;
        _quitButton.clicked         += OnQuitClicked;
        _playBackButton.clicked     += ClosePanel;
        _settingsBackButton.clicked += ClosePanel;
        _confirmButton.clicked      += OnConfirmCharacter;

        _continueButton.SetEnabled(SaveSystem.HayGuardado());
        _confirmButton.SetEnabled(false);

        // Tarjetas de personaje
        _cards = new[] { _root.Q<VisualElement>("CharacterCard0"), _root.Q<VisualElement>("CharacterCard1") };
        _cardCallbacks = new EventCallback<ClickEvent>[_cards.Length];
        for (int i = 0; i < _cards.Length; i++)
        {
            int index = i;
            _cardCallbacks[i] = _ => SelectCharacter(index);
            _cards[i].RegisterCallback(_cardCallbacks[i]);
        }

        // Escape / botón "cancelar" del gamepad cierra el panel
        _root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);

        // Audio
        _master = new VolumeChannel(_mixer, "MasterVolume",
            _root.Q<Slider>("MasterSlider"), _root.Q<Label>("MasterValueLabel"), 0.8f);
        _music = new VolumeChannel(_mixer, "MusicVolume",
            _root.Q<Slider>("MusicSlider"), _root.Q<Label>("MusicValueLabel"), 0.8f);
        _sfx = new VolumeChannel(_mixer, "SFXVolume",
            _root.Q<Slider>("SfxSlider"), _root.Q<Label>("SfxValueLabel"), 0.8f);
        _sfx.Changed += OnSfxChanged;

        // Fondo: video + degradado lateral
        if (_videoTexture != null)
            _videoBackground.style.backgroundImage = Background.FromRenderTexture(_videoTexture);

        _leftShadeTexture = CreateGradientTexture(Color.black, 0.88f, 0f, true);
        _leftShade.style.backgroundImage = new StyleBackground(_leftShadeTexture);

        // Degradado de los botones del menú (el color lo controla el USS con el tint)
        _buttonGradientTexture = CreateGradientTexture(Color.white, 1f, 0f, false);
        _root.Query<Button>(className: MenuButtonClass)
             .ForEach(b => b.style.backgroundImage = new StyleBackground(_buttonGradientTexture));

        // Animación de entrada + foco inicial en "Nueva partida"
        _menuContainer.schedule.Execute(() =>
        {
            _menuContainer.RemoveFromClassList(MenuHiddenClass);
            _playButton.Focus();
        }).StartingIn(100);

        // Parpadeo de luces del logo
        _flickerLoop = _logo.schedule.Execute(FlickerLogo).Every(4000);
    }

    void Start()
    {
        _master.ApplyCurrent();
        _music.ApplyCurrent();
        _sfx.ApplyCurrent();

        if (_videoPlayer != null && _videoTexture != null)
        {
            _videoPlayer.Stop();                       // por si "Play On Awake" ya lo arrancó
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _videoTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.playbackSpeed = _videoSpeed;
            _videoPlayer.prepareCompleted += OnVideoPrepared;
            _videoPlayer.Prepare();
        }
    }

    void Update()
    {
        // Refuerza la velocidad por si el reproductor la restablece (p. ej. al reiniciar el loop)
        if (_videoPlayer != null && _videoPlayer.isPlaying &&
            !Mathf.Approximately(_videoPlayer.playbackSpeed, _videoSpeed))
        {
            _videoPlayer.playbackSpeed = _videoSpeed;
        }
    }

    void OnDisable()
    {
        _flickerLoop?.Pause();
        _hidePanelItem?.Pause();
        _sfxPreviewItem?.Pause();

        _continueButton.clicked     -= OnContinueClicked;
        _playButton.clicked         -= OpenPlayPage;
        _settingsButton.clicked     -= OpenSettingsPage;
        _quitButton.clicked         -= OnQuitClicked;
        _playBackButton.clicked     -= ClosePanel;
        _settingsBackButton.clicked -= ClosePanel;
        _confirmButton.clicked      -= OnConfirmCharacter;

        for (int i = 0; i < _cards.Length; i++)
            _cards[i].UnregisterCallback(_cardCallbacks[i]);

        _root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);

        _sfx.Changed -= OnSfxChanged;
        _master.Dispose();
        _music.Dispose();
        _sfx.Dispose();

        if (_videoPlayer != null) _videoPlayer.prepareCompleted -= OnVideoPrepared;
        if (_leftShadeTexture != null) Destroy(_leftShadeTexture);
        if (_buttonGradientTexture != null) Destroy(_buttonGradientTexture);
    }

    // ------------------------------------------------------------------
    // Fondo
    // ------------------------------------------------------------------
    private void OnVideoPrepared(VideoPlayer source)
    {
        if (!source.canSetPlaybackSpeed)
            Debug.LogWarning("Este video/plataforma no permite cambiar la velocidad. Reexporta el video ya a cámara lenta.");

        source.Play();
        source.playbackSpeed = _videoSpeed;   // 0.5 = cámara lenta
    }

    // Degradado horizontal generado por código (no necesitas imágenes)
    private static Texture2D CreateGradientTexture(Color color, float alphaStart, float alphaEnd, bool smooth)
    {
        const int width = 256;
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        for (int x = 0; x < width; x++)
        {
            float t = x / (width - 1f);
            if (smooth) t = Mathf.SmoothStep(0f, 1f, t);
            tex.SetPixel(x, 0, new Color(color.r, color.g, color.b, Mathf.Lerp(alphaStart, alphaEnd, t)));
        }
        tex.Apply();
        return tex;
    }

    private void FlickerLogo()
    {
        _logo.AddToClassList(LogoDimClass);
        _logo.schedule.Execute(() => _logo.RemoveFromClassList(LogoDimClass)).StartingIn(70);
        _logo.schedule.Execute(() => _logo.AddToClassList(LogoDimClass)).StartingIn(140);
        _logo.schedule.Execute(() => _logo.RemoveFromClassList(LogoDimClass)).StartingIn(210);
    }

    // ------------------------------------------------------------------
    // Panel lateral
    // ------------------------------------------------------------------
    private void OpenPlayPage()
    {
        ClearCharacterSelection();
        ShowPage(_pagePlay, _playButton);
    }

    private void OpenSettingsPage() => ShowPage(_pageSettings, _settingsButton);

    private void ShowPage(VisualElement page, Button activeButton)
    {
        _pagePlay.AddToClassList(HiddenClass);
        _pageSettings.AddToClassList(HiddenClass);
        page.RemoveFromClassList(HiddenClass);

        // La selección de personaje usa un panel más ancho que las opciones
        _sidePanel.EnableInClassList(PanelWideClass, page == _pagePlay);

        SetActiveButton(activeButton);

        _hidePanelItem?.Pause();
        _sidePanel.RemoveFromClassList(HiddenClass);                       // display: flex
        _sidePanel.schedule.Execute(() => _sidePanel.RemoveFromClassList(PanelFadeClass)).StartingIn(20);
        _panelOpen = true;
    }

    private void ClosePanel()
    {
        if (!_panelOpen) return;
        _panelOpen = false;

        SetActiveButton(null);
        _sidePanel.AddToClassList(PanelFadeClass);                         // fade out
        _hidePanelItem = _sidePanel.schedule.Execute(() => _sidePanel.AddToClassList(HiddenClass)).StartingIn(320);
        PlayerPrefs.Save();                                                // guarda volúmenes
    }

    private void OnNavigationCancel(NavigationCancelEvent evt)
    {
        if (!_panelOpen) return;
        ClosePanel();
        evt.StopPropagation();
    }

    private void SetActiveButton(Button active)
    {
        _playButton.EnableInClassList(ActiveClass, active == _playButton);
        _settingsButton.EnableInClassList(ActiveClass, active == _settingsButton);
    }

    // ------------------------------------------------------------------
    // Selección de personaje
    // ------------------------------------------------------------------
    private void SelectCharacter(int index)
    {
        _selectedCharacter = index;
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].EnableInClassList(SelectedCardClass, i == index);
        _confirmButton.SetEnabled(index >= 0);
    }

    private void ClearCharacterSelection() => SelectCharacter(-1);

    private void OnConfirmCharacter()
    {
        if (_selectedCharacter < 0) return;

        // Avisamos al GameManager que inicie una nueva Run con este personaje
        if (GameManager.Instance != null)
        {
            GameManager.Instance.IniciarNuevaRun(_selectedCharacter);
        }
        else
        {
            Debug.LogWarning("No se encontró el GameManager en la escena del menú.");
        }

        // TODO (GameManager): aquí le pasaremos el personaje elegido y crearemos la run.
        PlayerPrefs.SetInt(SelectedCharacterKey, _selectedCharacter);
        SaveSystem.Borrar();
        SceneManager.LoadScene(_gameSceneName);
    }

    // ------------------------------------------------------------------
    // Audio: preview de SFX
    // ------------------------------------------------------------------
    // Suena 200 ms después del último cambio del slider (funciona con mouse, teclado y gamepad)
    private void OnSfxChanged(float value)
    {
        if (_sfxPreview == null) return;

        _sfxPreviewItem?.Pause();
        _sfxPreviewItem = _root.schedule.Execute(PlaySfxPreview).StartingIn(200);
    }

    private void PlaySfxPreview()
    {
        _sfxPreview.Stop();
        _sfxPreview.Play();
    }

    // ------------------------------------------------------------------
    // Resto de botones
    // ------------------------------------------------------------------
// 4. Reemplaza OnContinueClicked en MainMenuController.cs
    private void OnContinueClicked()
    {
        // Solo carga la escena si realmente existe y se pudo cargar la run
        if (GameManager.Instance != null && GameManager.Instance.CargarRunGuardada())
        {
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            Debug.LogWarning("No se encontró partida guardada válida.");
            _continueButton.SetEnabled(false); // Deshabilita el botón por seguridad
        }
    }

    private void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ------------------------------------------------------------------
    // Une un Slider + Label con un parámetro expuesto del AudioMixer
    // ------------------------------------------------------------------
    private class VolumeChannel
    {
        public readonly Slider Slider;
        public event System.Action<float> Changed;

        private readonly AudioMixer _mixer;
        private readonly string _param;
        private readonly string _prefsKey;
        private readonly Label _label;
        private readonly EventCallback<ChangeEvent<float>> _callback;

        public VolumeChannel(AudioMixer mixer, string param, Slider slider, Label label, float defaultValue)
        {
            _mixer = mixer;
            _param = param;
            _prefsKey = "vol_" + param;
            Slider = slider;
            _label = label;

            float saved = PlayerPrefs.GetFloat(_prefsKey, defaultValue);
            Slider.SetValueWithoutNotify(saved);
            UpdateLabel(saved);

            _callback = evt =>
            {
                Apply(evt.newValue);
                PlayerPrefs.SetFloat(_prefsKey, evt.newValue);
                Changed?.Invoke(evt.newValue);
            };
            Slider.RegisterValueChangedCallback(_callback);
        }

        public void ApplyCurrent() => Apply(Slider.value);

        private void Apply(float linear)
        {
            if (_mixer != null) _mixer.SetFloat(_param, LinearToDecibels(linear));
            UpdateLabel(linear);
        }

        private void UpdateLabel(float linear) =>
            _label.text = Mathf.RoundToInt(linear * 100f) + "%";

        public void Dispose() => Slider.UnregisterValueChangedCallback(_callback);

        private static float LinearToDecibels(float linear) =>
            linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}