using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarIntroduccionComic
{
    const string Clave = "L4.ComicIntro.QA", Carpeta = "Reports/ComicIntro/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina; static double siguiente, inicio; static bool segundoPlano, audioPausa; static float volumen;
    static Keyboard teclado; static Mouse mouse;
    static IntroduccionComic Comic => Object.FindFirstObjectByType<IntroduccionComic>();
    static ProbarIntroduccionComic()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Cómic/Probar rutas clic Esc y silencio")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Carpeta); Selection.activeObject = null; StageUtility.GoToMainStage();
        SessionState.SetString(Clave + ".escena", SceneManager.GetActiveScene().path); SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    static void Estado(PlayModeStateChange e)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (e == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear(); segundoPlano = Application.runInBackground; Application.runInBackground = true;
            audioPausa = AudioListener.pause; volumen = AudioListener.volume;
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking.dat"); RankingManager.SuspenderSincronizacionAutomatica = true;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
            { SessionState.SetBool(Clave + key + ".existe", PlayerPrefs.HasKey(key)); SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key)); }
            SessionState.SetBool(Clave + ".nombreExiste", PlayerPrefs.HasKey("PlayerRankingName")); SessionState.SetString(Clave + ".nombre", PlayerPrefs.GetString("PlayerRankingName"));
            teclado = InputSystem.AddDevice<Keyboard>("ComicIntroKeyboard"); mouse = InputSystem.AddDevice<Mouse>("ComicIntroMouse");
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null;
            if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            teclado = null; mouse = null; Time.timeScale = 1; Application.runInBackground = segundoPlano;
            AudioListener.pause = audioPausa; AudioListener.volume = volumen;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
                if (SessionState.GetBool(Clave + key + ".existe", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0)); else PlayerPrefs.DeleteKey(key);
            if (SessionState.GetBool(Clave + ".nombreExiste", false)) PlayerPrefs.SetString("PlayerRankingName", SessionState.GetString(Clave + ".nombre", "")); else PlayerPrefs.DeleteKey("PlayerRankingName");
            PlayerPrefs.Save();
        }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false); SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null; RankingManager.SuspenderSincronizacionAutomatica = false;
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[ComicIntroQA] " + resultados.Count + " comprobaciones; " + errores.Count + " errores.");
            EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 150) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .03f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static void Exigir(bool ok, string mensaje) { if (!ok) throw new Exception(mensaje); resultados.Add("PASS " + mensaje); }
    static void Privado(object obj, string metodo, params object[] args) => obj.GetType().GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    static void Boton(bool presionado) => InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, Screen.height * .5f), buttons = (ushort)(presionado ? 1 : 0) });
    static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    static void Confirmar(int personaje)
    {
        var menu = Object.FindFirstObjectByType<MainMenuController>();
        menu.GetComponent<UIDocument>().rootVisualElement.Q<TextField>("PlayerNameInput").value = "ComicQA";
        Privado(menu, "SelectCharacter", personaje); Privado(menu, "OnConfirmCharacter");
    }
    static void Capturar(string nombre) => ScreenCapture.CaptureScreenshot(Carpeta + nombre + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
    static IEnumerator Recorrido()
    {
        yield return .4f;
        var dependencias = AssetDatabase.GetDependencies(IntegrarIntroduccionComic.Escena, true);
        Exigir(!dependencias.Any(p => new[] { ".wav", ".mp3", ".ogg", ".flac", ".aiff", ".aif" }.Contains(Path.GetExtension(p).ToLowerInvariant())), "Escena del cómic sin dependencias de audio");
        foreach (string pagina in new[] { "Llegada", "RutaJason", "RutaMilena" })
        {
            var sprites = IntegrarIntroduccionComic.Vinetas(pagina);
            Exigir(sprites.Length == 4 && sprites.All(s => s.rect.yMax <= 976 && s.rect.xMin >= 8 && s.rect.xMax <= 1528), pagina + ": cuatro viñetas sin franja superior de títulos");
        }
        AudioListener.volume = .63f; AudioListener.pause = false;
        for (int personaje = 0; personaje < 2; personaje++)
        {
            if (personaje > 0) { SceneManager.LoadScene("MainMenu"); yield return .5f; }
            Boton(true); Confirmar(personaje); yield return .25f;
            Exigir(SceneManager.GetActiveScene().name == IntroduccionComic.NombreEscena && Comic != null && Comic.Personaje == personaje,
                "Menú dirige a introducción con personaje " + personaje);
            Exigir(Comic.TotalVinetas == 8 && Comic.IndiceActual == 0, "Ocho viñetas; confirmar personaje no omite la primera");
            Exigir(AudioListener.pause && AudioListener.volume == 0 && Time.timeScale == 0 && PauseMenuController.PauseBlocked,
                "Cómic silencioso y partida detenida");
            Exigir(SceneManager.GetActiveScene().GetRootGameObjects().All(r => r.GetComponentsInChildren<AudioSource>(true).Length == 0 && r.GetComponentsInChildren<AudioListener>(true).Length == 0), "Sin componentes de audio en la escena del cómic");
            var textos = Comic.GetComponentsInChildren<TextMeshProUGUI>();
            Exigir(textos.Any(t => t.text == "siguiente >>") && textos.Any(t => t.text.Contains("Esc") && t.text.Contains("Saltar")), "Indicadores siguiente >> y Esc visibles");
            Boton(false); yield return .06f;
            float tiempo = GameManager.Instance.tiempoRun; yield return .2f;
            Exigir(GameManager.Instance.tiempoRun == tiempo && Comic.IndiceActual == 0, "Sin avance automático ni tiempo de partida durante lectura");
            for (int i = 0; i < 8; i++)
            {
                var c = Comic; Sprite esperado = i < 4 ? c.llegada[i] : (personaje == 0 ? c.rutaJason : c.rutaMilena)[i - 4];
                Exigir(c.IndiceActual == i && c.SpriteActual == esperado, "Personaje " + personaje + ": viñeta " + (i + 1) + " de la ruta correcta");
                Exigir(c.GetComponentsInChildren<TextMeshProUGUI>().All(t => t.isActiveAndEnabled && t.textInfo.characterCount > 0 &&
                    t.mesh != null && t.mesh.vertexCount > 0 && t.canvasRenderer.GetAlpha() > .99f), "Indicadores dibujados en viñeta " + (i + 1));
                if (i == 0 || i == 4 || i == 7) { Capturar("personaje-" + personaje + "-vineta-" + (i + 1)); yield return .08f; }
                Boton(true); yield return .08f;
                if (i == 0)
                {
                    yield return .25f;
                    Exigir(Comic.IndiceActual == 1, "Mantener el clic avanza sólo una viñeta");
                }
                Boton(false); yield return .06f;
            }
            double limite = EditorApplication.timeSinceStartup + 10;
            while ((SceneManager.GetActiveScene().name != "Recepcion" || TransicionEntrePisos.EnCurso) && EditorApplication.timeSinceStartup < limite) yield return .1f;
            yield return .25f;
            Exigir(SceneManager.GetActiveScene().name == "Recepcion" && GameManager.Instance.personajeSeleccionado == personaje && !IntroduccionComic.EnCurso, "Último clic lleva a recepción con el personaje elegido");
            Exigir(!AudioListener.pause && Mathf.Approximately(AudioListener.volume, .63f), "Al salir se restaura el estado de audio previo");
        }
        foreach (var caso in new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 3), new Vector2Int(1, 7) })
        {
            SceneManager.LoadScene("MainMenu"); yield return .5f; Confirmar(caso.x);
            // Esc se acepta incluso en el primer Update, sin esperar el bloqueo del clic de confirmación.
            if (caso.y > 0)
            {
                yield return .2f;
                for (int i = 0; i < caso.y; i++) { Boton(true); yield return .06f; Boton(false); yield return .06f; }
            }
            Tecla(Key.Escape); yield return .08f; Tecla();
            double limite = EditorApplication.timeSinceStartup + 10;
            while ((SceneManager.GetActiveScene().name != "Recepcion" || TransicionEntrePisos.EnCurso) && EditorApplication.timeSinceStartup < limite) yield return .1f;
            yield return .2f;
            Exigir(SceneManager.GetActiveScene().name == "Recepcion" && !IntroduccionComic.EnCurso && GameManager.Instance.personajeSeleccionado == caso.x,
                "Esc omite toda la cinemática, personaje " + caso.x + ", desde viñeta " + (caso.y + 1));
            Exigir(!AudioListener.pause && Mathf.Approximately(AudioListener.volume, .63f), "Esc restaura audio y no deja mute persistente");
        }
        Exigir(GameManager.Instance.GuardarRun(), "Guardar partida de prueba al llegar a recepción");
        SceneManager.LoadScene("MainMenu"); yield return .5f;
        Privado(Object.FindFirstObjectByType<MainMenuController>(), "OnContinueClicked"); yield return .7f;
        Exigir(SceneManager.GetActiveScene().name == "Recepcion" && Comic == null, "Continuar conserva su flujo y no repite introducción");
        Exigir(errores.Count == 0, "Introducción sin errores ni excepciones");
    }
}
