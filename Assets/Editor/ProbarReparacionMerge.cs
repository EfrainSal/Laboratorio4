using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarReparacionMerge
{
    private const string Clave = "Laboratorio4.Reparacion.Test";
    private const string Informe = "Reports/Reparacion/playmode.txt";
    private static int fase;
    private static double inicio, etapa;
    private static bool terminando;
    private static GameObject jugador, enemigoDentro, enemigoFuera;
    private static Keyboard teclado;
    private static Mouse mouse;
    private static Vector3 posicion;
    private static int municion;
    private static Task ranking;
    private static string destino;
    private static readonly List<string> errores = new List<string>();
    private static readonly List<string> resultados = new List<string>();
    static ProbarReparacionMerge()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, t, tipo) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (tipo == LogType.Error || tipo == LogType.Exception)) errores.Add(m);
        };
    }

    [MenuItem("Laboratorio 4/Reparacion/Probar mecanicas y rankings %&F7")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Reports/Reparacion");
        File.WriteAllText(Informe, "Input real mediante dispositivos virtuales. Guardados y ranking aislados. Sin partidas ficticias en API real.\n");
        SessionState.SetInt(Clave + ".personaje", 0);
        SessionState.SetBool(Clave, true);
        Empezar();
    }
    private static void Empezar()
    {
        Selection.activeObject = null;
        StageUtility.GoToMainStage();
        EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        EditorApplication.EnterPlaymode();
    }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            SaveSystem.RutaDePruebas = Path.GetFullPath("Reports/Reparacion/run-pruebas.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath("Reports/Reparacion/ranking-pruebas.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            RankingManager.SaveRanking(new RankingData { version = 2 });
            inicio = EditorApplication.timeSinceStartup;
            fase = 0; terminando = false; errores.Clear(); resultados.Clear();
            SessionState.SetInt(Clave + ".mejor", PlayerPrefs.GetInt("MejorPuntuacion", 0));
        }
        if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            PlayerPrefs.SetInt("MejorPuntuacion", SessionState.GetInt(Clave + ".mejor", 0));
            PlayerPrefs.Save();
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SaveSystem.RutaDePruebas = null;
            RankingManager.RutaDePruebas = null;
            RankingManager.ApiDePruebas = null;
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
                Debug.Log("[Reparacion] Pruebas finalizadas: " + Informe);
            }
        }
    }
    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
        resultados.Add("PASS " + mensaje);
    }
    private static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    private static void MouseEstado(bool disparar)
    {
        var arma = jugador.GetComponent<ArmaIndependiente>();
        Vector2 punto = Camera.main.WorldToScreenPoint(arma.pivote.position + Vector3.right * 4);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = punto, buttons = (ushort)(disparar ? 1 : 0) });
    }
    private static void Avanzar(int siguiente) { fase = siguiente; etapa = EditorApplication.timeSinceStartup; }
    private static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false) || !EditorApplication.isPlaying || terminando || inicio == 0) return;
        double ahora = EditorApplication.timeSinceStartup;
        int id = SessionState.GetInt(Clave + ".personaje", 0);
        try
        {
            if (ahora - inicio > 110) throw new Exception("Timeout de pruebas, fase " + fase);
            if (fase == 0 && ahora - inicio > .7)
            {
                Exigir(SceneManager.GetActiveScene().name == "MainMenu" && GameManager.Instance != null, "Inicio desde MainMenu");
                destino = new SerializedObject(Object.FindFirstObjectByType<MainMenuController>()).FindProperty("_gameSceneName").stringValue;
                GameManager.Instance.IniciarNuevaRun(id);
                Avanzar(1); SceneManager.LoadScene(destino); return;
            }
            if (fase == 1 && ahora - etapa > 1 && SceneManager.GetActiveScene().name == destino)
            {
                Exigir(GameObject.FindGameObjectsWithTag("Player").Length == 1, "Un solo jugador activo");
                jugador = GameObject.FindGameObjectWithTag("Player");
                foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
                foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
                jugador.transform.position = new Vector3(50, 50, 0);
                Camera.main.GetComponent<LaboratorioYiba.Piso3V10.Piso3V10Camara>().enabled = false;
                Camera.main.transform.position = new Vector3(50, 50, Camera.main.transform.position.z);
                jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
                var input = jugador.GetComponent<PlayerInput>();
                Exigir(input.actions != null && input.actions.FindAction("Reload") != null && input.actions.FindAction("SwitchWeapon") != null, "Acciones completas conectadas al prefab");
                teclado = InputSystem.AddDevice<Keyboard>("TecladoPruebas");
                mouse = InputSystem.AddDevice<Mouse>("MousePruebas");
                input.neverAutoSwitchControlSchemes = true;
                input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
                MouseEstado(false);
                posicion = jugador.transform.position;
                Tecla(Key.D); Avanzar(2); return;
            }
            if (fase == 2 && ahora - etapa > .35)
            {
                Exigir(jugador.GetComponent<MovimientoPlayer>().DireccionEntrada.x > .9f && jugador.transform.position.x > posicion.x + .1f, "D mueve al jugador a traves de PlayerInput y Rigidbody2D");
                Tecla(); Avanzar(3); return;
            }
            if (fase == 3 && ahora - etapa > .15)
            {
                Exigir(jugador.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude < .01f, "Soltar movimiento detiene al jugador");
                var a = jugador.GetComponent<ArmaIndependiente>();
                Exigir(Vector2.Distance(a.cadera, new Vector2(-.04f, id == 0 ? .24f : .25f)) < .0001f, "Posicion personalizada del arma conservada");
                var s = jugador.GetComponent<PlayerShooting>();
                s.RestaurarMunicion(3, 5);
                Tecla(Key.R); Avanzar(4); return;
            }
            if (fase == 4 && ahora - etapa > .15)
            {
                var s = jugador.GetComponent<PlayerShooting>();
                Exigir(s.currentAmmoInClip == 8 && s.totalAmmo == 0 && s.ammoText.text == "8 / 0", "R recarga municion limitada y actualiza HUD");
                Tecla(); Avanzar(5); return;
            }
            if (fase == 5 && ahora - etapa > .1)
            {
                Tecla(Key.R); Avanzar(6); return;
            }
            if (fase == 6 && ahora - etapa > .1)
            {
                var s = jugador.GetComponent<PlayerShooting>();
                Exigir(s.currentAmmoInClip == 8 && s.totalAmmo == 0, "Sin reserva R no inventa balas");
                Tecla(); MouseEstado(true); municion = s.currentAmmoInClip; Avanzar(7); return;
            }
            if (fase == 7 && ahora - etapa > .2)
            {
                Exigir(jugador.GetComponent<PlayerShooting>().currentAmmoInClip < municion && Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length > 0, "Click dispara proyectil y consume municion");
                MouseEstado(false); Tecla(Key.Q); Avanzar(8); return;
            }
            if (fase == 8 && ahora - etapa > .2)
            {
                Exigir(jugador.GetComponent<WeaponManager>().isUsingMelee && !jugador.GetComponent<PlayerShooting>().enabled && jugador.GetComponent<PlayerCombat>().enabled, "Q cambia a machete por accion real");
                var visual = jugador.GetComponent<MacheteAnimacion>();
                var c = jugador.GetComponent<PlayerCombat>();
                Exigir(visual.AlcanceVisible && visual.fotogramas.Length == 8, "Ocho sprites de tajo e indicador visibles con machete");
                Exigir(Enumerable.Range(0, visual.Indicador.positionCount).All(i => Mathf.Abs(Vector3.Distance(visual.Indicador.GetPosition(i), c.attackPoint.position) - c.attackRange) < .001f), "Indicador usa exactamente el centro y radio de la colision");
                enemigoDentro = CrearEnemigo(c.attackPoint.position + Vector3.right * (c.attackRange - .03f), "Enemy_Melee");
                enemigoFuera = CrearEnemigo(c.attackPoint.position + Vector3.up * (c.attackRange + .2f), "Enemy_Ranged");
                Tecla(); Avanzar(9); return;
            }
            if (fase == 9 && ahora - etapa > .15)
            {
                // Otro collider en el mismo enemigo no debe duplicar daño.
                var segundo = enemigoDentro.AddComponent<CircleCollider2D>();
                segundo.radius = .01f;
                segundo.offset = Vector2.zero;
                Physics2D.SyncTransforms();
                MouseEstado(true); Avanzar(10); return;
            }
            if (fase == 10 && ahora - etapa > .06)
            {
                Exigir(Mathf.Abs(enemigoDentro.GetComponent<EnemyHealth>().CurrentHealth - (1000 - jugador.GetComponent<PlayerCombat>().attackDamage)) < .01f, "Tajo dana una vez al enemigo dentro, incluso con dos colliders (vida=" + enemigoDentro.GetComponent<EnemyHealth>().CurrentHealth + ")");
                Exigir(enemigoFuera.GetComponent<EnemyHealth>().CurrentHealth == 1000, "Tajo no alcanza enemigo fuera del radio");
                Exigir(enemigoDentro.GetComponent<RetroalimentacionDano>().GolpesMostrados == 1 && enemigoDentro.GetComponent<RetroalimentacionDano>().FlashActivo, "Enemigo recibe destello y numero al sufrir dano");
                Exigir(jugador.GetComponent<MacheteAnimacion>().Reproduciendo, "El ataque real reproduce la animacion de tajo");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Reports/Reparacion/" + (id == 0 ? "Jason" : "Milena") + "-tajo.png"));
                MouseEstado(false); Tecla(Key.R); Avanzar(11); return;
            }
            if (fase == 11 && ahora - etapa > .2)
            {
                Exigir(jugador.GetComponent<PlayerShooting>().currentAmmoInClip < 8, "Recarga bloqueada al usar machete");
                Tecla(); Object.Destroy(enemigoDentro); Object.Destroy(enemigoFuera);
                jugador.GetComponent<PlayerHealth>().isInvulnerable = false;
                float vida = jugador.GetComponent<PlayerHealth>().currentHealth;
                jugador.GetComponent<PlayerHealth>().TakeDamage(10, DeathCause.Mutante);
                Exigir(jugador.GetComponent<PlayerHealth>().currentHealth < vida && jugador.GetComponent<RetroalimentacionDano>().FlashActivo, "Jugador pierde vida y muestra feedback al recibir dano");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Reports/Reparacion/" + (id == 0 ? "Jason" : "Milena") + "-dano.png"));
                jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
                Tecla(Key.Q); Avanzar(12); return;
            }
            if (fase == 12 && ahora - etapa > .2)
            {
                Exigir(!jugador.GetComponent<WeaponManager>().isUsingMelee && !jugador.GetComponent<MacheteAnimacion>().AlcanceVisible, "Q restaura subfusil y oculta alcance melee");
                Tecla();
                var s = jugador.GetComponent<PlayerShooting>();
                enemigoDentro = CrearEnemigo(s.firePoint.position + s.firePoint.right * 2f, "Enemy_Ranged");
                enemigoDentro.GetComponent<CircleCollider2D>().radius = .15f;
                Avanzar(50); return;
            }
            if (fase == 50 && ahora - etapa > .15)
            {
                MouseEstado(true); Avanzar(51); return;
            }
            if (fase == 51 && ahora - etapa > .25)
            {
                float esperado = 1000 - jugador.GetComponent<PlayerShooting>().bulletPrefab.GetComponent<Bullet>().damage;
                Exigir(Mathf.Abs(enemigoDentro.GetComponent<EnemyHealth>().CurrentHealth - esperado) < .01f && enemigoDentro.GetComponent<RetroalimentacionDano>().GolpesMostrados == 1, "Bala dana al segundo Sorbo una vez y activa su feedback");
                MouseEstado(false); Object.Destroy(enemigoDentro);
                jugador.GetComponent<PlayerShooting>().RestaurarMunicion(2, 9);
                Object.FindFirstObjectByType<PauseMenuController>().Pause();
                Avanzar(13); return;
            }
            if (fase == 13 && ahora - etapa > .2)
            {
                Tecla(Key.Q, Key.R, Key.D); MouseEstado(true); Avanzar(14); return;
            }
            if (fase == 14 && ahora - etapa > .2)
            {
                Exigir(!jugador.GetComponent<WeaponManager>().isUsingMelee && jugador.GetComponent<PlayerShooting>().currentAmmoInClip == 2 && jugador.GetComponent<PlayerShooting>().totalAmmo == 9, "Pausa bloquea cambio de arma, disparo y recarga");
                Tecla(); MouseEstado(false);
                Object.FindFirstObjectByType<PauseMenuController>().Resume();
                // Restaurar antes de Start no debe rellenar el cargador luego.
                var temporal = Object.Instantiate(GameManager.Instance.ObtenerPrefabJugador(), new Vector3(80, 80), Quaternion.identity);
                temporal.name = "PruebaMunicionRestaurada";
                temporal.GetComponent<PlayerInput>().DeactivateInput();
                temporal.GetComponent<PlayerShooting>().RestaurarMunicion(4, 11);
                Avanzar(15); return;
            }
            if (fase == 15 && ahora - etapa > .15)
            {
                var temporal = GameObject.Find("PruebaMunicionRestaurada");
                Exigir(temporal.GetComponent<PlayerShooting>().currentAmmoInClip == 4 && temporal.GetComponent<PlayerShooting>().totalAmmo == 11, "Start conserva municion restaurada desde guardado");
                Object.Destroy(temporal);
                GameManager.Instance.RegistrarBajaEnemiga(25);
                jugador.GetComponent<PlayerHealth>().isInvulnerable = false;
                jugador.GetComponent<PlayerHealth>().TakeDamage(10000);
                GameManager.Instance.TerminarRunPorMuerte();
                var datos = RankingManager.LoadRanking();
                Exigir(datos.topScores.Count == 1 && datos.topScores[0].score == 25 && datos.topScores[0].characterId == id + 1 && datos.pendingUploads.Count == 1, "Muerte guarda score y personaje una vez, con envio pendiente");
                Exigir(datos.topTimes.Count == 0, "Una derrota no entra como victoria rapida");
                Avanzar(16); return;
            }
            if (fase == 16 && ahora - etapa > 2)
            {
                var pantalla = Object.FindFirstObjectByType<DeathScreenController>();
                var overlay = pantalla.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("DeathOverlay");
                Exigir(DeathScreenController.IsShowing && !overlay.ClassListContains("hidden"), "Game Over sigue apareciendo tras reparar input y rankings");
                Avanzar(17); pantalla.SendMessage("OnMenu"); return;
            }
            if (fase == 17 && ahora - etapa > .7 && SceneManager.GetActiveScene().name == "MainMenu")
            {
                var menu = Object.FindFirstObjectByType<MainMenuController>();
                menu.SendMessage("MostrarRanking");
                Exigir(menu.GetComponent<UIDocument>().rootVisualElement.Query<Label>().ToList().Any(l => l.text.Contains("Puntos: 25")), "Menu muestra puntuacion de la partida terminada");
                ranking = ProbarRanking(); Avanzar(18); return;
            }
            if (fase == 18 && ranking.IsCompleted)
            {
                if (ranking.IsFaulted) throw ranking.Exception.GetBaseException();
                Exigir(errores.Count == 0, "Sin errores ni excepciones en Play Mode");
                Terminar(true);
            }
        }
        catch (Exception e) { errores.Add(e.Message); Terminar(false); }
    }
    private static GameObject CrearEnemigo(Vector3 punto, string nombre)
    {
        var e = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + nombre + ".prefab"), punto, Quaternion.identity);
        e.GetComponent<EnemyHealth>().maxHealth = 1000;
        if (e.TryGetComponent<MeleeEnemyAI>(out var m)) m.enabled = false;
        if (e.TryGetComponent<RangedEnemyAI>(out var r)) r.enabled = false;
        if (e.TryGetComponent<EnemyAI>(out var a)) a.enabled = false;
        foreach (var collider in e.GetComponents<Collider2D>()) Object.Destroy(collider);
        var hitbox = e.AddComponent<CircleCollider2D>();
        hitbox.radius = .01f;
        hitbox.offset = Vector2.zero;
        e.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        return e;
    }

    private static async Task ProbarRanking()
    {
        // Migrar el formato antiguo sin perder ni duplicar sus dos copias.
        File.WriteAllText(RankingManager.RutaDePruebas, "{\"topScores\":[{\"playerName\":\"Legacy\",\"score\":12,\"time\":10,\"characterId\":1,\"date\":\"2026-01-01T00:00:00Z\",\"uploaded\":false}],\"topTimes\":[{\"playerName\":\"Legacy\",\"score\":12,\"time\":10,\"characterId\":1,\"date\":\"2026-01-01T00:00:00Z\",\"uploaded\":false}]}");
        var data = RankingManager.LoadRanking();
        Exigir(data.version == 2 && data.pendingUploads.Count == 1 && data.topScores[0].recordId == data.topTimes[0].recordId, "Migracion preserva ranking antiguo sin duplicar pendientes");
        for (int i = 0; i < 13; i++) RankingManager.RegistrarPartida("Test" + i, i * 10, 50 + i, i % 2, i == 0);
        data = RankingManager.LoadRanking();
        Exigir(data.topScores.Count == 10 && data.pendingUploads.Count == 14 && data.topTimes.Count == 2, "Cola conserva partidas fuera del top 10 y tiempos solo de victorias");
        Exigir(data.pendingUploads.Select(r => r.recordId).Distinct().Count() == 14, "Identificadores distintos aunque las partidas se creen el mismo segundo");
        var permiso = PlayerSettings.insecureHttpOption;
        using var server = new ServidorRankingPrueba();
        try
        {
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            RankingManager.ApiDePruebas = server.Url;
            await RankingManager.SincronizarAsync();
            Exigir(RankingManager.UltimoEstadoHttp == 503 && RankingManager.LoadRanking().pendingUploads.Count == 14, "HTTP 503 conserva toda la cola persistida");
            var envio = RankingManager.SincronizarAsync();
            Exigir(ReferenceEquals(envio, RankingManager.SincronizarAsync()), "Llamadas concurrentes comparten una sola sincronizacion");
            await server.SegundoRecibido.Task;
            RankingManager.RegistrarPartida("PartidaDuranteEnvio", 999, 120, 1, false);
            server.Continuar.TrySetResult(true);
            await envio;
            data = RankingManager.LoadRanking();
            Exigir(string.IsNullOrEmpty(RankingManager.UltimoError) && data.pendingUploads.Count == 0 && data.topScores.All(r => r.uploaded), "Reintento acepta 201, 200 y 204, marca tablas y vacia cola");
            Exigir(data.topScores.Any(r => r.playerName == "PartidaDuranteEnvio"), "Sincronizacion no sobrescribe partidas registradas durante await");
            Exigir(server.Cuerpos.Count == 16 && server.Cuerpos.All(c => c.Contains("\"uploaded\":true") && !c.Contains("recordId") && !c.Contains("victory")) && server.CredencialPresente, "POST real contra servidor local usa contrato de seis campos y cabecera API");
            File.WriteAllLines("Reports/Reparacion/payloads-prueba.jsonl", server.Cuerpos);
        }
        finally
        {
            PlayerSettings.insecureHttpOption = permiso;
            RankingManager.ApiDePruebas = null;
            server.Continuar.TrySetResult(true);
        }
    }

    private static void Terminar(bool ok)
    {
        terminando = true;
        string nombre = SessionState.GetInt(Clave + ".personaje", 0) == 0 ? "JASON" : "MILENA";
        File.AppendAllLines(Informe, new[] { "\n" + nombre + ": " + (ok ? "PASS" : "FAIL") }.Concat(resultados).Concat(errores.Select(e => "FAIL " + e)));
        Debug.Log("[Reparacion] " + nombre + ": " + (ok ? "PASS" : "FAIL") + " " + string.Join(" | ", errores));
        EditorApplication.ExitPlaymode();
    }

    private sealed class ServidorRankingPrueba : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        public readonly List<string> Cuerpos = new List<string>();
        public readonly TaskCompletionSource<bool> SegundoRecibido = new TaskCompletionSource<bool>();
        public readonly TaskCompletionSource<bool> Continuar = new TaskCompletionSource<bool>();
        public bool CredencialPresente = true;
        public string Url { get; }
        public ServidorRankingPrueba()
        {
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/ranking";
            _ = Task.Run(Atender);
        }
        private async Task Atender()
        {
            try
            {
                while (true)
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                    string linea; int longitud = 0; bool clave = false;
                    while (!string.IsNullOrEmpty(linea = await reader.ReadLineAsync()))
                    {
                        if (linea.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) longitud = int.Parse(linea.Split(':')[1].Trim());
                        if (linea.StartsWith("X-API-Key:", StringComparison.OrdinalIgnoreCase)) clave = linea.Split(':')[1].Trim().Length > 0;
                    }
                    var buffer = new char[longitud]; int leidos = 0;
                    while (leidos < longitud) leidos += await reader.ReadAsync(buffer, leidos, longitud - leidos);
                    Cuerpos.Add(new string(buffer)); CredencialPresente &= clave;
                    if (Cuerpos.Count == 2) { SegundoRecibido.TrySetResult(true); await Continuar.Task; }
                    string status = Cuerpos.Count == 1 ? "503 Service Unavailable" : Cuerpos.Count == 2 ? "201 Created" : Cuerpos.Count == 3 ? "200 OK" : "204 No Content";
                    byte[] respuesta = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(respuesta, 0, respuesta.Length);
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception e) { SegundoRecibido.TrySetException(e); }
        }
        public void Dispose() { Continuar.TrySetResult(true); listener.Stop(); }
    }
}
