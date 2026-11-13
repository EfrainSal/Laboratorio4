using System;
using System.IO;
using System.Linq;
using Laboratorio4.Animacion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IntegrarGameplayPiso3
{
    public const string Escena = "Assets/Scenes/Piso3_V10.unity";
    public const string HudPrefab = "Assets/Prefabs/UI/HUDGameplay.prefab";

    [MenuItem("Laboratorio 4/Gameplay/Integrar HUD armas y spawners")]
    public static void Integrar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        IntegrarPersonajesMainMenu.Integrar();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var destino = EditorSceneManager.OpenScene(Escena);
            CrearPrefabHUD(destino);
            var anterior = destino.GetRootGameObjects().FirstOrDefault(g => g.name == "HUDGameplay");
            if (anterior != null) UnityEngine.Object.DestroyImmediate(anterior);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefab), destino);
            hud.name = "HUDGameplay";
            var inicio = UnityEngine.Object.FindFirstObjectByType<InicioPersonajesDesdeMenu>();
            if (inicio == null) throw new Exception("Falta la integracion de MainMenu en Piso3");
            inicio.gestorNivel = hud.GetComponentInChildren<LevelManager>(true);
            inicio.gestorNivel.puntoDeInicio.position = inicio.jugadorOriginal.position;
            if (inicio.gestorNivel.healthBarFillUI == null || hud.GetComponentInChildren<DeathScreenController>(true) == null)
                throw new Exception("Las referencias de Debugscene no se copiaron completas");

            var viejo = destino.GetRootGameObjects().FirstOrDefault(g => g.name == "SpawnersSorbos");
            if (viejo != null) UnityEngine.Object.DestroyImmediate(viejo);
            var grupo = new GameObject("SpawnersSorbos");
            var plantilla = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawner_lvl2.prefab");
            int cantidad = Mathf.Min(3, inicio.sorbosOriginales.Length);
            for (int i = 0; i < cantidad; i++)
            {
                int indice = cantidad == 1 ? 0 : Mathf.RoundToInt(i * (inicio.sorbosOriginales.Length - 1f) / (cantidad - 1f));
                var go = (GameObject)PrefabUtility.InstantiatePrefab(plantilla, destino);
                go.transform.SetParent(grupo.transform, false);
                go.transform.position = inicio.sorbosOriginales[indice].transform.position;
                go.name = "Spawner_Sorbos_" + (i + 1);
                var spawner = go.GetComponent<EnemySpawner>();
                spawner.enemyPrefabs = new[] { inicio.sorboA, inicio.sorboB };
                spawner.activationRadius = 5f;
                spawner.minEnemies = 1; spawner.maxEnemies = 3; spawner.spawnRadius = 0.5f;
                foreach (var render in go.GetComponentsInChildren<SpriteRenderer>()) render.enabled = false;
            }
            var nivel = new SerializedObject(inicio.gestorNivel);
            nivel.FindProperty("spawnersRequeridos").intValue = cantidad;
            nivel.ApplyModifiedPropertiesWithoutUndo();
            foreach (string nombre in new[] { "Player_Jason", "Player_Milena", "Enemy_Melee", "Enemy_Ranged" })
            {
                string path = IntegrarPersonajesMainMenu.Prefabs + nombre + ".prefab";
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    prefab.GetComponent<SpriteRenderer>().sharedMaterial = inicio.materialEscenario;
                    var arma = prefab.GetComponent<ArmaIndependiente>();
                    if (arma != null) arma.visual.sharedMaterial = inicio.materialEscenario;
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            EditorSceneManager.SaveScene(destino);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Reports/Gameplay");
            File.WriteAllText("Reports/Gameplay/integracion.txt", "HUD, DeathScreen, PauseMenu y LevelManager reutilizados de Debugscene.\nUn solo jugador desde MainMenu.\nArma independiente y mira procedural en ambos prefabs.\n" + cantidad + " spawners originales con ambos Sorbos, radio 5, cooldown 10, oleadas de 1 a 3.\n");
            Debug.Log("[Gameplay] HUD, Game Over, arma, mira y spawners integrados.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static void CrearPrefabHUD(Scene destino)
    {
        var referencia = EditorSceneManager.OpenScene("Assets/Scenes/Debugscene.unity", OpenSceneMode.Additive);
        try
        {
            var raiz = new GameObject("HUDGameplay");
            SceneManager.MoveGameObjectToScene(raiz, referencia);
            var nombres = new[] { "Canvas", "PauseMenu", "UpgradePanel", "DeathScreen", "LevelManager", "EventSystem", "PlayerSpawn" };
            var objetos = referencia.GetRootGameObjects();
            foreach (string nombre in nombres)
            {
                var objeto = objetos.FirstOrDefault(g => g.name == nombre);
                if (objeto == null) throw new Exception("Falta " + nombre + " en Debugscene");
                objeto.transform.SetParent(raiz.transform, true);
            }
            Directory.CreateDirectory("Assets/Prefabs/UI");
            AssetDatabase.Refresh();
            PrefabUtility.SaveAsPrefabAsset(raiz, HudPrefab);
        }
        finally
        {
            EditorSceneManager.CloseScene(referencia, true);
            SceneManager.SetActiveScene(destino);
        }
    }
}
