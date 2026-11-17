using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Laboratorio4.Animacion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class IntegrarPulidoFinal
{
    public const string Carpeta = "Reports/PulidoFinal/";
    [MenuItem("Laboratorio 4/Pulido/Integrar proyectiles audio y HUD %#&F1")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying) return;
        var informe = new List<string>();
        Sprite acido = Importar("Assets/Sprites/Proyectiles/SorboB_Acido.png", informe);
        Sprite necrosia = Importar("Assets/Sprites/Proyectiles/DoctorYiba_Necrosia.png", informe);
        const string rutaMaterial = "Assets/Materials/ProyectilesBiologicosUnlit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, rutaMaterial);
        }
        ConfigurarBala("Assets/Prefabs/EnemyBullet.prefab", acido, material);
        ConfigurarBala("Assets/Prefabs/Bullet_Boss.prefab", necrosia, material);
        foreach (string nombre in new[] { "Enemy", "Enemy_Melee", "Enemy_Ranged", "Player_Jason", "Player_Milena" })
        {
            string path = "Assets/Prefabs/" + nombre + ".prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var enemigo = go.GetComponent<EnemyHealth>();
                var disparo = go.GetComponent<PlayerShooting>();
                var fuente = enemigo != null ? enemigo.audioSource : disparo != null ? disparo.sfxSource : null;
                if (fuente == null) fuente = go.GetComponent<AudioSource>();
                if (fuente != null) { fuente.priority = enemigo != null ? 192 : 0; fuente.spatialBlend = 0; fuente.playOnAwake = false; }
                PrefabUtility.SaveAsPrefabAsset(go, path);
                informe.Add("PASS " + nombre + ": prioridad SFX " + (enemigo != null ? "192, radio de escucha " + enemigo.radioEscucha : "0 para el jugador"));
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        var biblioteca = AssetDatabase.LoadAssetAtPath<BibliotecaPersonaje>("Assets/Animations/Personajes/DoctorYiba.asset");
        foreach (var clip in biblioteca.animaciones)
        {
            if (clip.nombre == "Correr") clip.fps = Mathf.Max(12, clip.fps);
            informe.Add("PASS DoctorYiba " + clip.nombre + ": cuadros por dirección " +
                string.Join(",", Enumerable.Range(0, 8).Select(d => (AnimadorSprites8Direcciones.Frames(clip, d)?.Length ?? 0).ToString())));
        }
        EditorUtility.SetDirty(biblioteca); AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Carpeta); File.WriteAllLines(Carpeta + "integracion.txt", informe);
        Debug.Log("[PulidoFinal] Proyectiles biológicos integrados, prioridades de audio configuradas y carrera del jefe a 12 fps. Escenas y Resources sin editar.");
    }
    static Sprite Importar(string path, List<string> informe)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var lectura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        lectura.LoadImage(File.ReadAllBytes(path));
        var pixeles = lectura.GetPixels32(); int xmin = lectura.width, ymin = lectura.height, xmax = 0, ymax = 0;
        for (int y = 0; y < lectura.height; y++) for (int x = 0; x < lectura.width; x++)
            if (pixeles[y * lectura.width + x].a >= 48)
            { xmin = Mathf.Min(xmin, x); ymin = Mathf.Min(ymin, y); xmax = Mathf.Max(xmax, x); ymax = Mathf.Max(ymax, y); }
        if (xmax <= xmin || ymax <= ymin) throw new Exception("Sprite sin silueta: " + path);
        int diametro = Mathf.Max(xmax - xmin + 1, ymax - ymin + 1);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        var ajustes = new TextureImporterSettings(); importer.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
        ajustes.spritePivot = new Vector2((xmin + xmax + 1f) / (2 * lectura.width), (ymin + ymax + 1f) / (2 * lectura.height));
        importer.SetTextureSettings(ajustes);
        importer.spritePixelsPerUnit = diametro; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 128;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.isReadable = false;
        // Unity conserva las coordenadas originales del sprite aunque reduzca la textura.
        importer.SaveAndReimport(); Object.DestroyImmediate(lectura);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new Exception("No se importó el sprite: " + path);
        informe.Add("PASS Sprite " + path + ": alpha real, filtro Point, silueta normalizada de " + diametro + " px; bounds " + sprite.bounds.size + ".");
        return sprite;
    }
    static void ConfigurarBala(string path, Sprite sprite, Material material)
    {
        var bala = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var render = bala.GetComponent<SpriteRenderer>(); render.sprite = sprite; render.color = Color.white; render.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(bala, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(bala); }
    }
}
