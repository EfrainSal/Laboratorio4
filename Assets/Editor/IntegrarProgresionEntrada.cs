using System;
using System.IO;
using System.Linq;
using Laboratorio4;
using Laboratorio4.Animacion;
using Laboratorio4.Escenario3;
using LaboratorioYiba.Piso3V10;
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
    public const string Laboratorio = "Assets/Scenes/E3_Laboratorio.unity";

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
            luz.lightType = Light2D.LightType.Global; luz.intensity = 0;
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
            if (!lista.Any(s => s.path == Piso3)) lista.Add(new EditorBuildSettingsScene(Piso3, true));
            if (!lista.Any(s => s.path == Laboratorio)) lista.Add(new EditorBuildSettingsScene(Laboratorio, true));
            EditorBuildSettings.scenes = lista.ToArray();
            ConfigurarPiso3();
            ConfigurarLaboratorio();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProgresionEntrada] Integrada entrada, Piso3_V10 y E3_Laboratorio con 3 spawners, tarjeta al Sorbo 10, puertas de transicion y consumibles.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    [MenuItem("Laboratorio 4/Progresion/Integrar Piso 3 y Laboratorio")]
    public static void IntegrarPiso3YLaboratorio()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null;
        StageUtility.GoToMainStage();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ConfigurarPiso3();
            ConfigurarLaboratorio();
            var lista = EditorBuildSettings.scenes.ToList();
            if (!lista.Any(s => s.path == Entrada)) lista.Add(new EditorBuildSettingsScene(Entrada, true));
            if (!lista.Any(s => s.path == Piso3)) lista.Add(new EditorBuildSettingsScene(Piso3, true));
            if (!lista.Any(s => s.path == Laboratorio)) lista.Add(new EditorBuildSettingsScene(Laboratorio, true));
            EditorBuildSettings.scenes = lista.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProgresionPiso3] Integrados Piso3_V10 -> E3_Laboratorio -> Nivel1 con PuertaTransicion, HUDGameplay, 3 Spawners, TarjetaAcceso y Botiquin.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static void ConfigurarPiso3()
    {
        var escena = EditorSceneManager.OpenScene(Piso3);
        var raices = escena.GetRootGameObjects();
        var hud = raices.FirstOrDefault(g => g.name == "HUDGameplay");
        if (hud == null)
        {
            hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(IntegrarGameplayPiso3.HudPrefab), escena);
            hud.name = "HUDGameplay";
        }
        var upgradePanel = hud.transform.Find("UpgradePanel");
        if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);

        var gestorNivel = hud.GetComponentInChildren<LevelManager>(true);
        var inicioPiso3 = Buscar<InicioPersonajesDesdeMenu>(escena);
        if (inicioPiso3 != null)
        {
            inicioPiso3.gestorNivel = gestorNivel;
            if (inicioPiso3.jugadorOriginal != null && gestorNivel.puntoDeInicio != null)
                gestorNivel.puntoDeInicio.position = inicioPiso3.jugadorOriginal.position;
        }

        var nivel = new SerializedObject(gestorNivel);
        nivel.FindProperty("enemigosParaAvanzar").intValue = 10;
        nivel.FindProperty("spawnersRequeridos").intValue = 0;
        nivel.FindProperty("contarSoloEnemigosDeSpawners").boolValue = true;
        nivel.FindProperty("llavePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TarjetaAcceso.prefab");
        nivel.ApplyModifiedPropertiesWithoutUndo();

        foreach (var spawner in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EnemySpawner>(true)))
        {
            spawner.evitarObstaculos = true;
            foreach (var r in spawner.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
        }

        foreach (var antiguo in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PuertaNivel>(true)).Select(p => p.gameObject).ToArray())
            Object.DestroyImmediate(antiguo);

        var puertaSalida = escena.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Piso3V10Puerta>(true))
            .FirstOrDefault(p => p.tipo == Piso3V10Puerta.TipoPuerta.SalidaAPiso4 || p.name == "Puerta_Salida");

        Vector3 posPuerta = puertaSalida != null ? puertaSalida.transform.position : new Vector3(35.92f, -3.88f, 0f);
        if (puertaSalida != null)
        {
            puertaSalida.cerrada = false;
            puertaSalida.Abrir();
        }

        var puerta = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PuertaTransicion.prefab"), escena);
        puerta.name = "PuertaTransicion";
        puerta.transform.position = posPuerta;
        puerta.transform.localScale = Vector3.one;
        var collider = puerta.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1.92f, 1.1f);
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        var logica = puerta.GetComponent<PuertaNivel>();
        logica.nombreSiguienteEscena = "E3_Laboratorio";
        logica.textoMensajeUI = ObtenerOCrearMensajeTarjeta(hud);

        EditorSceneManager.SaveScene(escena);
    }

    private static void ConfigurarLaboratorio()
    {
        var escena = EditorSceneManager.OpenScene(Laboratorio);
        var raices = escena.GetRootGameObjects();
        foreach (var antiguo in raices.Where(g => g.name == "GameplayEntrada" || g.name == "BotiquinInteractuable").ToArray())
            Object.DestroyImmediate(antiguo);

        int capaMuros = LayerMask.NameToLayer("Muros");
        foreach (var col in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider2D>(true)))
        {
            if (!col.isTrigger && col.attachedRigidbody == null)
                col.gameObject.layer = capaMuros;
        }

        var e3Hud = Buscar<E3Hud>(escena);
        if (e3Hud != null) e3Hud.enabled = false;

        var nivelRaiz = raices.FirstOrDefault(g => g.name == "E3_Nivel");
        var personajesProto = nivelRaiz != null ? nivelRaiz.transform.Find("E3_Personajes")?.gameObject : null;
        var sorbosProto = nivelRaiz != null ? nivelRaiz.transform.Find("E3_Sorbos")?.gameObject : null;

        var raiz = new GameObject("GameplayEntrada");
        var inicio = raiz.AddComponent<InicioGameplayEscenario1>();
        inicio.personajesPrototipo = personajesProto;
        inicio.sorbosPrototipo = sorbosProto;
        if (inicio.personajesPrototipo != null) inicio.personajesPrototipo.SetActive(false);
        if (inicio.sorbosPrototipo != null) inicio.sorbosPrototipo.SetActive(false);
        inicio.camaraE3 = Buscar<E3Camara>(escena);
        inicio.prefabJason = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Jason.prefab");

        var hud = Instanciar(IntegrarGameplayPiso3.HudPrefab, raiz.transform, escena);
        var upgradePanel = hud.transform.Find("UpgradePanel");
        if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);

        inicio.gestorNivel = hud.GetComponentInChildren<LevelManager>(true);
        inicio.puntoDeInicio = inicio.gestorNivel.puntoDeInicio;
        inicio.puntoDeInicio.position = new Vector3(5.88f, -9.0f, 0f);

        var nivel = new SerializedObject(inicio.gestorNivel);
        nivel.FindProperty("enemigosParaAvanzar").intValue = 10;
        nivel.FindProperty("spawnersRequeridos").intValue = 0;
        nivel.FindProperty("contarSoloEnemigosDeSpawners").boolValue = true;
        nivel.FindProperty("llavePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TarjetaAcceso.prefab");
        nivel.ApplyModifiedPropertiesWithoutUndo();

        var luz = new GameObject("LuzGameplay").AddComponent<Light2D>();
        luz.transform.SetParent(raiz.transform);
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 0f;

        var spawners = new GameObject("SpawnersSorbos").transform;
        spawners.SetParent(raiz.transform);
        var posiciones = new[]
        {
            new Vector3(14.28f, -8.29f, 0f),
            new Vector3(25.04f, -18.19f, 0f),
            new Vector3(32.61f, -7.79f, 0f)
        };
        for (int i = 0; i < 3; i++)
        {
            var go = Instanciar("Assets/Prefabs/Spawner_lvl" + (i + 1) + ".prefab", spawners, escena);
            go.transform.position = posiciones[i];
            go.GetComponent<EnemySpawner>().evitarObstaculos = true;
            foreach (var r in go.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
        }

        var puertaFinal = escena.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<E3Puerta>(true))
            .FirstOrDefault(p => p.esSalida || p.name == "E3_PuertaFinal");

        Vector3 salida = puertaFinal != null ? puertaFinal.transform.position : new Vector3(42.6f, -4.74f, 0f);
        if (puertaFinal != null)
        {
            puertaFinal.enabled = false;
            var aviso = puertaFinal.transform.parent != null ? puertaFinal.transform.parent.Find("E3_PuertaFinal_Aviso") : null;
            if (aviso != null) aviso.gameObject.SetActive(false);
        }

        var puerta = Instanciar("Assets/Prefabs/PuertaTransicion.prefab", raiz.transform, escena);
        puerta.transform.position = salida;
        puerta.transform.localScale = Vector3.one;
        var collider = puerta.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(2.16f, 1.1f);
        collider.offset = Vector2.zero;
        collider.isTrigger = true;

        var logica = puerta.GetComponent<PuertaNivel>();
        logica.nombreSiguienteEscena = "Nivel1";
        logica.textoMensajeUI = ObtenerOCrearMensajeTarjeta(hud);

        for (int i = 0; i < 3; i++)
        {
            string nombre = new[] { "Vendas", "Estimulante", "Botiquin" }[i];
            var item = Instanciar("Assets/Prefabs/" + nombre + ".prefab", raiz.transform, escena);
            item.transform.position = new Vector3(6.5f + i * 0.6f, -9.0f, 0f);
        }

        var botiquinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BotiquinInteractuable.prefab");
        if (botiquinPrefab != null)
        {
            var botiquin = (GameObject)PrefabUtility.InstantiatePrefab(botiquinPrefab, escena);
            botiquin.name = "BotiquinInteractuable";
            botiquin.transform.position = new Vector3(6.0f, -18.1f, 0f);
        }

        EditorSceneManager.SaveScene(escena);
    }

    private static TMPro.TextMeshProUGUI ObtenerOCrearMensajeTarjeta(GameObject hud)
    {
        var existente = hud.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
            .FirstOrDefault(t => t.name == "MensajeTarjeta" || t.name == "AlertaLlave");
        if (existente != null) return existente;

        var canvas = hud.GetComponentInChildren<Canvas>(true);
        var mensaje = new GameObject("MensajeTarjeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        mensaje.transform.SetParent(canvas.transform, false);
        var texto = mensaje.GetComponent<TMPro.TextMeshProUGUI>();
        texto.fontSize = 24;
        texto.alignment = TMPro.TextAlignmentOptions.Center;
        texto.rectTransform.anchoredPosition = new Vector2(0, -100);
        texto.rectTransform.sizeDelta = new Vector2(650, 50);
        return texto;
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
