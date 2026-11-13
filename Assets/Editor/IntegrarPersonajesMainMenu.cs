using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Laboratorio4.Animacion;
using LaboratorioYiba.Piso3V10;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IntegrarPersonajesMainMenu
{
    public const string Menu = "Assets/Scenes/MainMenu.unity";
    public const string Prefabs = "Assets/Prefabs/";
    public const string Bibliotecas = "Assets/Animations/Personajes/";
    private const string Arte = "Assets/Sprites/Escenarios/Piso3/Personajes/";
    private static readonly string[] Dirs = { "Sur", "Sureste", "Este", "Noreste", "Norte", "Noroeste", "Oeste", "Suroeste" };
    private static readonly Dictionary<string, BibliotecaPersonaje> datos = new Dictionary<string, BibliotecaPersonaje>();
    [Serializable] private class Info { public Entrada[] animaciones; }
    [Serializable] private class Entrada { public string ruta; public float fps; public bool loop; }

    [MenuItem("Laboratorio 4/Personajes/Integrar para MainMenu")]
    public static void Integrar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            CrearBibliotecas();
            foreach (string nombre in new[] { "Player_Jason", "Player_Milena", "Enemy_Melee", "Enemy_Ranged" })
            {
                string path = Prefabs + nombre + ".prefab";
                var go = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool jugador = nombre.StartsWith("Player_");
                    string personaje = jugador ? (nombre.Contains("Milena") ? "Milena" : "Jason") : (nombre.Contains("Ranged") ? "Zorbo2" : "Zorbo");
                    Configurar(go, personaje, jugador);
                    PrefabUtility.SaveAsPrefabAsset(go, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(go); }
            }
            var menu = EditorSceneManager.OpenScene(Menu);
            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (manager == null) throw new Exception("MainMenu no contiene GameManager");
            manager.prefabJason = Cargar("Player_Jason");
            manager.prefabMilena = Cargar("Player_Milena");
            var controlador = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            string destino = new SerializedObject(controlador).FindProperty("_gameSceneName").stringValue;
            EditorSceneManager.SaveScene(menu);
            string pathEscena = AssetDatabase.FindAssets(destino + " t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == destino);
            if (pathEscena == null) throw new Exception("Falta escena de inicio: " + destino);
            var escena = EditorSceneManager.OpenScene(pathEscena);
            if (UnityEngine.Object.FindFirstObjectByType<LevelManager>() == null)
            {
                var inicio = UnityEngine.Object.FindFirstObjectByType<InicioPersonajesDesdeMenu>();
                if (inicio == null) inicio = new GameObject("IntegracionPersonajesMainMenu").AddComponent<InicioPersonajesDesdeMenu>();
                inicio.jugadorOriginal = UnityEngine.Object.FindFirstObjectByType<Piso3V10Movimiento>()?.transform;
                inicio.sorbosOriginales = UnityEngine.Object.FindObjectsByType<Piso3V10Zorbo>(FindObjectsSortMode.None).OrderBy(z => z.name).ToArray();
                inicio.prefabJason = Cargar("Player_Jason"); inicio.prefabMilena = Cargar("Player_Milena");
                inicio.sorboA = Cargar("Enemy_Melee"); inicio.sorboB = Cargar("Enemy_Ranged");
                inicio.camara = UnityEngine.Object.FindFirstObjectByType<Piso3V10Camara>();
                inicio.materialEscenario = inicio.jugadorOriginal != null ? inicio.jugadorOriginal.GetComponent<SpriteRenderer>().sharedMaterial : null;
                if (inicio.jugadorOriginal == null || inicio.sorbosOriginales.Length == 0) throw new Exception("No se encontraron los puntos de aparicion del escenario");
                foreach (var collider in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                    if (!collider.isTrigger && collider.attachedRigidbody == null) collider.gameObject.layer = LayerMask.NameToLayer("Muros");
                EditorSceneManager.SaveScene(escena);
            }
            AssetDatabase.SaveAssets();
            Validar();
            Debug.Log("[Personajes] Integrados Jason, Milena y ambos Sorbos en la ruta MainMenu -> " + destino);
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    public static GameObject Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + nombre + ".prefab");

    private static void CrearBibliotecas()
    {
        Directory.CreateDirectory(Bibliotecas);
        AssetDatabase.Refresh();
        var info = JsonUtility.FromJson<Info>(File.ReadAllText(Arte + "Personajes_Animaciones.json"));
        foreach (string personaje in new[] { "Jason", "Milena", "Zorbo", "Zorbo2" })
        {
            string path = Bibliotecas + personaje + ".asset";
            var b = AssetDatabase.LoadAssetAtPath<BibliotecaPersonaje>(path);
            if (b == null) { b = ScriptableObject.CreateInstance<BibliotecaPersonaje>(); AssetDatabase.CreateAsset(b, path); }
            b.personaje = personaje;
            var clips = new List<ClipPersonaje>();
            foreach (var carpeta in Directory.GetDirectories(Arte + personaje).OrderBy(p => p, StringComparer.Ordinal))
            {
                string nombre = Path.GetFileName(carpeta);
                if (nombre == "Disparo" && (personaje == "Jason" || personaje == "Milena")) continue;
                var entrada = info.animaciones.FirstOrDefault(a => a.ruta == personaje + "/" + nombre);
                var clip = new ClipPersonaje { nombre = nombre, fps = entrada?.fps ?? 10f, bucle = entrada?.loop ?? false };
                for (int d = 0; d < 8; d++)
                {
                    string dir = carpeta + "/" + d + "_" + Dirs[d];
                    var sprites = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png").OrderBy(p => p, StringComparer.Ordinal)
                        .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/'))).ToArray() : Array.Empty<Sprite>();
                    if (sprites.Any(s => s == null)) throw new Exception("Sprite sin importar: " + dir);
                    clip.direcciones[d] = new SpritesDireccion { frames = sprites };
                }
                clips.Add(clip);
            }
            if (personaje == "Milena")
            {
                var correr = clips.First(a => a.nombre == "Correr");
                clips.Add(new ClipPersonaje { nombre = "Caminar", fps = correr.fps, bucle = true, direcciones = correr.direcciones });
            }
            b.animaciones = clips.ToArray(); datos[personaje] = b; EditorUtility.SetDirty(b);
        }
    }

    private static void Configurar(GameObject go, string personaje, bool jugador)
    {
        var anterior = go.GetComponent<Piso3V10JasonAnimacion>();
        if (anterior != null) UnityEngine.Object.DestroyImmediate(anterior);
        var movimientoAnterior = go.GetComponent<Piso3V10Movimiento>();
        if (movimientoAnterior != null) UnityEngine.Object.DestroyImmediate(movimientoAnterior);
        var animacionAnterior = go.GetComponent<Piso3V10Personaje8Dir>();
        if (animacionAnterior != null) UnityEngine.Object.DestroyImmediate(animacionAnterior);
        var anim = go.GetComponent<AnimadorSprites8Direcciones>();
        if (anim == null) anim = go.AddComponent<AnimadorSprites8Direcciones>();
        anim.biblioteca = datos[personaje];
        go.GetComponent<SpriteRenderer>().sprite = AnimadorSprites8Direcciones.Frames(datos[personaje].animaciones.First(a => a.nombre == "Quieto"), 0)[0];
        go.GetComponent<SpriteRenderer>().flipX = false;
        var orden = go.GetComponent<Piso3V10OrdenVisual>();
        if (orden == null) orden = go.AddComponent<Piso3V10OrdenVisual>();
        orden.dinamico = true;
        if (jugador)
        {
            if (go.GetComponent<AnimacionJasonDinamica>() == null) go.AddComponent<AnimacionJasonDinamica>();
            var movimiento = go.GetComponent<MovimientoPlayer>(); movimiento.enabled = true; movimiento.spriteCuerpo = go.GetComponent<SpriteRenderer>();
            go.GetComponent<PlayerHealth>().ocultarSpriteAlMorir = false;
            ConfigurarArma(go);
        }
        else
        {
            if (go.GetComponent<AnimacionEnemigoDinamica>() == null) go.AddComponent<AnimacionEnemigoDinamica>();
            var melee = go.GetComponent<MeleeEnemyAI>(); if (melee != null) melee.capaMuros = LayerMask.GetMask("Muros");
            var ranged = go.GetComponent<RangedEnemyAI>();
            if (ranged != null) { ranged.capaMuros = LayerMask.GetMask("Muros"); ranged.bulletPrefab = Cargar("EnemyBullet"); }
        }
        var cuerpo = go.GetComponent<Rigidbody2D>(); cuerpo.gravityScale = 0f; cuerpo.bodyType = RigidbodyType2D.Dynamic;
        cuerpo.freezeRotation = true; cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cap = go.GetComponent<CapsuleCollider2D>(); if (cap == null) cap = go.AddComponent<CapsuleCollider2D>();
        foreach (var c in go.GetComponents<Collider2D>()) if (c != cap && !c.isTrigger) c.enabled = false;
        cap.enabled = true; cap.direction = CapsuleDirection2D.Horizontal;
        cap.size = jugador ? new Vector2(0.44f, 0.22f) : new Vector2(0.32f, 0.18f); cap.offset = Vector2.up * cap.size.y * 0.5f;
        if (!jugador)
        {
            var hurtbox = go.GetComponent<BoxCollider2D>(); if (hurtbox == null) hurtbox = go.AddComponent<BoxCollider2D>();
            hurtbox.enabled = true; hurtbox.isTrigger = true;
            hurtbox.size = new Vector2(0.55f, 0.9f); hurtbox.offset = new Vector2(0f, 0.45f);
        }
    }

    [MenuItem("Laboratorio 4/Personajes/Validar prefabs")]
    public static void Validar()
    {
        var informe = new List<string>();
        foreach (string nombre in new[] { "Player_Jason", "Player_Milena", "Enemy_Melee", "Enemy_Ranged" })
        {
            var go = Cargar(nombre); var anim = go.GetComponent<AnimadorSprites8Direcciones>();
            if (anim?.biblioteca == null) throw new Exception("Sin biblioteca: " + nombre);
            string caminar = nombre == "Enemy_Ranged" ? "Correr" : "Caminar";
            foreach (string estado in new[] { "Quieto", caminar, nombre.StartsWith("Player") ? "Habilidad" : "Ataque", "Muerte" })
            {
                var clip = anim.Animaciones.FirstOrDefault(c => c.nombre == estado);
                for (int d = 0; d < 8; d++)
                {
                    var frames = AnimadorSprites8Direcciones.Frames(clip, d);
                    if (frames == null || frames.Any(s => s == null)) throw new Exception(nombre + "/" + estado + "/" + d);
                }
            }
            if (go.GetComponent<Piso3V10JasonAnimacion>() != null) throw new Exception("Controladores duplicados: " + nombre);
            informe.Add("PASS " + nombre + ": " + anim.biblioteca.personaje + ", estados y direcciones completos.");
        }
        Directory.CreateDirectory("Reports/Personajes"); File.WriteAllLines("Reports/Personajes/prefabs.txt", informe);
        Debug.Log("[Personajes] Validacion de prefabs: PASS");
    }

    private static void ConfigurarArma(GameObject go)
    {
        var sistema = go.GetComponent<ArmaIndependiente>();
        if (sistema == null) sistema = go.AddComponent<ArmaIndependiente>();
        var raiz = go.transform.Find("ItemArma");
        if (raiz == null) { raiz = new GameObject("ItemArma").transform; raiz.SetParent(go.transform, false); }
        raiz.localPosition = new Vector3(0f, 0.85f, 0f);
        var visual = raiz.Find("Visual");
        if (visual == null) { visual = new GameObject("Visual").transform; visual.SetParent(raiz, false); }
        var sprite = visual.GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = visual.gameObject.AddComponent<SpriteRenderer>();
        sistema.pivote = raiz; sistema.visual = sprite;
        sistema.subfusil = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Escenarios/Piso3/Armas/ArmaLaboratorio4/Apuntar/Arma_Apuntar_2_Este.png");
        sistema.machete = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Machete.png");
        sprite.sprite = sistema.subfusil;
        sprite.sharedMaterial = go.GetComponent<SpriteRenderer>().sharedMaterial;
        visual.localScale = Vector3.one * (0.9f / sistema.subfusil.bounds.size.x);
        visual.localPosition = new Vector3(0.25f, 0f, 0f);
        var movimiento = go.GetComponent<MovimientoPlayer>();
        var viejo = movimiento.pivoteArma;
        var disparo = go.GetComponent<PlayerShooting>();
        if (disparo.firePoint == null) disparo.firePoint = new GameObject("FirePoint").transform;
        disparo.firePoint.SetParent(raiz, false); disparo.firePoint.localPosition = new Vector3(0.7f, 0.04f, 0f);
        sistema.salidaDisparo = disparo.firePoint;
        var combate = go.GetComponent<PlayerCombat>();
        if (combate.attackPoint != null) { combate.attackPoint.SetParent(raiz, false); combate.attackPoint.localPosition = Vector3.right * 0.6f; }
        movimiento.pivoteArma = raiz;
        if (viejo != null && viejo != raiz) viejo.gameObject.SetActive(false);
        go.GetComponent<WeaponManager>().isUsingMelee = false;
        if (go.GetComponent<MiraPersonalizada>() == null) go.AddComponent<MiraPersonalizada>();
    }
}
