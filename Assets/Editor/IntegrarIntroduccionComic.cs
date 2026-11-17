using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class IntegrarIntroduccionComic
{
    public const string Escena = "Assets/Scenes/IntroduccionComic.unity";
    [MenuItem("Laboratorio 4/Cómic/Integrar introducción visual")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var configuracion = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ImportarPagina("Llegada"); ImportarPagina("RutaJason"); ImportarPagina("RutaMilena");
            var escena = File.Exists(Escena) ? EditorSceneManager.OpenScene(Escena) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var reproductor = Object.FindFirstObjectByType<IntroduccionComic>();
            if (reproductor == null) reproductor = new GameObject("ReproductorComic").AddComponent<IntroduccionComic>();
            // Recargar los subassets después de importar las tres páginas evita referencias descargadas.
            reproductor.llegada = Vinetas("Llegada"); reproductor.rutaJason = Vinetas("RutaJason"); reproductor.rutaMilena = Vinetas("RutaMilena");
            reproductor.escenaDestino = "Recepcion";
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camara = new GameObject("CamaraComic").AddComponent<Camera>();
                camara.clearFlags = CameraClearFlags.SolidColor; camara.backgroundColor = Color.black; camara.orthographic = true;
                camara.transform.position = new Vector3(0, 0, -10);
            }
            if (escena.GetRootGameObjects().Any(r => r.GetComponentsInChildren<AudioSource>(true).Length > 0 || r.GetComponentsInChildren<AudioListener>(true).Length > 0))
                throw new Exception("La escena del cómic debe permanecer sin componentes de audio");
            EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena, Escena);
            var escenas = EditorBuildSettings.scenes.ToList();
            int indice = escenas.FindIndex(s => s.path == Escena);
            if (indice < 0) escenas.Add(new EditorBuildSettingsScene(Escena, true)); else escenas[indice].enabled = true;
            EditorBuildSettings.scenes = escenas.ToArray();
            AssetDatabase.SaveAssets(); Directory.CreateDirectory("Reports/ComicIntro");
            File.WriteAllText("Reports/ComicIntro/integracion.txt", "PASS: 12 sprites recortados sin cabeceras; 8 viñetas por personaje; escena habilitada; ningún audio añadido.\n");
            Debug.Log("[ComicIntro] Introducción visual integrada. Ocho viñetas por personaje, sin audio ni cabeceras.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(configuracion); }
    }
    public static Sprite[] Vinetas(string pagina) => AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/CinematicaIntro/" + pagina + ".png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
    private static void ImportarPagina(string nombre)
    {
        string path = "Assets/Sprites/CinematicaIntro/" + nombre + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.spritePixelsPerUnit = 100;
        importer.GetSourceTextureWidthAndHeight(out int ancho, out int alto);
        if (ancho != 1536 || alto != 1024) throw new Exception("Cambió el tamaño de la página aprobada: " + path);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        // Los 48 px superiores son únicamente el título de página. Los bocadillos y cartelas narrativas se conservan.
        Rect[] regiones = { new Rect(8, 512, 752, 464), new Rect(776, 512, 752, 464), new Rect(8, 8, 752, 488), new Rect(776, 8, 752, 488) };
        var previos = provider.GetSpriteRects();
        var rects = regiones.Select((rect, i) => {
            string id = nombre + "_" + (i + 1).ToString("00");
            return new SpriteRect { name = id, rect = rect, spriteID = previos.FirstOrDefault(p => p.name == id)?.spriteID ?? GUID.Generate(), alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) };
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        if (Vinetas(nombre).Length != 4) throw new Exception("No se importaron cuatro viñetas: " + path);
    }
}
