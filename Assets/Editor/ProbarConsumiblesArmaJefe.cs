using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Laboratorio4.Animacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarConsumiblesArmaJefe
{
    private const string Clave = "Laboratorio4.ConsumiblesArmaJefe.Test";
    private const string Carpeta = "Reports/ConsumiblesArmaJefe/";
    private static int fase, personaje, dir, municion, victorias;
    private static float etapa, vida, velocidad, dano;
    private static double inicio;
    private static bool pulsado;
    private static string mapa;
    private static GameObject jugador;
    private static PlayerInventory inventario;
    private static PlayerHealth salud;
    private static PlayerShooting disparo;
    private static ArmaIndependiente arma;
    private static PlayerInput entrada;
    private static Keyboard teclado;
    private static Mouse mouse;
    private static Gamepad mando;
    private static Vector3 registroReposo;
    private static BossAI jefe;
    private static BossHealth saludJefe;
    private static readonly List<string> resultados = new List<string>();
    private static readonly List<string> errores = new List<string>();

    static ProbarConsumiblesArmaJefe()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, t, tipo) =>
        {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (tipo == LogType.Error || tipo == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Integracion/Probar consumibles arma y jefe %&F11")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null; StageUtility.GoToMainStage();
        Directory.CreateDirectory(Carpeta);
        SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        EditorApplication.EnterPlaymode();
    }
    private static void Victoria(RunSummary resumen) { victorias++; }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-prueba.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-prueba.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            RankingManager.SaveRanking(new RankingData { version = 2 });
            SessionState.SetInt(Clave + ".mejor", PlayerPrefs.GetInt("MejorPuntuacion", 0));
            GameManager.OnRunVictoria += Victoria;
            fase = personaje = victorias = 0; jugador = null;
            resultados.Clear(); errores.Clear();
            inicio = EditorApplication.timeSinceStartup;
        }
        if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            QuitarDispositivos(); GameManager.OnRunVictoria -= Victoria;
            PlayerPrefs.SetInt("MejorPuntuacion", SessionState.GetInt(Clave + ".mejor", 0)); PlayerPrefs.Save();
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null;
            RankingManager.SuspenderSincronizacionAutomatica = false;
            EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        }
    }
    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
        resultados.Add("PASS " + (fase < 20 || fase == 90 || fase == 91 ? personaje == 0 ? "Jason: " : "Milena: " : "Nivel1: ") + mensaje);
    }
    private static void Avanzar(int siguiente) { fase = siguiente; etapa = Time.unscaledTime; }
    private static void Teclas(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    private static void MouseEstado(bool valor) { pulsado = valor; ApuntarMouse(); }
    private static void ApuntarMouse()
    {
        if (jugador == null || mouse == null || Camera.main == null) return;
        float angulo = (-90f + dir * 45f) * Mathf.Deg2Rad;
        Vector3 destino = arma.pivote.position + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 2f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.WorldToScreenPoint(destino), buttons = (ushort)(pulsado ? 1 : 0) });
    }
    private static void Capturar(string nombre) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(Carpeta + nombre + ".png"));
    private static Vector3 PuntoArma(Vector2 pixel) => arma.visual.transform.TransformPoint((pixel - arma.visual.sprite.pivot) / arma.visual.sprite.pixelsPerUnit);
    private static Rect Limites(RectTransform rect)
    {
        var esquinas = new Vector3[4]; rect.GetWorldCorners(esquinas);
        return Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
    }
    private static void PrepararJugador()
    {
        jugador = GameObject.FindGameObjectWithTag("Player");
        salud = jugador.GetComponent<PlayerHealth>(); inventario = jugador.GetComponent<PlayerInventory>();
        disparo = jugador.GetComponent<PlayerShooting>(); arma = jugador.GetComponent<ArmaIndependiente>(); entrada = jugador.GetComponent<PlayerInput>();
        jugador.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        salud.isInvulnerable = true;
    }
    private static void PrepararInput()
    {
        teclado = InputSystem.AddDevice<Keyboard>("TecladoIntegracion");
        mouse = InputSystem.AddDevice<Mouse>("MouseIntegracion");
        mando = InputSystem.AddDevice<Gamepad>("MandoIntegracion");
        entrada.neverAutoSwitchControlSchemes = true;
        entrada.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
        dir = 2; MouseEstado(false);
    }
    private static void IniciarRutinaJefe(string nombre, params object[] args)
    {
        jefe.enabled = true;
        typeof(BossAI).GetField("isAttacking", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(jefe, true);
        jefe.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        var rutina = (IEnumerator)typeof(BossAI).GetMethod(nombre, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(jefe, args);
        jefe.StartCoroutine(rutina);
    }
    private static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false) || !EditorApplication.isPlaying || fase < 0) return;
        float t = Time.unscaledTime - etapa;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 150) throw new Exception("Timeout en fase " + fase);
            if (fase < 20) ApuntarMouse();
            if (fase == 0 && EditorApplication.timeSinceStartup - inicio > .6)
            {
                Exigir(Object.FindFirstObjectByType<BossAI>() == null, "MainMenu no contiene jefe");
                mapa = new SerializedObject(Object.FindFirstObjectByType<MainMenuController>()).FindProperty("_gameSceneName").stringValue;
                GameManager.Instance.IniciarNuevaRun(personaje); Avanzar(1); SceneManager.LoadScene(mapa); return;
            }
            if (fase == 1 && t > 1 && SceneManager.GetActiveScene().name == mapa)
            {
                PrepararJugador(); PrepararInput();
                foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) s.enabled = false;
                foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
                Exigir(Object.FindFirstObjectByType<BossAI>() == null, "El mapa de MainMenu no contiene jefe");
                Exigir(entrada.actions.name.StartsWith("Gameplay") && entrada.currentActionMap.name == "Player", "PlayerInput usa Gameplay/Player");
                foreach (string nombre in new[] { "ConsumirVenda", "ConsumirBotiquin", "ConsumirEstimulante" })
                    Exigir(entrada.actions.FindAction(nombre).controls.OfType<UnityEngine.InputSystem.Controls.KeyControl>().Any(), nombre + " tiene control de teclado resuelto");
                inventario.vendasCount = inventario.botiquinesCount = inventario.estimulantesCount = 0;
                inventario.AgregarObjeto(ItemPickup.PickupType.Venda); inventario.AgregarObjeto(ItemPickup.PickupType.Botiquin); inventario.AgregarObjeto(ItemPickup.PickupType.Estimulante);
                salud.currentHealth = 30; salud.UpdateHealthUI();
                velocidad = jugador.GetComponent<MovimientoPlayer>().velocidad; dano = jugador.GetComponent<PlayerCombat>().attackDamage;
                inventario.duracionEstimulante = .2f;
                Teclas(Key.Digit1); Avanzar(2); return;
            }
            if (fase == 2 && t > .15)
            {
                Exigir(salud.currentHealth == 30 + inventario.curaVendas && inventario.vendasCount == 0 && inventario.textoVendas.text == "0", "1 cura y descuenta venda, HUD conectado");
                Teclas(); Avanzar(3); return;
            }
            if (fase == 3 && t > .08f) { Teclas(Key.Digit2); Avanzar(4); return; }
            if (fase == 4 && t > .15)
            {
                Exigir(salud.currentHealth == 30 + inventario.curaVendas + inventario.curaBotiquin && inventario.botiquinesCount == 0, "2 cura y descuenta botiquin");
                Teclas(); Avanzar(5); return;
            }
            if (fase == 5 && t > .08f) { Teclas(Key.Digit3); Avanzar(6); return; }
            if (fase == 6 && t > .08f)
            {
                Exigir(salud.currentHealth == salud.maxHealth && inventario.estimulantesCount == 0 && jugador.GetComponent<MovimientoPlayer>().velocidad > velocidad, "3 cura, descuenta estimulante y aplica su bufo");
                Teclas(); Avanzar(7); return;
            }
            if (fase == 7 && t > .35f)
            {
                Exigir(Mathf.Abs(jugador.GetComponent<MovimientoPlayer>().velocidad - velocidad) < .001f && Mathf.Abs(jugador.GetComponent<PlayerCombat>().attackDamage - dano) < .001f, "El bufo termina sin alterar valores base");
                inventario.AgregarObjeto(ItemPickup.PickupType.Venda);
                Teclas(Key.Digit1); Avanzar(8); return;
            }
            if (fase == 8 && t > .12f)
            {
                Exigir(inventario.vendasCount == 1 && salud.currentHealth == salud.maxHealth, "Vida llena no consume una venda");
                Teclas(); salud.currentHealth = 40; salud.UpdateHealthUI();
                Object.FindFirstObjectByType<PauseMenuController>().Pause(); Avanzar(9); return;
            }
            if (fase == 9 && t > .15f) { Teclas(Key.Digit1); Avanzar(90); return; }
            if (fase == 90 && t > .15f)
            {
                Exigir(inventario.vendasCount == 1 && salud.currentHealth == 40, "Pausa bloquea curacion y consumo");
                Teclas(); Object.FindFirstObjectByType<PauseMenuController>().Resume();
                entrada.SwitchCurrentControlScheme("Gamepad", mando);
                InputSystem.QueueStateEvent(mando, new GamepadState().WithButton(GamepadButton.DpadLeft));
                Avanzar(91); return;
            }
            if (fase == 91 && t > .15f)
            {
                Exigir(inventario.vendasCount == 0 && salud.currentHealth == 40 + inventario.curaVendas, "El mando tambien usa la accion Gameplay de curacion");
                InputSystem.QueueStateEvent(mando, new GamepadState());
                entrada.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
                Camera.main.GetComponent<LaboratorioYiba.Piso3V10.Piso3V10Camara>().enabled = false;
                Camera.main.transform.position = new Vector3(jugador.transform.position.x, jugador.transform.position.y + .3f, Camera.main.transform.position.z);
                Camera.main.orthographicSize = 2.5f;
                jugador.GetComponent<WeaponManager>().EquipWeapon(false);
                disparo.RestaurarMunicion(disparo.maxClipSize, 60); disparo.fireRate = .4f; arma.duracionDisparoVisual = .25f;
                Exigir(arma.spritesApuntar.Length == 8 && arma.spritesDisparo.Length == 8 && arma.spritesApuntar.Concat(arma.spritesDisparo).All(s => s != null), "Los 16 sprites unicos del arma estan conectados");
                dir = 0; MouseEstado(false); Avanzar(11); return;
            }
            if (fase == 11 && t > .45f)
            {
                Exigir(arma.visual.sprite == arma.spritesApuntar[dir], "Reposo del arma, direccion " + dir);
                Vector3 ejeDibujo = Quaternion.Euler(0, 0, -90 + dir * 45) * Vector3.right;
                Exigir(Vector3.Dot(arma.visual.transform.TransformDirection(ejeDibujo), arma.pivote.right) > .99f && !arma.visual.flipY, "Orientacion del sprite correcta, direccion " + dir);
                registroReposo = PuntoArma(new Vector2(24, 24));
                municion = disparo.currentAmmoInClip; MouseEstado(true); Avanzar(12); return;
            }
            if (fase == 12 && t > .06f)
            {
                Exigir(disparo.currentAmmoInClip == municion - 1 && arma.DisparoVisualActivo && arma.visual.sprite == arma.spritesDisparo[dir], "Un disparo real muestra el fogonazo, direccion " + dir);
                Exigir(Vector3.Distance(registroReposo, PuntoArma(new Vector2(24, 40))) <= arma.retrocesoVisual + .003f, "El cuerpo del arma no salta al cambiar de lienzo, direccion " + dir);
                if (dir == 2 || dir == 6) Capturar((personaje == 0 ? "Jason" : "Milena") + "-disparo-" + dir);
                MouseEstado(false); Avanzar(13); return;
            }
            if (fase == 13 && t > .32f)
            {
                Exigir(disparo.currentAmmoInClip == municion - 1 && !arma.DisparoVisualActivo && arma.visual.sprite == arma.spritesApuntar[dir], "Soltar detiene balas y el arma vuelve a reposo, direccion " + dir);
                dir++;
                if (dir < 8) { MouseEstado(false); Avanzar(11); }
                else
                {
                    Exigir(!jugador.GetComponent<AnimadorSprites8Direcciones>().Tiene("Disparo"), "El cuerpo no usa sprites de disparo");
                    jugador.GetComponent<WeaponManager>().EquipWeapon(true); Avanzar(14);
                }
                return;
            }
            if (fase == 14 && t > .15f)
            {
                Exigir(arma.visual.sprite == arma.machete && !arma.DisparoVisualActivo && !jugador.GetComponent<MacheteAnimacion>().AlcanceVisible, "Machete conserva postura y circulo oculto");
                QuitarDispositivos(); jugador = null;
                if (personaje == 0)
                {
                    personaje = 1; GameManager.Instance.IniciarNuevaRun(personaje); Avanzar(1); SceneManager.LoadScene(mapa);
                }
                else
                {
                    GameManager.Instance.IniciarNuevaRun(0); Avanzar(20); SceneManager.LoadScene("Nivel1");
                }
                return;
            }
            if (fase == 20 && t > 1)
            {
                PrepararJugador(); entrada.DeactivateInput();
                Exigir(SceneManager.GetActiveScene().path == IntegrarConsumiblesArmaJefe.EscenaJefe && Object.FindObjectsByType<BossAI>(FindObjectsSortMode.None).Length == 1, "Un solo jefe en su escena independiente");
                jefe = Object.FindFirstObjectByType<BossAI>(); saludJefe = jefe.GetComponent<BossHealth>();
                var anim = jefe.GetComponent<AnimadorSprites8Direcciones>();
                Exigir(anim.biblioteca.personaje == "DoctorYiba" && anim.Animaciones.Length == 6 && anim.Animaciones.All(c => Enumerable.Range(0, 8).All(d => AnimadorSprites8Direcciones.Frames(c, d)?.Length > 0)), "Seis estados del jefe resuelven sus ocho direcciones");
                Exigir(saludJefe.bossHealthUIContainer.activeInHierarchy && saludJefe.healthBarFill.fillAmount == 1, "HUD de vida del jefe conectado y visible");
                var barraJugador = salud.healthBarFill.GetComponentInParent<Canvas>().transform.Find("HealthBarContainer").GetComponent<RectTransform>();
                Exigir(!Limites(barraJugador).Overlaps(Limites(saludJefe.bossHealthUIContainer.GetComponent<RectTransform>())), "HUD del jefe no tapa la vida del jugador");
                jefe.enabled = false; jefe.transform.position = Vector3.zero; jugador.transform.position = new Vector3(0, -5, 0);
                var seguimiento = Camera.main.GetComponent<SeguimientoCamara>(); if (seguimiento != null) seguimiento.enabled = false;
                Camera.main.transform.position = new Vector3(0, -1, -10); Camera.main.orthographicSize = 5.5f;
                jefe.enabled = true; Avanzar(21); return;
            }
            if (fase == 21 && t > .3f)
            {
                Exigir(jefe.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude > .1f && jefe.GetComponent<AnimadorSprites8Direcciones>().AnimacionActual == "Correr", "El jefe persigue y reproduce Correr");
                jefe.enabled = false; jefe.transform.position = Vector3.zero; jugador.transform.position = new Vector3(0, -1.3f, 0);
                salud.isInvulnerable = false; vida = salud.currentHealth;
                IniciarRutinaJefe("EjecutarZarpazo", jefe.damageZarpazo, jefe.knockbackZarpazo, Color.white); Avanzar(22); return;
            }
            if (fase == 22 && t > .1f)
            {
                Exigir(jefe.GetComponent<AnimadorSprites8Direcciones>().AnimacionActual == "Ataque", "Zarpazo vinculado a Ataque");
                Avanzar(23); return;
            }
            if (fase == 23 && t > .45f)
            {
                Exigir(salud.currentHealth < vida, "Zarpazo aplica dano al jugador");
                salud.isInvulnerable = true; jefe.enabled = false;
                saludJefe.TakeDamage(saludJefe.maxHealth * .35f);
                Exigir(jefe.CurrentPhase == 2 && Mathf.Abs(saludJefe.healthBarFill.fillAmount - .65f) < .001f, "Dano y HUD activan fase 2");
                jugador.transform.position = new Vector3(4, 0, 0);
                IniciarRutinaJefe("EjecutarBulletHell"); Avanzar(24); return;
            }
            if (fase == 24 && t > .1f)
            {
                Exigir(jefe.GetComponent<AnimadorSprites8Direcciones>().AnimacionActual == "BolaDePoder", "Bullet Hell vinculado a BolaDePoder");
                Avanzar(25); return;
            }
            if (fase == 25 && t > 1.02f)
            {
                var balas = Object.FindObjectsByType<BossBullet>(FindObjectsSortMode.None);
                Exigir(balas.Length == jefe.bulletsAmount && balas.All(b => b.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude > 0), "Bullet Hell genera el anillo de proyectiles en movimiento");
                Capturar("DoctorYiba-BulletHell");
                jefe.enabled = false; Avanzar(250); return;
            }
            if (fase == 250 && t > .12f)
            {
                foreach (var b in Object.FindObjectsByType<BossBullet>(FindObjectsSortMode.None)) Object.Destroy(b.gameObject);
                salud.isInvulnerable = false; vida = salud.currentHealth;
                Vector3 centro = jugador.GetComponent<Collider2D>().bounds.center;
                Object.Instantiate(jefe.bossBulletPrefab, centro + Vector3.left * 1.2f, Quaternion.Euler(0, 0, -90));
                Avanzar(26); return;
            }
            if (fase == 26 && t > .4f)
            {
                Exigir(salud.currentHealth < vida, "Proyectil del jefe dana mediante colision real");
                salud.isInvulnerable = true;
                saludJefe.TakeDamage(saludJefe.maxHealth * .34f);
                Exigir(jefe.CurrentPhase == 3 && Mathf.Abs(saludJefe.healthBarFill.fillAmount - .31f) < .001f, "Dano activa fase 3");
                Exigir(jefe.basicEnemiesToSummon.Length == 2 && jefe.basicEnemiesToSummon.All(p => p.GetComponent<EnemyHealth>() != null), "Invocacion referencia ambos Sorbos existentes");
                IniciarRutinaJefe("EjecutarInvocacion"); Avanzar(27); return;
            }
            if (fase == 27 && t > .1f)
            {
                Exigir(jefe.GetComponent<AnimadorSprites8Direcciones>().AnimacionActual == "CampoEnergia", "Invocacion vinculada a CampoEnergia");
                Avanzar(28); return;
            }
            if (fase == 28 && t > 1.55f)
            {
                var enemigos = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
                Exigir(enemigos.Length == jefe.enemiesToSpawn && enemigos.All(e => e.GetComponent<AnimadorSprites8Direcciones>().biblioteca != null), "Invocacion crea Sorbos con mecanicas y animaciones");
                foreach (var e in enemigos) Object.Destroy(e.gameObject);
                jefe.enabled = false;
                saludJefe.TakeDamage(10000); saludJefe.TakeDamage(10000);
                Exigir(saludJefe.IsDead && jefe.GetComponent<AnimadorSprites8Direcciones>().AnimacionActual == "Muerte" && !saludJefe.bossHealthUIContainer.activeSelf, "Muerte detiene la IA, oculta barra y reproduce sus sprites");
                Exigir(victorias == 1 && !GameManager.Instance.RunActiva && GameManager.Instance.puntuacionTotal == 500, "Matar al jefe concede una sola victoria y su puntuacion");
                Avanzar(29); return;
            }
            if (fase == 29 && t > 4)
            {
                Exigir(jefe == null && VictoryScreenController.IsShowing && DeathScreenController.GameplayBlocked && Time.timeScale == 0, "Victoria visible, cadaver retirado y gameplay bloqueado");
                var ranking = RankingManager.LoadRanking();
                Exigir(ranking.topScores.Any(r => r.score == 500 && r.victory) && ranking.topTimes.Count == 1, "Victoria del jefe llega al ranking aislado");
                Capturar("DoctorYiba-Victoria");
                var victoria = Object.FindFirstObjectByType<VictoryScreenController>();
                Exigir(victoria.GetComponent<UIDocument>().rootVisualElement.Q<Button>("ContinueButton").enabledSelf, "Boton de continuar habilitado");
                Avanzar(290); return;
            }
            if (fase == 290 && t > .15f)
            {
                Avanzar(30); Object.FindFirstObjectByType<VictoryScreenController>().SendMessage("OnContinue"); return;
            }
            if (fase == 30 && t > .8f)
            {
                Exigir(SceneManager.GetActiveScene().name == "MainMenu" && Object.FindFirstObjectByType<BossAI>() == null && Time.timeScale == 1, "Continuar vuelve a MainMenu sin colocar alli al jefe");
                Exigir(errores.Count == 0, "Flujo completo sin errores ni excepciones"); Terminar();
            }
        }
        catch (Exception e) { errores.Add(e.Message); Terminar(); }
    }
    private static void QuitarDispositivos()
    {
        foreach (var d in new InputDevice[] { teclado, mouse, mando }) if (d != null && d.added) InputSystem.RemoveDevice(d);
        teclado = null; mouse = null; mando = null; pulsado = false;
    }
    private static void Terminar()
    {
        fase = -1;
        File.WriteAllLines(Carpeta + "playmode.txt", new[] { errores.Count == 0 ? "RESULTADO: PASS" : "RESULTADO: FAIL" }.Concat(resultados).Concat(errores.Select(e => "FAIL " + e)));
        Debug.Log("[ConsumiblesArmaJefeTest] " + (errores.Count == 0 ? "PASS" : "FAIL") + " " + string.Join(" | ", errores));
        EditorApplication.ExitPlaymode();
    }
}
