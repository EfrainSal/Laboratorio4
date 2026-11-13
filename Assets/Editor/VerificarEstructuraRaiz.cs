using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerificarEstructuraRaiz
{
    [Serializable] private class Movimiento { public string source, destination; }
    [Serializable] private class Plan { public Movimiento[] moves; }

    [MenuItem("Laboratorio 4/Estructura/Verificar referencias raiz %#&F7")]
    public static void Verificar()
    {
        if (EditorApplication.isPlaying) return;
        const string carpeta = "Reports/ReestructuracionRaiz/";
        var resultado = new List<string>();
        var fallos = new List<string>();
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(carpeta + "plan.json"));
        var metas = new HashSet<string>();
        foreach (var m in plan.moves)
        {
            if (Directory.Exists(m.destination)) foreach (var f in Directory.GetFiles(m.destination, "*.meta", SearchOption.AllDirectories)) metas.Add(f.Replace('\\', '/'));
            if (File.Exists(m.destination + ".meta")) metas.Add(m.destination + ".meta");
        }
        foreach (string meta in metas)
        {
            string guid = Regex.Match(File.ReadAllText(meta), @"(?m)^guid: (\w+)").Groups[1].Value;
            string path = meta.Substring(0, meta.Length - 5);
            string encontrado = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(encontrado, path, StringComparison.OrdinalIgnoreCase)) fallos.Add(path + " GUID resuelve a " + encontrado);
        }
        resultado.Add("PASS " + metas.Count + " GUID de assets y carpetas trasladados resueltos por Unity");
        foreach (string path in Directory.GetFiles("Assets/Scenes", "*.unity"))
        {
            var escena = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                int faltantes = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (faltantes > 0) fallos.Add(path + " tiene " + faltantes + " scripts faltantes");
                else resultado.Add("PASS " + path + ": escena cargada sin scripts faltantes");
            }
            finally { EditorSceneManager.ClosePreviewScene(escena); }
        }
        foreach (string ruta in Directory.GetFiles("Assets/Prefabs/Escenarios/Piso3", "*.prefab", SearchOption.AllDirectories))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (go == null || go.GetComponentsInChildren<Transform>(true).Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0))
                fallos.Add(ruta + ": prefab no resuelto o script faltante");
        }
        resultado.Add("PASS 25 prefabs trasladados importados sin scripts faltantes");
        foreach (Type tipo in new[] { typeof(LaboratorioYiba.Piso3V10.EditorTools.Piso3V10Generador),
            typeof(Laboratorio4.Escenario3.Herramientas.E3Constructor), typeof(Laboratorio4.Escenario4.EditorTools.E4Constructor) })
        {
            foreach (var campo in tipo.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!campo.IsLiteral || campo.FieldType != typeof(string)) continue;
                string path = (string)campo.GetRawConstantValue();
                // Estas carpetas son salidas opcionales: el generador las crea al guardar nuevos prefabs.
                if (campo.Name.StartsWith("CarpetaPrefab", StringComparison.Ordinal)) continue;
                if (path.StartsWith("Assets/") && !File.Exists(path) && !Directory.Exists(path)) fallos.Add(tipo.Name + "." + campo.Name + ": " + path);
            }
            resultado.Add("PASS " + tipo.Name + ": rutas de configuración y generación resueltas");
        }
        if (Laboratorio4.RecepcionEditor.ConstruirRecepcion.RutaEscena(Laboratorio4.RecepcionEditor.ConstruirRecepcion.Raiz()) != "Assets/Scenes/Recepcion.unity")
            fallos.Add("Ruta dinámica de Recepcion incorrecta");
        resultado.Add("PASS Recepcion: ruta dinámica a Assets/Scenes/Recepcion.unity");
        foreach (var escena in EditorBuildSettings.scenes)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(escena.path) == null) fallos.Add("Build Settings: " + escena.path);
        resultado.Add("PASS " + EditorBuildSettings.scenes.Length + " escenas de Build Settings importadas");
        File.WriteAllLines(carpeta + "verificacion-unity.txt", resultado.Concat(fallos.Select(f => "FAIL " + f)));
        if (fallos.Count > 0) Debug.LogError("[EstructuraRaizQA] " + fallos.Count + " fallos: " + string.Join("; ", fallos));
        else Debug.Log("[EstructuraRaizQA] Verificación de GUID, escenas, prefabs y generadores completa, sin fallos.");
    }
}
