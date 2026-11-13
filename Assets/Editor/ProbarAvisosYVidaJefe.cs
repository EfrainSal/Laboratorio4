using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarAvisosYVidaJefe
{
    const string Clave = "L4.PruebaAvisosYVidaJefe", Carpeta = "Reports/AvisosYVidaJefe/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina;
    static double siguiente, inicio;
    static bool segundoPlano;
    static Keyboard teclado;
    static Mouse mouse;
    static Color fondoReferencia, bordeReferencia, textoReferencia;
    static PlayerHealth Jugador => LevelManager.Instance.JugadorActual.GetComponent<PlayerHealth>();
    static ProbarAvisosYVidaJefe()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/UI/Probar avisos y vida del jefe %#&F2")]
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
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking.dat"); RankingManager.SuspenderSincronizacionAutomatica = true;
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; QuitarDispositivos(); Time.timeScale = 1; Application.runInBackground = segundoPlano;
        }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false); File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[PruebaAvisosJefe] " + resultados.Count + " comprobaciones, " + errores.Count + " errores.");
            EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 100) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .03f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static void Exigir(bool ok, string detalle) { if (!ok) throw new Exception(detalle); resultados.Add("PASS " + detalle); }
    static void QuitarDispositivos()
    {
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        teclado = null; mouse = null;
    }
    static void PrepararInput()
    {
        QuitarDispositivos(); teclado = InputSystem.AddDevice<Keyboard>("PruebaAvisosKeyboard"); mouse = InputSystem.AddDevice<Mouse>("PruebaAvisosMouse");
        var input = Jugador.GetComponent<PlayerInput>(); input.neverAutoSwitchControlSchemes = true;
        input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
    }
    static void Tecla(params Key[] keys) { InputSystem.QueueStateEvent(teclado, new KeyboardState(keys)); }
    static void Capturar(string nombre) { ScreenCapture.CaptureScreenshot(Carpeta + nombre + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png"); }
    static Rect Limites(RectTransform t, RectTransform padre)
    {
        var b = RectTransformUtility.CalculateRelativeRectTransformBounds(padre, t);
        return Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
    }
    static IEnumerator Recorrido()
    {
        yield return .4f;
        foreach (string nombre in new[] { "Recepcion", "Escenario1_Entrada", "Piso3_V10", "E3_Laboratorio" })
        {
            GameManager.Instance.IniciarNuevaRun(0); SceneManager.LoadScene(nombre); yield return .7f;
            var recepcion = Object.FindFirstObjectByType<PantallaControlesInicio>();
            if (recepcion != null)
            {
                double limiteControles = EditorApplication.timeSinceStartup + 8;
                while (PantallaControlesInicio.IsShowing && EditorApplication.timeSinceStartup < limiteControles)
                { recepcion.Cerrar(); yield return .1f; }
            }
            Exigir(!DeathScreenController.GameplayBlocked && Time.timeScale == 1, nombre + ": partida activa antes de interactuar");
            Jugador.isInvulnerable = true;
            foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
            PrepararInput();
            var puerta = Object.FindObjectsByType<PuertaNivel>(FindObjectsSortMode.None).First(p => Application.CanStreamedLevelBeLoaded(p.nombreSiguienteEscena));
            var colJugador = Jugador.GetComponent<Collider2D>(); var colPuerta = puerta.GetComponents<Collider2D>().First(c => c.isTrigger);
            Jugador.transform.position += colPuerta.bounds.center - colJugador.bounds.center;
            var rb = Jugador.GetComponent<Rigidbody2D>(); rb.position = Jugador.transform.position; rb.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms(); yield return .25f; Jugador.GetComponent<PlayerInventory>().llavesCount = 0;
            Exigir(puerta.JugadorEnRango, nombre + ": jugador en el panel de la puerta (jugador=" + colJugador.bounds + ", panel=" + colPuerta.bounds + ")");
            if (recepcion != null)
            {
                var objetivo = recepcion.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ObjetivoRecepcion");
                fondoReferencia = objetivo.resolvedStyle.backgroundColor; bordeReferencia = objetivo.resolvedStyle.borderLeftColor;
                textoReferencia = objetivo.Q<Label>().resolvedStyle.color;
                Capturar("referencia-recepcion"); yield return .1f;
            }
            puerta.duracionMensaje = .8f; Tecla(Key.F); yield return .08f; Tecla(); yield return .12f;
            Exigir(SceneManager.GetActiveScene().name == nombre && puerta.AvisoVisible, nombre + ": F sin tarjeta muestra aviso sin cambiar de escena");
            Exigir(puerta.textoMensajeUI == null || !puerta.textoMensajeUI.gameObject.activeInHierarchy, nombre + ": franja gris antigua oculta");
            var aviso = Object.FindFirstObjectByType<AvisoPuertaUI>();
            VisualElement panel;
            if (recepcion != null)
            {
                Exigir(recepcion.AvisoPuertaVisible && aviso == null, "Recepcion reutiliza su panel sin duplicar avisos");
                panel = recepcion.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("ObjetivoRecepcion");
            }
            else
            {
                Exigir(aviso != null && aviso.Visible && aviso.Mensaje == puerta.mensajeFaltaLlave, nombre + ": texto de acceso correcto");
                panel = aviso.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("AvisoAccesoPuerta");
            }
            Exigir(panel.resolvedStyle.backgroundColor == fondoReferencia && panel.resolvedStyle.borderLeftColor == bordeReferencia &&
                panel.Q<Label>().resolvedStyle.color == textoReferencia && panel.Q<Label>().resolvedStyle.fontSize == 18 && panel.resolvedStyle.borderLeftWidth == 3,
                nombre + ": fondo, borde, texto y tipografía iguales a recepción");
            Exigir(panel.worldBound.yMin > panel.panel.visualTree.worldBound.height * .75f && panel.pickingMode == PickingMode.Ignore,
                nombre + ": aviso abajo sin bloquear clics");
            Capturar("aviso-" + nombre); yield return .1f;
            Tecla(Key.F); yield return .08f; Tecla(); yield return .45f;
            Exigir(puerta.AvisoVisible, nombre + ": repetir F renueva el aviso");
            yield return .45f;
            Exigir(!puerta.AvisoVisible, nombre + ": el aviso expira correctamente");
            Exigir(Object.FindObjectsByType<BossHealth>(FindObjectsSortMode.None).Length == 0, nombre + ": no se añadió jefe al mapa");
        }
        QuitarDispositivos();
        foreach (string nombre in new[] { "Escenario4_Jefe", "Nivel1" })
        {
            GameManager.Instance.IniciarNuevaRun(0); SceneManager.LoadScene(nombre); yield return .7f;
            Jugador.isInvulnerable = true;
            var jefe = Object.FindFirstObjectByType<BossHealth>(); jefe.GetComponent<BossAI>().enabled = false;
            var original = jefe.bossHealthUIContainer;
            var barra = original.GetComponent<RectTransform>(); var canvas = barra.parent as RectTransform;
            var vidaJugador = canvas.Find("HealthBarContainer") as RectTransform;
            Exigir(!Limites(barra, canvas).Overlaps(Limites(vidaJugador, canvas)), nombre + ": barras reales separadas");
            float anterior = jefe.CurrentHealth; jefe.TakeDamage(75);
            Exigir(jefe.CurrentHealth == anterior - 75 && Mathf.Approximately(jefe.healthBarFill.fillAmount, jefe.CurrentHealth / jefe.maxHealth), nombre + ": barra del jefe sigue mostrando daño");
            Capturar("barras-" + nombre); yield return .15f;
            // Probar las proporciones de CanvasScaler sin modificar GameView ni escenas guardadas.
            var temporal = new GameObject("PruebaDistribucionHUD", typeof(RectTransform)).GetComponent<RectTransform>();
            var copiaJugador = Object.Instantiate(vidaJugador, temporal, false); copiaJugador.name = "HealthBarContainer";
            var copiaJefe = Object.Instantiate(barra, temporal, false); jefe.bossHealthUIContainer = copiaJefe.gameObject;
            foreach (float ancho in new[] { 1920f, 1440f, 607.5f })
            {
                temporal.sizeDelta = new Vector2(ancho, 1080); jefe.AcomodarBarraVida();
                var a = Limites(copiaJugador, temporal); var b = Limites(copiaJefe, temporal);
                Exigir(!a.Overlaps(b) && b.xMin >= temporal.rect.xMin && b.xMax <= temporal.rect.xMax && b.yMin >= temporal.rect.yMin && b.yMax <= temporal.rect.yMax,
                    nombre + ": distribución sin solapamiento y dentro de ventana, ancho lógico " + ancho);
            }
            jefe.bossHealthUIContainer = original; jefe.AcomodarBarraVida(); Object.Destroy(temporal.gameObject);
            yield return .1f;
        }
        Exigir(errores.Count == 0, "Avisos y vida del jefe sin errores ni excepciones");
    }
}
