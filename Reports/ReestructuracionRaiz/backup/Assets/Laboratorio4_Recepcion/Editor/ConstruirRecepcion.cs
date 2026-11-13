#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Laboratorio4.Recepcion;

namespace Laboratorio4.RecepcionEditor
{
    /// <summary>
    /// Construye la escena "Recepcion" desde Datos/recepcion_layout.json.
    /// Menú: Laboratorio 4 > Recepción > Construir escena / Abrir escena.
    /// Todo queda dentro de Assets/Laboratorio4_Recepcion; no toca otras escenas ni carpetas.
    /// </summary>
    public static class ConstruirRecepcion
    {
        [Serializable] class Caja { public float cx, cy, w, h; }
        [Serializable] class Obj { public string nombre, sprite, tipo; public float x, y, w, h; public Caja[] colisiones; }
        [Serializable] class Puerta { public string nombre, sprite; public float x, y, w, h; }
        [Serializable] class Rect2 { public float x, y, w, h; }
        [Serializable] class Salida { public float x, y, w, h; public string escena; }
        [Serializable] class Punto { public float x, y; }
        [Serializable] class Layout
        {
            public float anchoPx, altoPx, ppu;
            public Obj[] objetos; public Puerta[] puertas; public Rect2[] muros;
            public Salida salida; public Punto jason, milena;
        }

        const int OrdenFondo = -32000, OrdenPiso = -31000, OrdenPared = -30900, OrdenCornisa = 20000;
        static readonly string[] Dirs = { "Norte", "Noreste", "Este", "Sureste", "Sur", "Suroeste", "Oeste", "Noroeste" };
        public const string NombreEscena = "Recepcion";

        public static string Raiz()
        {
            string g = AssetDatabase.FindAssets("recepcion_layout t:TextAsset").FirstOrDefault();
            if (g == null) return null;
            return Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(g))).Replace('\\', '/');
        }
        public static string RutaEscena(string raiz) => raiz + "/Scenes/" + NombreEscena + ".unity";

        [MenuItem("Laboratorio 4/Recepción/Construir escena (Recepcion)")]
        public static void ConstruirMenu()
        {
            string raiz = Raiz();
            if (raiz == null) { Debug.LogError("[Recepción] No se encontró recepcion_layout.json"); return; }
            if (File.Exists(RutaEscena(raiz)) &&
                !EditorUtility.DisplayDialog("Laboratorio 4", "Esto vuelve a crear la escena Recepcion desde cero (los cambios hechos a mano en ella se pierden). ¿Continuar?", "Construir", "Cancelar"))
                return;
            Construir();
        }

        [MenuItem("Laboratorio 4/Recepción/Abrir escena (Recepcion)")]
        public static void Abrir()
        {
            string raiz = Raiz(); if (raiz == null) return;
            string ruta = RutaEscena(raiz);
            if (!File.Exists(ruta)) { Construir(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ruta);
        }

        public static bool Construir()
        {
            string raiz = Raiz();
            if (raiz == null) { Debug.LogError("[Recepción] No se encontró recepcion_layout.json"); return false; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            ConfigurarTexturas(raiz);
            var L = JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(raiz + "/Datos/recepcion_layout.json").text);
            float P = L.ppu;
            Vector2 M(float x, float y) => new Vector2(x / P, -y / P);
            Sprite S(string rel)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(raiz + "/" + rel);
                if (s == null) Debug.LogWarning("[Recepción] Falta el sprite " + rel);
                return s;
            }

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- mapa
            var mapa = new GameObject("00_Mapa").transform;
            Render("Fondo", S("Sprites/Mapa/Recepcion_Fondo.png"), Vector2.zero, OrdenFondo, mapa);
            Render("Cornisa_Delante", S("Sprites/Mapa/Recepcion_Cornisa.png"), Vector2.zero, OrdenCornisa, mapa);
            var muros = new GameObject("Colisiones_Muros"); muros.transform.SetParent(mapa);
            foreach (var r in L.muros)
            {
                var b = muros.AddComponent<BoxCollider2D>();
                b.offset = M(r.x + r.w / 2, r.y + r.h / 2); b.size = new Vector2(r.w / P, r.h / P);
            }

            // ---------- objetos
            var piso = new GameObject("01_Piso_Manchas_Papeles").transform;
            var pared = new GameObject("02_Pared_Senales").transform;
            var objs = new GameObject("03_Objetos").transform;
            int ip = 0, iw = 0;
            foreach (var o in L.objetos)
            {
                if (o.tipo == "piso") { Render(o.nombre, S(o.sprite), M(o.x, o.y), OrdenPiso + ip++, piso); continue; }
                if (o.tipo == "pared") { Render(o.nombre, S(o.sprite), M(o.x, o.y), OrdenPared + iw++, pared); continue; }
                var go = Render(o.nombre, S(o.sprite), M(o.x, o.y), RecepcionOrdenPorPies.Calcular(-o.y / P), objs);
                foreach (var c in o.colisiones ?? new Caja[0])
                {
                    var b = go.AddComponent<BoxCollider2D>();
                    b.offset = new Vector2(c.cx / P, c.cy / P); b.size = new Vector2(c.w / P, c.h / P);
                }
            }

            // ---------- puertas
            var puertas = new GameObject("04_Puertas").transform;
            foreach (var p in L.puertas)
            {
                int orden = p.nombre == "Puerta_Entrada" ? OrdenCornisa + 1 : RecepcionOrdenPorPies.Calcular(-p.y / P);
                Render(p.nombre, S(p.sprite), M(p.x, p.y), orden, puertas);
            }
            var sal = new GameObject("Salida_Escenario1");
            sal.transform.SetParent(puertas);
            var tb = sal.AddComponent<BoxCollider2D>(); tb.isTrigger = true;
            tb.offset = M(L.salida.x + L.salida.w / 2, L.salida.y + L.salida.h / 2); tb.size = new Vector2(L.salida.w / P, L.salida.h / P);
            sal.AddComponent<RecepcionSalida>().escenaDestino = L.salida.escena;

            // ---------- personajes
            var pj = new GameObject("05_Personajes").transform;
            var jason = Personaje("Jason", raiz, M(L.jason.x, L.jason.y), pj, true);
            var milena = Personaje("Milena", raiz, M(L.milena.x, L.milena.y), pj, false);
            milena.GetComponent<RecepcionSeguidor>().objetivo = jason.transform;

            // ---------- cámara
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.027f, 0.035f);
            camGo.transform.position = new Vector3(jason.transform.position.x, jason.transform.position.y, -10f);
            camGo.AddComponent<AudioListener>();
            var seg = camGo.AddComponent<RecepcionCamara>();
            seg.objetivo = jason.transform;
            seg.limites = new Rect(0, -L.altoPx / P, L.anchoPx / P, L.altoPx / P);

            string ruta = RutaEscena(raiz);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            EditorSceneManager.SaveScene(escena, ruta);
            AgregarABuild(ruta);
            Debug.Log("[Recepción] Escena construida: " + ruta + " (" + L.objetos.Length + " objetos, " + L.muros.Length + " muros).");
            return true;
        }

        static GameObject Personaje(string nombre, string raiz, Vector2 pos, Transform padre, bool jugable)
        {
            var go = new GameObject(nombre); go.transform.SetParent(padre); go.transform.position = pos;
            if (jugable) go.tag = "Player";
            var sr = go.AddComponent<SpriteRenderer>();
            var quieto = Dirs.Select(d => AssetDatabase.LoadAssetAtPath<Sprite>($"{raiz}/Sprites/Personajes/{nombre}/{nombre}_{d}.png")).ToArray();
            sr.sprite = quieto[0];   // mira al norte, hacia la recepción
            sr.sortingOrder = RecepcionOrdenPorPies.Calcular(pos.y);
            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Horizontal; col.size = new Vector2(0.36f, 0.18f); col.offset = new Vector2(0f, 0.09f);
            go.AddComponent<RecepcionOrdenPorPies>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            var an = go.AddComponent<RecepcionAnimador>();
            an.quieto = quieto;
            an.caminar = new RecepcionAnimador.Cuadros[8];
            for (int i = 0; i < 8; i++)
                an.caminar[i] = new RecepcionAnimador.Cuadros { cuadros = CuadrosCaminar(raiz, nombre, Dirs[i]) };
            if (jugable) go.AddComponent<RecepcionJugador>();
            else go.AddComponent<RecepcionSeguidor>();
            return go;
        }

        // Jason solo trae caminar en 4 direcciones: las diagonales usan Este u Oeste.
        static Sprite[] CuadrosCaminar(string raiz, string nombre, string dir)
        {
            string carpeta = $"{raiz}/Sprites/Personajes/{nombre}/Caminar";
            if (!AssetDatabase.IsValidFolder(carpeta)) return new Sprite[0];
            Sprite[] Cargar(string d) => AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p).StartsWith($"{nombre}_Caminar_{d}_"))
                .OrderBy(p => p).Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s => s != null).ToArray();
            var c = Cargar(dir);
            if (c.Length > 0) return c;
            string alterno = (dir == "Noreste" || dir == "Sureste") ? "Este" : (dir == "Noroeste" || dir == "Suroeste") ? "Oeste" : "Sur";
            return Cargar(alterno);
        }

        static GameObject Render(string nombre, Sprite s, Vector2 pos, int orden, Transform padre)
        {
            var go = new GameObject(nombre); go.transform.SetParent(padre); go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = orden;
            return go;
        }

        // Ajusta la importación: sprites con pivote en los pies (abajo-centro) para la perspectiva 3/4.
        public static void ConfigurarTexturas(string raiz)
        {
            foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { raiz + "/Sprites" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
                bool personaje = p.Contains("/Personajes/"), mapa = p.Contains("/Mapa/");
                float ppu = personaje ? 100f / 3f : 100f;
                var alin = mapa ? SpriteAlignment.TopLeft : SpriteAlignment.BottomCenter;
                var filtro = personaje ? FilterMode.Point : FilterMode.Bilinear;
                var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
                bool ok = ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single &&
                          Mathf.Approximately(ti.spritePixelsPerUnit, ppu) && st.spriteAlignment == (int)alin && ti.filterMode == filtro &&
                          ti.textureCompression == TextureImporterCompression.Uncompressed && !ti.mipmapEnabled && ti.maxTextureSize >= 2048;
                if (ok) continue;
                ti.ReadTextureSettings(st);
                st.textureType = TextureImporterType.Sprite; st.spriteMode = (int)SpriteImportMode.Single;
                st.spriteAlignment = (int)alin; st.spritePixelsPerUnit = ppu; st.filterMode = filtro;
                st.mipmapEnabled = false; st.alphaIsTransparency = true; st.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(st);
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
        }

        static void AgregarABuild(string ruta)
        {
            var lista = EditorBuildSettings.scenes.ToList();
            if (lista.Any(s => s.path == ruta)) return;
            int i = lista.FindIndex(s => Path.GetFileNameWithoutExtension(s.path) == "Escenario1_Entrada");
            var nueva = new EditorBuildSettingsScene(ruta, true);
            if (i >= 0) lista.Insert(i, nueva); else lista.Add(nueva);
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }

    /// <summary>Al importar el paquete por primera vez ofrece construir y abrir la Recepción.</summary>
    [InitializeOnLoad]
    static class InstalarRecepcion
    {
        static string Clave => "Laboratorio4_Recepcion_v2_" + Application.dataPath;
        static InstalarRecepcion() { EditorApplication.delayCall += Primera; }

        static void Primera()
        {
            if (EditorPrefs.GetBool(Clave, false)) return;
            string raiz = ConstruirRecepcion.Raiz(); if (raiz == null) return;
            EditorPrefs.SetBool(Clave, true);
            if (EditorUtility.DisplayDialog("Laboratorio 4",
                "Se importó la Recepción (escenario antes del Escenario 1).\n¿Construir la escena y abrirla ahora?", "Sí", "Después"))
                ConstruirRecepcion.Construir();
            else
                Debug.Log("[Recepción] Puedes construirla luego en: Laboratorio 4 > Recepción > Construir escena.");
        }
    }
}
#endif
