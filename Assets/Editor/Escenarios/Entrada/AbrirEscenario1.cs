#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Laboratorio4.EditorTools
{
    /// <summary>
    /// Al importar el paquete (v4): borra los archivos de la v2 que ya no se usan, comprueba que
    /// existan los prefabs de objetos del proyecto, agrega la escena a Build Settings y ofrece abrirla.
    /// Menú: Laboratorio 4 > Abrir Escenario 1 (Entrada)
    /// </summary>
    [InitializeOnLoad]
    public static class AbrirEscenario1
    {
        [Serializable] class Lista { public string[] lista; }
        [Serializable] class ObjRef { public string nombre, prefab, prefabGuid; public bool propio; }
        [Serializable] class Layout { public ObjRef[] objetos; }

        static string Clave => "Laboratorio4_Escenario1_v6_Instalado_" + Application.dataPath;

        static AbrirEscenario1() { EditorApplication.delayCall += PrimeraVez; }

        static string RutaEscena()
        {
            string g = AssetDatabase.FindAssets("Escenario1_Entrada t:Scene").FirstOrDefault();
            return g == null ? null : AssetDatabase.GUIDToAssetPath(g);
        }

        static string Raiz(string rutaEscena) => "Assets";

        static void PrimeraVez()
        {
            if (EditorPrefs.GetBool(Clave, false)) return;
            string ruta = RutaEscena();
            if (ruta == null) return;
            EditorPrefs.SetBool(Clave, true);
            string raiz = Raiz(ruta);
            // La limpieza del paquete antiguo no se ejecuta sobre la estructura compartida.
            AgregarABuild(ruta);
            List<string> faltan = PrefabsQueFaltan(raiz);
            string aviso = faltan.Count == 0 ? "" :
                "\n\nAtención: no encontré estos prefabs en el proyecto (se verán como 'Missing Prefab'):\n" + string.Join(", ", faltan) +
                "\nSi los tienes con otro identificador, usa 'Laboratorio 4 > Reconstruir Escenario 1', que los busca por nombre.";
            if (EditorUtility.DisplayDialog("Laboratorio 4", "Se importó el Escenario 1 (versión 6).\n¿Quieres abrirlo ahora?" + aviso, "Abrir", "Después"))
                Abrir();
        }

        [MenuItem("Laboratorio 4/Abrir Escenario 1 (Entrada)")]
        public static void Abrir()
        {
            string ruta = RutaEscena();
            if (ruta == null) { Debug.LogError("[Laboratorio 4] No se encontró la escena Escenario1_Entrada."); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ruta);
        }

        static void Limpiar(string raiz)
        {
            string f = raiz + "/Settings/Escenarios/Entrada/limpieza.json";
            if (!File.Exists(f)) return;
            var l = JsonUtility.FromJson<Lista>(File.ReadAllText(f));
            var borrados = new List<string>();
            foreach (string rel in l.lista)
            {
                string r = raiz + "/" + rel;
                if (AssetDatabase.LoadMainAssetAtPath(r) != null && AssetDatabase.DeleteAsset(r)) borrados.Add(rel);
            }
            foreach (string sub in new[] { "/Sprites/Escenarios/Entrada/Objetos", "/Sprites/Escenarios/Entrada/Enemigos/Sorbos", "/Sprites/Escenarios/Entrada/Personajes/JasonSmith/Caminando", "/Sprites/Escenarios/Entrada/Personajes/JasonSmith/Estatico", "/Sprites/Escenarios/Entrada/Props", "/Sprites/Escenarios/Entrada/Mapa", "/Sprites/Escenarios/Entrada/Objetos_Escenario1" })
                if (AssetDatabase.IsValidFolder(raiz + sub) && AssetDatabase.FindAssets("", new[] { raiz + sub }).Length == 0) AssetDatabase.DeleteAsset(raiz + sub);
            if (borrados.Count > 0) Debug.Log("[Laboratorio 4] Limpieza de la versión anterior del Escenario 1: " + borrados.Count + " archivos.");
        }

        static List<string> PrefabsQueFaltan(string raiz)
        {
            var faltan = new List<string>();
            string f = raiz + "/Settings/Escenarios/Entrada/escenario1_layout.json";
            if (!File.Exists(f)) return faltan;
            var L = JsonUtility.FromJson<Layout>(File.ReadAllText(f));
            foreach (var o in L.objetos)
                if (!o.propio && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(o.prefabGuid)) && !faltan.Contains(o.prefab)) faltan.Add(o.prefab);
            return faltan;
        }

        static void AgregarABuild(string ruta)
        {
            var lista = EditorBuildSettings.scenes.ToList();
            if (lista.Any(s => s.path == ruta)) return;
            lista.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = lista.ToArray();
            Debug.Log("[Laboratorio 4] Escenario 1 agregado a Build Settings: " + ruta);
        }
    }
}
#endif
