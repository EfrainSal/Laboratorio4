using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Laboratorio4.Animacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class ProbarIntegracionGameplay
{
    private const string Clave = "Laboratorio4.Gameplay.Pruebas";
    private const string Informe = "Reports/Gameplay/playmode.txt";
    private static int fase, cantidadOleada, activaciones;
    private static double inicio, etapa;
    private static GameObject jugador, hud;
    private static PlayerHealth salud;
    private static EnemySpawner spawner;
    private static string destino;
    private static bool terminando;
    private static readonly List<string> resultados = new List<string>();
    private static readonly List<string> errores = new List<string>();

    static ProbarIntegracionGameplay()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (mensaje, traza, tipo) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (tipo == LogType.Error || tipo == LogType.Exception)) errores.Add(mensaje);
        };
    }

    [MenuItem("Laboratorio 4/Gameplay/Probar HUD arma mira spawn y Game Over")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Reports/Gameplay");
        File.WriteAllText(Informe, "Pruebas desde MainMenu. Guardado aislado dentro de Reports/Gameplay.\n");
        SessionState.SetInt(Clave + ".personaje", 0);
        SessionState.SetBool(Clave + ".ok", true);
        SessionState.SetBool(Clave, true);
        Empezar();
    }

    private static void Empezar()
    {
        EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        EditorApplication.EnterPlaymode();
    }

    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            SaveSystem.RutaDePruebas = Path.GetFullPath("Reports/Gameplay/run-pruebas.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath("Reports/Gameplay/ranking-pruebas.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            inicio = EditorApplication.timeSinceStartup;
            fase = 0; terminando = false; resultados.Clear(); errores.Clear();
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SaveSystem.RutaDePruebas = null;
            RankingManager.RutaDePruebas = null;
            RankingManager.SuspenderSincronizacionAutomatica = false;
            if (SessionState.GetInt(Clave + ".personaje", 0) == 0)
            {
                SessionState.SetInt(Clave + ".personaje", 1);
                EditorApplication.delayCall += Empezar;
            }
            else
            {
                SessionState.SetBool(Clave, false);
                EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
                Debug.Log("[Gameplay] Pruebas de Jason y Milena: " + (SessionState.GetBool(Clave + ".ok", false) ? "PASS" : "FAIL"));
            }
        }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
        resultados.Add("PASS " + mensaje);
    }

    private static T Leer<T>(object objeto, string campo) => (T)objeto.GetType().GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(objeto);

    private static void MoverMouse(Vector3 destinoMundo)
    {
        Exigir(Mouse.current != null, "Dispositivo de mouse disponible");
        InputSystem.QueueDeltaStateEvent(Mouse.current.position, (Vector2)Camera.main.WorldToScreenPoint(destinoMundo));
    }

    private static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false) || !EditorApplication.isPlaying || terminando || inicio == 0) return;
        double ahora = EditorApplication.timeSinceStartup;
        int id = SessionState.GetInt(Clave + ".personaje", 0);
        try
        {
            if (ahora - inicio > 80) throw new Exception("Tiempo limite de prueba");
            if (fase == 0 && ahora - inicio > 0.5)
            {
                Exigir(SceneManager.GetActiveScene().name == "MainMenu" && GameManager.Instance != null, "Arranque desde MainMenu");
                destino = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<MainMenuController>()).FindProperty("_gameSceneName").stringValue;
                GameManager.Instance.IniciarNuevaRun(id);
                fase = 1; etapa = ahora; SceneManager.LoadScene(destino);
                return;
            }
            if (fase == 1 && ahora - etapa > 0.8 && SceneManager.GetActiveScene().name == destino)
            {
                var jugadores = GameObject.FindGameObjectsWithTag("Player");
                Exigir(jugadores.Length == 1, "Un solo jugador activo y un solo spawneo de jugador");
                jugador = jugadores[0]; salud = jugador.GetComponent<PlayerHealth>();
                Exigir(jugador.GetComponent<AnimadorSprites8Direcciones>().biblioteca.personaje == (id == 0 ? "Jason" : "Milena"), "Personaje elegido conservado");
                var nivel = LevelManager.Instance;
                Exigir(nivel != null && salud.healthBarFill == nivel.healthBarFillUI && salud.healthText == nivel.healthTextUI, "LevelManager conecta el HUD de Debugscene al jugador existente");
                hud = salud.healthBarFill.GetComponentInParent<Canvas>().gameObject;
                Exigir(hud.activeInHierarchy && salud.healthBarFill.isActiveAndEnabled && salud.healthText.isActiveAndEnabled, "HUD de vida visible");
                salud.isInvulnerable = false;
                salud.TakeDamage(25f, DeathCause.Entorno);
                Exigir(Mathf.Abs(salud.healthBarFill.fillAmount - salud.currentHealth / salud.maxHealth) < 0.001f, "La barra de vida responde al dano real");
                Exigir(salud.healthText.text == Mathf.FloorToInt(salud.currentHealth) + " / " + Mathf.FloorToInt(salud.maxHealth), "Texto de vida actualizado");
                salud.isInvulnerable = true;
                Exigir(!jugador.GetComponent<AnimadorSprites8Direcciones>().Tiene("Disparo"), "Sin sprites de disparo del personaje");
                var arma = jugador.GetComponent<ArmaIndependiente>();
                Exigir(arma != null && arma.pivote.parent == jugador.transform && arma.visual.enabled, "Arma como objeto independiente del cuerpo");
                var mira = jugador.GetComponent<MiraPersonalizada>();
                Exigir(mira.EstaActiva && mira.Textura != null && mira.Textura.GetPixels32().Any(c => c.a != 0) && mira.Textura.GetPixels32().Where(c => c.a != 0).All(c => c.r == 255 && c.g == 255 && c.b == 255), "Mira blanca generada por codigo y activa");
                MoverMouse(arma.pivote.position + Vector3.right * 3f);
                fase = 2; etapa = ahora;
            }
            if (fase == 2 && ahora - etapa > 0.2)
            {
                var arma = jugador.GetComponent<ArmaIndependiente>();
                Exigir(Vector3.Dot(arma.pivote.right, Vector3.right) > 0.99f && !arma.visual.flipY, "Apuntado hacia la derecha sin volteo incorrecto");
                Exigir(Vector3.Distance(arma.pivote.localPosition, new Vector3(arma.cadera.x, arma.cadera.y, 0f)) < 0.001f, "Arma situada a la altura de la cadera");
                MoverMouse(arma.pivote.position + Vector3.left * 3f);
                fase = 3; etapa = ahora;
            }
            if (fase == 3 && ahora - etapa > 0.2)
            {
                var arma = jugador.GetComponent<ArmaIndependiente>();
                Exigir(Vector3.Dot(arma.pivote.right, Vector3.left) > 0.99f && arma.visual.flipY && !arma.visual.flipX, "Apuntado izquierdo conserva el arma derecha mediante flip vertical");
                jugador.GetComponent<WeaponManager>().EquipWeapon(true);
                // Objetivo fijo: comprobar el item separado de nuevas lecturas del mouse/camara.
                arma.Apuntar(arma.pivote.position + Vector3.left * 3f);
                Exigir(arma.visual.sprite == arma.machete && Mathf.Abs(Mathf.DeltaAngle(arma.visual.transform.localEulerAngles.z, 225f)) < 0.01f && jugador.GetComponent<PlayerCombat>().enabled, "Cambio a machete conserva orientacion y combate existentes");
                jugador.GetComponent<WeaponManager>().EquipWeapon(false);
                arma.Apuntar(arma.pivote.position + Vector3.left * 3f);
                Exigir(arma.visual.sprite == arma.subfusil && jugador.GetComponent<PlayerShooting>().enabled, "Cambio de vuelta al subfusil restaura el arma visible");
                var disparo = jugador.GetComponent<PlayerShooting>();
                int municion = disparo.currentAmmoInClip;
                jugador.SendMessage("Shoot");
                Exigir(disparo.currentAmmoInClip == municion - 1 && disparo.firePoint.parent == arma.pivote, "Disparo consume municion desde la boca del arma independiente");
                Exigir(jugador.GetComponent<AnimadorSprites8Direcciones>().AnimacionActual != "Disparo", "El disparo no cambia los sprites del cuerpo");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Reports/Gameplay/" + (id == 0 ? "Jason" : "Milena") + "-HUD.png"));
                UnityEngine.Object.FindFirstObjectByType<PauseMenuController>().Pause();
                fase = 4; etapa = ahora;
            }
            if (fase == 4 && ahora - etapa > 0.4)
            {
                Exigir(!jugador.GetComponent<MiraPersonalizada>().EstaActiva && Time.timeScale == 0f, "Pausa restaura el cursor para la interfaz");
                UnityEngine.Object.FindFirstObjectByType<PauseMenuController>().Resume();
                var todos = UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
                Exigir(todos.Length == 3 && todos.All(s => s.enemyPrefabs.Length == 2), "Tres spawners originales con ambas variantes de Sorbo");
                spawner = todos.OrderByDescending(s => Vector2.Distance(s.transform.position, jugador.transform.position)).First();
                foreach (var s in todos) s.enabled = s == spawner;
                foreach (var enemigo in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) enemigo.gameObject.SetActive(false);
                activaciones = Leer<int>(LevelManager.Instance, "spawnersActivados");
                jugador.transform.position = spawner.transform.position + Vector3.left * 1.5f;
                fase = 5; etapa = ahora;
            }
            if (fase == 5 && ahora - etapa > 0.4)
            {
                cantidadOleada = UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
                Exigir(cantidadOleada >= spawner.minEnemies && cantidadOleada <= spawner.maxEnemies, "EnemySpawner activa una oleada al entrar en su radio");
                Exigir(Leer<int>(LevelManager.Instance, "spawnersActivados") > activaciones, "Spawn registra la activacion en LevelManager");
                spawner.cooldownTiempo = 0.7f;
                typeof(EnemySpawner).GetField("ultimoSpawnTiempo", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(spawner, Time.time);
                fase = 6; etapa = ahora;
            }
            if (fase == 6 && ahora - etapa > 0.2)
            {
                Exigir(UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length == cantidadOleada, "Cooldown impide oleadas inmediatas");
                fase = 7;
            }
            if (fase == 7 && ahora - etapa > 0.95)
            {
                Exigir(UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length > cantidadOleada, "El spawner original vuelve a generar al vencer su cooldown");
                File.WriteAllText(SaveSystem.RutaDePruebas, "guardado aislado");
                GameManager.Instance.puntuacionTotal = 0;
                salud.isInvulnerable = false;
                salud.TakeDamage(salud.maxHealth * 10f, DeathCause.Mutante);
                Exigir(salud.IsDead && DeathScreenController.IsShowing, "La muerte real dispara Game Over mediante GameManager");
                Exigir(UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None).All(s => !s.enabled), "La muerte detiene todos los spawners");
                Exigir(!File.Exists(SaveSystem.RutaDePruebas), "Muerte consume solo el guardado aislado de prueba");
                fase = 8; etapa = ahora;
            }
            if (fase == 8 && ahora - etapa > 2f)
            {
                var pantalla = UnityEngine.Object.FindFirstObjectByType<DeathScreenController>();
                var overlay = pantalla.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("DeathOverlay");
                Exigir(!overlay.ClassListContains("hidden") && overlay.resolvedStyle.display != DisplayStyle.None && Time.timeScale == 0f, "Pantalla Game Over visible y juego congelado");
                Exigir(!hud.activeInHierarchy && !jugador.GetComponent<MiraPersonalizada>().EstaActiva && !jugador.GetComponent<ArmaIndependiente>().visual.enabled, "Game Over oculta HUD, arma y mira del gameplay");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Reports/Gameplay/" + (id == 0 ? "Jason" : "Milena") + "-GameOver.png"));
                fase = 9;
            }
            if (fase == 9 && ahora - etapa > 5f)
            {
                var pantalla = UnityEngine.Object.FindFirstObjectByType<DeathScreenController>();
                var raiz = pantalla.GetComponent<UIDocument>().rootVisualElement;
                Exigir(raiz.Q<Button>("RetryButton").enabledSelf && raiz.Q<Button>("MenuButton").enabledSelf, "Botones Reintentar y Menu habilitados");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Reports/Gameplay/" + (id == 0 ? "Jason" : "Milena") + "-GameOver.png"));
                fase = 12;
                return;
            }
            if (fase == 12 && ahora - etapa > 5.3f)
            {
                fase = 10; etapa = ahora;
                UnityEngine.Object.FindFirstObjectByType<DeathScreenController>().SendMessage("OnRetry");
                return;
            }
            if (fase == 10 && ahora - etapa > 0.8)
            {
                var jugadores = GameObject.FindGameObjectsWithTag("Player");
                Exigir(jugadores.Length == 1 && jugadores[0].GetComponent<AnimadorSprites8Direcciones>().biblioteca.personaje == (id == 0 ? "Jason" : "Milena"), "Reintentar conserva el personaje sin duplicarlo");
                jugador = jugadores[0]; salud = jugador.GetComponent<PlayerHealth>();
                Exigir(salud.currentHealth == salud.maxHealth && salud.healthBarFill.isActiveAndEnabled && Time.timeScale == 1f && !PauseMenuController.PauseBlocked, "Reintentar restaura vida, HUD y control del juego");
                Exigir(jugador.GetComponent<MiraPersonalizada>().EstaActiva, "Reintentar restaura la mira blanca");
                salud.isInvulnerable = false;
                salud.TakeDamage(salud.maxHealth * 10f, DeathCause.Entorno);
                fase = 11; etapa = ahora;
                UnityEngine.Object.FindFirstObjectByType<DeathScreenController>().SendMessage("OnMenu");
                return;
            }
            if (fase == 11 && ahora - etapa > 0.8)
            {
                Exigir(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1f && GameObject.FindGameObjectsWithTag("Player").Length == 0, "Game Over vuelve al MainMenu sin dejar el juego pausado");
                Exigir(errores.Count == 0, "Flujo completo sin errores ni excepciones");
                Terminar(true);
            }
        }
        catch (Exception error) { errores.Add(error.Message); Terminar(false); }
    }

    private static void Terminar(bool ok)
    {
        terminando = true;
        if (!ok) SessionState.SetBool(Clave + ".ok", false);
        string personaje = SessionState.GetInt(Clave + ".personaje", 0) == 0 ? "JASON" : "MILENA";
        File.AppendAllLines(Informe, new[] { "\n" + personaje + ": " + (ok ? "PASS" : "FAIL") }.Concat(resultados).Concat(errores.Select(e => "FAIL " + e)));
        Debug.Log("[Gameplay] " + personaje + ": " + (ok ? "PASS" : "FAIL") + " " + string.Join(" | ", errores));
        EditorApplication.ExitPlaymode();
    }
}
