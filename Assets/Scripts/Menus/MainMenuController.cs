using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private const string SelectedCharacterKey = "SelectedCharacter";
    private const string PlayerRankingNameKey = "PlayerRankingName"; 

    private const string HiddenClass = "hidden";
    private const string PanelFadeClass = "panel-fade";
    private const string PanelWideClass = "panel-wide";
    private const string MenuHiddenClass = "menu-hidden";
    private const string MenuButtonClass = "btn-menu";
    private const string LogoDimClass = "logo-dim";
    private const string ActiveClass = "btn-active";
    private const string SelectedCardClass = "card-selected";
    private const string SelectedInputModeClass = "input-mode-selected";

    [Header("Fondo de video")]
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RenderTexture _videoTexture;
    [SerializeField, Range(0.1f, 2f)] private float _videoSpeed = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioMixer _mixer;               
    [SerializeField] private AudioSource _sfxPreview;         

    [Header("Escenas")]
    [SerializeField] private string _gameSceneName = "Recepcion";
    [SerializeField] private string _introSceneName = IntroduccionComic.NombreEscena;

    [Header("Sprites de Personajes (0: Jason, 1: Milena)")]
    [SerializeField] private Sprite[] _characterSprites;

    private VisualElement _root, _menuContainer, _logo, _videoBackground, _leftShade;
    private VisualElement _sidePanel, _pagePlay, _pageSettings, _pageRanking;
    private Button _playButton, _continueButton, _settingsButton, _rankingButton, _quitButton;
    private Button _playBackButton, _confirmButton, _settingsBackButton, _rankingBackButton;
    private Button _inputModeAutoButton, _inputModeKbmButton, _inputModeGamepadButton;
    private Label _inputModeValueLabel, _inputModeHintLabel;
    private TextField _playerNameInput;
    private VisualElement _gamepadKeyboardPanel;
    private readonly List<Button> _gamepadKeyButtons = new List<Button>();
    private ScrollView _rankingListContainer;
    private VisualElement[] _cards;
    private EventCallback<ClickEvent>[] _cardCallbacks;
    private EventCallback<NavigationSubmitEvent>[] _cardSubmitCallbacks;
    private EventCallback<NavigationMoveEvent>[] _cardMoveCallbacks;
    private VolumeChannel _master, _music, _sfx;

    private IVisualElementScheduledItem _flickerLoop, _hidePanelItem, _sfxPreviewItem;
    private Texture2D _leftShadeTexture, _buttonGradientTexture;
    private int _selectedCharacter = -1;
    private bool _panelOpen;
    private bool _virtualKeyboardOpen;
    private float _ultimoInputMandoUI;
    
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
        _rankingListContainer = _root.Q<ScrollView>("RankingListContainer");

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

        _inputModeAutoButton    = _root.Q<Button>("InputModeAutoButton");
        _inputModeKbmButton     = _root.Q<Button>("InputModeKbmButton");
        _inputModeGamepadButton = _root.Q<Button>("InputModeGamepadButton");
        _inputModeValueLabel    = _root.Q<Label>("InputModeValueLabel");
        _inputModeHintLabel     = _root.Q<Label>("InputModeHintLabel");

        _continueButton.clicked     += OnContinueClicked;
        _playButton.clicked         += OpenPlayPage;
        _settingsButton.clicked     += OpenSettingsPage;
        _rankingButton.clicked      += OpenRankingPage;
        _quitButton.clicked         += OnQuitClicked;
        
        _playBackButton.clicked     += ClosePanel;
        _settingsBackButton.clicked += ClosePanel;
        _rankingBackButton.clicked  += ClosePanel;
        _confirmButton.clicked      += OnConfirmCharacter;

        if (_inputModeAutoButton != null)    _inputModeAutoButton.clicked    += () => SeleccionarModoEntrada(ModoEntrada.Automatico);
        if (_inputModeKbmButton != null)     _inputModeKbmButton.clicked     += () => SeleccionarModoEntrada(ModoEntrada.TecladoYRaton);
        if (_inputModeGamepadButton != null) _inputModeGamepadButton.clicked += () => SeleccionarModoEntrada(ModoEntrada.Mando);
        
        _playerNameInput.RegisterValueChangedCallback(evt => UpdateConfirmButtonState());
        ConfigurarInteraccionInputNombre();
        ConstruirTecladoVirtualMando();

        _continueButton.SetEnabled(SaveSystem.HayGuardado());
        _confirmButton.SetEnabled(false);

        _cards = new[] { _root.Q<VisualElement>("CharacterCard0"), _root.Q<VisualElement>("CharacterCard1") };
        _cardCallbacks = new EventCallback<ClickEvent>[_cards.Length];
        _cardSubmitCallbacks = new EventCallback<NavigationSubmitEvent>[_cards.Length];
        _cardMoveCallbacks = new EventCallback<NavigationMoveEvent>[_cards.Length];

        for (int i = 0; i < _cards.Length; i++)
        {
            int index = i;
            _cardCallbacks[i] = _ => SelectCharacter(index);
            _cardSubmitCallbacks[i] = evt =>
            {
                SelectCharacter(index);
                evt.StopPropagation();
            };
            _cardMoveCallbacks[i] = evt => OnCardNavigationMove(index, evt);

            _cards[i].RegisterCallback(_cardCallbacks[i]);
            _cards[i].RegisterCallback(_cardSubmitCallbacks[i]);
            _cards[i].RegisterCallback(_cardMoveCallbacks[i]);

            if (_characterSprites != null && i < _characterSprites.Length && _characterSprites[i] != null)
            {
                var portrait = _cards[i].Q<VisualElement>(className: "char-portrait");
                if (portrait != null)
                {
                    portrait.style.backgroundImage = new StyleBackground(_characterSprites[i]);
                    var placeholder = portrait.Q<Label>(className: "portrait-placeholder");
                    if (placeholder != null)
                    {
                        placeholder.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        ConfigurarNavegacionBotonesAccion();
        ConfigurarNavegacionAjustes();

        _root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        _root.RegisterCallback<NavigationMoveEvent>(OnRootNavigationMove, TrickleDown.TrickleDown);
        _root.RegisterCallback<NavigationSubmitEvent>(OnRootNavigationSubmit, TrickleDown.TrickleDown);
        _root.RegisterCallback<PointerDownEvent>(OnRootPointerEvent, TrickleDown.TrickleDown);
        _root.RegisterCallback<ClickEvent>(OnRootClickEvent, TrickleDown.TrickleDown);

        _master = new VolumeChannel(_mixer, "MasterVolume", _root.Q<Slider>("MasterSlider"), _root.Q<Label>("MasterValueLabel"), 0.8f);
        _music = new VolumeChannel(_mixer, "MusicVolume", _root.Q<Slider>("MusicSlider"), _root.Q<Label>("MusicValueLabel"), 0.8f);
        _sfx = new VolumeChannel(_mixer, "SFXVolume", _root.Q<Slider>("SfxSlider"), _root.Q<Label>("SfxValueLabel"), 0.8f);
        _sfx.Changed += OnSfxChanged;

        ActualizarUIModoEntrada();

        if (_videoTexture != null) _videoBackground.style.backgroundImage = Background.FromRenderTexture(_videoTexture);

        _leftShadeTexture = CreateGradientTexture(Color.black, 0.88f, 0f, true);
        _leftShade.style.backgroundImage = new StyleBackground(_leftShadeTexture);

        _buttonGradientTexture = CrearGradienteBoton();
        _root.Query<Button>(className: MenuButtonClass)
             .ForEach(b => b.style.backgroundImage = new StyleBackground(_buttonGradientTexture));

        _menuContainer.schedule.Execute(() =>
        {
            _menuContainer.RemoveFromClassList(MenuHiddenClass);
            _playButton.Focus();
        }).StartingIn(100);

        _flickerLoop = _logo.schedule.Execute(FlickerLogo).Every(4000);
        
        _playerNameInput.value = PlayerPrefs.GetString(PlayerRankingNameKey, "");
        UpdateConfirmButtonState();
    }

    void Start()
    {
        _master.ApplyCurrent();
        _music.ApplyCurrent();
        _sfx.ApplyCurrent();

        RankingManager.IntentarSubirRecordsPendientes();

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

        GestorModoEntrada.ActualizarDeteccionAutomatica();
        ProcesarAtajosMandoEnMenu();
    }

    void OnDisable()
    {
        GestorModoEntrada.EscribiendoNombreJugador = false;
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

        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].UnregisterCallback(_cardCallbacks[i]);
            _cards[i].UnregisterCallback(_cardSubmitCallbacks[i]);
            _cards[i].UnregisterCallback(_cardMoveCallbacks[i]);
        }

        _root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        _root.UnregisterCallback<NavigationMoveEvent>(OnRootNavigationMove, TrickleDown.TrickleDown);
        _root.UnregisterCallback<NavigationSubmitEvent>(OnRootNavigationSubmit, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerDownEvent>(OnRootPointerEvent, TrickleDown.TrickleDown);
        _root.UnregisterCallback<ClickEvent>(OnRootClickEvent, TrickleDown.TrickleDown);

        _sfx.Changed -= OnSfxChanged;
        _master.Dispose();
        _music.Dispose();
        _sfx.Dispose();

        if (_videoPlayer != null) _videoPlayer.prepareCompleted -= OnVideoPrepared;
        if (_leftShadeTexture != null) Destroy(_leftShadeTexture);
        if (_buttonGradientTexture != null) Destroy(_buttonGradientTexture);
    }

    // ------------------------------------------------------------------
    // Filtrado por Modo de Entrada y Navegación con Mando
    // ------------------------------------------------------------------
    private bool EsElementoDeInputNombre(EventBase evt)
    {
        if (_playerNameInput == null || evt?.target is not VisualElement ve) return false;
        return ve == _playerNameInput || _playerNameInput.Contains(ve);
    }

    private void OnRootPointerEvent(PointerDownEvent evt)
    {
        // Si el modo forzado es Mando, ignoramos los clics del ratón salvo en la caja de texto del nombre
        if (!GestorModoEntrada.PermitirTecladoYRaton(EsElementoDeInputNombre(evt)))
        {
            evt.StopImmediatePropagation();
        }
    }

    private void OnRootClickEvent(ClickEvent evt)
    {
        // En UI Toolkit, NavigationSubmitEvent en un Button dispara ClickEvent internamente.
        // Sólo bloqueamos ClickEvent si proviene del ratón cuando está forzado el modo Mando.
        if (GestorModoEntrada.ModoActual == ModoEntrada.Mando &&
            Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame &&
            !EsElementoDeInputNombre(evt))
        {
            evt.StopImmediatePropagation();
        }
    }

    private void OnRootNavigationMove(NavigationMoveEvent evt)
    {
        if (!PermitirEventoNavegacionUI(EsElementoDeInputNombre(evt)))
        {
            evt.StopImmediatePropagation();
            return;
        }

        // Si el jugador está dentro del TextField del nombre y mueve arriba o abajo con el mando/cruceta,
        // sacamos el foco del TextField para que no quede atrapado dentro del texto.
        var focused = _root.panel?.focusController?.focusedElement as VisualElement;
        if (focused != null && (_playerNameInput == focused || _playerNameInput.Contains(focused)))
        {
            if (evt.direction == NavigationMoveEvent.Direction.Up)
            {
                evt.StopImmediatePropagation();
                _playerNameInput.Blur();
                int cardIdx = _selectedCharacter >= 0 ? _selectedCharacter : 0;
                _cards[cardIdx].Focus();
            }
            else if (evt.direction == NavigationMoveEvent.Direction.Down)
            {
                evt.StopImmediatePropagation();
                _playerNameInput.Blur();
                if (_virtualKeyboardOpen && _gamepadKeyButtons.Count > 0)
                {
                    _gamepadKeyButtons[0].Focus();
                }
                else if (_confirmButton.enabledSelf)
                {
                    _confirmButton.Focus();
                }
                else
                {
                    _playBackButton.Focus();
                }
            }
        }
    }

    private void OnRootNavigationSubmit(NavigationSubmitEvent evt)
    {
        if (!PermitirEventoNavegacionUI(EsElementoDeInputNombre(evt)))
        {
            evt.StopImmediatePropagation();
            return;
        }

        var focused = _root.panel?.focusController?.focusedElement as VisualElement;
        if (focused != null && (_playerNameInput == focused || _playerNameInput.Contains(focused)))
        {
            // Al pulsar A / Cruz sobre el campo de nombre con mando, abrimos/enfocamos el teclado en pantalla
            if (GestorModoEntrada.UsandoMando || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                evt.StopImmediatePropagation();
                MostrarTecladoVirtualMando(true);
            }
        }
    }

    private bool PermitirEventoNavegacionUI(bool esInputNombre)
    {
        if (GestorModoEntrada.ModoActual == ModoEntrada.TecladoYRaton)
        {
            // Bloquear si viene del mando
            if (Gamepad.current != null && HayBotonMandoReciente())
                return false;
            return true;
        }
        if (GestorModoEntrada.ModoActual == ModoEntrada.Mando)
        {
            if (esInputNombre) return true;
            // Bloquear flechas/enter/tab del teclado si no estamos en el input de nombre
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame &&
                (Gamepad.current == null || !HayBotonMandoReciente()))
                return false;
            return true;
        }
        return true;
    }

    private static bool HayBotonMandoReciente()
    {
        var g = Gamepad.current;
        if (g == null) return false;
        return g.leftStick.ReadValue().sqrMagnitude > 0.15f
               || g.dpad.ReadValue().sqrMagnitude > 0.15f
               || g.buttonSouth.isPressed || g.buttonSouth.wasPressedThisFrame || g.buttonSouth.wasReleasedThisFrame
               || g.buttonEast.isPressed || g.buttonEast.wasPressedThisFrame || g.buttonEast.wasReleasedThisFrame
               || g.buttonWest.isPressed || g.buttonWest.wasPressedThisFrame
               || g.buttonNorth.isPressed || g.buttonNorth.wasPressedThisFrame
               || g.startButton.isPressed || g.startButton.wasPressedThisFrame;
    }

    private void ConfigurarInteraccionInputNombre()
    {
        if (_playerNameInput == null) return;

        _playerNameInput.RegisterCallback<FocusInEvent>(_ =>
        {
            GestorModoEntrada.EscribiendoNombreJugador = true;
        });

        _playerNameInput.RegisterCallback<FocusOutEvent>(_ =>
        {
            GestorModoEntrada.EscribiendoNombreJugador = false;
        });

        _playerNameInput.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (_confirmButton.enabledSelf)
                {
                    _confirmButton.Focus();
                }
                else
                {
                    _playBackButton.Focus();
                }
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                _playerNameInput.Blur();
                int idx = _selectedCharacter >= 0 ? _selectedCharacter : 0;
                _cards[idx].Focus();
                evt.StopPropagation();
            }
        });
    }

    private void ConstruirTecladoVirtualMando()
    {
        if (_pagePlay == null || _playerNameInput == null) return;

        _gamepadKeyboardPanel = new VisualElement { name = "GamepadNamePanel" };
        _gamepadKeyboardPanel.AddToClassList("gamepad-name-panel");
        _gamepadKeyboardPanel.AddToClassList(HiddenClass);

        var titulo = new Label("TECLADO DE MANDO (A: Escribir · X: Borrar · Y: Nombre rápido · B: Cerrar)");
        titulo.AddToClassList("gamepad-name-title");
        _gamepadKeyboardPanel.Add(titulo);

        var grid = new VisualElement();
        grid.AddToClassList("gamepad-keys-grid");
        _gamepadKeyboardPanel.Add(grid);

        string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
        _gamepadKeyButtons.Clear();

        foreach (char c in caracteres)
        {
            char letra = c;
            var btn = new Button(() => AgregarCaracterNombre(letra)) { text = letra.ToString() };
            btn.AddToClassList("btn-gamepad-key");
            grid.Add(btn);
            _gamepadKeyButtons.Add(btn);
        }

        var btnBorrar = new Button(BorrarUltimoCaracterNombre) { text = "BORRAR" };
        btnBorrar.AddToClassList("btn-gamepad-key");
        btnBorrar.AddToClassList("btn-gamepad-key-action");
        grid.Add(btnBorrar);
        _gamepadKeyButtons.Add(btnBorrar);

        var btnRapido = new Button(AsignarNombrePorDefecto) { text = "AUTO" };
        btnRapido.AddToClassList("btn-gamepad-key");
        btnRapido.AddToClassList("btn-gamepad-key-action");
        grid.Add(btnRapido);
        _gamepadKeyButtons.Add(btnRapido);

        var btnListo = new Button(CerrarTecladoVirtualYPasarAConfirmar) { text = "LISTO" };
        btnListo.AddToClassList("btn-gamepad-key");
        btnListo.AddToClassList("btn-gamepad-key-action");
        grid.Add(btnListo);
        _gamepadKeyButtons.Add(btnListo);

        int idxInput = _pagePlay.IndexOf(_playerNameInput);
        if (idxInput >= 0 && idxInput + 2 <= _pagePlay.childCount)
            _pagePlay.Insert(idxInput + 2, _gamepadKeyboardPanel);
        else
            _pagePlay.Add(_gamepadKeyboardPanel);

        ConfigurarNavegacionGridTecladoVirtual();
    }

    private void ConfigurarNavegacionGridTecladoVirtual()
    {
        const int columnas = 10;
        int total = _gamepadKeyButtons.Count;
        for (int i = 0; i < total; i++)
        {
            int idx = i;
            _gamepadKeyButtons[i].RegisterCallback<NavigationMoveEvent>(evt =>
            {
                int destino = -1;
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Left:
                        destino = idx > 0 ? idx - 1 : total - 1;
                        break;
                    case NavigationMoveEvent.Direction.Right:
                        destino = (idx + 1) % total;
                        break;
                    case NavigationMoveEvent.Direction.Up:
                        if (idx - columnas >= 0) destino = idx - columnas;
                        else
                        {
                            evt.StopPropagation();
                            MostrarTecladoVirtualMando(false);
                            _playerNameInput.Focus();
                            return;
                        }
                        break;
                    case NavigationMoveEvent.Direction.Down:
                        if (idx + columnas < total) destino = idx + columnas;
                        else
                        {
                            evt.StopPropagation();
                            if (_confirmButton.enabledSelf) _confirmButton.Focus();
                            else _playBackButton.Focus();
                            return;
                        }
                        break;
                }

                if (destino >= 0 && destino < total)
                {
                    evt.StopPropagation();
                    _gamepadKeyButtons[destino].Focus();
                }
            });
        }
    }

    private void MostrarTecladoVirtualMando(bool mostrar)
    {
        if (_gamepadKeyboardPanel == null) return;
        _virtualKeyboardOpen = mostrar;
        _gamepadKeyboardPanel.EnableInClassList(HiddenClass, !mostrar);
        if (mostrar && _gamepadKeyButtons.Count > 0)
        {
            _sidePanel.schedule.Execute(() => _gamepadKeyButtons[0].Focus()).StartingIn(20);
        }
    }

    private void AgregarCaracterNombre(char c)
    {
        if (_playerNameInput == null) return;
        string actual = _playerNameInput.value ?? "";
        if (actual.Length >= 14) return;
        _playerNameInput.value = actual + c;
    }

    private void BorrarUltimoCaracterNombre()
    {
        if (_playerNameInput == null) return;
        string actual = _playerNameInput.value ?? "";
        if (actual.Length > 0)
        {
            _playerNameInput.value = actual.Substring(0, actual.Length - 1);
        }
    }

    private void AsignarNombrePorDefecto()
    {
        if (_playerNameInput == null) return;
        string sugerido = _selectedCharacter == 1 ? "MILENA" : (_selectedCharacter == 0 ? "JASON" : "AGENTE");
        _playerNameInput.value = sugerido;
    }

    private void CerrarTecladoVirtualYPasarAConfirmar()
    {
        MostrarTecladoVirtualMando(false);
        if (_confirmButton != null && _confirmButton.enabledSelf)
            _confirmButton.Focus();
        else
            _playBackButton?.Focus();
    }

    private void OnCardNavigationMove(int index, NavigationMoveEvent evt)
    {
        switch (evt.direction)
        {
            case NavigationMoveEvent.Direction.Left:
                if (index > 0)
                {
                    _cards[index - 1].Focus();
                    evt.StopPropagation();
                }
                break;
            case NavigationMoveEvent.Direction.Right:
                if (index + 1 < _cards.Length)
                {
                    _cards[index + 1].Focus();
                    evt.StopPropagation();
                }
                break;
            case NavigationMoveEvent.Direction.Down:
                _playerNameInput.Focus();
                evt.StopPropagation();
                break;
        }
    }

    private void ConfigurarNavegacionBotonesAccion()
    {
        if (_playBackButton != null)
        {
            _playBackButton.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Up)
                {
                    if (_virtualKeyboardOpen && _gamepadKeyButtons.Count > 0)
                        _gamepadKeyButtons[_gamepadKeyButtons.Count - 1].Focus();
                    else
                        _playerNameInput.Focus();
                    evt.StopPropagation();
                }
                else if (evt.direction == NavigationMoveEvent.Direction.Right && _confirmButton.enabledSelf)
                {
                    _confirmButton.Focus();
                    evt.StopPropagation();
                }
            });
        }

        if (_confirmButton != null)
        {
            _confirmButton.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Up)
                {
                    if (_virtualKeyboardOpen && _gamepadKeyButtons.Count > 0)
                        _gamepadKeyButtons[_gamepadKeyButtons.Count - 1].Focus();
                    else
                        _playerNameInput.Focus();
                    evt.StopPropagation();
                }
                else if (evt.direction == NavigationMoveEvent.Direction.Left)
                {
                    _playBackButton.Focus();
                    evt.StopPropagation();
                }
            });
        }
    }

    private void ConfigurarNavegacionAjustes()
    {
        var sfxSlider = _root.Q<Slider>("SfxSlider");
        Button[] modeButtons = { _inputModeAutoButton, _inputModeKbmButton, _inputModeGamepadButton };

        if (sfxSlider != null && _inputModeAutoButton != null)
        {
            sfxSlider.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Down)
                {
                    BotonModoActual().Focus();
                    evt.StopPropagation();
                }
            });
        }

        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] == null) continue;
            int idx = i;
            modeButtons[i].RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Left && idx > 0)
                {
                    modeButtons[idx - 1]?.Focus();
                    evt.StopPropagation();
                }
                else if (evt.direction == NavigationMoveEvent.Direction.Right && idx + 1 < modeButtons.Length)
                {
                    modeButtons[idx + 1]?.Focus();
                    evt.StopPropagation();
                }
                else if (evt.direction == NavigationMoveEvent.Direction.Up && sfxSlider != null)
                {
                    sfxSlider.Focus();
                    evt.StopPropagation();
                }
                else if (evt.direction == NavigationMoveEvent.Direction.Down && _settingsBackButton != null)
                {
                    _settingsBackButton.Focus();
                    evt.StopPropagation();
                }
            });
        }

        if (_settingsBackButton != null && _inputModeAutoButton != null)
        {
            _settingsBackButton.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Up)
                {
                    BotonModoActual().Focus();
                    evt.StopPropagation();
                }
            });
        }
    }

    private Button BotonModoActual()
    {
        return GestorModoEntrada.ModoActual switch
        {
            ModoEntrada.TecladoYRaton => _inputModeKbmButton ?? _inputModeAutoButton,
            ModoEntrada.Mando => _inputModeGamepadButton ?? _inputModeAutoButton,
            _ => _inputModeAutoButton
        };
    }

    private void SeleccionarModoEntrada(ModoEntrada modo)
    {
        GestorModoEntrada.EstablecerModo(modo);
        ActualizarUIModoEntrada();
    }

    private void ActualizarUIModoEntrada()
    {
        ModoEntrada modo = GestorModoEntrada.ModoActual;
        if (_inputModeValueLabel != null)
            _inputModeValueLabel.text = GestorModoEntrada.ObtenerNombreModo(modo);

        _inputModeAutoButton?.EnableInClassList(SelectedInputModeClass, modo == ModoEntrada.Automatico);
        _inputModeKbmButton?.EnableInClassList(SelectedInputModeClass, modo == ModoEntrada.TecladoYRaton);
        _inputModeGamepadButton?.EnableInClassList(SelectedInputModeClass, modo == ModoEntrada.Mando);

        if (_inputModeHintLabel != null)
        {
            _inputModeHintLabel.text = modo switch
            {
                ModoEntrada.TecladoYRaton => "Fuerza controles por teclado y ratón, ignorando el mando aunque esté conectado.",
                ModoEntrada.Mando => "Fuerza el uso del mando, ignorando teclado y ratón (salvo para escribir tu nombre).",
                _ => "Detecta automáticamente si estás usando teclado y ratón o mando."
            };
        }
    }

    private void ProcesarAtajosMandoEnMenu()
    {
        if (!GestorModoEntrada.PermitirMando || Gamepad.current == null) return;

        var g = Gamepad.current;

        // Si el usuario mueve el mando y ningún elemento tiene foco visible, asegurar que haya foco
        if (Time.unscaledTime - _ultimoInputMandoUI > 0.15f &&
            (g.dpad.ReadValue().sqrMagnitude > 0.25f || g.leftStick.ReadValue().sqrMagnitude > 0.25f))
        {
            _ultimoInputMandoUI = Time.unscaledTime;
            var focused = _root.panel?.focusController?.focusedElement;
            if (focused == null)
            {
                if (_panelOpen)
                {
                    if (!_pagePlay.ClassListContains(HiddenClass)) _cards[Mathf.Max(0, _selectedCharacter)].Focus();
                    else if (!_pageSettings.ClassListContains(HiddenClass)) _root.Q<Slider>("MasterSlider")?.Focus();
                    else if (!_pageRanking.ClassListContains(HiddenClass)) _rankingBackButton?.Focus();
                }
                else
                {
                    _playButton?.Focus();
                }
            }
        }

        // Atajos rápidos en la página de Nueva Partida cuando el teclado virtual está activo o el campo de nombre tiene foco
        if (_panelOpen && !_pagePlay.ClassListContains(HiddenClass))
        {
            var focused = _root.panel?.focusController?.focusedElement as VisualElement;
            bool enZonaNombre = _virtualKeyboardOpen || (focused != null && (_playerNameInput == focused || _playerNameInput.Contains(focused)));
            if (enZonaNombre)
            {
                if (g.buttonWest.wasPressedThisFrame) // X / Cuadrado -> Borrar carácter
                {
                    BorrarUltimoCaracterNombre();
                }
                else if (g.buttonNorth.wasPressedThisFrame) // Y / Triángulo -> Nombre rápido sugerido
                {
                    AsignarNombrePorDefecto();
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Fondo
    // ------------------------------------------------------------------
    private void OnVideoPrepared(VideoPlayer source)
    {
        source.Play();
        source.playbackSpeed = _videoSpeed;   
    }

    public static Texture2D CrearGradienteBoton() => CreateGradientTexture(Color.white, 1f, 0f, false);

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
        MostrarTecladoVirtualMando(false);
        ClearCharacterSelection();
        ShowPage(_pagePlay, _playButton);
        _sidePanel.schedule.Execute(() => _cards[0].Focus()).StartingIn(40);
    }

    private void OpenSettingsPage()
    {
        _lastFocusedMenuButton = _settingsButton;
        ActualizarUIModoEntrada();
        ShowPage(_pageSettings, _settingsButton);
        _sidePanel.schedule.Execute(() => _root.Q<Slider>("MasterSlider")?.Focus()).StartingIn(40);
    }

    private void OpenRankingPage()
    {
        _lastFocusedMenuButton = _rankingButton;
        MostrarRanking(); 
        ShowPage(_pageRanking, _rankingButton);
        _sidePanel.schedule.Execute(() => _rankingBackButton.Focus()).StartingIn(40);
    }

    private void MostrarRanking()
    {
        _rankingListContainer.Clear();

        var datosRanking = RankingManager.LoadRanking();

        if (datosRanking.topScores.Count == 0)
        {
            var noDataLabel = new Label("No hay registros en la base de datos local.");
            noDataLabel.AddToClassList("ranking-empty");
            _rankingListContainer.Add(noDataLabel);
            return;
        }

        int posicion = 1;
        foreach (var record in datosRanking.topScores)
        {
            var fila = new VisualElement();
            fila.AddToClassList("ranking-row");

            var posicionLabel = new Label($"#{posicion}");
            posicionLabel.AddToClassList("ranking-pos");

            string tiempoFormateado = GameManager.Instance != null 
                ? GameManager.Instance.FormatearTiempo(record.time) 
                : Mathf.FloorToInt(record.time / 60) + ":" + Mathf.FloorToInt(record.time % 60).ToString("00");

            var infoLabel = new Label($"{record.playerName} - Puntos: {record.score} - Tiempo: {tiempoFormateado}");
            infoLabel.AddToClassList("ranking-info");

            fila.Add(posicionLabel);
            fila.Add(infoLabel);

            _rankingListContainer.Add(fila);
            posicion++;
        }
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
        MostrarTecladoVirtualMando(false);
        GestorModoEntrada.EscribiendoNombreJugador = false;

        SetActiveButton(null);
        _sidePanel.AddToClassList(PanelFadeClass);                         
        _hidePanelItem = _sidePanel.schedule.Execute(() => _sidePanel.AddToClassList(HiddenClass)).StartingIn(320);
        PlayerPrefs.Save();                                                
        
        _lastFocusedMenuButton?.Focus();
    }

    private void OnNavigationCancel(NavigationCancelEvent evt)
    {
        if (!PermitirEventoNavegacionUI(false))
        {
            evt.StopPropagation();
            return;
        }
        if (!_panelOpen) return;

        if (_virtualKeyboardOpen)
        {
            MostrarTecladoVirtualMando(false);
            _playerNameInput?.Focus();
            evt.StopPropagation();
            return;
        }

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

        // Si se seleccionó con mando, avanzar el foco automáticamente hacia el nombre o al botón Confirmar
        if ( index >= 0 && (GestorModoEntrada.UsandoMando || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)))
        {
            string currentName = _playerNameInput.value?.Trim() ?? "";
            bool hasValidName = currentName.Length >= 3 && currentName.Length <= 14;
            _sidePanel.schedule.Execute(() =>
            {
                if (hasValidName && _confirmButton.enabledSelf)
                    _confirmButton.Focus();
                else
                    _playerNameInput.Focus();
            }).StartingIn(30);
        }
    }

    private void ClearCharacterSelection() => SelectCharacter(-1);
    
    private void UpdateConfirmButtonState()
    {
        bool hasCharacter = _selectedCharacter >= 0;
        string currentName = _playerNameInput.value?.Trim() ?? "";
        bool hasValidName = currentName.Length >= 3 && currentName.Length <= 14;
        
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
        if (!Application.CanStreamedLevelBeLoaded(_introSceneName))
        { Debug.LogError("Falta habilitar la introducción del cómic en Build Settings: " + _introSceneName); return; }

        GestorModoEntrada.EscribiendoNombreJugador = false;
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
        SceneManager.LoadScene(_introSceneName);
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
            string escena = GameManager.Instance.DatosCargados.escenaActual;
            if (string.IsNullOrEmpty(escena)) escena = "Piso3_V10";
            SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(escena) ? escena : _gameSceneName);
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
