using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public enum ModoEntrada
{
    Automatico = 0,
    TecladoYRaton = 1,
    Mando = 2
}

/// <summary>
/// Gestiona la preferencia de dispositivo de entrada del usuario:
/// 0 = Automático (detecta en tiempo real si se usa Teclado/Ratón o Mando)
/// 1 = Teclado y ratón (fuerza el uso de teclado y ratón e ignora el mando)
/// 2 = Mando (fuerza el uso del mando e ignora teclado y ratón, salvo en el input del nombre del jugador)
/// </summary>
public static class GestorModoEntrada
{
    public const string ClavePreferencia = "ModoEntradaPreferido";

    private static ModoEntrada _modoActual = ModoEntrada.Automatico;
    private static bool _inicializado;
    private static bool _ultimoDispositivoFueMando;
    private static Vector2 _ultimaPosicionRaton;
    private static bool _posicionRatonInicializada;

    public static event System.Action<ModoEntrada> OnModoCambiado;
    public static event System.Action<bool> OnDispositivoActivoCambiado;
    public static event System.Action<bool> OnConexionMandoCambiada;

    /// <summary>
    /// Indica si hay al menos un mando conectado actualmente.
    /// </summary>
    public static bool HayMandoConectado => Gamepad.current != null || Gamepad.all.Count > 0;

    /// <summary>
    /// Indica si la caja de texto del nombre del jugador tiene el foco o permite escritura por teclado.
    /// Incluso cuando el modo forzado es Mando, el teclado físico puede escribir aquí.
    /// </summary>
    public static bool EscribiendoNombreJugador { get; set; }

    public static ModoEntrada ModoActual
    {
        get
        {
            AsegurarInicializado();
            if (_modoActual == ModoEntrada.Mando && !HayMandoConectado)
            {
                ManejarFaltaDeMando();
            }
            return _modoActual;
        }
    }

    /// <summary>
    /// Devuelve true si actualmente se debe usar la interfaz/apuntado/controles de Mando.
    /// </summary>
    public static bool UsandoMando
    {
        get
        {
            AsegurarInicializado();
            if (!HayMandoConectado)
            {
                if (_modoActual == ModoEntrada.Mando)
                {
                    ManejarFaltaDeMando();
                }
                _ultimoDispositivoFueMando = false;
                return false;
            }

            switch (_modoActual)
            {
                case ModoEntrada.TecladoYRaton:
                    return false;
                case ModoEntrada.Mando:
                    return true;
                default:
                    ActualizarDeteccionAutomatica();
                    return _ultimoDispositivoFueMando;
            }
        }
    }

    public static bool PermitirMando => ModoActual != ModoEntrada.TecladoYRaton && HayMandoConectado;

    public static bool PermitirTecladoYRaton(bool esInputNombre = false)
    {
        if (esInputNombre || EscribiendoNombreJugador) return true;
        return ModoActual != ModoEntrada.Mando;
    }

    public static string ObtenerNombreModo(ModoEntrada modo)
    {
        return modo switch
        {
            ModoEntrada.TecladoYRaton => "TECLADO Y RATÓN",
            ModoEntrada.Mando => "MANDO",
            _ => "AUTOMÁTICO"
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarSubsistema()
    {
        _inicializado = false;
        EscribiendoNombreJugador = false;
        _posicionRatonInicializada = false;
        InputSystem.onEvent -= AlRecibirEventoInput;
        InputSystem.onDeviceChange -= AlCambiarDispositivo;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InicializarAlArrancar()
    {
        AsegurarInicializado();
    }

    private static void AsegurarInicializado()
    {
        if (_inicializado) return;
        _inicializado = true;

        int guardado = PlayerPrefs.GetInt(ClavePreferencia, (int)ModoEntrada.Automatico);
        if (guardado < 0 || guardado > 2) guardado = 0;
        _modoActual = (ModoEntrada)guardado;

        if (_modoActual == ModoEntrada.Mando && !HayMandoConectado)
        {
            _modoActual = ModoEntrada.Automatico;
            PlayerPrefs.SetInt(ClavePreferencia, (int)_modoActual);
            PlayerPrefs.Save();
        }

        _ultimoDispositivoFueMando = HayMandoConectado && _modoActual == ModoEntrada.Mando;

        InputSystem.onEvent -= AlRecibirEventoInput;
        InputSystem.onEvent += AlRecibirEventoInput;
        InputSystem.onDeviceChange -= AlCambiarDispositivo;
        InputSystem.onDeviceChange += AlCambiarDispositivo;
    }

    private static void AlCambiarDispositivo(InputDevice device, InputDeviceChange change)
    {
        if (device is not Gamepad) return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Enabled:
                OnConexionMandoCambiada?.Invoke(HayMandoConectado);
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.Disabled:
                bool quedaAlgunMando = HayMandoConectado;
                if (!quedaAlgunMando)
                {
                    ManejarDesconexionDeMando();
                }
                OnConexionMandoCambiada?.Invoke(quedaAlgunMando);
                break;
        }
    }

    private static void ManejarFaltaDeMando()
    {
        if (_modoActual != ModoEntrada.Mando && !_ultimoDispositivoFueMando) return;
        ManejarDesconexionDeMando();
    }

    private static void ManejarDesconexionDeMando()
    {
        bool estabaUsandoMando = _modoActual == ModoEntrada.Mando || _ultimoDispositivoFueMando;
        _ultimoDispositivoFueMando = false;

        if (_modoActual == ModoEntrada.Mando)
        {
            _modoActual = ModoEntrada.Automatico;
            PlayerPrefs.SetInt(ClavePreferencia, (int)_modoActual);
            PlayerPrefs.Save();
            OnModoCambiado?.Invoke(_modoActual);
        }

        AplicarModoATodosLosPlayerInput();
        OnDispositivoActivoCambiado?.Invoke(false);

        if (estabaUsandoMando)
        {
            PausarPartidaPorSeguridad();
        }
    }

    private static void PausarPartidaPorSeguridad()
    {
        if (!Application.isPlaying) return;
        var menuPausa = Object.FindAnyObjectByType<PauseMenuController>();
        if (menuPausa != null && menuPausa.isActiveAndEnabled && !PauseMenuController.IsPaused && !PauseMenuController.PauseBlocked)
        {
            UpgradePanelController.TryClose();
            menuPausa.Pause();
            Debug.Log("[GestorModoEntrada] Mando desconectado: se pausó la partida y el modo de entrada cambió automáticamente a Automático.");
        }
    }

    public static void EstablecerModo(ModoEntrada nuevoModo)
    {
        AsegurarInicializado();

        // Capa de seguridad: no permitir activar modo Mando si no hay un mando conectado
        if (nuevoModo == ModoEntrada.Mando && !HayMandoConectado)
        {
            nuevoModo = ModoEntrada.Automatico;
        }

        bool cambioModo = _modoActual != nuevoModo;
        bool mandoAntes = _ultimoDispositivoFueMando || _modoActual == ModoEntrada.Mando;

        _modoActual = nuevoModo;
        if (_modoActual == ModoEntrada.Mando) _ultimoDispositivoFueMando = true;
        else if (_modoActual == ModoEntrada.TecladoYRaton) _ultimoDispositivoFueMando = false;

        PlayerPrefs.SetInt(ClavePreferencia, (int)_modoActual);
        PlayerPrefs.Save();

        AplicarModoATodosLosPlayerInput();

        if (cambioModo)
        {
            OnModoCambiado?.Invoke(_modoActual);
        }

        bool mandoAhora = UsandoMando;
        if (mandoAntes != mandoAhora)
        {
            OnDispositivoActivoCambiado?.Invoke(mandoAhora);
        }
    }

    public static void CiclarModo(int direccion = 1)
    {
        int total = 3;
        int actual = (int)ModoActual;
        int siguiente = (actual + direccion % total + total) % total;
        if ((ModoEntrada)siguiente == ModoEntrada.Mando && !HayMandoConectado)
        {
            siguiente = (siguiente + direccion % total + total) % total;
        }
        EstablecerModo((ModoEntrada)siguiente);
    }

    private static void AlRecibirEventoInput(InputEventPtr eventPtr, InputDevice device)
    {
        if (device == null || !eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        if (_modoActual != ModoEntrada.Automatico) return;

        if (device is Gamepad gamepad)
        {
            if (HayActividadEnMando(gamepad))
            {
                CambiarEstadoDetectado(true);
            }
        }
        else if (device is Keyboard || device is Mouse)
        {
            if (HayActividadEnTecladoORaton())
            {
                CambiarEstadoDetectado(false);
            }
        }
    }

    public static void ActualizarDeteccionAutomatica()
    {
        AsegurarInicializado();

        if (!HayMandoConectado && (_modoActual == ModoEntrada.Mando || _ultimoDispositivoFueMando))
        {
            ManejarDesconexionDeMando();
            return;
        }

        if (_modoActual != ModoEntrada.Automatico) return;

        if (Gamepad.current != null && HayActividadEnMando(Gamepad.current))
        {
            CambiarEstadoDetectado(true);
            return;
        }

        if (HayActividadEnTecladoORaton())
        {
            CambiarEstadoDetectado(false);
        }
    }

    private static void CambiarEstadoDetectado(bool usandoMando)
    {
        if (_ultimoDispositivoFueMando == usandoMando) return;
        _ultimoDispositivoFueMando = usandoMando;
        OnDispositivoActivoCambiado?.Invoke(_ultimoDispositivoFueMando);
    }

    private static bool HayActividadEnMando(Gamepad g)
    {
        if (g == null) return false;
        if (g.leftStick.ReadValue().sqrMagnitude > 0.04f || g.rightStick.ReadValue().sqrMagnitude > 0.04f) return true;
        if (g.dpad.ReadValue().sqrMagnitude > 0.1f) return true;
        if (g.leftTrigger.ReadValue() > 0.15f || g.rightTrigger.ReadValue() > 0.15f) return true;

        return g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed
               || g.leftShoulder.isPressed || g.rightShoulder.isPressed
               || g.startButton.isPressed || g.selectButton.isPressed
               || g.leftStickButton.isPressed || g.rightStickButton.isPressed;
    }

    private static bool HayActividadEnTecladoORaton()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        var m = Mouse.current;
        if (m != null)
        {
            if (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || m.middleButton.wasPressedThisFrame)
                return true;

            Vector2 pos = m.position.ReadValue();
            if (!_posicionRatonInicializada)
            {
                _ultimaPosicionRaton = pos;
                _posicionRatonInicializada = true;
            }
            else if ((pos - _ultimaPosicionRaton).sqrMagnitude > 9f)
            {
                _ultimaPosicionRaton = pos;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sincroniza el esquema y las máscaras de enlace (bindingMask) de un PlayerInput según el modo elegido.
    /// </summary>
    public static void AplicarAPlayerInput(PlayerInput playerInput)
    {
        if (playerInput == null || !playerInput.isActiveAndEnabled) return;
        AsegurarInicializado();

        if (!HayMandoConectado && (_modoActual == ModoEntrada.Mando || _ultimoDispositivoFueMando))
        {
            ManejarDesconexionDeMando();
        }

        var actions = playerInput.actions;

        if (_modoActual == ModoEntrada.TecladoYRaton)
        {
            playerInput.neverAutoSwitchControlSchemes = true;
            if (actions != null) actions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");

            if (Keyboard.current != null && Mouse.current != null)
                playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
            else if (Keyboard.current != null)
                playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current);
        }
        else if (_modoActual == ModoEntrada.Mando && HayMandoConectado)
        {
            playerInput.neverAutoSwitchControlSchemes = true;
            if (actions != null) actions.bindingMask = InputBinding.MaskByGroup("Gamepad");

            if (Gamepad.current != null)
                playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
        }
        else
        {
            playerInput.neverAutoSwitchControlSchemes = false;
            if (actions != null) actions.bindingMask = null;

            if (_ultimoDispositivoFueMando && Gamepad.current != null && playerInput.currentControlScheme != "Gamepad")
            {
                playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
            }
            else if (!_ultimoDispositivoFueMando && Keyboard.current != null && playerInput.currentControlScheme != "Keyboard&Mouse")
            {
                if (Mouse.current != null)
                    playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
                else
                    playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current);
            }
        }
    }

    public static void AplicarModoATodosLosPlayerInput()
    {
        var inputs = Object.FindObjectsByType<PlayerInput>();
        foreach (var pi in inputs)
        {
            AplicarAPlayerInput(pi);
        }
    }

    /// <summary>
    /// Comprueba si un CallbackContext de una InputAction proviene de un dispositivo permitido por el modo actual.
    /// </summary>
    public static bool ContextoPermitido(InputAction.CallbackContext ctx, bool esInputNombre = false)
    {
        var dev = ctx.control?.device;
        if (dev is Gamepad) return PermitirMando;
        if (dev is Keyboard || dev is Mouse) return PermitirTecladoYRaton(esInputNombre);
        return true;
    }
}
