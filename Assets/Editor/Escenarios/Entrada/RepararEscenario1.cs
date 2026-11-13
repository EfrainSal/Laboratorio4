#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Laboratorio4.EditorTools
{
    /// <summary>Menú: Laboratorio 4 > Reparar Escenario 1 (escena abierta). Repara sin mover nada y guarda.</summary>
    public static class RepararEscenario1
    {
        [MenuItem("Laboratorio 4/Reparar Escenario 1 (escena abierta)")]
        public static void Reparar()
        {
            var escena = EditorSceneManager.GetActiveScene();
            if (!escena.name.Contains("Escenario1"))
            {
                EditorUtility.DisplayDialog("Laboratorio 4", "Abre primero la escena Escenario1_Entrada. Esta herramienta no toca otras escenas.", "OK");
                return;
            }
            string g = AssetDatabase.FindAssets("escenario1_layout t:TextAsset").FirstOrDefault();
            var layout = g == null ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(g));
            var mods = new List<Object>();
            string r = ReparacionEscenario1.Reparar(layout, true, mods);
            if (Object.FindFirstObjectByType<GuardianEscenario1>() == null)
            {
                var go = GameObject.Find("SeleccionPersonaje") ?? new GameObject("E1_Guardian");
                go.AddComponent<GuardianEscenario1>().layout = layout;
                r += " Añadido GuardianEscenario1.";
            }
            foreach (var o in mods) if (o != null) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log(r);
            EditorUtility.DisplayDialog("Laboratorio 4", r, "OK");
        }
    }
}
#endif
