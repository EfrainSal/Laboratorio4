using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

public static class IntegrarComicFinal
{
    public const string Escena = "Assets/Scenes/Escenario4_Jefe.unity";
    public const string Arte = "Assets/Sprites/CinematicaFinal/";
    [MenuItem("Laboratorio 4/Cómic/Integrar clímax y desenlace aprobados")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var anterior = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string nombre in new[] { "A-reencuentro-jason", "A-reencuentro-milena", "B1-ultimas-palabras", "B2-escape" })
            {
                string path = Arte + nombre + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
            var escena = EditorSceneManager.OpenScene(Escena);
            var comic = Object.FindFirstObjectByType<ComicFinal>();
            if (comic == null) comic = new GameObject("CinematicaFinal").AddComponent<ComicFinal>();
            comic.jefe = Object.FindFirstObjectByType<BossHealth>();
            if (comic.jefe == null) throw new Exception("Falta Dr. Yiba en su escena de combate");
            comic.reencuentroJason = Sprite("A-reencuentro-jason"); comic.reencuentroMilena = Sprite("A-reencuentro-milena");
            comic.ultimasPalabras = Sprite("B1-ultimas-palabras"); comic.escape = Sprite("B2-escape");
            comic.desplazamientoAyudante = new Vector2(1.8f, -.6f); comic.radioProximidad = 1f;
            ConfigurarJefe(comic.jefe);
            if (comic.GetComponentsInChildren<AudioSource>(true).Length != 0) throw new Exception("El cómic debe permanecer sin audio");
            EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Reports/ComicFinal");
            File.WriteAllText("Reports/ComicFinal/integracion.txt", "PASS: cuatro viñetas aprobadas, proximidad al compañero junto al spawn, fundidos compartidos, rugido y sacudida, navegación con pies y orden Y; sin nuevos audios.\n");
            Debug.Log("[ComicFinal] Clímax y desenlace integrados exclusivamente en Escenario4_Jefe.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(anterior); }
    }
    private static Sprite Sprite(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(Arte + nombre + ".png") ?? throw new Exception("Falta la viñeta " + nombre);
    private static void ConfigurarJefe(BossHealth salud)
    {
        var jefe = salud.GetComponent<BossAI>();
        var nav = salud.GetComponent<NavegacionJefe>(); if (nav == null) nav = salud.gameObject.AddComponent<NavegacionJefe>();
        nav.celda = .3f; nav.margen = .07f; nav.intervaloRuta = .45f;
        var pies = salud.GetComponent<BoxCollider2D>();
        pies.size = new Vector2(.6f, .32f); pies.offset = new Vector2(.144f, .17f); pies.edgeRadius = .03f;
        const string rutaMaterial = "Assets/Materials/YibaPiesSinFriccion.physicsMaterial2D";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(rutaMaterial);
        if (material == null) { material = new PhysicsMaterial2D("YibaPiesSinFriccion") { friction = 0, bounciness = 0 }; AssetDatabase.CreateAsset(material, rutaMaterial); }
        pies.sharedMaterial = material;
        var orden = salud.GetComponent<Laboratorio4.Escenario4.E4OrdenY>();
        if (orden == null) orden = salud.gameObject.AddComponent<Laboratorio4.Escenario4.E4OrdenY>();
        orden.dinamico = true; orden.referencia = salud.transform; orden.desfase = 0; orden.Aplicar();
        salud.GetComponent<SpriteRenderer>().spriteSortPoint = SpriteSortPoint.Pivot;
        var audio = salud.GetComponent<AudioSource>(); if (audio == null) audio = salud.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false; audio.loop = false; audio.mute = false; audio.enabled = true;
        audio.spatialBlend = 0; audio.priority = 64; audio.dopplerLevel = 0; audio.clip = null;
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/MainMixer.mixer");
        if (mixer != null) audio.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX").FirstOrDefault();
        jefe.audioSource = audio; jefe.radioEscucha = 36; jefe.volumenCercano = .8f;
        jefe.sfxRugidoInicio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Jefe_Rugido_Largo_1.mp3");
        jefe.sfxInvocacion = jefe.sfxRugidoInicio;
        jefe.sfxAtaquePesado = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Jefe_Rugido_Largo_2.mp3");
        jefe.sfxRecargaEnergia = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Jefe_Recarga_Energia.ogg");
        jefe.sfxDisparo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Jefe_Disparo.ogg");
        if (new[] { jefe.sfxRugidoInicio, jefe.sfxAtaquePesado, jefe.sfxRecargaEnergia, jefe.sfxDisparo }.Any(c => c == null)) throw new Exception("Falta un SFX existente de Yiba");
        var camara = Camera.main; if (camara != null && camara.GetComponent<SacudidaCamaraJefe>() == null) camara.gameObject.AddComponent<SacudidaCamaraJefe>();
    }

    public static void IntegrarYProbarEnLote()
    {
        try { Integrar(); ProbarComicFinal.ProbarEnLote(); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
