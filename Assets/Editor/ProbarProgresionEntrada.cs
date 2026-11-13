using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarProgresionEntrada
{
    private const string Clave = "L4.PruebaProgresion";
    private const string Carpeta = "Reports/ProgresionEntrada/";
    private static IEnumerator rutina;
    private static double siguiente, inicio;
    private static Keyboard teclado;
    private static Mouse mouse;
    private static GameObject jugador;
    private static bool segundoPlanoAnterior;
    private static readonly List<string> resultados = new List<string>();
    private static readonly List<string> errores = new List<string>();

    static ProbarProgresionEntrada()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Progresion/Probar recorrido completo %#&F9")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Carpeta);
        Selection.activeObject = null; StageUtility.GoToMainStage();
        SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.EnterPlaymode();
    }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            segundoPlanoAnterior = Application.runInBackground;
            Application.runInBackground = true;
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            RankingManager.SaveRanking(new RankingData { version = 2 });
            SessionState.SetInt(Clave + ".mejor", PlayerPrefs.GetInt("MejorPuntuacion", 0));
            resultados.Clear(); errores.Clear(); inicio = EditorApplication.timeSinceStartup;
            siguiente = inicio; rutina = Recorrido();
        }
        if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            Application.runInBackground = segundoPlanoAnterior;
            QuitarDispositivos();
            PlayerPrefs.SetInt("MejorPuntuacion", SessionState.GetInt(Clave + ".mejor", 0)); PlayerPrefs.Save();
            rutina = null;
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(e => "ERROR " + e)));
            Debug.Log("[PruebaProgresion] " + resultados.Count(s => s.StartsWith("PASS")) + " comprobaciones, " + errores.Count + " errores.");
        }
    }
    private static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 180) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float tiempo ? tiempo : .02f);
        }
        catch (Exception e)
        {
            errores.Add(e.ToString()); rutina = null; EditorApplication.ExitPlaymode();
        }
    }
    private static void Exigir(bool cumple, string detalle)
    {
        if (!cumple) throw new Exception(detalle);
        resultados.Add("PASS " + detalle);
    }
    private static void Emparejar()
    {
        QuitarDispositivos();
        jugador = GameObject.FindGameObjectWithTag("Player");
        teclado = InputSystem.AddDevice<Keyboard>("PruebaEntradaTeclado");
        mouse = InputSystem.AddDevice<Mouse>("PruebaEntradaMouse");
        var input = jugador.GetComponent<PlayerInput>();
        input.neverAutoSwitchControlSchemes = true;
        input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
        jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
        foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
        foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Congelar(e);
    }
    private static void QuitarDispositivos()
    {
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        teclado = null; mouse = null;
    }
    private static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    private static void Apuntar(Vector2 direccion, bool pulsado = false)
    {
        var pantalla = Camera.main.WorldToScreenPoint(jugador.transform.position + (Vector3)direccion * 2);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pantalla, buttons = (ushort)(pulsado ? 1 : 0) });
    }
    private static void Congelar(EnemyHealth e)
    {
        foreach (var b in e.GetComponents<MonoBehaviour>()) if (b is EnemyAI || b is MeleeEnemyAI || b is RangedEnemyAI) b.enabled = false;
        var rb = e.GetComponent<Rigidbody2D>(); if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }
    private static ItemPickup[] Tarjetas() => Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).Where(p => p.tipoDeObjeto == ItemPickup.PickupType.Llave).ToArray();
    private static void Posicionar(Vector3 p)
    {
        jugador.transform.position = p;
        var rb = jugador.GetComponent<Rigidbody2D>(); rb.position = p; rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    private static IEnumerator Recorrido()
    {
        yield return .7f;
        string destino = "Escenario1_Entrada";
        Exigir(Application.CanStreamedLevelBeLoaded(destino), "Entrada incluida en Build Settings (el prólogo se verifica en ProbarInicioRecepcion)");
        GameManager.Instance.IniciarNuevaRun(0); SceneManager.LoadScene(destino);
        yield return 1f;
        Emparejar();
        Exigir(Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).Length == 1, "Sólo hay un jugador real activo");
        Exigir(Object.FindObjectsByType<Laboratorio4.ControladorJugador>(FindObjectsSortMode.None).Length == 0, "Controladores del prototipo inactivos");
        Exigir(Object.FindObjectsByType<BossAI>(FindObjectsSortMode.None).Length == 0, "Dr. Yiba ausente de Entrada");
        Exigir(LevelManager.Instance.healthBarFillUI != null && Object.FindFirstObjectByType<DeathScreenController>() != null, "HUD y pantalla de muerte conectados");
        var spawners = Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        Exigir(spawners.Length == 3, "Tres spawners en Entrada");
        foreach (int i in new[] { 1, 2, 3 })
            Exigir(spawners.Any(s => s.name == "Spawner_lvl" + i), "Usado prefab original Spawner_lvl" + i);
        foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
        yield return .1f;
        var posicion = jugador.transform.position;
        Tecla(Key.D); yield return .25f; Tecla(); yield return .1f;
        Exigir(jugador.transform.position.x > posicion.x + .1f, "Movimiento D responde en Entrada");
        var salud = jugador.GetComponent<PlayerHealth>();
        var inventario = jugador.GetComponent<PlayerInventory>();
        salud.currentHealth = 25; inventario.vendasCount = inventario.botiquinesCount = inventario.estimulantesCount = 2;
        Tecla(Key.Digit1); yield return .1f; Tecla(); yield return .1f;
        Exigir(Mathf.Approximately(salud.currentHealth, 35) && inventario.vendasCount == 1 && inventario.botiquinesCount == 2, "1 consume sólo venda y cura 10");
        Tecla(Key.Digit2); yield return .1f; Tecla(); yield return .1f;
        Exigir(Mathf.Approximately(salud.currentHealth, salud.maxHealth) && inventario.estimulantesCount == 1 && inventario.botiquinesCount == 2, "2 consume sólo adrenalina y restaura toda la vida");
        salud.currentHealth = 65;
        Tecla(Key.Digit3); yield return .1f; Tecla(); yield return .1f;
        Exigir(Mathf.Approximately(salud.currentHealth, 95) && inventario.botiquinesCount == 1, "3 consume botiquín y cura 30");
        var arma = jugador.GetComponent<ArmaIndependiente>();
        var armas = jugador.GetComponent<WeaponManager>();
        var disparo = jugador.GetComponent<PlayerShooting>();
        armas.EquipWeapon(true); Apuntar(Vector2.right); yield return .15f;
        Exigir(arma.invertirFiloMachete && arma.visual.flipY, "Filo reflejado conservando mango y punta");
        Exigir(Mathf.Abs(arma.visual.transform.localScale.x * arma.machete.bounds.size.x - arma.anchoArma * .5f) < .001f, "Machete mide exactamente la mitad");
        Exigir(!jugador.GetComponent<MacheteAnimacion>().AlcanceVisible, "Círculo amarillo oculto");
        Vector2 agarre = (Vector2.Scale(arma.agarreMachete, arma.machete.rect.size) - arma.machete.pivot) / arma.machete.pixelsPerUnit;
        if (arma.visual.flipY) agarre.y *= -1;
        var mano = arma.visual.transform.TransformPoint(agarre);
        Exigir(Vector3.Distance(mano, arma.pivote.TransformPoint(arma.posicionManoMachete)) < .001f, "Mango anclado a la mano al reflejar y escalar");
        // No sobrescribir una captura que pueda estar abierta en un visor externo.
        ScreenCapture.CaptureScreenshot(Carpeta + "machete-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png"); yield return .2f;
        Apuntar(Vector2.left); yield return .15f;
        Exigir(!arma.visual.flipY, "Filo y agarre se reflejan hacia la izquierda");
        armas.EquipWeapon(false); disparo.RestaurarMunicion(30, 60);
        foreach (int d in Enumerable.Range(0, 8))
        {
            float angulo = (-90 + d * 45) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            Apuntar(dir); yield return .15f;
            Exigir(arma.visual.sprite == arma.spritesApuntar[d], "Sprite de reposo dirección " + d);
            yield return disparo.fireRate;
            Apuntar(dir, true); yield return .04f;
            Exigir(arma.DisparoVisualActivo && arma.visual.sprite == arma.spritesDisparo[d], "Fogonazo dirección " + d);
            Apuntar(dir); yield return .15f;
        }
        int balas = disparo.currentAmmoInClip;
        yield return disparo.fireRate * 2;
        Exigir(disparo.currentAmmoInClip == balas, "Soltar clic detiene el disparo");
        Tecla(Key.Q); yield return .1f; Tecla(); yield return .1f;
        Exigir(armas.isUsingMelee, "Q cambia al machete");
        Tecla(Key.Q); yield return .1f; Tecla(); yield return .1f;
        disparo.RestaurarMunicion(1, 40); Tecla(Key.R); yield return .1f;
        Exigir(disparo.IsReloading, "R inicia la recarga temporizada");
        Tecla(); yield return disparo.reloadDuration + .2f;
        Exigir(disparo.currentAmmoInClip > 1 && disparo.totalAmmo < 40, "R recarga el subfusil");
        // La puerta no permite avanzar sin tarjeta, usando el input real del jugador.
        var puerta = Object.FindFirstObjectByType<PuertaNivel>();
        Posicionar(puerta.transform.position); yield return .2f;
        Tecla(Key.F); yield return .1f; Tecla(); yield return .1f;
        Exigir(SceneManager.GetActiveScene().name == destino && inventario.llavesCount == 0, "Puerta F rechaza jugador sin tarjeta");
        Exigir(puerta.AvisoVisible && !puerta.textoMensajeUI.gameObject.activeSelf, "Puerta informa que falta la tarjeta con el aviso de recepción");
        Posicionar(new Vector3(13, -11, 0)); yield return .1f;
        foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
        yield return .1f;
        // Invocar las oleadas existentes para comprobar su configuración y contabilización.
        foreach (var s in spawners)
        {
            int antes = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
            typeof(EnemySpawner).GetMethod("SpawnEnemies", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(s, null);
            yield return .1f;
            var nuevos = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Where(e => e.OrigenSpawner == s).ToArray();
            Exigir(nuevos.Length >= s.minEnemies && nuevos.Length <= s.maxEnemies, "Oleada original " + s.name + " respeta cantidades");
            foreach (var e in nuevos) Congelar(e);
        }
        var enemigos = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        while (enemigos.Length < 11)
        {
            typeof(EnemySpawner).GetMethod("SpawnEnemies", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(spawners[0], null);
            yield return .1f;
            enemigos = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (var e in enemigos) Congelar(e);
        }
        var ajeno = Object.Instantiate(spawners[0].enemyPrefabs[0], new Vector3(14,-11,0), Quaternion.identity).GetComponent<EnemyHealth>();
        yield return .1f; Congelar(ajeno); ajeno.TakeDamage(10000);
        Exigir(LevelManager.Instance.EnemigosDerrotados == 0, "Enemigo ajeno a spawners no cuenta para tarjeta");
        for (int n = 0; n < 9; n++) { enemigos[n].TakeDamage(10000); enemigos[n].TakeDamage(10000); }
        Exigir(LevelManager.Instance.EnemigosDerrotados == 9 && Tarjetas().Length == 0, "9 bajas únicas no generan tarjeta; muerte duplicada no suma");
        // Guardar y continuar desde MainMenu conserva escena y cuota parcial.
        Exigir(GameManager.Instance.GuardarRun(), "Guardado de prueba cifrado correcto");
        var datos = SaveSystem.Cargar();
        Exigir(datos.escenaActual == destino && datos.enemigosDerrotadosPiso == 9, "Guardado contiene piso y cuota parcial");
        QuitarDispositivos(); SceneManager.LoadScene("MainMenu"); yield return .7f;
        typeof(MainMenuController).GetMethod("OnContinueClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Object.FindFirstObjectByType<MainMenuController>(), null);
        yield return 1f; Emparejar();
        Exigir(SceneManager.GetActiveScene().name == destino && LevelManager.Instance.EnemigosDerrotados == 9, "Continuar vuelve a Entrada y restaura las 9 bajas");
        spawners = Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        typeof(EnemySpawner).GetMethod("SpawnEnemies", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(spawners[0], null);
        yield return .15f;
        var final = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).First(e => e.OrigenSpawner != null);
        Congelar(final); Vector3 lugar = final.transform.position; final.TakeDamage(10000);
        Exigir(LevelManager.Instance.EnemigosDerrotados == 10 && Tarjetas().Length == 1, "El Sorbo número 10 suelta una tarjeta");
        Exigir(Vector3.Distance(Tarjetas()[0].transform.position, lugar) < .01f, "Tarjeta aparece donde cayó el Sorbo");
        var extra = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).First(e => e.CurrentHealth > 0);
        extra.TakeDamage(10000);
        Exigir(Tarjetas().Length == 1, "Bajas posteriores no duplican tarjeta");
        Posicionar(Tarjetas()[0].transform.position); yield return .2f;
        inventario = jugador.GetComponent<PlayerInventory>(); salud = jugador.GetComponent<PlayerHealth>(); disparo = jugador.GetComponent<PlayerShooting>();
        Exigir(inventario.llavesCount == 1 && Tarjetas().Length == 0, "Pickup físico recoge la tarjeta una sola vez");
        salud.currentHealth = 47; inventario.vendasCount = 4; inventario.botiquinesCount = 3; inventario.estimulantesCount = 2;
        disparo.RestaurarMunicion(7, 41);
        jugador.GetComponent<PlayerExperience>().RestaurarProgreso(3, 17);
        puerta = Object.FindFirstObjectByType<PuertaNivel>(); Posicionar(puerta.transform.position); yield return .2f;
        Tecla(Key.F); yield return .1f;
        float limiteCarga = Time.unscaledTime + 10;
        while (TransicionEntrePisos.EnCurso && Time.unscaledTime < limiteCarga) yield return .05f;
        yield return .2f;
        Exigir(SceneManager.GetActiveScene().name == "Piso3_V10" && GameManager.Instance.pisoActual == 2, "F con tarjeta avanza exactamente un piso a Piso3");
        Emparejar(); Tecla(); yield return .1f;
        inventario = jugador.GetComponent<PlayerInventory>(); disparo = jugador.GetComponent<PlayerShooting>();
        Exigir(inventario.llavesCount == 0 && inventario.vendasCount == 4 && inventario.botiquinesCount == 3 && inventario.estimulantesCount == 2, "Puerta consume tarjeta y conserva consumibles");
        Exigir(Mathf.Approximately(jugador.GetComponent<PlayerHealth>().currentHealth, 47) && disparo.currentAmmoInClip == 7 && disparo.totalAmmo == 41, "Transición conserva vida y munición");
        Exigir(jugador.GetComponent<PlayerExperience>().nivelActual == 3 && Mathf.Approximately(jugador.GetComponent<PlayerExperience>().experienciaActual, 17), "Transición conserva nivel y experiencia");
        Exigir(Object.FindObjectsByType<BossAI>(FindObjectsSortMode.None).Length == 0, "Dr. Yiba ausente de Piso3");
        QuitarDispositivos();
        GameManager.Instance.IniciarNuevaRun(1); SceneManager.LoadScene(destino); yield return 1f;
        Emparejar();
        Exigir(jugador.name.StartsWith("Player_Milena"), "Nueva partida Milena usa su prefab real en Entrada");
        Exigir(jugador.GetComponent<ArmaIndependiente>().escalaMachete == .5f && jugador.GetComponent<ArmaIndependiente>().spritesDisparo.All(s => s != null), "Milena comparte machete reducido y 8 fogonazos");
        var vida = jugador.GetComponent<PlayerHealth>(); vida.isInvulnerable = false; vida.TakeDamage(10000);
        Exigir(vida.IsDead && DeathScreenController.GameplayBlocked, "Muerte bloquea controles y abre Game Over");
        var pantallaDerrota = Object.FindFirstObjectByType<DeathScreenController>();
        var botonReintentar = pantallaDerrota.GetComponent<UIDocument>().rootVisualElement.Q<Button>("RetryButton");
        float limitePantalla = Time.unscaledTime + 12;
        while (!botonReintentar.enabledSelf && Time.unscaledTime < limitePantalla) yield return .05f;
        Exigir(botonReintentar.enabledSelf && DeathScreenController.IsShowing && Time.timeScale == 0 && SceneManager.GetActiveScene().name == destino && !TransicionEntrePisos.EnCurso, "Game Over espera la decisión del jugador en el mapa de derrota");
        botonReintentar.Focus(); yield return .15f;
        // La prueba específica de derrota cubre el evento UI; aquí verificar la regresión de la partida.
        pantallaDerrota.SendMessage("OnRetry");
        yield return .05f;
        float limiteDerrota = Time.unscaledTime + 10;
        while (TransicionEntrePisos.EnCurso && Time.unscaledTime < limiteDerrota) yield return .05f;
        Exigir(SceneManager.GetActiveScene().name == "Recepcion", "Reintentar en Game Over devuelve a Recepcion");
        Exigir(errores.Count == 0, "Sin errores ni excepciones durante el recorrido");
    }
}
