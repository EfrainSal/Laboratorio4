using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarPersistenciaLootDerrota
{
    const string Clave = "L4.PruebaPersistenciaLootDerrota";
    const string Carpeta = "Reports/PersistenciaLootDerrota/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina;
    static double siguiente, inicio;
    static bool segundoPlano;
    static int muertes;
    static PlayerHealth Salud => LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null
        ? LevelManager.Instance.JugadorActual.GetComponent<PlayerHealth>() : Object.FindFirstObjectByType<PlayerHealth>();
    static ProbarPersistenciaLootDerrota()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Partida/Probar vida loot y derrota %#&F3")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Carpeta); Selection.activeObject = null; StageUtility.GoToMainStage();
        SessionState.SetString(Clave + ".escena", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    static void Estado(PlayModeStateChange e)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (e == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear(); muertes = 0;
            segundoPlano = Application.runInBackground; Application.runInBackground = true;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
            { SessionState.SetBool(Clave + key + ".existe", PlayerPrefs.HasKey(key)); SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key)); }
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            RankingManager.SaveRanking(new RankingData { version = 2 });
            GameManager.OnRunTerminada += RegistrarMuerte;
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; Time.timeScale = 1; Application.runInBackground = segundoPlano;
            GameManager.OnRunTerminada -= RegistrarMuerte;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
                if (SessionState.GetBool(Clave + key + ".existe", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0));
                else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[PruebaPartida] " + resultados.Count + " comprobaciones, " + errores.Count + " errores.");
            EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    static void RegistrarMuerte(RunSummary resumen) { muertes++; }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 180) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .03f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static void Exigir(bool cumple, string detalle)
    { if (!cumple) throw new Exception(detalle); resultados.Add("PASS " + detalle); }
    static IEnumerator EsperarTransicion()
    {
        float limite = Time.unscaledTime + 15;
        while (TransicionEntrePisos.EnCurso)
        { if (Time.unscaledTime > limite) throw new Exception("La transición no terminó"); yield return .05f; }
        yield return .3f;
    }
    static IEnumerator CerrarControles()
    {
        yield return .5f;
        var controles = Object.FindFirstObjectByType<PantallaControlesInicio>();
        if (controles != null && PantallaControlesInicio.IsShowing) { controles.Cerrar(); yield return .5f; }
    }
    static void DetenerCombate()
    {
        Time.timeScale = 0;
        if (Salud != null) Salud.isInvulnerable = true;
        foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
    }
    static IEnumerator Recorrido()
    {
        yield return .5f;
        var gm = GameManager.Instance;
        Exigir(gm != null, "Singleton del menú presente y persistente");
        for (int personaje = 0; personaje < 2; personaje++)
        {
            gm.IniciarNuevaRun(personaje); gm.nivelMejoraVida = 2;
            SceneManager.LoadScene("Recepcion"); yield return .6f;
            var cerrar = CerrarControles(); while (cerrar.MoveNext()) yield return cerrar.Current;
            DetenerCombate();
            // El panel de mejoras está desactivado en el HUD actual: preparar una run
            // que ya compró dos mejoras, sin cambiar la configuración del prefab.
            float maximo = 140; Salud.maxHealth = maximo; Salud.currentHealth = maximo;
            string[] destinos = { "Escenario1_Entrada", "Piso3_V10", "E3_Laboratorio", "Escenario4_Jefe", "Nivel1" };
            float[] valores = { 100f, 37.5f, 100f, maximo - .75f, 63.25f };
            for (int i = 0; i < destinos.Length; i++)
            {
                Salud.currentHealth = valores[i];
                Exigir(TransicionEntrePisos.CargarEscena(destinos[i]), "Cambio aceptado a " + destinos[i]);
                var espera = EsperarTransicion(); while (espera.MoveNext()) yield return espera.Current;
                DetenerCombate();
                var panel = Object.FindObjectsByType<UpgradePanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
                if (panel != null)
                {
                    // Ejecutar la misma reaplicación que usa el panel cuando está activo.
                    var mejoras = (IEnumerator)typeof(UpgradePanelController).GetMethod("ApplyStoredUpgradesWhenReady",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(panel, null);
                    while (mejoras.MoveNext()) yield return .03f;
                }
                Exigir(GameManager.Instance == gm && Mathf.Approximately(Salud.currentHealth, valores[i]), destinos[i] + ": vida exacta " + valores[i] + " del personaje " + personaje);
                Exigir(Mathf.Approximately(Salud.maxHealth, maximo), destinos[i] + ": máximo conservado sin duplicar mejoras");
                if (destinos[i] == "Escenario4_Jefe")
                {
                    var muestra = new GameObject("Prueba salud de prototipo");
                    var vidaE4 = muestra.AddComponent<Laboratorio4.Escenario4.E4Vida>();
                    muestra.AddComponent<VidaJugadorPrototipo>();
                    var saludMuestra = muestra.GetComponent<PlayerHealth>(); saludMuestra.RestaurarVida(valores[i], maximo);
                    muestra.GetComponent<VidaJugadorPrototipo>().Sincronizar(); vidaE4.Recibir(1);
                    Exigir(Mathf.Approximately(saludMuestra.currentHealth, valores[i] - 1), "Daño del prototipo descuenta vida real sin perder decimales");
                    Object.Destroy(muestra);
                }
            }
        }
        var loot = ProbarLoot(); while (loot.MoveNext()) yield return loot.Current;
        var guardar = ProbarGuardado(); while (guardar.MoveNext()) yield return guardar.Current;
        for (int personaje = 0; personaje < 2; personaje++)
        {
            gm.IniciarNuevaRun(personaje); SceneManager.LoadScene("Escenario1_Entrada"); yield return .6f;
            var derrota = ProbarDerrota(personaje); while (derrota.MoveNext()) yield return derrota.Current;
        }
        gm.IniciarNuevaRun(0); SceneManager.LoadScene("Escenario4_Jefe"); yield return .6f;
        var derrotaPrototipo = ProbarDerrota(0, true); while (derrotaPrototipo.MoveNext()) yield return derrotaPrototipo.Current;
        Exigir(errores.Count == 0, "Recorrido completo sin errores ni excepciones");
    }

    static IEnumerator ProbarLoot()
    {
        foreach (string nombre in new[] { "Enemy_Melee", "Enemy_Ranged" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + nombre + ".prefab");
            Exigir(prefab.GetComponent<EnemyHealth>().vendaDropPrefab.GetComponent<ItemPickup>().tipoDeObjeto == ItemPickup.PickupType.Venda, nombre + ": prefab de vendas conectado");
            foreach (int modo in new[] { 0, 1, 2, 3 })
            {
                int vendas = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).Count(p => p.tipoDeObjeto == ItemPickup.PickupType.Venda);
                int municion = Object.FindObjectsByType<AmmoDrop>(FindObjectsSortMode.None).Length;
                var enemigo = Object.Instantiate(prefab, new Vector3(100 + modo, 100, 0), Quaternion.identity).GetComponent<EnemyHealth>();
                enemigo.dropChance = (modo & 1) != 0 ? 100 : 0; enemigo.vendaDropChance = (modo & 2) != 0 ? 100 : 0;
                enemigo.experienciaAlMorir = 0; yield return .15f;
                enemigo.TakeDamage(10000); enemigo.TakeDamage(10000); yield return .1f;
                Exigir(Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).Count(p => p.tipoDeObjeto == ItemPickup.PickupType.Venda) - vendas == ((modo & 2) != 0 ? 1 : 0), nombre + ": tirada de vendas independiente y sin duplicados, modo " + modo);
                Exigir(Object.FindObjectsByType<AmmoDrop>(FindObjectsSortMode.None).Length - municion == ((modo & 1) != 0 ? 1 : 0), nombre + ": tirada original de munición, modo " + modo);
            }
        }
        var venda = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).First(p => p.tipoDeObjeto == ItemPickup.PickupType.Venda);
        var inv = Salud.GetComponent<PlayerInventory>(); int antes = inv.vendasCount;
        venda.transform.position = Salud.GetComponent<Collider2D>().bounds.center;
        Time.timeScale = 1; Physics2D.SyncTransforms(); yield return .15f; Time.timeScale = 0;
        Exigir(inv.vendasCount == antes + 1 && venda == null, "Venda dropeada se recoge físicamente una sola vez");
        Salud.currentHealth = 50;
        typeof(PlayerInventory).GetMethod("ConsumirVenda", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(inv, null);
        Exigir(Salud.currentHealth > 50 && inv.vendasCount == antes, "La venda obtenida cura y se consume");
    }
    static IEnumerator ProbarGuardado()
    {
        var gm = GameManager.Instance; Salud.currentHealth = 100;
        float maximo = Salud.maxHealth;
        Exigir(gm.GuardarRun() && SaveSystem.Cargar().vidaMaxima == maximo, "Guardado conserva vida actual y máxima");
        Exigir(gm.CargarRunGuardada(), "Continuar acepta el guardado");
        SceneManager.LoadScene("Escenario1_Entrada"); yield return .6f; DetenerCombate();
        Exigir(Salud.currentHealth == 100 && Salud.maxHealth == maximo, "Continuar no cura ni duplica mejoras de vida");
    }
    static IEnumerator ProbarDerrota(int personaje, bool prototipo = false)
    {
        var gm = GameManager.Instance; DetenerCombate();
        var escenaOrigen = SceneManager.GetActiveScene().handle;
        int antes = muertes;
        gm.puntuacionTotal = 123 + antes; gm.pisoActual = 4;
        SaveSystem.Guardar(new SaveData { vidaActual = 42 });
        var salud = Salud; salud.isInvulnerable = false;
        if (prototipo && salud.GetComponent<Laboratorio4.Escenario4.E4Vida>() != null)
        { var vida = salud.GetComponent<Laboratorio4.Escenario4.E4Vida>(); vida.invulnerable = false; vida.Recibir(10000); }
        else { salud.TakeDamage(10000); salud.TakeDamage(10000); }
        Exigir(salud.IsDead && muertes == antes + 1 && DeathScreenController.GameplayBlocked, "Derrota se registra una vez y bloquea controles");
        var pantalla = Object.FindFirstObjectByType<DeathScreenController>();
        var raiz = pantalla.GetComponent<UIDocument>().rootVisualElement;
        var reintentar = raiz.Q<Button>("RetryButton");
        float limite = Time.unscaledTime + 12;
        while (!reintentar.enabledSelf)
        { if (Time.unscaledTime > limite) throw new Exception("Game Over no habilitó Reintentar"); yield return .05f; }
        Exigir(DeathScreenController.IsShowing && Time.timeScale == 0 && !raiz.Q<VisualElement>("DeathOverlay").ClassListContains("hidden"), "Game Over visible y acción pausada antes de reintentar");
        Exigir(SceneManager.GetActiveScene().handle == escenaOrigen && !TransicionEntrePisos.EnCurso && !gm.RunActiva && gm.pisoActual == 4 && gm.puntuacionTotal == 123 + antes,
            "La derrota conserva escena y resumen sin iniciar otra run");
        gm.TerminarRunPorMuerte();
        yield return 2f;
        Exigir(muertes == antes + 1 && SceneManager.GetActiveScene().handle == escenaOrigen && !TransicionEntrePisos.EnCurso,
            "Esperar en Game Over no cambia de escena ni duplica la derrota");
        Directory.CreateDirectory("Reports/FlujoGameOver");
        ScreenCapture.CaptureScreenshot("Reports/FlujoGameOver/perdiste-" + personaje + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
        yield return .1f;
        using (var pulsacion = NavigationSubmitEvent.GetPooled()) reintentar.SendEvent(pulsacion);
        yield return .05f;
        Exigir(TransicionEntrePisos.EnCurso && !reintentar.enabledSelf && !raiz.Q<Button>("MenuButton").enabledSelf, "Sólo Reintentar inicia el fundido y bloquea dobles pulsaciones");
        // Una segunda notificación durante el fundido no vuelve a iniciar la run.
        pantalla.SendMessage("OnRetry");
        var espera = EsperarTransicion(); while (espera.MoveNext()) yield return espera.Current;
        Exigir(SceneManager.GetActiveScene().name == "Recepcion", "Reintentar lleva a Recepcion" + (prototipo ? " desde E4" : " con personaje " + personaje));
        Exigir(gm.personajeSeleccionado == personaje && gm.pisoActual == 1 && gm.RunActiva, "Nueva partida conserva personaje y reinicia piso");
        Exigir(Salud.currentHealth == 100 && Salud.maxHealth == 100 && !Salud.IsDead && !gm.usarDatosDeTransicion, "No se arrastran vida cero ni mejoras de la run perdida");
        Exigir(!SaveSystem.HayGuardado(), "Guardado de la run perdida eliminado");
        var ranking = RankingManager.LoadRanking();
        Exigir(ranking.topScores.Count(p => p.score == 123 + antes && !p.victory) == 1, "Ranking conserva la puntuación de derrota una sola vez");
        var cerrar = CerrarControles(); while (cerrar.MoveNext()) yield return cerrar.Current;
        Exigir(Time.timeScale == 1 && !DeathScreenController.GameplayBlocked && !PauseMenuController.PauseBlocked, "Recepcion recupera tiempo, input y pausa");
        ScreenCapture.CaptureScreenshot(Carpeta + "regreso-" + personaje + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png"); yield return .2f;
    }
}
