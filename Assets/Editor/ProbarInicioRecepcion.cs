using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Laboratorio4.Recepcion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarInicioRecepcion
{
    private const string Clave = "L4.PruebaInicioRecepcion";
    private const string Carpeta = "Reports/InicioRecepcion/";
    private static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    private static IEnumerator rutina;
    private static double siguiente, inicio;
    private static Keyboard teclado;
    private static Mouse mouse;
    private static Gamepad mando;
    private static GameObject jugador;
    private static PantallaControlesInicio pantalla;
    private static bool ejecutarEnSegundoPlanoAnterior;
    private static bool cargaEnNegro;
    private static int personajeCaptura;
    private static float fuenteMenu, bordeMenu;

    static ProbarInicioRecepcion()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Recepción/Probar flujo de inicio %#&F5")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Carpeta); Selection.activeObject = null; StageUtility.GoToMainStage();
        SessionState.SetBool(Clave, true); EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            SceneManager.sceneLoaded += RegistrarCarga;
            ejecutarEnSegundoPlanoAnterior = Application.runInBackground;
            Application.runInBackground = true;
            foreach (string key in new[] { "PlayerRankingName", "SelectedCharacter", "MejorPuntuacion" })
            {
                SessionState.SetBool(Clave + key + "existe", PlayerPrefs.HasKey(key));
                if (key == "PlayerRankingName") SessionState.SetString(Clave + key, PlayerPrefs.GetString(key));
                else SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key));
            }
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-prueba.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-prueba.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            RankingManager.SaveRanking(new RankingData { version = 2 });
            resultados.Clear(); errores.Clear(); inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            SceneManager.sceneLoaded -= RegistrarCarga;
            Application.runInBackground = ejecutarEnSegundoPlanoAnterior;
            QuitarDispositivos(); rutina = null;
            foreach (string key in new[] { "PlayerRankingName", "SelectedCharacter", "MejorPuntuacion" })
                if (!SessionState.GetBool(Clave + key + "existe", false)) PlayerPrefs.DeleteKey(key);
                else if (key == "PlayerRankingName") PlayerPrefs.SetString(key, SessionState.GetString(Clave + key, ""));
                else PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0));
            PlayerPrefs.Save();
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(e => "ERROR " + e)));
            Debug.Log("[PruebaInicioRecepcion] " + resultados.Count + " comprobaciones, " + errores.Count + " errores.");
        }
    }
    private static void RegistrarCarga(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Escenario1_Entrada" || !TransicionEntrePisos.EnCurso) return;
        cargaEnNegro = TransicionEntrePisos.Opacidad >= .999f;
        ScreenCapture.CaptureScreenshot("Reports/FundidosPisos/negro-al-cargar-" + personajeCaptura + ".png");
    }
    private static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 120) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float t ? t : .02f);
        }
        catch (Exception e) { errores.Add(e.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    private static void Exigir(bool cumple, string descripcion)
    {
        if (!cumple) throw new Exception(descripcion);
        resultados.Add("PASS " + descripcion);
    }
    private static void ConfirmarPersonaje(int id)
    {
        var menu = Object.FindFirstObjectByType<MainMenuController>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MainMenuController).GetField("_selectedCharacter", flags).SetValue(menu, id);
        ((TextField)typeof(MainMenuController).GetField("_playerNameInput", flags).GetValue(menu)).value = "PRUEBA LOCAL";
        typeof(MainMenuController).GetMethod("OnConfirmCharacter", flags).Invoke(menu, null);
    }
    private static void Emparejar(bool usarMando)
    {
        QuitarDispositivos(); jugador = GameObject.FindGameObjectWithTag("Player");
        teclado = InputSystem.AddDevice<Keyboard>("PruebaRecepcionKeyboard");
        mouse = InputSystem.AddDevice<Mouse>("PruebaRecepcionMouse");
        mando = InputSystem.AddDevice<Gamepad>("PruebaRecepcionGamepad");
        var input = jugador.GetComponent<PlayerInput>(); input.neverAutoSwitchControlSchemes = true;
        if (usarMando) input.SwitchCurrentControlScheme("Gamepad", mando);
        else input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
        pantalla = Object.FindFirstObjectByType<PantallaControlesInicio>();
        Tecla(); InputSystem.QueueStateEvent(mando, new GamepadState());
    }
    private static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    private static void Raton(Vector2 posicion, bool pulsado) => InputSystem.QueueStateEvent(mouse, new MouseState { position = posicion, buttons = (ushort)(pulsado ? 1 : 0) });
    private static void Guiar(Vector2 destino, bool mantenerF = false)
    {
        Vector2 delta = destino - (Vector2)jugador.transform.position;
        var keys = new List<Key>();
        if (delta.x > .08f) keys.Add(Key.D); else if (delta.x < -.08f) keys.Add(Key.A);
        if (delta.y > .08f) keys.Add(Key.W); else if (delta.y < -.08f) keys.Add(Key.S);
        if (mantenerF) keys.Add(Key.F);
        Tecla(keys.ToArray()); Raton(Camera.main.WorldToScreenPoint((Vector3)destino), false);
    }
    private static void QuitarDispositivos()
    {
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (mando != null && mando.added) InputSystem.RemoveDevice(mando);
        teclado = null; mouse = null; mando = null;
    }
    private static IEnumerator Recorrido()
    {
        yield return .7f;
        Directory.CreateDirectory("Reports/FundidosPisos");
        var menuReferencia = Object.FindFirstObjectByType<MainMenuController>().GetComponent<UIDocument>().rootVisualElement.Q<Button>("PlayButton");
        fuenteMenu = menuReferencia.resolvedStyle.fontSize; bordeMenu = menuReferencia.resolvedStyle.borderLeftWidth;
        ScreenCapture.CaptureScreenshot("Reports/FundidosPisos/menu-referencia.png"); yield return .1f;
        Exigir(new SerializedObject(Object.FindFirstObjectByType<MainMenuController>()).FindProperty("_gameSceneName").stringValue == "Recepcion", "Iniciar partida apunta a la escena real Recepcion");
        for (int id = 0; id < 2; id++)
        {
            personajeCaptura = id;
            ConfirmarPersonaje(id); yield return .9f; Emparejar(id == 1); yield return .15f;
            Exigir(SceneManager.GetActiveScene().name == "Recepcion", "Recepcion carga antes de Entrada, personaje " + id);
            Exigir(Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).Length == 1 && jugador.name.StartsWith(id == 0 ? "Player_Jason" : "Player_Milena"), "Sólo aparece el prefab seleccionado, personaje " + id);
            Exigir(Object.FindObjectsByType<RecepcionJugador>(FindObjectsSortMode.None).Length == 0, "Controlador del prototipo desactivado");
            Exigir(PantallaControlesInicio.IsShowing && DeathScreenController.GameplayBlocked && Time.timeScale == 0, "Ayuda oscurecida visible y partida detenida");
            Exigir(LevelManager.Instance.healthBarFillUI != null, "HUD real conectado en Recepcion");
            var root = pantalla.GetComponent<UIDocument>().rootVisualElement;
            var boton = root.Q<Button>("EntendidoButton");
            Exigir(boton.ClassListContains("btn-menu") && pantalla.estilosMenu != null,
                "Entendido comparte la hoja de estilos y la clase del menú");
            Exigir(Mathf.Approximately(boton.resolvedStyle.fontSize, fuenteMenu) && Mathf.Approximately(boton.resolvedStyle.borderLeftWidth, bordeMenu),
                "Botón de controles conserva tipografía y borde lateral del menú");
            var textura = boton.resolvedStyle.backgroundImage.texture;
            Exigir(textura != null && textura.GetPixel(0, 0).a > .99f && textura.GetPixel(textura.width - 1, 0).a < .01f,
                "Botón de controles usa el mismo degradado del menú");
            var filas = root.Query<Label>().ToList().Where(l => l.name.StartsWith("Binding_")).ToArray();
            Exigir(filas.Length == 10 && filas.All(l => !string.IsNullOrWhiteSpace(l.text) && l.text != "—"), "Diez controles muestran sus asignaciones actuales");
            var bounds = pantalla.Panel.worldBound; var viewport = root.worldBound;
            Exigir(bounds.xMin >= viewport.xMin && bounds.yMin >= viewport.yMin && bounds.xMax <= viewport.xMax + 1 && bounds.yMax <= viewport.yMax + 1, "Panel completo dentro de la pantalla");
            ScreenCapture.CaptureScreenshot(Carpeta + (id == 0 ? "controles-jason.png" : "controles-milena-mando.png")); yield return .15f;
            ScreenCapture.CaptureScreenshot("Reports/FundidosPisos/controles-estilo-menu-" + id + ".png"); yield return .1f;
            var posicion = jugador.transform.position; float reloj = GameManager.Instance.tiempoRun;
            var arma = jugador.GetComponent<WeaponManager>(); bool melee = arma.isUsingMelee;
            if (id == 0)
            {
                Tecla(Key.W, Key.Q, Key.R, Key.Space, Key.Escape);
                Raton(new Vector2(Screen.width * .5f, Screen.height * .5f), true);
            }
            else InputSystem.QueueStateEvent(mando, new GamepadState { leftStick = Vector2.up, rightTrigger = 1 });
            yield return .25f;
            Exigir(Vector3.Distance(posicion, jugador.transform.position) < .001f && Mathf.Approximately(reloj, GameManager.Instance.tiempoRun), "Ayuda bloquea movimiento y cronómetro");
            Exigir(arma.isUsingMelee == melee && !jugador.GetComponent<MacheteAnimacion>().Reproduciendo && !PauseMenuController.IsPaused, "Ayuda bloquea combate, habilidad y menú de pausa");
            Tecla(); Raton(Vector2.zero, false); InputSystem.QueueStateEvent(mando, new GamepadState()); yield return .1f;
            if (id == 0) Tecla(Key.Enter); else InputSystem.QueueStateEvent(mando, new GamepadState().WithButton(GamepadButton.South));
            yield return .1f; Tecla(); InputSystem.QueueStateEvent(mando, new GamepadState()); yield return .45f;
            Exigir(!PantallaControlesInicio.IsShowing && !DeathScreenController.GameplayBlocked && Time.timeScale == 1, "Entendido restaura el juego con teclado o mando");
            Exigir(root.Q<VisualElement>("ObjetivoRecepcion").resolvedStyle.display != DisplayStyle.None, "Objetivo de la tarjeta permanece visible al jugar");
            if (id == 0)
            {
                jugador.GetComponent<PlayerHealth>().currentHealth = 69;
                jugador.GetComponent<PlayerInventory>().vendasCount = 3;
                jugador.GetComponent<PlayerShooting>().RestaurarMunicion(9, 42);
                Exigir(GameManager.Instance.GuardarRun(), "Guardar en Recepcion conserva la partida");
                QuitarDispositivos(); SceneManager.LoadScene("MainMenu"); yield return .7f;
                typeof(MainMenuController).GetMethod("OnContinueClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Object.FindFirstObjectByType<MainMenuController>(), null);
                yield return .8f; Emparejar(false); yield return .1f;
                Exigir(SceneManager.GetActiveScene().name == "Recepcion" && Mathf.Approximately(jugador.GetComponent<PlayerHealth>().currentHealth, 69), "Continuar vuelve a Recepcion y recupera la salud guardada");
                yield return .4f;
                root = pantalla.GetComponent<UIDocument>().rootVisualElement;
                var centro = root.Q<Button>("EntendidoButton").worldBound.center;
                Vector2 clic = new Vector2(centro.x / root.worldBound.width * Screen.width, Screen.height - centro.y / root.worldBound.height * Screen.height);
                Raton(clic, true); yield return .1f; Raton(clic, false); yield return .5f;
                Exigir(!PantallaControlesInicio.IsShowing, "Botón Entendido responde al clic del mouse");
            }
            var tarjeta = Object.FindFirstObjectByType<TarjetaSalidaRecepcion>();
            Exigir(tarjeta != null && tarjeta.GetComponent<ItemPickup>() == null, "Tarjeta única con recogida de inventario y sin doble controlador");
            var puerta = Object.FindFirstObjectByType<PuertaNivel>();
            var puertaVisual = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(r => r.name == "Puerta_Salida_Escenario1");
            var boundsTarjeta = tarjeta.GetComponent<SpriteRenderer>().bounds;
            Exigir(boundsTarjeta.min.x > puertaVisual.bounds.max.x && Mathf.Abs(boundsTarjeta.min.x - puertaVisual.bounds.max.x - .12f) < .02f,
                "Tarjeta colocada exactamente al lado derecho de la puerta");
            Exigir(boundsTarjeta.center.y < puertaVisual.bounds.min.y && Vector3.Distance(tarjeta.transform.position, puerta.transform.position) < 2,
                "Tarjeta en el suelo, junto al panel de salida");
            var entrada = jugador.GetComponent<PlayerInput>();
            entrada.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
            InputSystem.QueueStateEvent(mando, new GamepadState()); Tecla(); yield return .1f;
            ScreenCapture.CaptureScreenshot(Carpeta + "recepcion-jugable-" + id + ".png"); yield return .15f;
            // Rodear físicamente el detector del pasillo y visitar primero el panel sin tarjeta.
            foreach (Vector2 punto in new[] { new Vector2(8.7f, -5.8f), new Vector2(8.7f, -3.3f), (Vector2)puerta.transform.position })
            {
                float limite = Time.unscaledTime + 5;
                while (Vector2.Distance(jugador.transform.position, punto) > .16f && Time.unscaledTime < limite)
                { Guiar(punto); yield return .04f; }
                Tecla(); yield return .1f;
                Exigir(Vector2.Distance(jugador.transform.position, punto) <= .2f, "Ruta física de recepción transitable: " + punto);
            }
            Exigir(puerta.JugadorEnRango && jugador.GetComponent<PlayerInventory>().llavesCount == 0, "Panel accesible antes de recoger la tarjeta");
            Tecla(Key.F); yield return .15f; Tecla(); yield return .1f;
            Exigir(SceneManager.GetActiveScene().name == "Recepcion" && puerta.textoMensajeUI.gameObject.activeSelf,
                "F sin tarjeta impide salir y muestra el aviso");
            float zoom = Camera.main.orthographicSize; Camera.main.orthographicSize = 2.5f; yield return .35f;
            ScreenCapture.CaptureScreenshot("Reports/PanelRecepcion/puerta-y-tarjeta-" + id + ".png"); yield return .15f;
            Camera.main.orthographicSize = zoom;
            Vector2 posicionTarjeta = tarjeta.transform.position;
            foreach (Vector2 punto in new[] { new Vector2(posicionTarjeta.x, -3.4f), posicionTarjeta })
            {
                float limite = Time.unscaledTime + 3;
                while (Vector2.Distance(jugador.transform.position, punto) > .16f && Time.unscaledTime < limite)
                { Guiar(punto); yield return .04f; }
                Tecla(); yield return .1f;
            }
            yield return .8f;
            Exigir(SceneManager.GetActiveScene().name == "Recepcion" && tarjeta.Recogida && jugador.GetComponent<PlayerInventory>().llavesCount == 1,
                "Recoger tarjeta sólo suma una unidad al inventario y no cambia de escena");
            Exigir(!DeathScreenController.GameplayBlocked && Time.timeScale == 1, "Recoger tarjeta no bloquea ni congela al jugador");
            if (id == 0)
            {
                Exigir(GameManager.Instance.GuardarRun(), "Se puede guardar con la tarjeta en posesión");
                QuitarDispositivos(); SceneManager.LoadScene("MainMenu"); yield return .7f;
                typeof(MainMenuController).GetMethod("OnContinueClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Object.FindFirstObjectByType<MainMenuController>(), null);
                yield return .9f; Emparejar(false); yield return .15f;
                Exigir(jugador.GetComponent<PlayerInventory>().llavesCount == 1
                    && Object.FindObjectsByType<TarjetaSalidaRecepcion>(FindObjectsSortMode.None).Length == 0,
                    "Continuar conserva la tarjeta guardada sin duplicarla en el suelo");
                Tecla(Key.Enter); yield return .1f; Tecla(); yield return .45f;
                puerta = Object.FindFirstObjectByType<PuertaNivel>();
            }
            Exigir(!puerta.JugadorEnRango, "Tarjeta recogida fuera del alcance del panel: hay que acercarse");
            Tecla(Key.F); yield return .12f;
            Exigir(SceneManager.GetActiveScene().name == "Recepcion" && jugador.GetComponent<PlayerInventory>().llavesCount == 1,
                "F con tarjeta lejos del panel no permite salir ni consume la tarjeta");
            float llegada = Time.unscaledTime + 3;
            while (Vector2.Distance(jugador.transform.position, puerta.transform.position) > .16f && Time.unscaledTime < llegada)
            { Guiar(puerta.transform.position, true); yield return .04f; }
            Tecla(Key.F); yield return .2f;
            Exigir(puerta.JugadorEnRango && SceneManager.GetActiveScene().name == "Recepcion",
                "Llegar al panel con F ya mantenida requiere una nueva pulsación");
            Tecla(); yield return .1f;
            ScreenCapture.CaptureScreenshot("Reports/PanelRecepcion/tarjeta-en-inventario-" + id + ".png"); yield return .15f;
            cargaEnNegro = false;
            bool oscureciendo = false, iluminando = false, entradaBloqueada = false, duplicadoRechazado = false;
            bool capturaSalida = false, capturaEntrada = false;
            Tecla(Key.F);
            float finTransicion = Time.unscaledTime + 10;
            while ((SceneManager.GetActiveScene().name == "Recepcion" || TransicionEntrePisos.EnCurso) && Time.unscaledTime < finTransicion)
            {
                if (TransicionEntrePisos.EnCurso)
                {
                    entradaBloqueada |= DeathScreenController.GameplayBlocked && Time.timeScale == 0;
                    if (!duplicadoRechazado) duplicadoRechazado = !TransicionEntrePisos.CargarEscena("Piso3_V10");
                    float alpha = TransicionEntrePisos.Opacidad;
                    bool origen = SceneManager.GetActiveScene().name == "Recepcion";
                    if (alpha > .01f && alpha < .99f)
                    {
                        oscureciendo |= origen; iluminando |= !origen;
                        if (origen && alpha > .3f && !capturaSalida)
                        { capturaSalida = true; ScreenCapture.CaptureScreenshot("Reports/FundidosPisos/oscureciendo-" + id + ".png"); }
                        if (!origen && alpha < .7f && !capturaEntrada)
                        { capturaEntrada = true; ScreenCapture.CaptureScreenshot("Reports/FundidosPisos/iluminando-" + id + ".png"); }
                    }
                }
                yield return .02f;
            }
            Tecla();
            Exigir(oscureciendo && iluminando, "Hay fundido de salida y entrada a ambos lados del cambio de piso");
            Exigir(cargaEnNegro, "La escena nueva se carga con la pantalla completamente negra");
            Exigir(entradaBloqueada && duplicadoRechazado, "El fundido bloquea controles y solicitudes de carga duplicadas");
            Exigir(!TransicionEntrePisos.EnCurso && TransicionEntrePisos.Opacidad == 0, "El fundido termina transparente sin dejar una pantalla negra");
            Exigir(!Object.FindFirstObjectByType<TransicionEntrePisos>().GetComponent<CanvasGroup>().blocksRaycasts, "La cortina deja de interceptar los clics al terminar");
            Exigir(SceneManager.GetActiveScene().name == "Escenario1_Entrada", "F frente al panel con tarjeta carga Entrada, personaje " + id);
            yield return .5f; QuitarDispositivos(); jugador = GameObject.FindGameObjectWithTag("Player");
            foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
            Exigir(jugador.name.StartsWith(id == 0 ? "Player_Jason" : "Player_Milena"), "Se conserva el personaje elegido en Entrada");
            Exigir(jugador.GetComponent<PlayerInventory>().llavesCount == 0, "La tarjeta de Recepcion se consume antes de Entrada");
            Exigir(GameManager.Instance.pisoActual == 1, "El panel del prólogo conserva el índice del primer piso");
            Exigir(!PantallaControlesInicio.BloqueaJuego && Time.timeScale == 1 && !PauseMenuController.PauseBlocked, "Transición libera tiempo, controles y pausa");
            if (id == 0)
                Exigir(Mathf.Approximately(jugador.GetComponent<PlayerHealth>().currentHealth, 69) && jugador.GetComponent<PlayerInventory>().vendasCount == 3 && jugador.GetComponent<PlayerShooting>().currentAmmoInClip == 9, "Entrada conserva vida, inventario y munición de Recepcion");
            Exigir(LevelManager.Instance.EnemigosDerrotados == 0 && !LevelManager.Instance.TarjetaGenerada, "La cuota y tarjeta del siguiente piso empiezan independientes");
            Exigir(Object.FindObjectsByType<BossAI>(FindObjectsSortMode.None).Length == 0, "No se agregó jefe al flujo inicial");
            GameManager.Instance.IniciarNuevaRun(0); SceneManager.LoadScene("MainMenu"); yield return .7f;
        }
        Exigir(errores.Count == 0, "Flujo completo sin errores ni excepciones");
    }
}
