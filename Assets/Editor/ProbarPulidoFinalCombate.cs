using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarPulidoFinalCombate
{
    const string Clave = "L4.PulidoCombate.QA", Carpeta = "Reports/PulidoFinalCombate/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina; static double siguiente, inicio; static bool segundoPlano;
    static GameObject Jugador => LevelManager.Instance.JugadorActual;
    static ProbarPulidoFinalCombate()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, p, t) => { if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m); };
    }
    [MenuItem("Laboratorio 4/Pulido/Probar machete munición y anuncios %&F6")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying) return;
        IntegrarPulidoFinalCombate.Integrar(); Directory.CreateDirectory(Carpeta);
        SessionState.SetString(Clave + ".escena", SceneManager.GetActiveScene().path); SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    static void Estado(PlayModeStateChange e)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (e == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear(); segundoPlano = Application.runInBackground; Application.runInBackground = true;
            foreach (var k in new[] { "SelectedCharacter", "MejorPuntuacion", GestorModoEntrada.ClavePreferencia })
            { SessionState.SetBool(Clave + k + ".existe", PlayerPrefs.HasKey(k)); SessionState.SetInt(Clave + k, PlayerPrefs.GetInt(k)); }
            GestorModoEntrada.EstablecerModo(ModoEntrada.TecladoYRaton);
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-qa.dat"); RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-qa.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true; inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Aplanar(Recorrido());
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; Time.timeScale = 1; Application.runInBackground = segundoPlano;
            GestorModoEntrada.EstablecerModo((ModoEntrada)SessionState.GetInt(Clave + GestorModoEntrada.ClavePreferencia, 0));
            foreach (var k in new[] { "SelectedCharacter", "MejorPuntuacion", GestorModoEntrada.ClavePreferencia })
                if (SessionState.GetBool(Clave + k + ".existe", false)) PlayerPrefs.SetInt(k, SessionState.GetInt(Clave + k, 0)); else PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
        }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false); SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null; RankingManager.SuspenderSincronizacionAutomatica = false;
            File.WriteAllLines(Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[PulidoCombateQA] " + resultados.Count + " comprobaciones; " + errores.Count + " errores.");
            EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 180) throw new Exception("Tiempo agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .03f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static IEnumerator Aplanar(IEnumerator r)
    { while (r.MoveNext()) { if (r.Current is IEnumerator sub) { var a = Aplanar(sub); while (a.MoveNext()) yield return a.Current; } else yield return r.Current; } }
    static void Exigir(bool b, string m) { if (!b) throw new Exception(m); resultados.Add("PASS " + m); }
    static void Preparar()
    {
        Jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
        foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
        foreach (var s in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) s.gameObject.SetActive(false);
    }
    static void Mover(Vector2 p)
    { Jugador.transform.position = p; Jugador.GetComponent<Rigidbody2D>().position = p; Jugador.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; Physics2D.SyncTransforms(); }
    static IEnumerator Recorrido()
    {
        yield return .3f;
        for (int personaje = 0; personaje < 2; personaje++)
        {
            GameManager.Instance.IniciarNuevaRun(personaje); SceneManager.LoadScene("Recepcion"); yield return .8f;
            var controles = Object.FindFirstObjectByType<PantallaControlesInicio>();
            while (PantallaControlesInicio.BloqueaJuego) { controles.Cerrar(); yield return .15f; }
            Preparar();
            Exigir(Object.FindFirstObjectByType<AnuncioPisoUI>() == null, "Recepción conserva su interfaz de controles sin banner duplicado");
            var arma = Jugador.GetComponent<ArmaIndependiente>(); arma.controlManual = true;
            Jugador.GetComponent<WeaponManager>().EquipWeapon(true);
            Exigir(arma.posicionManoMachete == Vector2.zero, "Personaje " + personaje + ": prefab centra el mango en cadera");
            var centro = Jugador.transform.TransformPoint(arma.cadera);
            foreach (float tajo in new[] { 0f, -60f, -120f }) for (int angulo = 0; angulo < 360; angulo += 10)
            {
                arma.AnguloTajo = tajo; arma.Apuntar((Vector2)centro + new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad)) * 5);
                var punto = (Vector2.Scale(arma.agarreMachete, arma.visual.sprite.rect.size) - arma.visual.sprite.pivot) / arma.visual.sprite.pixelsPerUnit;
                if (arma.visual.flipY) punto.y = -punto.y;
                Exigir(Vector2.Distance(arma.visual.transform.TransformPoint(punto), centro) < .0001f, "Personaje " + personaje + ": mango fijo a " + angulo + "°, tajo " + tajo);
            }
            arma.AnguloTajo = 0; arma.Apuntar((Vector2)centro + Vector2.down * 5);
            ScreenCapture.CaptureScreenshot(Carpeta + "machete-sur-" + personaje + ".png"); yield return .15f;
            Jugador.GetComponent<WeaponManager>().EquipWeapon(false); arma.Apuntar((Vector2)centro + Vector2.right * 5);
            Exigir((Vector2)arma.pivote.localPosition == arma.cadera, "Subfusil conserva su altura original");
            Jugador.GetComponent<WeaponManager>().EquipWeapon(true);
            var inv = Jugador.GetComponent<PlayerInventory>(); var disparo = Jugador.GetComponent<PlayerShooting>();
            var grupoOriginal = inv.audioSource.outputAudioMixerGroup; inv.audioSource.Stop(); disparo.totalAmmo = 17;
            var drop = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ammo.prefab"), Jugador.GetComponent<Collider2D>().bounds.center, Quaternion.identity).GetComponent<AmmoDrop>();
            Exigir(drop.sonidoRecogida != null && grupoOriginal != null && grupoOriginal.name == "SFX", "Munición usa clip existente y ruta de mezcla SFX");
            drop.minAmmo = drop.maxAmmo = 4;
            double limite = EditorApplication.timeSinceStartup + 2;
            while (drop != null && EditorApplication.timeSinceStartup < limite) yield return .01f;
            Exigir(drop == null && disparo.totalAmmo == 21, "Recogida física de munición funciona con machete equipado y suma una sola vez");
            Exigir(inv.audioSource.isPlaying && inv.audioSource.outputAudioMixerGroup == grupoOriginal, "Sonido sigue reproduciéndose desde el jugador después de destruir drop");
            yield return Golpes();
        }
        foreach (var escena in new[] { "Escenario1_Entrada", "Piso3_V10", "E3_Laboratorio", "Escenario4_Jefe" })
        {
            Exigir(TransicionEntrePisos.CargarEscena(escena), "Inicia transición real a " + escena);
            double limite = EditorApplication.timeSinceStartup + 12;
            while (TransicionEntrePisos.EnCurso && EditorApplication.timeSinceStartup < limite)
            {
                var actual = Object.FindFirstObjectByType<AnuncioPisoUI>();
                if (actual != null) Exigir(!actual.Visible, "Banner espera mientras la transición cubre el piso");
                yield return .1f;
            }
            Preparar(); yield return .65f;
            var banner = Object.FindFirstObjectByType<AnuncioPisoUI>();
            Exigir(banner != null && banner.Visible && !banner.GetComponent<CanvasGroup>().blocksRaycasts && Time.timeScale == 1, escena + ": anuncio visible sin bloquear juego ni botones");
            Exigir(banner.Titulo == new[] { "", "PRIMER PISO", "SEGUNDO PISO", "TERCER PISO", "CUARTO PISO" }[AnuncioPisoUI.NumeroPiso(escena)], "Título correcto para orden real de progresión");
            ScreenCapture.CaptureScreenshot(Carpeta + "anuncio-" + escena + ".png"); yield return 3f;
            Exigir(banner == null, "Banner se desvanece y retira al terminar");
        }
    }
    static IEnumerator Golpes()
    {
        Mover(new Vector2(500, 500)); var combate = Jugador.GetComponent<PlayerCombat>();
        var punto = new GameObject("AtaqueQA"); var anterior = combate.attackPoint; var capas = combate.enemyLayers;
        combate.attackPoint = punto.transform; combate.enemyLayers = ~0;
        var enemigoGo = new GameObject("SorboQA"); var caja = enemigoGo.AddComponent<BoxCollider2D>(); caja.size = Vector2.one;
        var enemigo = enemigoGo.AddComponent<EnemyHealth>(); enemigo.maxHealth = 2000;
        var muro = new GameObject("MuroQA"); var pared = muro.AddComponent<BoxCollider2D>();
        yield return .1f;
        // Pared tras el enemigo, tangente y con ligera penetración: sólo el centro quedaría tapado.
        foreach (float penetracion in new[] { 0f, .5f, .52f, .62f })
        {
            var origen = (Vector2)Jugador.GetComponent<Collider2D>().bounds.center;
            enemigoGo.transform.position = origen + Vector2.right * .9f; punto.transform.position = enemigoGo.transform.position;
            muro.transform.position = origen + Vector2.right * (1.9f - penetracion); pared.size = new Vector2(1f, 4);
            Physics2D.SyncTransforms(); float vida = enemigo.CurrentHealth;
            typeof(PlayerCombat).GetMethod("Attack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combate, null);
            Exigir(enemigo.CurrentHealth == vida - combate.attackDamage, "Machete daña Sorbo contra pared; penetración=" + penetracion);
        }
        var inicio = (Vector2)Jugador.GetComponent<Collider2D>().bounds.center;
        foreach (var direccion in new[] { Vector2.right, Vector2.up, new Vector2(1, 1).normalized })
        {
            enemigoGo.transform.position = inicio + direccion * 1.8f; punto.transform.position = enemigoGo.transform.position;
            muro.transform.position = inicio + direccion * .9f; muro.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg);
            pared.size = new Vector2(.12f, 4); Physics2D.SyncTransforms(); float vida = enemigo.CurrentHealth;
            typeof(PlayerCombat).GetMethod("Attack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combate, null);
            Exigir(enemigo.CurrentHealth == vida, "Machete bloqueado por muro frontal en dirección " + direccion);
        }
        combate.attackPoint = anterior; combate.enemyLayers = capas; Object.Destroy(punto); Object.Destroy(enemigoGo); Object.Destroy(muro);
    }
}
