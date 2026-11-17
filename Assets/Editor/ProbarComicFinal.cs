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
public static class ProbarComicFinal
{
    private const string Clave = "L4.ComicFinal.QA", Carpeta = "Reports/ComicFinal/";
    private static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    private static IEnumerator rutina;
    private static double siguiente, inicio;
    private static Keyboard teclado;
    private static Mouse mouse;
    private static bool segundoPlano, audioPausa;
    private static float volumen;
    private static int victorias;
    private static RunSummary resumen;
    private static ComicFinal Comic => Object.FindFirstObjectByType<ComicFinal>();
    static ProbarComicFinal()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (mensaje, pila, tipo) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (tipo == LogType.Exception || tipo == LogType.Error)) errores.Add(mensaje);
        };
    }
    [MenuItem("Laboratorio 4/Cómic/Probar clímax desenlace y victoria")]
    public static void Probar() { Empezar(false); }
    public static void ProbarEnLote() { Empezar(true); }
    private static void Empezar(bool lote)
    {
        if (EditorApplication.isPlaying) throw new Exception("Finaliza Play antes de probar");
        if (!lote && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Carpeta);
        SessionState.SetString(Clave + ".escena", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Clave + ".lote", lote); SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.EnterPlaymode();
    }
    private static void Estado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear(); victorias = 0; GameManager.OnRunVictoria += Victoria;
            segundoPlano = Application.runInBackground; Application.runInBackground = true;
            volumen = AudioListener.volume; audioPausa = AudioListener.pause;
            string id = Guid.NewGuid().ToString("N");
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-" + id + ".dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-" + id + ".dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
            { SessionState.SetBool(Clave + key + ".existe", PlayerPrefs.HasKey(key)); SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key)); }
            SessionState.SetBool(Clave + ".nombreExiste", PlayerPrefs.HasKey("PlayerRankingName")); SessionState.SetString(Clave + ".nombre", PlayerPrefs.GetString("PlayerRankingName"));
            PlayerPrefs.SetString("PlayerRankingName", "ComicFinalQA");
            teclado = InputSystem.AddDevice<Keyboard>("ComicFinalKeyboard"); mouse = InputSystem.AddDevice<Mouse>("ComicFinalMouse");
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Aplanar(Recorrido());
        }
        if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; GameManager.OnRunVictoria -= Victoria;
            if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            Time.timeScale = 1; Application.runInBackground = segundoPlano;
            AudioListener.pause = audioPausa; AudioListener.volume = volumen;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
                if (SessionState.GetBool(Clave + key + ".existe", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0)); else PlayerPrefs.DeleteKey(key);
            if (SessionState.GetBool(Clave + ".nombreExiste", false)) PlayerPrefs.SetString("PlayerRankingName", SessionState.GetString(Clave + ".nombre", "")); else PlayerPrefs.DeleteKey("PlayerRankingName");
            PlayerPrefs.Save();
        }
        if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null; RankingManager.SuspenderSincronizacionAutomatica = false;
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[ComicFinalQA] " + resultados.Count + " comprobaciones; " + errores.Count + " errores.");
            if (SessionState.GetBool(Clave + ".lote", false)) EditorApplication.Exit(errores.Count == 0 ? 0 : 1);
            else EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    private static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 180) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .04f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    private static void Victoria(RunSummary summary) { resumen = summary; victorias++; }
    private static void Exigir(bool bien, string mensaje) { if (!bien) throw new Exception(mensaje); resultados.Add("PASS " + mensaje); }
    private static void Boton(bool pulsado) => InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, Screen.height * .5f), buttons = (ushort)(pulsado ? 1 : 0) });
    private static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    private static void Capturar(string nombre) => ScreenCapture.CaptureScreenshot(Carpeta + nombre + ".png");
    private static IEnumerator Aplanar(IEnumerator origen)
    {
        while (origen.MoveNext())
        {
            if (origen.Current is IEnumerator anidada)
            { var sub = Aplanar(anidada); while (sub.MoveNext()) yield return sub.Current; }
            else yield return origen.Current;
        }
    }
    private static IEnumerator EsperarFundido(ComicFinal comic, string mensaje)
    {
        float maxima = TransicionEntrePisos.Opacidad;
        double limite = EditorApplication.timeSinceStartup + 5;
        while (comic.EnTransicion && EditorApplication.timeSinceStartup < limite)
        { maxima = Mathf.Max(maxima, TransicionEntrePisos.Opacidad); yield return .02f; }
        Exigir(!comic.EnTransicion && !TransicionEntrePisos.EnCurso && maxima > .99f, mensaje + ": misma cortina llega a negro completo y vuelve a cero");
        yield return .08f;
    }
    private static IEnumerator Recorrido()
    {
        yield return .5f;
        Exigir(Comic == null && Object.FindFirstObjectByType<BossAI>() == null, "MainMenu sin cómic final ni jefe");
        for (int personaje = 0; personaje < 2; personaje++)
        {
            if (personaje > 0) { SceneManager.LoadScene("MainMenu"); yield return .4f; }
            var gm = GameManager.Instance; gm.IniciarNuevaRun(personaje); gm.pisoActual = 4; gm.puntuacionTotal = 140; gm.tiempoRun = 321;
            AudioListener.volume = .63f; AudioListener.pause = false;
            SceneManager.LoadScene("Escenario4_Jefe"); yield return .7f;
            var comic = Comic; var jugador = LevelManager.Instance.JugadorActual; var boss = comic.jefe;
            Exigir(comic != null && jugador != null && boss != null, "Referencias del mapa final completas, personaje " + personaje);
            Exigir(!comic.ReencuentroTerminado && !ComicFinal.EnCurso && Time.timeScale == 1, "El piso permite caminar antes de acercarse al compañero");
            Exigir(!comic.zonaReencuentro.Contains(jugador.transform.position), "Punto de aparición fuera del umbral del jefe");
            var ayudante = LevelManager.Instance.AyudanteActual;
            Exigir(ayudante != null && comic.zonaReencuentro.Contains(ayudante.transform.position) &&
                Vector2.Distance(ayudante.transform.position, comic.posicionAyudante) < .01f,
                "El otro protagonista espera junto al spawn sin disparar ni activar la viñeta");
            Exigir(Vector2.Distance(ayudante.transform.position, jugador.transform.position) < 3 && comic.TriggerProximidad != null && comic.TriggerProximidad.isTrigger,
                "Compañero cercano y CircleCollider2D Trigger real");
            var posicionJefe = boss.transform.position; float vidaJefe = boss.CurrentHealth;
            boss.TakeDamage(100); yield return .2f;
            Exigir(boss.CurrentHealth == vidaJefe && Vector3.Distance(posicionJefe, boss.transform.position) < .01f, "Sin combate ni daño al jefe antes del reencuentro");
            var rb = jugador.GetComponent<Rigidbody2D>(); rb.linearVelocity = Vector2.zero; rb.position = comic.zonaReencuentro.center;
            jugador.transform.position = comic.zonaReencuentro.center; Physics2D.SyncTransforms();
            Boton(true); yield return .18f;
            Exigir(ComicFinal.EnCurso && !comic.EsDesenlace && !comic.ReencuentroTerminado, "Cruzar el umbral abre una sola viñeta sin consumir el clic de entrada");
            Exigir(comic.EnTransicion && TransicionEntrePisos.Opacidad > 0 && comic.SpriteActual == null, "Proximidad oscurece primero el gameplay, sin salto directo al cómic");
            yield return EsperarFundido(comic, "Entrada al reencuentro");
            Exigir(comic.SpriteActual == (personaje == 0 ? comic.reencuentroJason : comic.reencuentroMilena), "Reencuentro de la ruta elegida " + personaje);
            Exigir(Time.timeScale == 0 && DeathScreenController.GameplayBlocked && PauseMenuController.PauseBlocked, "Lectura congela acción y bloquea controles de combate y pausa");
            Exigir(AudioListener.pause && AudioListener.volume == 0 && comic.GetComponentsInChildren<AudioSource>(true).Length == 0, "Cómic completamente silencioso y sin AudioSource");
            var textos = comic.GetComponentsInChildren<TextMeshProUGUI>();
            Exigir(textos.Length == 2 && textos.Any(t => t.text == "siguiente >>") && textos.Any(t => t.text.Contains("Esc")), "Sólo navegación inferior, sin títulos superiores");
            Capturar("A-reencuentro-" + personaje); Boton(false); yield return .1f;
            float tiempo = gm.tiempoRun; int municion = jugador.GetComponent<PlayerShooting>().currentAmmoInClip;
            Tecla(Key.R); yield return .15f; Tecla();
            Exigir(gm.tiempoRun == tiempo && jugador.GetComponent<PlayerShooting>().currentAmmoInClip == municion, "Sin tiempo de run ni disparos durante lectura");
            if (personaje == 0) Boton(true); else Tecla(Key.Escape);
            yield return .15f; Boton(false); Tecla();
            yield return EsperarFundido(comic, "Salida al combate");
            Exigir(comic.ReencuentroTerminado && !ComicFinal.EnCurso && Time.timeScale == 1, "Clic/Esc inician combate en el mismo mapa");
            Exigir(!AudioListener.pause && Mathf.Approximately(AudioListener.volume, .63f), "Se restaura el audio previo al combatir");
            var ai = boss.GetComponent<BossAI>(); var shake = Camera.main.GetComponent<SacudidaCamaraJefe>();
            Exigir(ai.RugidosIniciales == 1 && ai.audioSource.isPlaying && ai.UltimoSonido == ai.sfxRugidoInicio && ai.audioSource.volume > .1f,
                "Rugido audible desde la entrada, sobre AudioSource propio y grupo SFX");
            Exigir(shake != null && shake.EnCurso && shake.DesfaseActual.sqrMagnitude > .00001f, "Rugido y sacudida de cámara arrancan juntos");
            ai.IniciarCombateDramatico(); yield return 1.3f;
            Exigir(ai.RugidosIniciales == 1 && !shake.EnCurso && shake.DesfaseActual == Vector3.zero, "Inicio sólo una vez y sacudida finaliza sin dejar desplazamiento");
            Exigir(Vector3.Distance(posicionJefe, boss.transform.position) > .01f, "Dr. Yiba reanuda su IA después de la viñeta");
            if (personaje == 0) yield return ProbarMovimientoYAudio(boss, jugador);
            int anteriores = victorias;
            boss.TakeDamage(100000);
            Exigir(!gm.RunActiva && victorias == anteriores + 1 && resumen.puntuacion == 2140, "Victoria y puntuación originales registradas una vez al derrotar a Yiba");
            Exigir(comic.EnTransicion && comic.SpriteActual != comic.ultimasPalabras,
                "Muerte de Yiba inicia fundido antes del epílogo (transición=" + comic.EnTransicion + ", opacidad=" + TransicionEntrePisos.Opacidad + ")");
            yield return EsperarFundido(comic, "Entrada al desenlace");
            Exigir(ComicFinal.EnCurso && comic.EsDesenlace && comic.IndiceActual == 0 && comic.SpriteActual == comic.ultimasPalabras, "Derrota abre confesión antes de mostrar victoria");
            Exigir(!VictoryScreenController.IsShowing && Time.timeScale == 0 && AudioListener.volume == 0, "Victoria espera al desenlace y permanece silencioso");
            Capturar("B1-confesion-" + personaje);
            yield return .1f;
            if (personaje == 1) { Tecla(Key.Escape); yield return .15f; Tecla(); }
            else
            {
                Boton(true); yield return .18f; Boton(false);
                Exigir(comic.IndiceActual == 0 && comic.EnTransicion && TransicionEntrePisos.Opacidad > 0, "Siguiente oscurece antes de sustituir la viñeta");
                yield return EsperarFundido(comic, "Paso de confesión a escape");
                Exigir(comic.IndiceActual == 1 && comic.SpriteActual == comic.escape && !VictoryScreenController.IsShowing, "Clic muestra escape con refuerzos antes del resumen");
                Capturar("B2-escape"); yield return .12f;
                Boton(true); yield return .15f; Boton(false);
            }
            yield return EsperarFundido(comic, "Cierre hacia victoria");
            Exigir(!ComicFinal.EnCurso && VictoryScreenController.IsShowing && Time.timeScale == 0, "Último clic/Esc abre pantalla existente de Incursión Completada");
            var victory = Object.FindFirstObjectByType<VictoryScreenController>(); var root = victory.GetComponent<UIDocument>().rootVisualElement;
            Exigir(root.Q<Label>("TitleMain").text == "INCURSIÓN COMPLETADA" && root.Q<VisualElement>("DeathBackdrop").style.backgroundColor.value == Color.black,
                "Pantalla original de victoria sobre fondo negro");
            Exigir(root.Q<VisualElement>("DeathVignette").style.display.value == DisplayStyle.None && root.Q<VisualElement>("ScanBar").style.display.value == DisplayStyle.None,
                "Fondo negro sin tintes ni barridos verdes sobre el epílogo");
            Exigir(root.Q<Label>("BestValue").text.Length > 0 && victorias == anteriores + 1, "Resumen conserva estadísticas sin registrar otra victoria al cerrar cómic");
            Exigir(RankingManager.LoadRanking().pendingUploads.Count == personaje + 1, "Una entrada de ranking por run, sin enviar pruebas a la API");
            yield return 3f; Capturar("victoria-negro-" + personaje); yield return .15f;
        }
        // Abandonar una escena con una lectura activa no puede dejar la siguiente escena congelada o muda.
        SceneManager.LoadScene("MainMenu"); yield return .4f;
        GameManager.Instance.IniciarNuevaRun(0); SceneManager.LoadScene("Escenario4_Jefe"); yield return .6f;
        var c = Comic; LevelManager.Instance.JugadorActual.transform.position = c.zonaReencuentro.center; yield return .2f;
        Exigir(ComicFinal.EnCurso, "Nueva run reinicia el reencuentro");
        SceneManager.LoadScene("MainMenu"); yield return .4f;
        Exigir(Comic == null && !ComicFinal.EnCurso && !TransicionEntrePisos.EnCurso && Time.timeScale == 1 && !AudioListener.pause && AudioListener.volume > 0, "Salir durante fundido cancela la cortina y restaura tiempo y audio");
        Exigir(errores.Count == 0, "Recorrido sin errores ni excepciones");
    }
    private static IEnumerator ProbarMovimientoYAudio(BossHealth salud, GameObject jugador)
    {
        var ai = salud.GetComponent<BossAI>(); var nav = salud.GetComponent<NavegacionJefe>();
        var pies = salud.GetComponent<BoxCollider2D>(); var cuerpo = salud.GetComponent<Rigidbody2D>();
        Exigir(pies.size.y < .4f && pies.sharedMaterial != null && pies.sharedMaterial.friction == 0 && nav != null,
            "Pies compactos sin fricción y navegación con despeje propio");
        jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
        var aliado = LevelManager.Instance.AyudanteActual.GetComponent<AyudanteJefeAI>(); aliado.enabled = false;
        var reproducir = typeof(BossAI).GetMethod("ReproducirSFX", BindingFlags.NonPublic | BindingFlags.Instance);
        var mixer = ai.audioSource.outputAudioMixerGroup.audioMixer; mixer.GetFloat("SFXVolume", out float volumenSfx);
        mixer.SetFloat("SFXVolume", 0); float maxRms = 0;
        foreach (var clip in new[] { ai.sfxAtaquePesado, ai.sfxRecargaEnergia, ai.sfxDisparo, ai.sfxInvocacion })
        {
            reproducir.Invoke(ai, new object[] { clip }); yield return .25f;
            var muestras = new float[512]; ai.audioSource.GetOutputData(muestras, 0);
            maxRms = Mathf.Max(maxRms, Mathf.Sqrt(muestras.Sum(v => v * v) / muestras.Length));
            Exigir(ai.audioSource.isPlaying && ai.audioSource.volume > .1f && ai.UltimoSonido == clip && !ai.audioSource.mute,
                "SFX reproducido y audible: " + clip.name);
        }
        mixer.SetFloat("SFXVolume", volumenSfx);
        Exigir(maxRms > .00001f, "Las muestras de salida de Yiba contienen señal de audio real");
        foreach (string nombre in new[] { "E4_MuroCobertura_08", "E4_MuroCobertura_09" })
        {
            // Las coberturas son sprites; sus BoxCollider2D reales están centralizados en E4_ColisionMuros.
            var visual = GameObject.Find(nombre);
            var muro = GameObject.Find("E4_ColisionMuros").GetComponents<Collider2D>()
                .OrderBy(c => ((Vector2)c.bounds.center - (Vector2)visual.transform.position).sqrMagnitude).First();
            var limites = muro.bounds;
            Vector2 izquierda = new Vector2(limites.min.x - 1.1f, limites.center.y - .25f);
            Vector2 derecha = new Vector2(limites.max.x + 1.1f, limites.center.y - .25f);
            cuerpo.position = izquierda; salud.transform.position = izquierda;
            var rbJugador = jugador.GetComponent<Rigidbody2D>(); rbJugador.position = derecha; rbJugador.linearVelocity = Vector2.zero; jugador.transform.position = derecha;
            cuerpo.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
            Exigir(!nav.PasoLibre(izquierda, derecha) && nav.Calcular(izquierda, derecha), nombre + ": ruta alrededor de cobertura que bloquea línea recta");
            float rodeo = 0; bool penetro = false; double fin = EditorApplication.timeSinceStartup + 18;
            var registro = new List<string> { "x,y" };
            while (Vector2.Distance(cuerpo.position, derecha) > 1.9f && EditorApplication.timeSinceStartup < fin)
            {
                rodeo = Mathf.Max(rodeo, Mathf.Abs(cuerpo.position.y - izquierda.y));
                penetro |= pies.Distance(muro).isOverlapped;
                registro.Add(cuerpo.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + cuerpo.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                yield return .08f;
            }
            File.WriteAllLines("Reports/YibaFlujo/recorrido-" + nombre + ".csv", registro);
            Exigir(Vector2.Distance(cuerpo.position, derecha) < 1.9f && rodeo > .3f && !penetro,
                nombre + ": Yiba rodea la esquina y alcanza al jugador sin atravesar el muro");
        }
        ai.enabled = false;
        cuerpo.position = new Vector2(21.6f, -5.4f); salud.transform.position = cuerpo.position; cuerpo.linearVelocity = Vector2.zero;
        jugador.transform.position = new Vector2(23.6f, -7); jugador.GetComponent<Rigidbody2D>().position = jugador.transform.position;
        yield return .8f;
        var orden = salud.GetComponent<Laboratorio4.Escenario4.E4OrdenY>(); var sprite = salud.GetComponent<SpriteRenderer>();
        Exigir(orden != null && orden.dinamico && sprite.sortingOrder == Mathf.RoundToInt(-salud.transform.position.y * 100),
            "Yiba usa profundidad dinámica por sus pies, en lugar del orden cero");
        Exigir(sprite.sortingOrder > GameObject.Find("E4_Puerta_Salida_01").GetComponent<SpriteRenderer>().sortingOrder,
            "Cabeza y cuerpo se dibujan delante del muro/puerta superior");
        Capturar("Yiba-pared-superior"); yield return .08f; ai.enabled = true;
    }
}
