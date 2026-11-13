using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.InputSystem;

public static class RepararMecanicasMerge
{
    [MenuItem("Laboratorio 4/Reparacion/Aplicar controles y feedback")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying) throw new Exception("Salir de Play antes de integrar");
        const string ruta = "Assets/Sprites/Combate/Machete_Tajo.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.GetSourceTextureWidthAndHeight(out int ancho, out int alto);
        importer.spritePixelsPerUnit = ancho / 4f;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = Enumerable.Range(0, 8).Select(i => new SpriteRect
        {
            name = "Tajo_" + i.ToString("00"), spriteID = GUID.Generate(),
            rect = new Rect((i % 4) * ancho / 4f, (1 - i / 4) * alto / 2f, ancho / 4f, alto / 2f),
            alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f)
        }).ToArray();
        // Reutilizar IDs al volver a aplicar para no invalidar referencias.
        foreach (var rect in rects)
        {
            var previo = provider.GetSpriteRects().FirstOrDefault(r => r.name == rect.name);
            if (previo != null) rect.spriteID = previo.spriteID;
        }
        provider.SetSpriteRects(rects);
        var nombres = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nombres.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/Gameplay.inputactions");
        if (input == null || sprites.Length != 8) throw new Exception("Faltan controles o fotogramas");
        foreach (string nombre in new[] { "Player_Jason", "Player_Milena", "Enemy_Melee", "Enemy_Ranged" })
        {
            string path = "Assets/Prefabs/" + nombre + ".prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (go.GetComponent<RetroalimentacionDano>() == null) go.AddComponent<RetroalimentacionDano>();
                if (nombre.StartsWith("Player"))
                {
                    var playerInput = go.GetComponent<PlayerInput>();
                    playerInput.actions = input;
                    playerInput.defaultActionMap = "Player";
                    playerInput.defaultControlScheme = "Keyboard&Mouse";
                    playerInput.notificationBehavior = PlayerNotifications.SendMessages;
                    var animacion = go.GetComponent<MacheteAnimacion>() ?? go.AddComponent<MacheteAnimacion>();
                    animacion.fotogramas = sprites;
                }
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Reparacion] Controles, tajo y feedback integrados. Posiciones del arma conservadas.");
    }
}
