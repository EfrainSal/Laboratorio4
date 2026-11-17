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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarPulidoFinal
{
    const string Clave = "L4.PulidoFinal.QA", Carpeta = "Reports/PulidoFinal/";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina;
    static double siguiente, inicio;
    static bool segundoPlano;
    static GameObject Jugador => LevelManager.Instance.JugadorActual;
    static ProbarPulidoFinal()
    {
        EditorApplication.playModeStateChanged += Estado; EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Pulido/Probar audio proyectiles jefe y HUD")]
    public static void Probar() => Iniciar(false);
    [MenuItem("Laboratorio 4/Pulido/Probar HUD estático")]
    public static void ProbarHUD() => Iniciar(true);
    static void Iniciar(bool soloHUD)
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Carpeta); Selection.activeObject = null; StageUtility.GoToMainStage();
        SessionState.SetString(Clave + ".escena", SceneManager.GetActiveScene().path); SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".soloHUD", soloHUD);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    static void Estado(PlayModeStateChange e)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (e == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear(); segundoPlano = Application.runInBackground; Application.runInBackground = true;
            SaveSystem.RutaDePruebas = Path.GetFullPath(Carpeta + "run-qa.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(Carpeta + "ranking-qa.dat"); RankingManager.SuspenderSincronizacionAutomatica = true;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
            { SessionState.SetBool(Clave + key + ".existe", PlayerPrefs.HasKey(key)); SessionState.SetInt(Clave + key, PlayerPrefs.GetInt(key)); }
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            rutina = null; Time.timeScale = 1; Application.runInBackground = segundoPlano;
            foreach (string key in new[] { "MejorPuntuacion", "SelectedCharacter" })
                if (SessionState.GetBool(Clave + key + ".existe", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Clave + key, 0)); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false); SaveSystem.RutaDePruebas = RankingManager.RutaDePruebas = null;
            RankingManager.SuspenderSincronizacionAutomatica = false;
            File.WriteAllLines(Carpeta + (SessionState.GetBool(Clave + ".soloHUD", false) ? "hud-estatico.txt" : "pruebas.txt"), resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[PulidoFinalQA] " + resultados.Count + " comprobaciones; " + errores.Count + " errores.");
            EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 120) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .015f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static void Exigir(bool ok, string texto) { if (!ok) throw new Exception(texto); resultados.Add("PASS " + texto); }
    static void Privado(object objeto, string metodo, params object[] args) => objeto.GetType().GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(objeto, args);
    static void Campo(object objeto, string nombre, object valor) => objeto.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(objeto, valor);
    static void Capturar(string nombre) => ScreenCapture.CaptureScreenshot(Carpeta + nombre + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
    static void PrepararMapa()
    {
        Jugador.GetComponent<PlayerHealth>().isInvulnerable = true;
        foreach (var s in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
        foreach (var enemigo in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        { enemigo.StopAllCoroutines(); enemigo.gameObject.SetActive(false); }
        foreach (var ayudante in Object.FindObjectsByType<AyudanteJefeAI>(FindObjectsSortMode.None)) ayudante.gameObject.SetActive(false);
    }
    static IEnumerator Recorrido()
    {
        bool soloHUD = SessionState.GetBool(Clave + ".soloHUD", false);
        yield return .3f;
        foreach (string path in soloHUD ? Array.Empty<string>() : new[] { "Assets/Prefabs/EnemyBullet.prefab", "Assets/Prefabs/Bullet_Boss.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); var sr = prefab.GetComponent<SpriteRenderer>();
            Exigir(sr.sprite != null && AssetDatabase.GetAssetPath(sr.sprite).StartsWith("Assets/Sprites/Proyectiles/"), path + ": sprite nuevo enlazado");
            Exigir(sr.color == Color.white && sr.sharedMaterial.shader.name.Contains("Unlit"), path + ": color propio y visible en oscuridad");
            var importador = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sr.sprite));
            Exigir(importador.filterMode == FilterMode.Point && !importador.mipmapEnabled && sr.sprite.texture.width <= 128,
                path + ": textura pixel art sin suavizado ni mipmaps");
            // Sprite.bounds incluye margen transparente; la silueta opaca normalizada debe ocupar una unidad.
            Exigir(sr.sprite.bounds.size.x > .9f && sr.sprite.bounds.size.x < 3f && Mathf.Approximately(prefab.transform.localScale.x, .3f)
                && Mathf.Abs(prefab.GetComponent<CircleCollider2D>().radius - .5f) < .001f, path + ": escala y colisión conservadas (bounds " + sr.sprite.bounds.size + ")");
        }
        for (int personaje = 0; personaje < 2; personaje++)
        {
            GameManager.Instance.IniciarNuevaRun(personaje); SceneManager.LoadScene("Piso3_V10"); yield return 1.2f;
            PrepararMapa(); Exigir(!DeathScreenController.GameplayBlocked, "Personaje " + personaje + ": mapa activo");
            var arma = Jugador.GetComponent<ArmaIndependiente>(); var wm = Jugador.GetComponent<WeaponManager>(); arma.controlManual = true;
            foreach (bool melee in new[] { true, false })
            {
                wm.EquipWeapon(melee);
                var imagen = (melee ? wm.macheteIcon : wm.subfusilIcon).GetComponent<Image>();
                var spriteFijo = melee ? wm.spriteMacheteHUD : wm.spriteSubfusilHUD;
                var rect = imagen.rectTransform; var posicion = rect.anchoredPosition; var escala = rect.localScale; var tamano = rect.sizeDelta;
                Exigir(spriteFijo != null && rect.localRotation == Quaternion.identity && imagen.GetComponentInChildren<PlayerShooting>(true) == null &&
                    imagen.GetComponentInChildren<PlayerCombat>(true) == null && imagen.GetComponentInChildren<ArmaIndependiente>(true) == null &&
                    imagen.GetComponent<SpriteRenderer>() == null, "Personaje " + personaje + ": " + (melee ? "machete" : "subfusil") + " es Image UI sin componentes de arma");
                for (int dir = 0; dir < 8; dir++)
                {
                    Vector2 mirada = Quaternion.Euler(0, 0, dir * 45) * Vector2.down;
                    arma.Apuntar((Vector2)Jugador.transform.position + mirada * 10); yield return .025f;
                    var icono = (melee ? wm.macheteIcon : wm.subfusilIcon).GetComponent<Image>();
                    Exigir(icono.isActiveAndEnabled && icono.sprite == spriteFijo && rect.localRotation == Quaternion.identity &&
                        rect.anchoredPosition == posicion && rect.localScale == escala && rect.sizeDelta == tamano,
                        "Personaje " + personaje + ": HUD fijo a la derecha, " + (melee ? "machete" : "subfusil") + ", apuntado del jugador " + dir);
                }
                if (soloHUD) { Capturar("HUD-estatico-" + personaje + (melee ? "-machete" : "-subfusil")); yield return .04f; }
            }
            if (soloHUD)
            {
                var disparo = Jugador.GetComponent<PlayerShooting>(); disparo.RestaurarMunicion(30, 60);
                Privado(disparo, "Shoot"); arma.Apuntar((Vector2)Jugador.transform.position + Vector2.left * 10);
                Exigir(disparo.currentAmmoInClip == 29 && arma.DisparoVisualActivo && wm.subfusilIcon.GetComponent<Image>().sprite == wm.spriteSubfusilHUD,
                    "Personaje " + personaje + ": disparo y fogonazo sólo en arma jugable, HUD conserva imagen estática");
                yield return .18f;
                Exigir(wm.subfusilIcon.GetComponent<Image>().sprite == wm.spriteSubfusilHUD, "Personaje " + personaje + ": HUD continúa estático después del disparo");
                continue;
            }
            // 3 segundos de silencio sirven como señal reproducible, sin añadir ruido durante la prueba.
            var clip = AudioClip.Create("QA_RadioEscucha", 132300, 1, 44100, false);
            var emisor = new GameObject("QA_Emisor"); emisor.transform.position = Jugador.transform.position + Vector3.right * 2;
            var referencia = emisor.AddComponent<AudioSource>(); referencia.volume = 1;
            referencia.outputAudioMixerGroup = Jugador.GetComponent<PlayerShooting>().sfxSource.outputAudioMixerGroup;
            Exigir(AudioEnemigosCercanos.Reproducir(clip, emisor.transform, referencia), "Personaje " + personaje + ": enemigo cercano audible");
            var mezcla = AudioEnemigosCercanos.Instancia;
            Exigir(mezcla.VocesActivas == 1, "Una voz por enemigo");
            AudioEnemigosCercanos.Reproducir(clip, emisor.transform, referencia);
            Exigir(mezcla.VocesActivas == 1, "Daño/alerta/muerte no se superponen en un emisor");
            var voz = mezcla.GetComponentsInChildren<AudioSource>().First(a => a.isPlaying);
            Exigir(voz.priority == 192 && voz.outputAudioMixerGroup == referencia.outputAudioMixerGroup && voz.volume > .5f, "Prioridad baja del enemigo y mixer SFX conservado");
            emisor.transform.position = Jugador.transform.position + Vector3.right * 8; yield return .06f;
            Exigir(voz.volume > 0 && voz.volume < .1f, "Atenuación a ocho unidades");
            emisor.transform.position = Jugador.transform.position + Vector3.right * 12; yield return .06f;
            Exigir(!voz.isPlaying && !AudioEnemigosCercanos.Reproducir(clip, emisor.transform, referencia), "Enemigos fuera de radio inaudibles");
            for (int i = 0; i < 20; i++)
            {
                var e = new GameObject("QA_Multitud"); e.transform.position = Jugador.transform.position + Vector3.right * (2 + i * .2f);
                AudioEnemigosCercanos.Reproducir(clip, e.transform, referencia);
            }
            Exigir(mezcla.VocesActivas == 4, "Multitud de veinte limitada a cuatro voces Sorbos");
            float volumen = mezcla.GetComponentsInChildren<AudioSource>().Where(a => a.isPlaying).Sum(a => a.volume);
            var shooting = Jugador.GetComponent<PlayerShooting>(); shooting.RestaurarMunicion(30, 60);
            Privado(shooting, "Shoot"); arma.Apuntar((Vector2)Jugador.transform.position + Vector2.right * 10); yield return .025f;
            Exigir(shooting.currentAmmoInClip == 29 && shooting.sfxSource.priority == 0 && Mathf.Approximately(mezcla.GananciaEnemigos, .25f), "Disparo real consume bala, prioridad cero y atenúa enemigos al 25%");
            Exigir(mezcla.GetComponentsInChildren<AudioSource>().Where(a => a.isPlaying).Sum(a => a.volume) <= volumen * .26f, "Voces activas reducidas al disparar");
            Exigir(arma.DisparoVisualActivo && wm.subfusilIcon.GetComponent<Image>().sprite == wm.spriteSubfusilHUD, "HUD estático mientras el arma jugable muestra su fogonazo");
            Capturar("HUD-personaje-" + personaje); yield return .2f;
            Exigir(Mathf.Approximately(mezcla.GananciaEnemigos, 1), "Mezcla de enemigos recuperada tras disparo");
            for (int i = 0; i < 5; i++)
            {
                var e = new GameObject("QA_VozJefe"); e.transform.position = Jugador.transform.position + Vector3.up * (2 + i);
                AudioEnemigosCercanos.Reproducir(clip, e.transform, referencia, 14, .65f, 1, true);
            }
            Exigir(mezcla.VocesActivas == 6, "Jefe con dos voces propias y Sorbos con cuatro");
            Privado(mezcla, "Silenciar");
            emisor.transform.position = Jugador.transform.position + Vector3.right * 2;
            AudioEnemigosCercanos.Reproducir(clip, emisor.transform, referencia); Object.Destroy(emisor); yield return .04f;
            Exigir(mezcla.VocesActivas == 1, "Sonido de muerte sobrevive a desaparición del emisor dentro de radio");
        }
        if (soloHUD) { Exigir(errores.Count == 0, "HUD estático sin errores ni excepciones"); yield break; }
        GameManager.Instance.IniciarNuevaRun(0); SceneManager.LoadScene("Escenario4_Jefe"); yield return 1.2f;
        PrepararMapa(); Exigir(AudioEnemigosCercanos.Instancia.VocesActivas == 0, "Cambio de piso limpia sonidos antiguos");
        var boss = Object.FindFirstObjectByType<BossAI>(); Exigir(boss != null, "Jefe probado exclusivamente en Escenario4_Jefe");
        var rb = boss.GetComponent<Rigidbody2D>(); var anim = boss.GetComponent<AnimadorSprites8Direcciones>();
        Vector2 centroCombate = rb.position;
        foreach (var col in boss.GetComponentsInChildren<Collider2D>()) col.enabled = false;
        var movimiento = Jugador.GetComponent<MovimientoPlayer>(); movimiento.enabled = false;
        Jugador.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        rb.position = (Vector2)Jugador.transform.position + Vector2.left * 12; rb.linearVelocity = Vector2.zero;
        Campo(boss, "attackTimer", 0f); yield return .35f;
        var frames = new HashSet<int>(); bool caminataContinua = true; float minima = float.MaxValue, maxima = 0;
        for (int i = 0; i < 18; i++)
        {
            minima = Mathf.Min(minima, rb.linearVelocity.magnitude); maxima = Mathf.Max(maxima, rb.linearVelocity.magnitude);
            caminataContinua &= rb.linearVelocity.magnitude > boss.moveSpeed * .8f && anim.AnimacionActual == "Correr";
            frames.Add(anim.FotogramaActual); yield return .05f;
        }
        Exigir(caminataContinua && frames.Count == 8, "Jefe persigue sin microparadas y reproduce los ocho cuadros de carrera (velocidad " + minima + "–" + maxima + ", objetivo " + boss.moveSpeed + ", frames " + string.Join(",", frames) + ", estado " + anim.AnimacionActual + ")");
        Exigir(rb.interpolation == RigidbodyInterpolation2D.Interpolate && rb.collisionDetectionMode == CollisionDetectionMode2D.Continuous,
            "Jefe con interpolación física y colisión continua");
        // Hacer los ataques en el punto original del jefe, lejos de la puerta de aparición del jugador.
        Jugador.transform.position = centroCombate + Vector2.right * 1.5f;
        Jugador.GetComponent<Rigidbody2D>().position = Jugador.transform.position;
        rb.position = centroCombate; rb.linearVelocity = Vector2.zero;
        Campo(boss, "isAttacking", true); Campo(boss, "attackTimer", 100f);
        var zarpazo = (IEnumerator)typeof(BossAI).GetMethod("EjecutarZarpazo", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(boss, new object[] {20f, 0f, Color.white, false});
        boss.StartCoroutine(zarpazo); frames.Clear();
        for (int i = 0; i < 26; i++)
        {
            if (anim.AnimacionActual == "Ataque") frames.Add(anim.FotogramaActual);
            yield return .025f;
        }
        Exigir(frames.Contains(8) && frames.Count == 9, "Zarpazo conserva nueve cuadros, incluido impacto y recuperación");
        yield return .2f;
        Exigir(anim.AnimacionActual != "Ataque", "Zarpazo vuelve a locomoción al finalizar");
        Campo(boss, "isAttacking", true); boss.StartCoroutine((IEnumerator)typeof(BossAI).GetMethod("EjecutarBulletHell", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(boss, null));
        yield return .5f;
        Exigir(anim.AnimacionActual == "BolaDePoder" && anim.FotogramaActual >= 3 && anim.FotogramaActual <= 5, "Carga distribuye nueve cuadros durante el segundo de preparación");
        yield return .65f;
        var balas = Object.FindObjectsByType<BossBullet>(FindObjectsSortMode.None);
        Exigir(balas.Length > 0 && balas.All(b => AssetDatabase.GetAssetPath(b.GetComponent<SpriteRenderer>().sprite).EndsWith("DoctorYiba_Necrosia.png")), "Ataque real del jefe usa esporas Necrosia nuevas (balas " + balas.Length + ", prefab " + AssetDatabase.GetAssetPath(boss.bossBulletPrefab) + ", sprites " + string.Join(",", balas.Select(b => AssetDatabase.GetAssetPath(b.GetComponent<SpriteRenderer>().sprite))) + ")");
        Capturar("jefe-proyectiles"); yield return .5f;
        Exigir(errores.Count == 0, "Ejecución sin errores ni excepciones");
    }
}
