using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Aplica exclusivamente el manifiesto revisado. Conserva .meta y GUID mediante AssetDatabase.
public static class ReestructurarCarpetasRaiz
{
    [Serializable] private class Movimiento { public string source, destination; }
    [Serializable] private class Cambio { public string path, staged; }
    [Serializable] private class Plan { public Movimiento[] moves; public Cambio[] rewrites; public string[] roots; }
    private const string Informe = "Reports/ReestructuracionRaiz/";

    [MenuItem("Laboratorio 4/Estructura/Aplicar estructura raiz %#&F8")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Informe + "plan.json"));
        if (File.Exists(Informe + "aplicado.txt")) throw new InvalidOperationException("La migración ya fue aplicada.");
        foreach (var m in plan.moves)
        {
            Comprobar(m.source); Comprobar(m.destination);
            if (File.Exists(m.destination) || Directory.Exists(m.destination)) throw new IOException("Destino ocupado: " + m.destination);
        }
        foreach (var c in plan.rewrites) { Comprobar(c.path); Comprobar(c.staged); }
        Selection.activeObject = null; StageUtility.GoToMainStage();
        var build = EditorBuildSettings.scenes.Select(s => new EditorBuildSettingsScene(Mapear(s.path, plan), s.enabled)).ToArray();
        var completados = new List<string>();
        EditorApplication.LockReloadAssemblies();
        AssetDatabase.DisallowAutoRefresh();
        try
        {
            foreach (var m in plan.moves)
            {
                Asegurar(Path.GetDirectoryName(m.destination).Replace('\\', '/'));
                string error = AssetDatabase.MoveAsset(m.source, m.destination);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                completados.Add(m.source + " -> " + m.destination);
                File.WriteAllLines(Informe + "movimientos-aplicados.txt", completados);
            }
            foreach (var c in plan.rewrites) File.WriteAllBytes(c.path, File.ReadAllBytes(c.staged));
            // Sólo quedan contenedores vacíos y sus metas. Archivarlos fuera de Assets, sin borrar contenido.
            foreach (string source in plan.roots)
            {
                Comprobar(source);
                string[] restos = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                if (restos.Any(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
                    throw new IOException("Quedó contenido sin trasladar en " + source);
                string archivo = Informe + "contenedores-archivados/" + source;
                Directory.CreateDirectory(Path.GetDirectoryName(archivo));
                Directory.Move(Path.GetFullPath(source), Path.GetFullPath(archivo));
                if (File.Exists(source + ".meta")) File.Move(source + ".meta", archivo + ".meta");
            }
            File.WriteAllText(Informe + "aplicado.txt", "Aplicados " + completados.Count + " movimientos, sin sobrescribir assets ni modificar Resources.\n");
        }
        finally
        {
            AssetDatabase.AllowAutoRefresh();
            EditorApplication.UnlockReloadAssemblies();
        }
        EditorBuildSettings.scenes = build;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[EstructuraRaiz] Migración aplicada. Assets en carpetas principales; GUID conservados y Resources excluido.");
    }
    private static string Mapear(string path, Plan plan)
    {
        foreach (var m in plan.moves.OrderByDescending(x => x.source.Length))
            if (path == m.source || path.StartsWith(m.source + "/", StringComparison.Ordinal)) return m.destination + path.Substring(m.source.Length);
        return path;
    }
    private static void Asegurar(string path)
    {
        Comprobar(path);
        if (AssetDatabase.IsValidFolder(path)) return;
        string padre = Path.GetDirectoryName(path).Replace('\\', '/');
        Asegurar(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(path));
    }
    private static void Comprobar(string path)
    {
        string raiz = Path.GetFullPath(".").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string absoluto = Path.GetFullPath(path);
        string protegido = Path.GetFullPath("Assets/Resources");
        if (!absoluto.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)
            || absoluto.Equals(protegido, StringComparison.OrdinalIgnoreCase)
            || absoluto.StartsWith(protegido + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ruta fuera del ámbito permitido: " + path);
    }
}
