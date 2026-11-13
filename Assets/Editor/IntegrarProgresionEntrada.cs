using System;
using System.IO;
using System.Linq;
using Laboratorio4;
using Laboratorio4.Animacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class IntegrarProgresionEntrada
{
    public const string Entrada = "Assets/Scenes/Escenario1_Entrada.unity";
    public const string Piso3 = "Assets/Scenes/Piso3_V10.unity";

    [MenuItem("Laboratorio 4/Progresion/Integrar entrada tarjeta y machete")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null;
        StageUtility.GoToMainStage();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            CrearTarjeta();
            CrearPuerta();
            ConfigurarJugadores();
            var escena = EditorSceneManager.OpenScene(Entrada);
            var raices = escena.GetRootGameObjects();
            foreach (var antiguo in raices.Where(g => g.name == "GameplayEntrada")) Object.DestroyImmediate(antiguo);
            var raiz = new GameObject("GameplayEntrada");
            var inicio = raiz.AddComponent<InicioGameplayEscenario1>();
            inicio.personajesPrototipo = raices.First(g => g.name == "E1_Personajes");
            inicio.sorbosPrototipo = raices.First(g => g.name == "E1_Sorbos");
            inicio.selectorPrototipo = raices.First(g => g.name == "SeleccionPersonaje");
            inicio.personajesPrototipo.SetActive(false);
            inicio.sorbosPrototipo.SetActive(false);
            inicio.selectorPrototipo.SetActive(false);
            inicio.camara = Buscar<CamaraSeguimiento>(escena);
            inicio.prefabJason = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Jason.prefab");
            var hud = Instanciar(IntegrarGameplayPiso3.HudPrefab, raiz.transform, escena);
            inicio.gestorNivel = hud.GetComponentInChildren<LevelManager>(true);
            inicio.puntoDeInicio = inicio.gestorNivel.puntoDeInicio;
            inicio.puntoDeInicio.position = new Vector3(6.96f, -12.78f, 0);
            var nivel = new SerializedObject(inicio.gestorNivel);
            nivel.FindProperty("enemigosParaAvanzar").intValue = 10;
            nivel.FindProperty("spawnersRequeridos").intValue = 0;
            nivel.FindProperty("contarSoloEnemigosDeSpawners").boolValue = true;
            nivel.FindProperty("llavePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TarjetaAcceso.prefab");
            nivel.ApplyModifiedPropertiesWithoutUndo();
            var luz = new GameObject("LuzGameplay").AddComponent<Light2D>();
            luz.transform.SetParent(raiz.transform);
            luz.lightType = Light2D.LightType.Global; luz.intensity = 1;
            var spawners = new GameObject("SpawnersSorbos").transform;
            spawners.SetParent(raiz.transform);
            var posiciones = new[] { new Vector3(13, -11, 0), new Vector3(24, -12, 0), new Vector3(43.4f, -13.7f, 0) };
            for (int i = 0; i < 3; i++)
            {
                var go = Instanciar("Assets/Prefabs/Spawner_lvl" + (i + 1) + ".prefab", spawners, escena);
                go.transform.position = posiciones[i];
                go.GetComponent<EnemySpawner>().evitarObstaculos = true;
                foreach (var r in go.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
            }
            // Reutilizar el trigger de Debugscene sobre la puerta dibujada del escenario.
            var salidaOriginal = Buscar<PuertaSalida>(escena);
            Vector3 salida = salidaOriginal.transform.position;
            salidaOriginal.gameObject.SetActive(false);
            var puerta = Instanciar("Assets/Prefabs/PuertaTransicion.prefab", raiz.transform, escena);
            puerta.transform.position = salida;
            puerta.transform.localScale = Vector3.one;
            var collider = puerta.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1.92f, .9f); collider.offset = Vector2.zero;
            var logica = puerta.GetComponent<PuertaNivel>();
            logica.textoMensajeUI = hud.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "AlertaLlave");
            if (logica.textoMensajeUI == null)
            {
                var canvas = hud.GetComponentInChildren<Canvas>(true);
                var mensaje = new GameObject("MensajeTarjeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
                mensaje.transform.SetParent(canvas.transform, false);
                var texto = mensaje.GetComponent<TMPro.TextMeshProUGUI>();
                texto.fontSize = 24; texto.alignment = TMPro.TextAlignmentOptions.Center;
                texto.rectTransform.anchoredPosition = new Vector2(0, -100);
                texto.rectTransform.sizeDelta = new Vector2(650, 50);
                logica.textoMensajeUI = texto;
            }
            for (int i = 0; i < 3; i++)
            {
                string nombre = new[] { "Vendas", "Estimulante", "Botiquin" }[i];
                var item = Instanciar("Assets/Prefabs/" + nombre + ".prefab", raiz.transform, escena);
                item.transform.position = new Vector3(8.7f + i * .7f, -12.8f, 0);
            }
            EditorSceneManager.SaveScene(escena);

            var menu = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var controlador = new SerializedObject(Buscar<MainMenuController>(menu));
            controlador.FindProperty("_gameSceneName").stringValue = "Escenario1_Entrada";
            controlador.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(menu);
            var lista = EditorBuildSettings.scenes.ToList();
            if (!lista.Any(s => s.path == Entrada)) lista.Add(new EditorBuildSettingsScene(Entrada, true));
            EditorBuildSettings.scenes = lista.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProgresionEntrada] Integrada entrada, 3 spawners originales, tarjeta al Sorbo 10, puerta F a Piso3 y machete al 50%.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static void ConfigurarJugadores()
    {
        foreach (var nombre in new[] { "Player_Jason", "Player_Milena" })
        {
            string path = "Assets/Prefabs/" + nombre + ".prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var arma = go.GetComponent<ArmaIndependiente>();
                arma.invertirFiloMachete = true; arma.escalaMachete = .5f;
                go.GetComponent<MacheteAnimacion>().mostrarAlcance = false;
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
    }
    private static void CrearTarjeta()
    {
        var go = PrefabUtility.LoadPrefabContents("Assets/Prefabs/llave.prefab");
        try
        {
            go.name = "TarjetaAcceso";
            go.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/TarjetaAcceso.png").OfType<Sprite>().First();
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/TarjetaAcceso.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }
    private static void CrearPuerta()
    {
        var referencia = EditorSceneManager.OpenScene("Assets/Scenes/Debugscene.unity", OpenSceneMode.Additive);
        try
        {
            var fuente = Buscar<PuertaNivel>(referencia);
            var copia = Object.Instantiate(fuente.gameObject);
            copia.name = "PuertaTransicion";
            var puerta = copia.GetComponent<PuertaNivel>();
            puerta.nombreSiguienteEscena = "Piso3_V10";
            puerta.accionInteractuar = null;
            puerta.textoMensajeUI = null;
            puerta.mensajeFaltaLlave = "Necesito la Tarjeta de Acceso. Elimina 10 Sorbos y recógela. [F] Interactuar";
            foreach (var r in copia.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(copia, "Assets/Prefabs/PuertaTransicion.prefab");
            Object.DestroyImmediate(copia);
        }
        finally { EditorSceneManager.CloseScene(referencia, true); }
    }
    private static T Buscar<T>(Scene escena) where T : Component => escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static GameObject Instanciar(string path, Transform padre, Scene escena)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), escena);
        go.transform.SetParent(padre, true);
        return go;
    }
}
