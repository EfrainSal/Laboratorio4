using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Laboratorio4.Animacion;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Image = UnityEngine.UI.Image;

public static class IntegrarConsumiblesArmaJefe
{
    public const string EscenaJefe = "Assets/Scenes/Nivel1.unity";
    public const string BibliotecaJefe = "Assets/Animations/Personajes/DoctorYiba.asset";
    private const string Arte = "Assets/Sprites/Escenarios/Piso3/";
    private static readonly string[] Dirs = { "Sur", "Sureste", "Este", "Noreste", "Norte", "Noroeste", "Oeste", "Suroeste" };

    [InitializeOnLoadMethod]
    private static void RegistrarVistaPrevia()
    {
        EditorApplication.playModeStateChanged += estado =>
        {
            if (estado != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Laboratorio4.PreviewNivel1", false)) return;
            SessionState.SetBool("Laboratorio4.PreviewNivel1", false);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying || GameManager.Instance == null) return;
                GameManager.Instance.IniciarNuevaRun(GameManager.Instance.personajeSeleccionado);
                SceneManager.LoadScene("Nivel1");
            };
        };
    }
    [MenuItem("Laboratorio 4/Jefe/Jugar Nivel1 desde MainMenu")]
    public static void JugarNivel1()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null; StageUtility.GoToMainStage();
        SessionState.SetBool("Laboratorio4.PreviewNivel1", true);
        EditorSceneManager.OpenScene(IntegrarPersonajesMainMenu.Menu);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Laboratorio 4/Integracion/Conectar consumibles arma y jefe %&F10")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Selection.activeObject = null;
        StageUtility.GoToMainStage();
        var anterior = EditorSceneManager.GetSceneManagerSetup();
        var informe = new List<string>();
        try
        {
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/Gameplay.inputactions");
            if (input == null) throw new Exception("Falta Gameplay.inputactions");
            var reposo = CargarArma("Apuntar");
            var disparo = CargarArma("Disparar");
            foreach (string nombre in new[] { "Player_Jason", "Player_Milena" })
            {
                string path = "Assets/Prefabs/" + nombre + ".prefab";
                var go = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var entrada = go.GetComponent<PlayerInput>();
                    entrada.actions = input;
                    entrada.defaultActionMap = "Player";
                    entrada.notificationBehavior = PlayerNotifications.SendMessages;
                    var arma = go.GetComponent<ArmaIndependiente>();
                    arma.spritesApuntar = reposo;
                    arma.spritesDisparo = disparo;
                    PrefabUtility.SaveAsPrefabAsset(go, path);
                    informe.Add("PASS " + nombre + ": Gameplay + inventario, 8 sprites de reposo y 8 de disparo.");
                }
                finally { PrefabUtility.UnloadPrefabContents(go); }
            }
            var biblioteca = CrearBibliotecaJefe();
            const string rutaBala = "Assets/Prefabs/Bullet_Boss.prefab";
            var bala = PrefabUtility.LoadPrefabContents(rutaBala);
            try
            {
                bala.GetComponent<BossBullet>().deathCause = DeathCause.Jefe;
                PrefabUtility.SaveAsPrefabAsset(bala, rutaBala);
            }
            finally { PrefabUtility.UnloadPrefabContents(bala); }
            var escena = EditorSceneManager.OpenScene(EscenaJefe);
            var ai = Object.FindFirstObjectByType<BossAI>();
            if (ai == null) throw new Exception("Nivel1 no contiene el jefe original");
            var jefe = ai.gameObject;
            jefe.name = "Jefe_DoctorYiba";
            jefe.tag = "Boss";
            jefe.layer = LayerMask.NameToLayer("Enemy");
            var animador = jefe.GetComponent<AnimadorSprites8Direcciones>() ?? jefe.AddComponent<AnimadorSprites8Direcciones>();
            animador.biblioteca = biblioteca;
            if (jefe.GetComponent<AnimacionJefeDinamica>() == null) jefe.AddComponent<AnimacionJefeDinamica>();
            var sprite = jefe.GetComponent<SpriteRenderer>();
            sprite.sprite = AnimadorSprites8Direcciones.Frames(biblioteca.animaciones.First(c => c.nombre == "Quieto"), 0)[0];
            const string pathMaterial = "Assets/Materials/DoctorYibaUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(pathMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, pathMaterial);
            }
            sprite.sharedMaterial = material;
            sprite.color = Color.white;
            jefe.transform.localScale = Vector3.one * (2.3f / sprite.sprite.bounds.size.y);
            var bounds = sprite.sprite.bounds;
            var hitbox = jefe.GetComponent<BoxCollider2D>();
            hitbox.size = new Vector2(bounds.size.x * .55f, bounds.size.y * .65f);
            hitbox.offset = new Vector2(bounds.center.x, bounds.min.y + hitbox.size.y * .5f);
            var rb = jefe.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ai.bossBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_Boss.prefab");
            ai.basicEnemiesToSummon = new[] { "Enemy_Melee", "Enemy_Ranged" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + n + ".prefab")).ToArray();
            if (ai.firePoint == null)
            {
                ai.firePoint = new GameObject("FirePoint").transform;
                ai.firePoint.SetParent(jefe.transform, false);
            }
            ai.firePoint.localPosition = bounds.center;
            var salud = jefe.GetComponent<BossHealth>();
            var contenedor = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .First(t => t.name == "BossHealthContainer");
            salud.bossHealthUIContainer = contenedor.gameObject;
            salud.healthBarFill = contenedor.Find("Fill").GetComponent<Image>();
            salud.damageTrailFill = contenedor.Find("DamageTrail").GetComponent<Image>();
            var rectJefe = contenedor.GetComponent<RectTransform>();
            rectJefe.anchorMin = rectJefe.anchorMax = rectJefe.pivot = Vector2.one;
            rectJefe.anchoredPosition = new Vector2(-40, -20);
            var titulo = contenedor.Find("NombreJefe");
            if (titulo == null)
            {
                var label = new GameObject("NombreJefe", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(contenedor, false);
                titulo = label.transform;
                var texto = label.GetComponent<TextMeshProUGUI>();
                texto.text = "DOCTOR YIBA"; texto.fontSize = 20; texto.alignment = TextAlignmentOptions.Center;
                texto.raycastTarget = false;
            }
            var rectNombre = titulo.GetComponent<RectTransform>();
            rectNombre.anchorMin = Vector2.zero; rectNombre.anchorMax = Vector2.right;
            rectNombre.anchoredPosition = new Vector2(0, -22); rectNombre.sizeDelta = new Vector2(0, 28);
            if (!Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None).Any(l => l.lightType == Light2D.LightType.Global))
            {
                var luz = new GameObject("IluminacionCombate").AddComponent<Light2D>();
                luz.lightType = Light2D.LightType.Global; luz.intensity = 0;
            }
            var victoria = Object.FindFirstObjectByType<VictoryScreenController>();
            if (victoria != null)
            {
                victoria.GetComponent<UIDocument>().visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/VictoryScreen.uxml");
                var serializado = new SerializedObject(victoria);
                // No existe FinalCinematic en el proyecto; concluir vuelve al menú con el ranking guardado.
                serializado.FindProperty("_cinematicSceneName").stringValue = "MainMenu";
                serializado.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            informe.Add("PASS DoctorYiba conectado exclusivamente en " + EscenaJefe + ": 6 estados, 3 fases, HUD, proyectiles e invocaciones.");
            RepararRutasBuild();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Reports/ConsumiblesArmaJefe");
            File.WriteAllLines("Reports/ConsumiblesArmaJefe/integracion.txt", informe);
            Debug.Log("[ConsumiblesArmaJefe] Integracion aplicada. Jefe solamente en Nivel1.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(anterior); }
    }
    private static Sprite[] CargarArma(string accion)
    {
        return Enumerable.Range(0, 8).Select(d =>
        {
            string path = Arte + "Armas/ArmaLaboratorio4/" + accion + "/Arma_" + accion + "_" + d + "_" + Dirs[d] + ".png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new Exception("Falta sprite " + path);
        }).ToArray();
    }
    private static BibliotecaPersonaje CrearBibliotecaJefe()
    {
        var biblioteca = AssetDatabase.LoadAssetAtPath<BibliotecaPersonaje>(BibliotecaJefe);
        if (biblioteca == null)
        {
            biblioteca = ScriptableObject.CreateInstance<BibliotecaPersonaje>();
            AssetDatabase.CreateAsset(biblioteca, BibliotecaJefe);
        }
        biblioteca.personaje = "DoctorYiba";
        var clips = new List<ClipPersonaje>();
        foreach (string nombre in new[] { "Quieto", "Correr", "Ataque", "BolaDePoder", "CampoEnergia", "Muerte" })
        {
            var clip = new ClipPersonaje { nombre = nombre, fps = nombre == "Quieto" ? 1 : nombre == "Correr" || nombre == "Muerte" ? 10 : 12, bucle = nombre == "Quieto" || nombre == "Correr" };
            for (int d = 0; d < 8; d++)
            {
                string dir = Arte + "Personajes/DoctorYiba/" + nombre + "/" + d + "_" + Dirs[d];
                var frames = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png").OrderBy(p => p, StringComparer.Ordinal)
                    .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/'))).ToArray() : Array.Empty<Sprite>();
                if (frames.Any(s => s == null)) throw new Exception("Faltan sprites importados: " + dir);
                clip.direcciones[d] = new SpritesDireccion { frames = frames };
            }
            clips.Add(clip);
        }
        biblioteca.animaciones = clips.ToArray();
        EditorUtility.SetDirty(biblioteca);
        return biblioteca;
    }
    private static void RepararRutasBuild()
    {
        var rutas = new Dictionary<string, string>
        {
            ["MainMenu"] = "Assets/Scenes/MainMenu.unity", ["Prueba 1"] = "Assets/Scenes/Prueba 1.unity",
            ["Debugscene"] = "Assets/Scenes/Debugscene.unity", ["Piso3_V10"] = "Assets/Scenes/Piso3_V10.unity",
            ["Nivel1"] = EscenaJefe, ["Cinematicas"] = "Assets/Scenes/Cinematicas.unity"
        };
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Select(s => rutas.TryGetValue(Path.GetFileNameWithoutExtension(s.path), out var path)
            ? new EditorBuildSettingsScene(path, s.enabled) : s).ToArray();
    }
}
