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
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarArmasInteraccion
{
    const string Clave = "L4.ArmasInteraccion.QA", Carpeta = "Reports/ArmasInteraccion/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina; static double siguiente, inicio; static bool segundoPlano; static Keyboard teclado; static Mouse mouse;
    static GameObject Jugador => LevelManager.Instance.JugadorActual;
    static AvisoInteraccionUI Aviso => Jugador.GetComponent<AvisoInteraccionUI>();
    static ProbarArmasInteraccion()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Partida/Probar arma persistente y avisos F")]
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
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
            { SessionState.SetBool(Clave + key + ".existe", PlayerPrefs.HasKey(key)); SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key)); }
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; QuitarInput(); Time.timeScale = 1; Application.runInBackground = segundoPlano;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
                if (SessionState.GetBool(Clave + key + ".existe", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0)); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false); SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null; RankingManager.SuspenderSincronizacionAutomatica = false;
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[ArmasInteraccionQA] " + resultados.Count + " comprobaciones; " + errores.Count + " errores.");
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
    static void Exigir(bool ok, string texto) { if (!ok) throw new Exception(texto); resultados.Add("PASS " + texto); }
    static void QuitarInput()
    {
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse); teclado = null; mouse = null;
    }
    static void Preparar()
    {
        QuitarInput(); teclado = InputSystem.AddDevice<Keyboard>("ArmasInteraccionKeyboard"); mouse = InputSystem.AddDevice<Mouse>("ArmasInteraccionMouse");
        var input = Jugador.GetComponent<PlayerInput>(); input.neverAutoSwitchControlSchemes = true; input.SwitchCurrentControlScheme("Keyboard&Mouse", teclado, mouse);
        Jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
        foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
        foreach (var s in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) s.gameObject.SetActive(false);
        foreach (var s in Object.FindObjectsByType<NotaInteractable>(FindObjectsSortMode.None)) s.gameObject.SetActive(false);
        foreach (var s in Object.FindObjectsByType<BotiquinInteractable>(FindObjectsSortMode.None)) s.gameObject.SetActive(false);
    }
    static void Tecla(params Key[] keys) => InputSystem.QueueStateEvent(teclado, new KeyboardState(keys));
    static void Mover(Vector3 posicion)
    {
        Jugador.transform.position = posicion; var rb = Jugador.GetComponent<Rigidbody2D>(); rb.position = posicion; rb.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
    }
    static void Acercar(Collider2D trigger)
    {
        Mover(Jugador.transform.position + trigger.bounds.center - Jugador.GetComponent<Collider2D>().bounds.center);
    }
    static void Arma(bool melee, string detalle)
    {
        var w = Jugador.GetComponent<WeaponManager>();
        Exigir(w.isUsingMelee == melee && w.meleeWeapon.enabled == melee && w.rangedWeapon.enabled != melee &&
            (melee ? w.macheteIcon : w.subfusilIcon).activeInHierarchy &&
            (melee ? w.macheteIcon : w.subfusilIcon).GetComponent<Image>().sprite == (melee ? w.spriteMacheteHUD : w.spriteSubfusilHUD), detalle);
    }
    static void Capturar(string nombre) => ScreenCapture.CaptureScreenshot(Carpeta + nombre + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
    static IEnumerator Recorrido()
    {
        yield return .3f;
        Exigir(!JsonUtility.FromJson<SaveData>("{\"version\":1}").armaCuerpoACuerpo, "Guardado antiguo sin campo de arma conserva subfusil predeterminado");
        for (int personaje = 0; personaje < 2; personaje++)
        {
            GameManager.Instance.IniciarNuevaRun(personaje); SceneManager.LoadScene("Recepcion"); yield return .8f;
            var controles = Object.FindFirstObjectByType<PantallaControlesInicio>();
            double limite = EditorApplication.timeSinceStartup + 8;
            while (PantallaControlesInicio.BloqueaJuego && EditorApplication.timeSinceStartup < limite) { controles.Cerrar(); yield return .15f; }
            Preparar(); Exigir(!Aviso.Visible, "Personaje " + personaje + ": sin aviso lejos de interactuables");
            for (int piso = 0; piso < 3; piso++)
            {
                if (piso > 0) Preparar();
                var w = Jugador.GetComponent<WeaponManager>();
                if (piso == 0) w.EquipWeapon(true);
                Arma(piso != 2, "Personaje " + personaje + ": arma y HUD correctos al llegar a " + SceneManager.GetActiveScene().name);
                if (piso == 2) break;
                var puerta = PuertaNivel.Activas.First(p => Application.CanStreamedLevelBeLoaded(p.nombreSiguienteEscena));
                string destino = puerta.nombreSiguienteEscena;
                Acercar(puerta.GetComponents<Collider2D>().First(c => c.isTrigger)); yield return .25f;
                Exigir(puerta.JugadorEnRango && Aviso.Visible && Aviso.Objetivo == puerta && Aviso.Mensaje.Contains("F"), "Aviso F de puerta en " + SceneManager.GetActiveScene().name);
                float duracionOriginal = puerta.duracionMensaje; puerta.duracionMensaje = .6f;
                Jugador.GetComponent<PlayerInventory>().llavesCount = 0;
                Tecla(Key.F); yield return .08f; Tecla(); yield return .08f;
                Exigir(Aviso.Visible && Aviso.Mensaje == puerta.mensajeFaltaLlave && puerta.AvisoVisible,
                    "F sin tarjeta reemplaza el texto en el mismo aviso de interacción");
                Exigir(!Object.FindObjectsByType<AvisoPuertaUI>(FindObjectsSortMode.None).Any(a => a.Visible) &&
                    (controles == null || !controles.AvisoPuertaVisible), "No aparece otro aviso de tarjeta debajo");
                Capturar("sin-tarjeta-" + personaje + "-" + piso); yield return .65f;
                Exigir(Aviso.Visible && Aviso.Mensaje.Contains("Oprime F"), "Al expirar vuelve la indicación F en el mismo panel");
                puerta.duracionMensaje = duracionOriginal;
                var pausa = Object.FindFirstObjectByType<PauseMenuController>(); pausa.Pause(); yield return .12f;
                Exigir(!Aviso.Visible, "Aviso oculto al pausar"); pausa.Resume(); yield return .12f;
                Exigir(Aviso.Visible, "Aviso vuelve al reanudar");
                Capturar("puerta-" + personaje + "-" + piso); yield return .06f;
                Mover(Jugador.transform.position + Vector3.up * 15); yield return .15f;
                Exigir(!Aviso.Visible && !puerta.JugadorEnRango, "Aviso de puerta desaparece al salir de rango");
                Acercar(puerta.GetComponents<Collider2D>().First(c => c.isTrigger)); yield return .15f;
                if (piso == 1) w.EquipWeapon(false);
                Jugador.GetComponent<PlayerInventory>().llavesCount = 1;
                var salud = Jugador.GetComponent<PlayerHealth>(); salud.RestaurarVida(63, 100);
                Tecla(Key.F); yield return .09f; Tecla();
                limite = EditorApplication.timeSinceStartup + 10;
                while ((SceneManager.GetActiveScene().name != destino || TransicionEntrePisos.EnCurso) && EditorApplication.timeSinceStartup < limite) yield return .1f;
                yield return .2f;
                Exigir(SceneManager.GetActiveScene().name == destino && !TransicionEntrePisos.EnCurso, "F con tarjeta cambia realmente de piso");
                Exigir(Mathf.Approximately(Jugador.GetComponent<PlayerHealth>().currentHealth, 63), "Persistencia de vida sin regresión");
            }
            // Instancias reales de los prefabs; sólo se mueven/ajustan en Play Mode, nunca se guardan escenas.
            var nota = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Nota.prefab"), Jugador.transform.position + Vector3.right * 4, Quaternion.identity).GetComponent<NotaInteractable>();
            nota.spriteNotaAltaResolucion = wSprite();
            Acercar(nota.GetComponent<Collider2D>()); yield return .25f;
            Exigir(Aviso.Visible && Aviso.Objetivo == nota && Aviso.Mensaje.Contains("leer"), "Personaje " + personaje + ": aviso F de nota");
            Capturar("nota-" + personaje); yield return .06f;
            Tecla(Key.F); yield return .08f; Tecla(); yield return .08f;
            Exigir(UIManagerNotas.Instance.NotaActiva && !Aviso.Visible, "F abre nota y oculta prompt durante lectura");
            Tecla(Key.F); yield return .08f; Tecla(); yield return .08f;
            Exigir(!UIManagerNotas.Instance.NotaActiva && !Aviso.Visible && Jugador.GetComponent<InteraccionJugador>().currentInteractable == null, "F cierra nota y libera prompt y referencia de interacción");
            Mover(Jugador.transform.position + Vector3.up * 12); yield return .15f;
            Exigir(!Aviso.Visible, "Aviso de nota desaparece al alejarse"); Object.Destroy(nota.gameObject);
            var bot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BotiquinInteractuable.prefab"), Jugador.transform.position + Vector3.right * 4, Quaternion.identity).GetComponent<BotiquinInteractable>();
            bot.objetoASoltar = null;
            Acercar(bot.GetComponents<Collider2D>().First(c => c.isTrigger)); yield return .25f;
            Exigir(Aviso.Visible && Aviso.Objetivo == bot && Aviso.Mensaje.Contains("botiquín"), "Personaje " + personaje + ": aviso F de botiquín cerrado");
            Capturar("botiquin-" + personaje); yield return .06f;
            Tecla(Key.F); yield return .08f; Tecla(); yield return .08f;
            Exigir(bot.YaAbierto && !Aviso.Visible, "F abre contenedor y retira aviso del botiquín ya abierto");
            Object.Destroy(bot.gameObject);
            Jugador.GetComponent<WeaponManager>().EquipWeapon(true);
            Exigir(GameManager.Instance.GuardarRun(), "Guardar partida conserva arma equipada");
            Exigir(SaveSystem.Cargar().armaCuerpoACuerpo, "Archivo cifrado contiene machete equipado");
            Exigir(GameManager.Instance.CargarRunGuardada(), "Continuar lee guardado de prueba");
            SceneManager.LoadScene(GameManager.Instance.DatosCargados.escenaActual); yield return .8f; Preparar();
            Arma(true, "Personaje " + personaje + ": continuar restaura machete y HUD");
            GameManager.Instance.IniciarNuevaRun(personaje);
            Exigir(!GameManager.Instance.usarDatosDeTransicion && !GameManager.Instance.armaCuerpoACuerpoPendiente, "Nueva partida limpia arma de transición anterior");
        }
        Exigir(errores.Count == 0, "Mecánicas y avisos sin errores ni excepciones");
    }
    static Sprite wSprite() => Jugador.GetComponent<WeaponManager>().spriteMacheteHUD;
}
