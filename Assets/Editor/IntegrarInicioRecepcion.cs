using System;
using System.IO;
using System.Linq;
using Laboratorio4.Recepcion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public static class IntegrarInicioRecepcion
{
    public const string Escena = "Assets/Scenes/Recepcion.unity";
    [InitializeOnLoadMethod]
    private static void AplicarCorreccionSolicitada()
    {
        const string solicitud = "Reports/PanelRecepcion/aplicar-y-probar.solicitud";
        if (!File.Exists(solicitud)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(solicitud, solicitud + ".procesando");
            try { CorregirPanel(); ProbarInicioRecepcion.Probar(); }
            catch (Exception error)
            {
                File.WriteAllText("Reports/PanelRecepcion/error-aplicacion.txt", error.ToString());
                Debug.LogException(error);
            }
        };
    }
    [MenuItem("Laboratorio 4/Recepción/Integrar inicio y controles %#&F6")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null; StageUtility.GoToMainStage();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var escena = EditorSceneManager.OpenScene(Escena);
            var raices = escena.GetRootGameObjects();
            foreach (var viejo in raices.Where(g => g.name == "GameplayRecepcion")) Object.DestroyImmediate(viejo);
            var raiz = new GameObject("GameplayRecepcion");
            var inicio = raiz.AddComponent<InicioGameplayRecepcion>();
            inicio.personajesPrototipo = raices.First(g => g.name == "05_Personajes");
            inicio.personajesPrototipo.SetActive(false);
            inicio.camara = Buscar<RecepcionCamara>(escena);
            inicio.prefabJason = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Jason.prefab");
            var hud = Instanciar("Assets/Prefabs/UI/HUDGameplay.prefab", raiz.transform, escena);
            inicio.gestorNivel = hud.GetComponentInChildren<LevelManager>(true);
            inicio.puntoDeInicio = inicio.gestorNivel.puntoDeInicio;
            inicio.puntoDeInicio.position = new Vector3(9.3f, -11.85f, 0);
            var nivel = new SerializedObject(inicio.gestorNivel);
            nivel.FindProperty("enemigosParaAvanzar").intValue = int.MaxValue;
            nivel.FindProperty("spawnersRequeridos").intValue = 0;
            nivel.FindProperty("llavePrefab").objectReferenceValue = null;
            nivel.ApplyModifiedPropertiesWithoutUndo();
            var luz = new GameObject("LuzRecepcion").AddComponent<Light2D>();
            luz.transform.SetParent(raiz.transform, false); luz.lightType = Light2D.LightType.Global; luz.intensity = 0;
            foreach (var salida in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RecepcionSalida>(true))) salida.gameObject.SetActive(false);

            const string rutaPanel = "Assets/UI/Inicio/ControlesPanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(rutaPanel);
            if (panel == null)
            {
                panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset"));
                panel.name = "ControlesPanelSettings"; panel.sortingOrder = 1000;
                AssetDatabase.CreateAsset(panel, rutaPanel);
            }
            var ui = new GameObject("PantallaControles"); ui.transform.SetParent(raiz.transform, false);
            var documento = ui.AddComponent<UIDocument>(); documento.panelSettings = panel; documento.sortingOrder = 1000;
            var controles = ui.AddComponent<PantallaControlesInicio>();
            controles.estilos = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/Inicio/ControlesInicio.uss");
            controles.estilosMenu = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/MainMenuStyles.uss");
            controles.iconoTarjeta = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TarjetaAcceso.prefab").GetComponent<SpriteRenderer>().sprite;
            inicio.pantallaControles = controles;

            var tarjeta = Instanciar("Assets/Prefabs/TarjetaAcceso.prefab", raiz.transform, escena);
            tarjeta.name = "TarjetaSalidaRecepcion";
            Object.DestroyImmediate(tarjeta.GetComponent<ItemPickup>());
            tarjeta.AddComponent<TarjetaSalidaRecepcion>();
            tarjeta.GetComponent<SpriteRenderer>().sortingOrder = 900;
            ConfigurarPanelYTarjeta(escena, raiz, controles);
            EditorSceneManager.SaveScene(escena);

            var menu = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var controlador = new SerializedObject(Buscar<MainMenuController>(menu));
            controlador.FindProperty("_gameSceneName").stringValue = "Recepcion";
            controlador.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(menu);
            var escenas = EditorBuildSettings.scenes.ToList();
            foreach (string ruta in new[] { Escena, "Assets/Scenes/Escenario1_Entrada.unity" })
            {
                var existente = escenas.FirstOrDefault(s => s.path == ruta);
                if (existente != null) existente.enabled = true; else escenas.Add(new EditorBuildSettingsScene(ruta, true));
            }
            EditorBuildSettings.scenes = escenas.ToArray(); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Reports/InicioRecepcion");
            File.WriteAllText("Reports/InicioRecepcion/integracion.txt", "MainMenu -> Recepcion -> recoger tarjeta junto a la salida -> F en el panel -> Escenario1_Entrada.\nHUD y prefabs reales de ambos personajes. Ayuda procedural UI Toolkit y bloqueo durante la introducción.\n");
            Debug.Log("[InicioRecepcion] Inicio, controles, jugador y panel con tarjeta integrados.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    [MenuItem("Laboratorio 4/Recepción/Corregir tarjeta y panel de salida %#&F4")]
    public static void CorregirPanel()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null; StageUtility.GoToMainStage();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var escena = EditorSceneManager.OpenScene(Escena);
            var raiz = escena.GetRootGameObjects().First(g => g.name == "GameplayRecepcion");
            ConfigurarPanelYTarjeta(escena, raiz, raiz.GetComponentInChildren<PantallaControlesInicio>(true));
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[PanelRecepcion] Tarjeta junto a salida y panel F configurados; controles conservados.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    private static void ConfigurarPanelYTarjeta(Scene escena, GameObject raiz, PantallaControlesInicio controles)
    {
        var puertaVisual = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true))
            .First(r => r.name == "Puerta_Salida_Escenario1");
        var tarjeta = raiz.GetComponentInChildren<TarjetaSalidaRecepcion>(true);
        var renderTarjeta = tarjeta.GetComponent<SpriteRenderer>();
        Bounds puertaBounds = puertaVisual.bounds;
        tarjeta.transform.position = new Vector3(puertaBounds.max.x + renderTarjeta.bounds.extents.x + .12f, puertaBounds.min.y - .55f, 0);
        PrefabUtility.RecordPrefabInstancePropertyModifications(tarjeta.transform);
        var panel = raiz.GetComponentsInChildren<PuertaNivel>(true).FirstOrDefault(p => p.name == "PanelSalidaRecepcion");
        if (panel == null)
        {
            var go = Instanciar("Assets/Prefabs/PuertaTransicion.prefab", raiz.transform, escena);
            go.name = "PanelSalidaRecepcion"; panel = go.GetComponent<PuertaNivel>();
        }
        panel.transform.position = new Vector3(puertaVisual.transform.position.x, puertaBounds.min.y - .65f, 0);
        panel.transform.localScale = Vector3.one;
        var trigger = panel.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true; trigger.offset = Vector2.zero; trigger.size = new Vector2(1.4f, 1f);
        panel.nombreSiguienteEscena = "Escenario1_Entrada"; panel.avanzarContadorPiso = false; panel.accionInteractuar = null;
        panel.mensajeFaltaLlave = "Necesito recoger la Tarjeta de Acceso que está junto a esta puerta.";
        panel.textoMensajeUI = raiz.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(t => t.name == "AlertaLlave");
        panel.GetComponent<SpriteRenderer>().enabled = false;
        controles.panelSalida = panel;
        controles.estilosMenu = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/MainMenuStyles.uss");
        PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
        PrefabUtility.RecordPrefabInstancePropertyModifications(panel.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
        foreach (var salida in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RecepcionSalida>(true))) salida.gameObject.SetActive(false);
    }
    private static T Buscar<T>(Scene escena) where T : Component => escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static GameObject Instanciar(string path, Transform padre, Scene escena)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), escena);
        go.transform.SetParent(padre, true); return go;
    }
}
