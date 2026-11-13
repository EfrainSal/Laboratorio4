using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProbarOscuridadPisos
{
    const string Clave = "L4.PruebaOscuridadPisos";
    static readonly List<string> resultados = new List<string>(), errores = new List<string>();
    static IEnumerator rutina;
    static double siguiente, inicio;
    static bool segundoPlano;
    static SceneSetup[] anterior;

    static ProbarOscuridadPisos()
    {
        EditorApplication.playModeStateChanged += Estado;
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (m, s, t) => {
            if (SessionState.GetBool(Clave, false) && EditorApplication.isPlaying && (t == LogType.Error || t == LogType.Exception)) errores.Add(m);
        };
    }
    [MenuItem("Laboratorio 4/Iluminación/Probar oscuridad en Play %#&F12")]
    public static void Probar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(IntegrarOscuridadPisos.Carpeta);
        Selection.activeObject = null; StageUtility.GoToMainStage();
        anterior = EditorSceneManager.GetSceneManagerSetup();
        // Guardar la disposición antes del cambio de dominio de Play.
        SessionState.SetString(Clave + ".escena", anterior.Length == 1 ? anterior[0].path : "Assets/Scenes/Piso3_V10.unity");
        SessionState.SetBool(Clave, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); EditorApplication.EnterPlaymode();
    }
    static void Estado(PlayModeStateChange e)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (e == PlayModeStateChange.EnteredPlayMode)
        {
            resultados.Clear(); errores.Clear();
            segundoPlano = Application.runInBackground; Application.runInBackground = true;
            SaveSystem.RutaDePruebas = Path.GetFullPath(IntegrarOscuridadPisos.Carpeta + "run-prueba.dat");
            RankingManager.RutaDePruebas = Path.GetFullPath(IntegrarOscuridadPisos.Carpeta + "ranking-prueba.dat");
            RankingManager.SuspenderSincronizacionAutomatica = true;
            inicio = siguiente = EditorApplication.timeSinceStartup; rutina = Recorrido();
        }
        if (e == PlayModeStateChange.ExitingPlayMode)
        { rutina = null; Time.timeScale = 1; Application.runInBackground = segundoPlano; }
        if (e == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Clave, false);
            File.WriteAllLines(IntegrarOscuridadPisos.Carpeta + "pruebas.txt", resultados.Concat(errores.Select(m => "ERROR " + m)));
            Debug.Log("[PruebaOscuridad] " + resultados.Count + " comprobaciones, " + errores.Count + " errores.");
            EditorSceneManager.OpenScene(SessionState.GetString(Clave + ".escena", "Assets/Scenes/Piso3_V10.unity"));
        }
    }
    static void Actualizar()
    {
        if (rutina == null || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 100) throw new Exception("Tiempo de prueba agotado");
            if (!rutina.MoveNext()) { rutina = null; EditorApplication.ExitPlaymode(); return; }
            siguiente = EditorApplication.timeSinceStartup + (rutina.Current is float s ? s : .05f);
        }
        catch (Exception ex) { errores.Add(ex.ToString()); rutina = null; EditorApplication.ExitPlaymode(); }
    }
    static void Exigir(bool cumple, string detalle)
    { if (!cumple) throw new Exception(detalle); resultados.Add("PASS " + detalle); }

    static IEnumerator Recorrido()
    {
        yield return .4f;
        Exigir(GameManager.Instance != null, "Inicio desde MainMenu con GameManager");
        // Los dos mapas modulares usan controles antiguos: se verifican en edición, sin alterar su input.
        foreach (string nombre in IntegrarOscuridadPisos.Mapas.Where(n => !n.Contains("Modular")))
        {
            GameManager.Instance.IniciarNuevaRun(0);
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/" + nombre + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return .5f;
            var controles = Object.FindFirstObjectByType<PantallaControlesInicio>();
            if (controles != null && PantallaControlesInicio.IsShowing) { controles.Cerrar(); yield return .5f; }
            Time.timeScale = 0;
            var referencia = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Jason.prefab").GetComponentInChildren<Light2D>();
            var material = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Jason.prefab").GetComponent<SpriteRenderer>().sharedMaterial;
            var escena = SceneManager.GetActiveScene();
            var luces = IntegrarOscuridadPisos.Componentes<Light2D>(escena).Where(l => l.enabled && l.gameObject.activeInHierarchy).ToArray();
            Exigir(luces.Where(l => l.lightType == Light2D.LightType.Global).All(l => l.intensity == 0), nombre + ": sin iluminación ambiental que lave la oscuridad");
            var locales = luces.Where(l => l.lightType == Light2D.LightType.Point).ToArray();
            Exigir(locales.Length > 0, nombre + ": el jugador tiene luz local activa");
            Exigir(locales.All(l => Mathf.Approximately(l.intensity, referencia.intensity) &&
                Mathf.Approximately(l.pointLightOuterRadius, referencia.pointLightOuterRadius) &&
                Mathf.Approximately(l.falloffIntensity, referencia.falloffIntensity) && l.color == referencia.color),
                nombre + ": intensidad, radio, caída y color iguales a Piso3");
            Exigir(!IntegrarOscuridadPisos.Componentes<SpriteRenderer>(escena).Any(r => r.enabled && r.sprite != null && r.sharedMaterial != null && r.sharedMaterial.shader.name == "Sprites/Default"),
                nombre + ": las piezas del mapa responden a la luz 2D");
            ScreenCapture.CaptureScreenshot(IntegrarOscuridadPisos.Carpeta + nombre + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
            yield return .2f;

            // Prueba del render real de URP, con la misma luz en un área aislada del mapa.
            var prueba = new GameObject("PruebaRenderOscuridad");
            prueba.transform.position = new Vector3(10000, 10000, 0);
            var luz = Object.Instantiate(referencia.gameObject, prueba.transform, false);
            luz.transform.localPosition = Vector3.zero;
            var textura = new Texture2D(8, 8); textura.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray()); textura.Apply();
            var sprite = Sprite.Create(textura, new Rect(0, 0, 8, 8), Vector2.one * .5f, 8);
            foreach (float x in new[] { 0f, 18f })
            {
                var go = new GameObject("Muestra"); go.transform.SetParent(prueba.transform, false); go.transform.localPosition = new Vector3(x, 0, 0);
                var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sharedMaterial = material;
            }
            var cam = new GameObject("CamaraPrueba").AddComponent<Camera>();
            cam.transform.SetParent(prueba.transform, false); cam.transform.localPosition = new Vector3(9, 0, -10);
            cam.orthographic = true; cam.orthographicSize = 5; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var rt = new RenderTexture(300, 100, 24); cam.targetTexture = rt;
            yield return .25f;
            var lectura = new Texture2D(300, 100, TextureFormat.RGB24, false);
            var previo = RenderTexture.active; RenderTexture.active = rt;
            lectura.ReadPixels(new Rect(0, 0, 300, 100), 0, 0); lectura.Apply(); RenderTexture.active = previo;
            float cerca = lectura.GetPixel(60, 50).grayscale, lejos = lectura.GetPixel(240, 50).grayscale;
            Exigir(cerca > .2f && lejos < .03f, nombre + ": render iluminado junto al jugador y negro fuera de alcance (" + cerca.ToString("F3") + "/" + lejos.ToString("F3") + ")");
            cam.targetTexture = null; Object.Destroy(prueba); Object.Destroy(lectura); Object.Destroy(sprite); Object.Destroy(textura); rt.Release(); Object.Destroy(rt);
            Time.timeScale = 1; yield return .1f;
        }
        Exigir(errores.Count == 0, "Recorrido de iluminación sin errores ni excepciones");
    }
}
