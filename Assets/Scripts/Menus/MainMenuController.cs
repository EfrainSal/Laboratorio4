using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private const string SelectedCharacterKey = "SelectedCharacter";
    private const string PlayerRankingNameKey = "PlayerRankingName"; // Nueva clave para el ranking

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
    [SerializeField] private AudioMixer _mixer;               
    [SerializeField] private AudioSource _sfxPreview;         

    [Header("Escenas")]
    [SerializeField] private string _gameSceneName = "SampleScene";

    private VisualElement _root, _menuContainer, _logo, _videoBackground, _leftShade;
    private VisualElement _sidePanel, _pagePlay, _pageSettings, _pageRanking;
    private Button _playButton, _continueButton, _settingsButton, _rankingButton, _quitButton;
    private Button _playBackButton, _confirmButton, _settingsBackButton, _rankingBackButton;
    private TextField _playerNameInput;
    private VisualElement[] _cards;
    private EventCallback<ClickEvent>[] _cardCallbacks;
    private VolumeChannel _master, _music, _sfx;

    private IVisualElementScheduledItem _flickerLoop, _hidePanelItem, _sfxPreviewItem;
    private Texture2D _leftShadeTexture, _buttonGradientTexture;
    private int _selectedCharacter = -1;
    private bool _panelOpen;
    
    // Almacena el botón que abrió el panel para devolverle el foco al usar el mando
    private VisualElement _lastFocusedMenuButton; 

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
        _pageRanking     = _root.Q<VisualElement>("PageRanking");

        _continueButton     = _root.Q<Button>("ContinueButton");
        _playButton         = _root.Q<Button>("PlayButton");
        _settingsButton     = _root.Q<Button>("SettingsButton");
        _rankingButton      = _root.Q<Button>("RankingButton");
        _quitButton         = _root.Q<Button>("QuitButton");
        
        _playBackButton     = _root.Q<Button>("PlayBackButton");
        _confirmButton      = _root.Q<Button>("ConfirmButton");
        _settingsBackButton = _root.Q<Button>("BackButton");
        _rankingBackButton  = _root.Q<Button>("RankingBackButton");
        
        _playerNameInput    = _root.Q<TextField>("PlayerNameInput");

        _continueButton.clicked     += OnContinueClicked;
        _playButton.clicked         += OpenPlayPage;
        _settingsButton.clicked     += OpenSettingsPage;
        _rankingButton.clicked      += OpenRankingPage;
        _quitButton.clicked         += OnQuitClicked;
        
        _playBackButton.clicked     += ClosePanel;
        _settingsBackButton.clicked += ClosePanel;
        _rankingBackButton.clicked  += ClosePanel;
        _confirmButton.clicked      += OnConfirmCharacter;
        
        // Registrar cambio de texto para habilitar/deshabilitar botón confirmar
        _playerNameInput.RegisterValueChangedCallback(evt => UpdateConfirmButtonState());

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

        _root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);

        // Audio
        _master = new VolumeChannel(_mixer, "MasterVolume", _root.Q<Slider>("MasterSlider"), _root.Q<Label>("MasterValueLabel"), 0.8f);
        _music = new VolumeChannel(_mixer, "MusicVolume", _root.Q<Slider>("MusicSlider"), _root.Q<Label>("MusicValueLabel"), 0.8f);
        _sfx = new VolumeChannel(_mixer, "SFXVolume", _root.Q<Slider>("SfxSlider"), _root.Q<Label>("SfxValueLabel"), 0.8f);
        _sfx.Changed += OnSfxChanged;

        if (_videoTexture != null) _videoBackground.style.backgroundImage = Background.FromRenderTexture(_videoTexture);

        _leftShadeTexture = CreateGradientTexture(Color.black, 0.88f, 0f, true);
        _leftShade.style.backgroundImage = new StyleBackground(_leftShadeTexture);

        _buttonGradientTexture = CreateGradientTexture(Color.white, 1f, 0f, false);
        _root.Query<Button>(className: MenuButtonClass)
             .ForEach(b => b.style.backgroundImage = new StyleBackground(_buttonGradientTexture));

        _menuContainer.schedule.Execute(() =>
        {
            _menuContainer.RemoveFromClassList(MenuHiddenClass);
            _playButton.Focus();
        }).StartingIn(100);

        _flickerLoop = _logo.schedule.Execute(FlickerLogo).Every(4000);
        
        // Precargar último nombre usado
        _playerNameInput.value = PlayerPrefs.GetString(PlayerRankingNameKey, "");
    }

    void Start()
    {
        _master.ApplyCurrent();
        _music.ApplyCurrent();
        _sfx.ApplyCurrent();

        if (_videoPlayer != null && _videoTexture != null)
        {
            _videoPlayer.Stop();                       
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
        _rankingButton.clicked      -= OpenRankingPage;
        _quitButton.clicked         -= OnQuitClicked;
        _playBackButton.clicked     -= ClosePanel;
        _settingsBackButton.clicked -= ClosePanel;
        _rankingBackButton.clicked  -= ClosePanel;
        _confirmButton.clicked      -= OnConfirmCharacter;

        for (int i = 0; i < _cards.Length; i++) _cards[i].UnregisterCallback(_cardCallbacks[i]);

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
        source.Play();
        source.playbackSpeed = _videoSpeed;   
    }

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
        _lastFocusedMenuButton = _playButton;
        ClearCharacterSelection();
        ShowPage(_pagePlay, _playButton);
        _cards[0].Focus(); // Foco para mando
    }

    private void OpenSettingsPage()
    {
        _lastFocusedMenuButton = _settingsButton;
        ShowPage(_pageSettings, _settingsButton);
        _root.Q<Slider>("MasterSlider").Focus(); // Foco para mando
    }

    private void OpenRankingPage()
    {
        _lastFocusedMenuButton = _rankingButton;
        ShowPage(_pageRanking, _rankingButton);
        _rankingBackButton.Focus(); // Foco para mando
    }

    private void ShowPage(VisualElement page, Button activeButton)
    {
        _pagePlay.AddToClassList(HiddenClass);
        _pageSettings.AddToClassList(HiddenClass);
        _pageRanking.AddToClassList(HiddenClass);
        page.RemoveFromClassList(HiddenClass);

        _sidePanel.EnableInClassList(PanelWideClass, page == _pagePlay);
        SetActiveButton(activeButton);

        _hidePanelItem?.Pause();
        _sidePanel.RemoveFromClassList(HiddenClass);                       
        _sidePanel.schedule.Execute(() => _sidePanel.RemoveFromClassList(PanelFadeClass)).StartingIn(20);
        _panelOpen = true;
    }

    private void ClosePanel()
    {
        if (!_panelOpen) return;
        _panelOpen = false;

        SetActiveButton(null);
        _sidePanel.AddToClassList(PanelFadeClass);                         
        _hidePanelItem = _sidePanel.schedule.Execute(() => _sidePanel.AddToClassList(HiddenClass)).StartingIn(320);
        PlayerPrefs.Save();                                                
        
        // DEVOLVER EL FOCO AL MENÚ PARA EL MANDO
        _lastFocusedMenuButton?.Focus();
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
        _rankingButton.EnableInClassList(ActiveClass, active == _rankingButton);
    }

    // ------------------------------------------------------------------
    // Selección de personaje
    // ------------------------------------------------------------------
    private void SelectCharacter(int index)
    {
        _selectedCharacter = index;
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].EnableInClassList(SelectedCardClass, i == index);
        
        UpdateConfirmButtonState();
    }

    private void ClearCharacterSelection() => SelectCharacter(-1);
    
    private void UpdateConfirmButtonState()
    {
        bool hasCharacter = _selectedCharacter >= 0;
        string currentName = _playerNameInput.value?.Trim() ?? "";
        bool hasValidName = currentName.Length >= 3 && currentName.Length <= 12;
        
        // Feedback visual en el input (rojo si es inválido o está vacío, verde si es válido)
        if (hasValidName)
        {
            _playerNameInput.RemoveFromClassList("input-invalid");
            _playerNameInput.AddToClassList("input-valid");
        }
        else
        {
            _playerNameInput.RemoveFromClassList("input-valid");
            _playerNameInput.AddToClassList("input-invalid");
        }

        _confirmButton.SetEnabled(hasCharacter && hasValidName);
    }

    private void OnConfirmCharacter()
    {
        if (_selectedCharacter < 0 || string.IsNullOrWhiteSpace(_playerNameInput.value)) return;

        // GUARDAR NOMBRE PARA EL RANKING
        PlayerPrefs.SetString(PlayerRankingNameKey, _playerNameInput.value.Trim());
        PlayerPrefs.SetInt(SelectedCharacterKey, _selectedCharacter);
        PlayerPrefs.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.IniciarNuevaRun(_selectedCharacter);
        }
        else
        {
            Debug.LogWarning("No se encontró el GameManager en la escena del menú.");
        }

        SaveSystem.Borrar();
        SceneManager.LoadScene(_gameSceneName);
    }

    // ------------------------------------------------------------------
    // Audio: preview de SFX
    // ------------------------------------------------------------------
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
    private void OnContinueClicked()
    {
        if (GameManager.Instance != null && GameManager.Instance.CargarRunGuardada())
        {
            SceneManager.LoadScene(_gameSceneName);
        }
        else
        {
            Debug.LogWarning("No se encontró partida guardada válida.");
            _continueButton.SetEnabled(false); 
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
    // VolumeChannel
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

        private void UpdateLabel(float linear) => _label.text = Mathf.RoundToInt(linear * 100f) + "%";
        public void Dispose() => Slider.UnregisterValueChangedCallback(_callback);
        private static float LinearToDecibels(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}