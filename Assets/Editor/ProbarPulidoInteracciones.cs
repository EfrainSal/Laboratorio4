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
public static class ProbarPulidoInteracciones
{
    const string Clave = "L4.Pulido.QA", Carpeta = "Reports/PulidoInteracciones/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina;
    static double siguiente, inicio;
    static Keyboard teclado; static Mouse raton; static Gamepad mando;
    static bool segundoPlano;
    static GameObject Jugador => LevelManager.Instance.JugadorActual;
    static InteraccionJugador Interaccion => Jugador.GetComponent<InteraccionJugador>();
    static AvisoInteraccionUI Aviso => Jugador.GetComponent<AvisoInteraccionUI>();
    static ProbarPulidoInteracciones()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, p, t) => { if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m); };
    }
    [MenuItem("Laboratorio 4/Pulido/Probar interacciones combate y consumibles %&F12")]
    public static void Probar() { Empezar(false); }
    public static void ProbarEnLote() { Empezar(true); }
    static void Empezar(bool lote)
    {
        Directory.CreateDirectory(Carpeta); SessionState.SetString(Clave + ".escena", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Clave + ".lote", lote); SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear(); segundoPlano = Application.runInBackground; Application.runInBackground = true;
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-prueba.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-prueba.dat"); RankingManager.SuspenderSincronizacionAutomatica = true;
            foreach (var key in new[] { "MejorPuntuacion", "SelectedCharacter", GestorModoEntrada.ClavePreferencia })
            { SessionState.SetBool(Clave + key + ".existe", PlayerPrefs.HasKey(key)); SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key)); }
            teclado = InputSystem.AddDevice<Keyboard>("PulidoKeyboard"); raton = InputSystem.AddDevice<Mouse>("PulidoMouse"); mando = InputSystem.AddDevice<Gamepad>("PulidoGamepad");
            GestorModoEntrada.EstablecerModo(ModoEntrada.TecladoYRaton);
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Aplanar(Recorrido());
        }
        if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; Time.timeScale = 1; Application.runInBackground = segundoPlano;
            if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
            if (raton != null && raton.added) InputSystem.RemoveDevice(raton);
            if (mando != null && mando.added) InputSystem.RemoveDevice(mando);
            GestorModoEntrada.EstablecerModo((ModoEntrada)SessionState.GetInt(Clave + GestorModoEntrada.ClavePreferencia, 0));
            foreach (var key in new[] { "MejorPuntuacion", "SelectedCharacter", GestorModoEntrada.ClavePreferencia })
                if (SessionState.GetBool(Clave + key + ".existe", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0)); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false); SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null; RankingManager.SuspenderSincronizacionAutomatica = false;
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(e => "ERROR " + e)));
            Debug.Log("[PulidoQA] " + resultados.Count + " comprobaciones; " + errores.Count + " errores.");
            if (SessionState.GetBool(Clave + ".lote", false)) EditorApplication.Exit(errores.Count == 0 ? 0 : 1);
            else EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 180) throw new Exception("Tiempo agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float f ? f : .03f);
        }
        catch (Exception e) { errores.Add(e.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static IEnumerator Aplanar(IEnumerator r)
    { while (r.MoveNext()) { if (r.Current is IEnumerator sub) { var a = Aplanar(sub); while (a.MoveNext()) yield return a.Current; } else yield return r.Current; } }
    static void Exigir(bool b, string m) { if (!b) throw new Exception(m); resultados.Add("PASS " + m); }
    static void Invocar(object o, string metodo) => o.GetType().GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(o, null);
    static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    static IEnumerator PulsarF() { Tecla(Key.F); yield return .12f; Tecla(); yield return .12f; }
    static void Mover(Vector2 p)
    { Jugador.transform.position = p; var rb = Jugador.GetComponent<Rigidbody2D>(); rb.position = p; rb.linearVelocity = Vector2.zero; Physics2D.SyncTransforms(); }
    static void Acercar(Collider2D c) => Mover((Vector2)Jugador.transform.position + (Vector2)c.bounds.center - (Vector2)Jugador.GetComponent<Collider2D>().bounds.center);
    static void Preparar()
    {
        var pi = Jugador.GetComponent<PlayerInput>(); pi.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, raton);
        Jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
        foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
        foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
    }
    static IEnumerator Recorrido()
    {
        yield return .4f;
        for (int personaje = 0; personaje < 2; personaje++)
        {
            GameManager.Instance.IniciarNuevaRun(personaje); SceneManager.LoadScene("Recepcion"); yield return 1f;
            var controles = Object.FindFirstObjectByType<PantallaControlesInicio>();
            for (int i = 0; i < 20 && PantallaControlesInicio.BloqueaJuego; i++) { controles.Cerrar(); yield return .15f; }
            Preparar();
            Exigir(!DeathScreenController.GameplayBlocked, "Jugador " + personaje + ": partida activa después de controles");
            var nota = Object.FindObjectsByType<NotaInteractable>(FindObjectsSortMode.None).Single(n => n.name == IntegrarPulidoInteracciones.NombreNota);
            Exigir(nota.textoNota.Contains("TODAS LAS NOTAS") && nota.textoNota.Contains("TODOS LOS SORBOS") && nota.textoNota.Contains("portapapeles"), "Nota tutorial explica ambos contadores y tarjeta");
            Exigir(nota.GetComponentsInChildren<Collider2D>().All(c => c.isTrigger), "Nota sin collider sólido");
            Acercar(nota.GetComponent<Collider2D>()); yield return .25f;
            Exigir(Interaccion.currentInteractable == nota && Aviso.Visible && Aviso.Mensaje.Contains("F"), "Jugador " + personaje + ": F detecta nota tutorial");
            var panel = (VisualElement)typeof(AvisoInteraccionUI).GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Aviso);
            Exigir(panel.Q<Label>().style.color.value == (Color)new Color32(110, 190, 193, 255), "Aviso celeste #6EBEC1");
            yield return PulsarF();
            Exigir(UIManagerNotas.Instance.NotaActiva && nota.YaLeida && !Aviso.Visible, "F abre y registra lectura sin dejar prompt");
            Canvas.ForceUpdateCanvases();
            var cuerpo = UIManagerNotas.Instance.panelNota.GetComponentsInChildren<TextMeshProUGUI>().First(t => t.name == "Contenido"); cuerpo.ForceMeshUpdate();
            Exigir(!cuerpo.isTextOverflowing && cuerpo.text.Contains("oprime F"), "Tutorial completo cabe en pantalla y muestra la tecla actual");
            if (personaje == 0) { ScreenCapture.CaptureScreenshot(Carpeta + "tutorial.png"); yield return .15f; }
            yield return PulsarF();
            Exigir(!UIManagerNotas.Instance.NotaActiva && Interaccion.currentInteractable == null && !Aviso.Visible, "F cierra sin reabrir al soltar ni conservar referencia o prompt");
            Mover((Vector2)nota.transform.position + Vector2.right * 4); yield return .2f;
            Exigir(Interaccion.NotaCercana == null && Interaccion.currentInteractable == null && !Aviso.Visible, "Salir de nota limpia contacto y aviso");
            Acercar(nota.GetComponent<Collider2D>()); yield return .2f;
            Exigir(Interaccion.NotaCercana == nota, "Salir y volver permite releer");
            GestorModoEntrada.EstablecerModo(ModoEntrada.Automatico);
            InputSystem.QueueStateEvent(mando, new GamepadState { rightStick = Vector2.right }); yield return .25f;
            Exigir(GestorModoEntrada.UsandoMando && Aviso.Mensaje.Contains(" A "), "Cambio automático a mando actualiza prompt a A (modo=" + GestorModoEntrada.ModoActual + ", aviso=" + Aviso.Mensaje + ")");
            InputSystem.QueueStateEvent(mando, new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.South)); yield return .15f;
            InputSystem.QueueStateEvent(mando, new GamepadState { rightStick = Vector2.right }); yield return .15f;
            Exigir(UIManagerNotas.Instance.NotaActiva && cuerpo.text.Contains("oprime A"), "Botón A abre nota y actualiza documento (activa=" + UIManagerNotas.Instance.NotaActiva + ", objetivo=" + Interaccion.NotaCercana + ", esquema=" + Jugador.GetComponent<PlayerInput>().currentControlScheme + ", texto=" + TextoInteraccion.Boton() + ")");
            InputSystem.QueueStateEvent(mando, new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.South)); yield return .15f;
            InputSystem.QueueStateEvent(mando, new GamepadState()); yield return .15f;
            Exigir(!UIManagerNotas.Instance.NotaActiva && Interaccion.currentInteractable == null, "Botón A cierra y limpia referencia");
            Tecla(Key.LeftShift); yield return .15f; Tecla(); yield return .15f;
            Exigir(!GestorModoEntrada.UsandoMando && TextoInteraccion.Boton() == "F", "Cambio automático a teclado restaura F");
            GestorModoEntrada.EstablecerModo(ModoEntrada.TecladoYRaton); Preparar();
            var puerta = PuertaNivel.Activas.First(); Acercar(puerta.GetComponents<Collider2D>().First(c => c.isTrigger));
            Jugador.GetComponent<PlayerInventory>().llavesCount = 0; yield return .2f; yield return PulsarF();
            Exigir(!UIManagerNotas.Instance.NotaActiva && Aviso.Objetivo == puerta && puerta.AvisoVisible && Aviso.Mensaje == TextoInteraccion.Formatear(puerta.mensajeFaltaLlave), "Tras cerrar nota, F interactúa con puerta y reemplaza aviso por falta de tarjeta");
            var botiquin = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BotiquinInteractuable.prefab"), new Vector3(480, 480), Quaternion.identity).GetComponent<BotiquinInteractable>();
            Acercar(botiquin.GetComponents<Collider2D>().First(c => c.isTrigger)); yield return .2f; yield return PulsarF();
            Exigir(botiquin.YaAbierto && !UIManagerNotas.Instance.NotaActiva, "Tras cerrar nota, F abre botiquín sin reabrir documento");
            yield return ProbarInventario(personaje);
        }
        yield return ProbarColisiones();
    }
    static IEnumerator ProbarInventario(int personaje)
    {
        var inv = Jugador.GetComponent<PlayerInventory>(); var salud = Jugador.GetComponent<PlayerHealth>();
        Mover(new Vector2(500, 500)); yield return .15f;
        inv.vendasCount = 19; Exigir(inv.IntentarAgregarObjeto(ItemPickup.PickupType.Venda) && inv.vendasCount == 20, "Jugador " + personaje + ": venda alcanza máximo 20");
        Exigir(!inv.IntentarAgregarObjeto(ItemPickup.PickupType.Venda) && inv.vendasCount == 20, "Rechaza venda 21");
        inv.estimulantesCount = 9; Exigir(inv.IntentarAgregarObjeto(ItemPickup.PickupType.Estimulante) && inv.estimulantesCount == 10, "Jeringas alcanzan máximo 10");
        Exigir(!inv.IntentarAgregarObjeto(ItemPickup.PickupType.Estimulante) && inv.estimulantesCount == 10, "Rechaza jeringa 11");
        var campo = typeof(PantallaControlesInicio).GetField("avisoPuerta", BindingFlags.Instance | BindingFlags.NonPublic);
        Exigir(((string)campo.GetValue(Object.FindFirstObjectByType<PantallaControlesInicio>())).Contains("JERINGAS AL MÁXIMO"), "Aviso informa límite de jeringas incluso después de límite de vendas");
        var drop = new GameObject("VendaPrueba"); drop.tag = "Drop"; drop.transform.position = Jugador.GetComponent<Collider2D>().bounds.center;
        var c = drop.AddComponent<CircleCollider2D>(); c.isTrigger = true; c.radius = .3f; var pickup = drop.AddComponent<ItemPickup>(); pickup.tipoDeObjeto = ItemPickup.PickupType.Venda;
        yield return .2f; Exigir(pickup != null && inv.vendasCount == 20, "Venda rechazada permanece en suelo");
        salud.currentHealth = 50; Tecla(Key.Digit1); yield return .1f; Tecla(); yield return .1f;
        Exigir(salud.currentHealth == 60 && inv.vendasCount == 20 && pickup == null, "Tecla 1 cura y permite recoger venda pendiente con OnTriggerStay");
        var color = inv.FeedbackVendaActual;
        Exigir(color.a > 0 && color.g > color.r && color.g > color.b, "Consumir venda produce destello verde");
        ScreenCapture.CaptureScreenshot(Carpeta + "destello-venda-" + personaje + ".png");
        yield return .8f; Exigir(inv.FeedbackVendaActual.a == 0, "Destello verde termina sin quedar fijo");
        inv.vendasCount = 100; inv.estimulantesCount = 100; inv.ActualizarUI();
        Exigir(inv.vendasCount == 20 && inv.estimulantesCount == 10, "Restaurar cantidades antiguas respeta límites");
    }
    static GameObject Proyectil(Type tipo, Vector2 p, Vector2 velocidad)
    {
        var go = new GameObject("BalaQA"); go.transform.position = p;
        var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<CircleCollider2D>().isTrigger = true; go.GetComponent<CircleCollider2D>().radius = .04f;
        go.AddComponent(tipo); rb.linearVelocity = velocidad;
        if (tipo == typeof(BossBullet)) { go.transform.rotation = Quaternion.Euler(0, 0, -90); go.GetComponent<BossBullet>().speed = velocidad.magnitude; }
        return go;
    }
    static IEnumerator ProbarColisiones()
    {
        var centro = new Vector2(600, 600); Mover(centro - Vector2.right * 10);
        var notaGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Nota.prefab"), centro, Quaternion.identity);
        foreach (var tipo in new[] { typeof(Bullet), typeof(EnemyBullet), typeof(BossBullet) })
        {
            var b = Proyectil(tipo, centro - Vector2.right, Vector2.right * 5); yield return .5f;
            Exigir(b != null && b.transform.position.x > centro.x + .5f, tipo.Name + " atraviesa nota usando física real"); Object.Destroy(b);
        }
        var enemigoGo = new GameObject("SorboPrueba"); enemigoGo.transform.position = centro + Vector2.right * .65f; enemigoGo.layer = LayerMask.NameToLayer("Enemy");
        if (enemigoGo.layer < 0) enemigoGo.layer = 6;
        enemigoGo.AddComponent<BoxCollider2D>().size = Vector2.one * .35f; var enemigo = enemigoGo.AddComponent<EnemyHealth>(); enemigo.maxHealth = 200;
        var pared = new GameObject("MuroPrueba"); pared.transform.position = centro; pared.AddComponent<BoxCollider2D>().size = new Vector2(.15f, 2);
        foreach (var tipo in new[] { typeof(Bullet), typeof(EnemyBullet), typeof(BossBullet) })
        {
            var bala = Proyectil(tipo, centro - Vector2.right, Vector2.right * 5); yield return .4f;
            Exigir(bala == null, tipo.Name + " sigue chocando con paredes sólidas");
        }
        Mover(centro - Vector2.right * .65f); yield return .2f;
        var combate = Jugador.GetComponent<PlayerCombat>(); var arma = Jugador.GetComponent<ArmaIndependiente>(); arma.enabled = false;
        // Colocar deliberadamente el punto de ataque al otro lado del muro: la línea debe partir del jugador.
        var original = combate.attackPoint; var punto = new GameObject("PuntoAtaqueQA"); punto.transform.position = enemigoGo.transform.position;
        combate.attackPoint = punto.transform; combate.enemyLayers = ~0; Physics2D.SyncTransforms(); float vida = enemigo.CurrentHealth;
        Invocar(combate, "Attack"); yield return .1f; Exigir(enemigo.CurrentHealth == vida, "Machete no daña a través de pared incluso con attackPoint al otro lado");
        Object.Destroy(pared); yield return .1f; Physics2D.SyncTransforms(); Invocar(combate, "Attack"); yield return .1f;
        Exigir(enemigo.CurrentHealth == vida - combate.attackDamage, "Machete daña una vez sin obstáculo");
        combate.attackPoint = original; Object.Destroy(punto); Object.Destroy(notaGo); Object.Destroy(enemigoGo);
        SceneManager.LoadScene("Escenario4_Jefe"); yield return 1f; Preparar();
        var comic = Object.FindFirstObjectByType<ComicFinal>();
        double limite = EditorApplication.timeSinceStartup + 6;
        while ((TransicionEntrePisos.EnCurso || comic.TriggerProximidad == null) && EditorApplication.timeSinceStartup < limite) yield return .1f;
        comic.NotificarProximidad(); yield return 1f; comic.Saltar();
        limite = EditorApplication.timeSinceStartup + 6;
        while ((!comic.ReencuentroTerminado || ComicFinal.EnCurso) && EditorApplication.timeSinceStartup < limite) yield return .1f;
        Exigir(comic.ReencuentroTerminado && !ComicFinal.EnCurso, "Combate de Yiba desbloqueado después del reencuentro");
        var jefe = Object.FindFirstObjectByType<BossHealth>(); jefe.GetComponent<BossAI>().enabled = false;
        jefe.GetComponent<AnimacionJefeDinamica>().TerminarAccion(); yield return .2f;
        // Mantener un fotograma estable para que los disparos de prueba apunten a pies/cadera/cabeza reales,
        // no al margen transparente de un fotograma de rugido que cambia antes de llegar la bala.
        jefe.GetComponent<AnimacionJefeDinamica>().enabled = false;
        jefe.GetComponent<Laboratorio4.Animacion.AnimadorSprites8Direcciones>().enabled = false;
        jefe.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; jefe.transform.position = new Vector2(650, 650); jefe.GetComponent<Rigidbody2D>().position = jefe.transform.position;
        var ayudante = Object.FindFirstObjectByType<AyudanteJefeAI>(); if (ayudante != null) ayudante.gameObject.SetActive(false);
        var pies = jefe.GetComponent<BoxCollider2D>(); var h = jefe.GetComponentInChildren<HurtboxJefe>(); h.Ajustar(); Physics2D.SyncTransforms();
        Exigir(!pies.isTrigger && h.GetComponent<BoxCollider2D>().isTrigger && h.transform.parent == jefe.transform && pies.size == new Vector2(.6f, .32f), "Yiba conserva collider de pies y hurtbox independiente");
        var limites = h.GetComponent<Collider2D>().bounds;
        Exigir(limites.size.y > pies.bounds.size.y * 2, "Hurtbox cubre cuerpo completo por encima de los pies");
        Mover(new Vector2(640, 645)); yield return .2f;
        foreach (float altura in new[] { .12f, .5f, .85f })
        {
            float vidaAntes = jefe.CurrentHealth; var p = new Vector2(limites.min.x - .5f, Mathf.Lerp(limites.min.y, limites.max.y, altura));
            var b = Proyectil(typeof(Bullet), p, Vector2.right * 6); yield return .5f;
            Exigir(jefe.CurrentHealth == vidaAntes - 15 && b == null, "Yiba recibe exactamente un impacto a altura " + altura + " (vida=" + jefe.CurrentHealth + ", anterior=" + vidaAntes + ", bala=" + (b != null ? b.transform.position.ToString() : "destruida") + ", origen=" + p + ", tiempo=" + Time.timeScale + ", simulando=" + jefe.GetComponent<Rigidbody2D>().simulated + ", hurtboxActiva=" + h.GetComponent<Collider2D>().enabled + ", espera=" + ComicFinal.EsperaCombate(jefe) + ", posición=" + jefe.transform.position + ", hurtbox=" + h.GetComponent<Collider2D>().bounds + ")");
        }
    }
}
