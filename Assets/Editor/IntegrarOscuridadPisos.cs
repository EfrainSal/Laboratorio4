using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class IntegrarOscuridadPisos
{
    public const string Carpeta = "Reports/OscuridadPisos/";
    public static readonly string[] Mapas = {
        "Recepcion", "Escenario1_Entrada", "Piso3_V10", "E3_Laboratorio", "Escenario4_Jefe",
        "Nivel1", "Debugscene", "Prueba 1", "Piso3_Modular", "Piso3_Final_Modular"
    };
    private static readonly HashSet<string> ControladoresJugador = new HashSet<string> {
        "MovimientoPlayer", "Piso3V10Movimiento", "Piso3TestPlayer", "Piso3TestMovement", "ControladorJugador",
        "RecepcionJugador", "E3Jugador", "E4Jugador"
    };

    [MenuItem("Laboratorio 4/Iluminación/Aplicar oscuridad de Piso3 a los mapas %#&F11")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var anterior = EditorSceneManager.GetSceneManagerSetup();
        Selection.activeObject = null; StageUtility.GoToMainStage();
        var informe = new List<string>();
        try
        {
            foreach (string nombre in Mapas)
            {
                // Piso3 es la referencia visual: conservar su escena exactamente como está.
                if (nombre == "Piso3_V10") { informe.Add("PASS Piso3_V10 conservado como referencia."); continue; }
                var escena = EditorSceneManager.OpenScene("Assets/Scenes/" + nombre + ".unity");
                // Abrir otra escena puede descargar assets que sólo conserva una referencia de C#.
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Jason.prefab");
                var referencia = prefab.GetComponentsInChildren<Light2D>(true).Single(l => l.lightType == Light2D.LightType.Point);
                var material = prefab.GetComponent<SpriteRenderer>().sharedMaterial;
                int globales = 0, luces = 0, materiales = 0;
                foreach (var luz in Componentes<Light2D>(escena).Where(l => l.lightType == Light2D.LightType.Global))
                {
                    if (luz.intensity == 0) continue;
                    luz.intensity = 0; EditorUtility.SetDirty(luz);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(luz); globales++;
                }
                // También los personajes de los mapas de prototipo necesitan la misma luz local.
                // Los prefabs reales ya incluyen la luz y no se modifican ni duplican.
                foreach (var jugador in Componentes<MonoBehaviour>(escena).Where(c => c != null &&
                    ControladoresJugador.Contains(c.GetType().Name)).Select(c => c.gameObject).Distinct().ToArray())
                {
                    if (jugador.GetComponentsInChildren<Light2D>(true).Any(l => l.lightType != Light2D.LightType.Global)) continue;
                    var luz = Object.Instantiate(referencia.gameObject, jugador.transform, false);
                    luz.name = "LuzJugador_Piso3";
                    // Mantener el radio de mundo incluso en personajes de prototipo escalados.
                    var escala = jugador.transform.lossyScale;
                    luz.transform.localScale = new Vector3(1 / Mathf.Max(.001f, Mathf.Abs(escala.x)),
                        1 / Mathf.Max(.001f, Mathf.Abs(escala.y)), 1);
                    luces++;
                }
                foreach (var render in Componentes<SpriteRenderer>(escena))
                {
                    // Algunas piezas importadas de Entrada ignoraban las luces 2D.
                    if (render.sharedMaterial == null || render.sharedMaterial.shader.name != "Sprites/Default") continue;
                    render.sharedMaterial = material; EditorUtility.SetDirty(render);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(render); materiales++;
                }
                if (globales + luces + materiales > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena);
                }
                informe.Add("PASS " + nombre + ": luces globales atenuadas=" + globales +
                    ", luces locales añadidas=" + luces + ", materiales compatibles=" + materiales);
            }
            Directory.CreateDirectory(Carpeta); File.WriteAllLines(Carpeta + "integracion.txt", informe);
            Debug.Log("[OscuridadPisos] Oscuridad de Piso3 aplicada a los mapas jugables. Menú, cinemáticas y galería conservados.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(anterior); }
    }

    [MenuItem("Laboratorio 4/Iluminación/Revisar oscuridad de los mapas %#&F10")]
    public static void Revisar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var anterior = EditorSceneManager.GetSceneManagerSetup();
        var informe = new List<string>();
        Selection.activeObject = null; StageUtility.GoToMainStage();
        try
        {
            foreach (string nombre in Mapas)
            {
                var escena = EditorSceneManager.OpenScene("Assets/Scenes/" + nombre + ".unity");
                informe.Add("SCENE " + nombre);
                foreach (var luz in Componentes<Light2D>(escena))
                    informe.Add("LIGHT " + Ruta(luz.transform) + " active=" + luz.gameObject.activeInHierarchy + " type=" + luz.lightType +
                        " intensity=" + luz.intensity + " radius=" + luz.pointLightOuterRadius + " scale=" + luz.transform.lossyScale);
                foreach (var grupo in Componentes<SpriteRenderer>(escena).GroupBy(r => r.sharedMaterial != null ? r.sharedMaterial.shader.name : "null"))
                    informe.Add("MATERIAL " + grupo.Key + " count=" + grupo.Count() + " example=" + Ruta(grupo.First().transform));
                foreach (var grupo in Componentes<MonoBehaviour>(escena).Where(c => c != null &&
                    (c.GetType().Name.Contains("Jugador") || c.GetType().Name.Contains("Movimiento") || c.GetType().Name.Contains("Player")))
                    .GroupBy(c => c.GetType().FullName))
                    informe.Add("PLAYER " + grupo.Key + " " + string.Join(", ", grupo.Select(c => Ruta(c.transform))));
            }
            Directory.CreateDirectory(Carpeta); File.WriteAllLines(Carpeta + "auditoria.txt", informe);
            Debug.Log("[OscuridadPisos] Auditoría terminada.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(anterior); }
    }
    public static IEnumerable<T> Componentes<T>(Scene escena) where T : Component =>
        escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    public static string Ruta(Transform t) => t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;
}
