using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Revisión visual en la escena real, con guardados aislados y sin publicar puntuaciones.
[InitializeOnLoad]
public static class RevisarMacheteReposo
{
    private const string Clave = "Laboratorio4.MacheteReposo.Revision";
    private const string Carpeta = "Reports/MacheteReposo/";
    private static int fase, personaje;
    private static double inicio, etapa;
    private static float inicioTajo;
    private static Vector2 direccion;
    private static Vector2 cadera;
    private static GameObject jugador;
    private static ArmaIndependiente arma;
    private static Mouse mouse;
    private static Keyboard teclado;
    private static string escena;
    private static readonly List<string> resultados = new List<string>();
    private static readonly List<string> errores = new List<string>();

    static RevisarMacheteReposo()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (mensaje, traza, tipo) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (tipo == LogType.Error || tipo == LogType.Exception)) errores.Add(mensaje);
        };
    }
    [MenuItem("Laboratorio 4/Machete/Revisar postura y agarre %&F8")]
    public static void Revisar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null;
        StageUtility.GoToMainStage();
        Directory.CreateDirectory(Carpeta);
        SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        EditorApplication.EnterPlaymode();
    }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-prueba.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-prueba.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            fase = personaje = 0;
            jugador = null; arma = null; resultados.Clear(); errores.Clear();
            inicio = EditorApplication.timeSinceStartup;
        }
        if (estado == PlayModeStateChange.ExitingPlayMode) QuitarDispositivos();
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null;
            RankingManager.SuspenderSincronizacionAutomatica = false;
            EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        }
    }
    private static void Exigir(bool valor, string mensaje)
    {
        if (!valor) throw new Exception(mensaje);
        resultados.Add("PASS " + (personaje == 0 ? "Jason: " : "Milena: ") + mensaje);
    }
    private static void Avanzar(int siguiente) { fase = siguiente; etapa = EditorApplication.timeSinceStartup; }
    private static void Capturar(string nombre) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(Carpeta + (personaje == 0 ? "Jason-" : "Milena-") + nombre + ".png"));

    // Puntos medidos sobre el arte original (rect de 316 x 268): mango y extremo de la hoja.
    private static Vector3 PuntoArte(Vector2 pixel)
    {
        Vector2 local = (pixel - arma.visual.sprite.pivot) / arma.visual.sprite.pixelsPerUnit;
        if (arma.visual.flipY) local.y = -local.y;
        return arma.visual.transform.TransformPoint(local);
    }
    private static void ComprobarAgarre()
    {
        Vector3 mano = arma.pivote.TransformPoint(new Vector3(arma.posicionManoMachete.x,
            direccion.x < 0 ? -arma.posicionManoMachete.y : arma.posicionManoMachete.y));
        Exigir(Vector3.Distance(PuntoArte(new Vector2(246, 214)), mano) < .02f, "El mango permanece en la mano");
    }
    private static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false) || !EditorApplication.isPlaying || fase < 0) return;
        double ahora = EditorApplication.timeSinceStartup;
        try
        {
            if (ahora - inicio > 60) throw new Exception("Timeout de revisión: fase " + fase);
            if (jugador != null && mouse != null)
            {
                Vector2 pantalla = Camera.main.WorldToScreenPoint(arma.pivote.position + (Vector3)direccion * 4f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pantalla });
            }
            if (fase == 0 && ahora - inicio > .6)
            {
                escena = new SerializedObject(Object.FindFirstObjectByType<MainMenuController>()).FindProperty("_gameSceneName").stringValue;
                GameManager.Instance.IniciarNuevaRun(personaje);
                Avanzar(1); SceneManager.LoadScene(escena); return;
            }
            if (fase == 1 && ahora - etapa > 1 && SceneManager.GetActiveScene().name == escena)
            {
                jugador = GameObject.FindGameObjectWithTag("Player");
                arma = jugador.GetComponent<ArmaIndependiente>();
                cadera = arma.cadera;
                Exigir(jugador.GetComponent<Laboratorio4.Animacion.AnimadorSprites8Direcciones>().biblioteca.personaje == (personaje == 0 ? "Jason" : "Milena"), "Personaje elegido desde MainMenu");
                foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
                foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
                jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
                jugador.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                Camera.main.GetComponent<LaboratorioYiba.Piso3V10.Piso3V10Camara>().enabled = false;
                Camera.main.transform.position = new Vector3(jugador.transform.position.x, jugador.transform.position.y + .3f, Camera.main.transform.position.z);
                Camera.main.orthographicSize = 2.5f;
                teclado = InputSystem.AddDevice<Keyboard>("TecladoRevisionMachete");
                mouse = InputSystem.AddDevice<Mouse>("MouseRevisionMachete");
                var input = jugador.GetComponent<PlayerInput>();
                input.neverAutoSwitchControlSchemes = true;
                input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
                jugador.GetComponent<WeaponManager>().EquipWeapon(true);
                jugador.GetComponent<MacheteAnimacion>().duracion = 1f; // Sólo en esta revisión, para observar el recorrido.
                direccion = Vector2.right;
                Avanzar(2); return;
            }
            if ((fase == 2 || fase == 6) && ahora - etapa > .3)
            {
                ComprobarAgarre();
                Vector3 punta = PuntoArte(new Vector2(20, 24));
                Vector3 mango = PuntoArte(new Vector2(246, 214));
                Exigir(punta.y > mango.y + .5f && Mathf.Abs(punta.x - mango.x) < .2f, "Hoja levantada en reposo hacia " + (direccion.x > 0 ? "derecha" : "izquierda"));
                Exigir(!jugador.GetComponent<MacheteAnimacion>().AlcanceVisible, "Circulo amarillo oculto");
                Capturar(direccion.x > 0 ? "reposo-derecha" : "reposo-izquierda");
                Avanzar(fase == 2 ? 3 : 7); return;
            }
            if ((fase == 3 || fase == 7) && ahora - etapa > .2)
            {
                jugador.GetComponent<PlayerCombat>().SendMessage("Attack");
                inicioTajo = Time.time;
                Avanzar(fase == 3 ? 4 : 8); return;
            }
            if ((fase == 4 || fase == 8) && Time.time - inicioTajo > .45f)
            {
                ComprobarAgarre();
                Vector3 filo = PuntoArte(new Vector2(20, 24)) - PuntoArte(new Vector2(246, 214));
                Exigir(Vector2.Dot(filo, direccion) > .4f, "El tajo dirige la hoja hacia el objetivo");
                Capturar(direccion.x > 0 ? "tajo-derecha" : "tajo-izquierda");
                Avanzar(fase == 4 ? 5 : 9); return;
            }
            if (fase == 5 && Time.time - inicioTajo > 1.1f)
            {
                direccion = Vector2.left; Avanzar(6); return;
            }
            if (fase == 9 && Time.time - inicioTajo > 1.1f)
            {
                jugador.GetComponent<WeaponManager>().EquipWeapon(false);
                Avanzar(10); return;
            }
            if (fase == 10 && ahora - etapa > .2)
            {
                Exigir(arma.cadera == cadera, "Altura de cadera conservada");
                Exigir(Vector3.Distance(arma.visual.transform.localPosition, new Vector3(.25f, 0)) < .001f
                    && Quaternion.Angle(arma.visual.transform.localRotation, Quaternion.identity) < .001f
                    && Vector3.Distance(arma.salidaDisparo.localPosition, new Vector3(.7f, -.04f)) < .001f,
                    "Subfusil y salida de bala conservan su configuracion");
                QuitarDispositivos(); jugador = null; arma = null;
                if (personaje == 0)
                {
                    personaje = 1; GameManager.Instance.IniciarNuevaRun(personaje);
                    Avanzar(1); SceneManager.LoadScene(escena);
                }
                else Terminar();
            }
        }
        catch (Exception e) { errores.Add(e.Message); Terminar(); }
    }
    private static void QuitarDispositivos()
    {
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        mouse = null; teclado = null;
    }
    private static void Terminar()
    {
        fase = -1;
        File.WriteAllLines(Carpeta + "playmode.txt", new[] { errores.Count == 0 ? "RESULTADO: PASS" : "RESULTADO: FAIL" }.Concat(resultados).Concat(errores.Select(e => "FAIL " + e)));
        Debug.Log("[MacheteReposo] " + (errores.Count == 0 ? "PASS" : "FAIL") + " " + string.Join(" | ", errores));
        EditorApplication.ExitPlaymode();
    }
}
