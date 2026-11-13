using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario3.Herramientas
{
    /// <summary>
    /// Construye la escena del Escenario 3 a partir de Datos/E3_Nivel.json.
    /// Menú: Laboratorio 4 > Escenario 3 > Construir escena.
    /// Usa tus prefabs de objetos, los sprites de tus puertas y los de tus personajes (por GUID; si no los
    /// encuentra, los busca por nombre). Todo lo de la escena empieza por "E3_".
    /// </summary>
    public static class E3Constructor
    {
        public const string Raiz = "Assets/Laboratorio4_Escenario3";
        public const string RutaEscena = Raiz + "/Escenas/E3_Laboratorio.unity";
        public const string RutaDatos = Raiz + "/Datos/E3_Nivel.json";
        const string GuidMaterial = "a97c105638bdf8b4a8650670310a4cd3";
        const string GuidSombra = "61df3b468243cc546928c9aa955a0583";
        const string GuidJason = "330039fa2aed42c42acdf956b7495c8b";
        const string GuidSorbo = "d7bc066202a4e94498734d82b8a3b3b8";
        const string GuidSorboAcorazado = "10ffcc24c6f09b64d9fd474c5d186f20";

        static readonly Dictionary<string, float> Fps = new Dictionary<string, float>
        { { "Quieto", 1f }, { "Caminar", 10f }, { "Correr", 12f }, { "Disparo", 14f }, { "Habilidad", 12f }, { "Ataque", 12f }, { "Muerte", 10f } };
        static readonly HashSet<string> Bucle = new HashSet<string> { "Quieto", "Caminar", "Correr" };

        static Material material;
        static Sprite sombra;
        static List<string> avisos;

        [MenuItem("Laboratorio 4/Escenario 3/Construir escena")]
        public static void MenuConstruir()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string r = Construir();
            if (File.Exists(RutaEscena)) EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Laboratorio 4 - Escenario 3", r, "OK");
        }

        [MenuItem("Laboratorio 4/Escenario 3/Abrir escena")]
        public static void MenuAbrir()
        {
            if (!File.Exists(RutaEscena)) { MenuConstruir(); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        public static string Construir()
        {
            try { return ConstruirInterno(); }
            catch (Exception e)
            {
                Debug.LogError("[Escenario 3] No se pudo construir la escena: " + e);
                return "No se pudo construir la escena. Mira el error [Escenario 3] en la Consola.\n\n" + e.Message;
            }
        }

        static string ConstruirInterno()
        {
            avisos = new List<string>();
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(RutaDatos);
            if (ta == null) return "No encuentro " + RutaDatos;
            E3NivelDatos d = JsonUtility.FromJson<E3NivelDatos>(ta.text);
            ConfigurarTexturas();
            material = Cargar<Material>(GuidMaterial, null, "t:Material");
            sombra = Cargar<Sprite>(GuidSombra, "SombraObjeto", "t:Sprite");

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject raiz = new GameObject("E3_Nivel");
            Transform mapa = Hijo(raiz.transform, "E3_Mapa");
            Transform cornisa = Hijo(raiz.transform, "E3_Cornisa");
            Transform puertas = Hijo(raiz.transform, "E3_Puertas");
            Transform objetos = Hijo(raiz.transform, "E3_Objetos");
            Transform decorado = Hijo(raiz.transform, "E3_Decorado");
            Transform sorbos = Hijo(raiz.transform, "E3_Sorbos");
            Transform personajes = Hijo(raiz.transform, "E3_Personajes");

            // ---------------- fondo (piso + muros + caras frontales)
            foreach (E3Imagen im in d.fondo)
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(Raiz + "/Arte/Fondo/" + im.archivo + ".png");
                SpriteRenderer sr = Render(mapa, im.archivo, s, new Vector2(im.x, im.y));
                sr.sortingOrder = -30000;
                sr.gameObject.isStatic = true;
            }

            // ---------------- cornisa sur (tapa plateada) en tiras ordenadas por el borde del muro
            foreach (E3Imagen im in d.cornisa)
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(Raiz + "/Arte/Cornisa/" + im.archivo + ".png");
                SpriteRenderer sr = Render(cornisa, im.archivo, s, new Vector2(im.x, im.y));
                E3OrdenY o = sr.gameObject.AddComponent<E3OrdenY>();
                o.dinamico = false;
                o.desfase = im.orden - Mathf.RoundToInt(-im.y * 100f);
                o.Aplicar();
                sr.gameObject.isStatic = true;
            }

            // ---------------- colisiones de muros
            GameObject muros = new GameObject("E3_ColisionMuros");
            muros.transform.SetParent(mapa, false);
            muros.isStatic = true;
            E3Muros marca = muros.AddComponent<E3Muros>();
            foreach (E3Rect r in d.muros)
            {
                BoxCollider2D b = muros.AddComponent<BoxCollider2D>();
                b.offset = new Vector2(r.cx, r.cy);
                b.size = new Vector2(r.w, r.h);
            }
            marca.esperadas = d.muros.Length;

            // ---------------- puertas (tus sprites) con colisión sólida en la base
            foreach (E3PuertaDato p in d.puertas)
            {
                Sprite s = Cargar<Sprite>(p.sprite, p.spriteNombre, "t:Sprite");
                SpriteRenderer sr = Render(puertas, p.nombre, s, new Vector2(p.x, p.y));
                sr.transform.localScale = new Vector3(p.escala, p.escala, 1f);
                E3OrdenY o = sr.gameObject.AddComponent<E3OrdenY>();
                o.dinamico = false;
                o.Aplicar();
                GameObject bloqueo = new GameObject(p.nombre + "_Bloqueo");
                bloqueo.transform.SetParent(puertas, false);
                bloqueo.isStatic = true;
                BoxCollider2D bc = bloqueo.AddComponent<BoxCollider2D>();
                bc.offset = new Vector2(p.colision.cx, p.colision.cy);
                bc.size = new Vector2(p.colision.w, p.colision.h);
                E3Puerta pu = sr.gameObject.AddComponent<E3Puerta>();
                pu.esSalida = p.final;
                if (p.final)
                {
                    GameObject zona = new GameObject(p.nombre + "_Aviso");
                    zona.transform.SetParent(puertas, false);
                    BoxCollider2D z = zona.AddComponent<BoxCollider2D>();
                    z.isTrigger = true;
                    z.offset = new Vector2(p.aviso.cx, p.aviso.cy);
                    z.size = new Vector2(p.aviso.w, p.aviso.h);
                    pu.zonaAviso = z;
                }
            }

            // ---------------- objetos (instancias de tus prefabs), colisión solo en la base
            int nObj = 0;
            foreach (E3Objeto ob in d.objetos)
            {
                GameObject go = Instanciar(ob, objetos);
                if (go == null) continue;
                foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
                BoxCollider2D b = go.GetComponent<BoxCollider2D>();
                if (b == null) b = go.AddComponent<BoxCollider2D>();
                b.enabled = true;
                b.isTrigger = false;
                b.size = new Vector2(ob.cw, ob.ch);
                b.offset = new Vector2(0f, ob.ch * 0.5f);
                nObj++;
            }
            int nDec = 0;
            foreach (E3Objeto ob in d.decorado)
            {
                GameObject go = Instanciar(ob, decorado);
                if (go == null) continue;
                foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
                nDec++;
            }

            // ---------------- personajes
            E3Clip[] clipsJason = CargarClips(GuidJason, "Jason");
            E3Clip[] clipsSorbo = CargarClips(GuidSorbo, "Zorbo");
            E3Clip[] clipsAcorazado = CargarClips(GuidSorboAcorazado, "Zorbo2");
            Sprite bala = AssetDatabase.LoadAssetAtPath<Sprite>(Raiz + "/Arte/E3_Bala.png");

            GameObject jason = Personaje(personajes, "E3_Jason", new Vector2(d.jason.x, d.jason.y), clipsJason, 0, new Vector2(0.34f, 0.16f), 0.62f);
            jason.GetComponent<E3Vida>().maxima = 6;
            E3Jugador jug = jason.AddComponent<E3Jugador>();
            jug.spriteBala = bala;
            jug.materialSprites = material;

            int nS = 0;
            foreach (E3SorboDato sd in d.sorbos)
            {
                bool acor = sd.tipo == "Acorazado";
                int dir = acor ? new[] { 0, 2, 6 }[nS % 3] : (nS * 3) % 8;
                GameObject so = Personaje(sorbos, sd.nombre, new Vector2(sd.x, sd.y), acor ? clipsAcorazado : clipsSorbo, dir,
                                          acor ? new Vector2(0.5f, 0.2f) : new Vector2(0.4f, 0.16f), acor ? 0.9f : 0.7f);
                so.GetComponent<E3Vida>().maxima = acor ? 4 : 3;
                E3Sorbo ia = so.AddComponent<E3Sorbo>();
                ia.acorazado = acor;
                ia.velocidad = acor ? 1.25f : 1.6f;
                ia.distanciaAtaque = acor ? 0.62f : 0.55f;
                nS++;
            }

            // ---------------- sistema y cámara
            GameObject sistema = new GameObject("E3_Sistema");
            sistema.transform.SetParent(raiz.transform, false);
            E3Guardian gu = sistema.AddComponent<E3Guardian>();
            gu.datos = ta;
            gu.raiz = raiz.transform;
            sistema.AddComponent<E3Hud>();

            GameObject camGo = new GameObject("E3_Camara");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(d.jason.x, d.jason.y + 0.3f, -10f);
            camGo.AddComponent<AudioListener>();
            E3Camara ec = camGo.AddComponent<E3Camara>();
            ec.objetivo = jason.transform;
            ec.referencia = raiz.transform;
            ec.limites = Rect.MinMaxRect(d.limites.x0, d.limites.y0, d.limites.x1, d.limites.y1);

            Directory.CreateDirectory(Raiz + "/Escenas");
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AgregarABuild();
            AssetDatabase.Refresh();

            string res = "Escenario 3 construido: " + d.muros.Length + " colisiones de muro, " + nObj + " objetos, " + nDec +
                         " decorados, " + nS + " Sorbos, 2 puertas.";
            if (avisos.Count > 0) res += "\n\nAvisos:\n- " + string.Join("\n- ", avisos.ToArray());
            Debug.Log("[Escenario 3] " + res);
            return res;
        }

        // ==================================================================== ayudas
        static Transform Hijo(Transform padre, string nombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            return go.transform;
        }

        static SpriteRenderer Render(Transform padre, string nombre, Sprite s, Vector2 pos)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            if (material != null) sr.sharedMaterial = material;
            if (s == null) avisos.Add("Falta el sprite de " + nombre);
            return sr;
        }

        static T Cargar<T>(string guid, string nombre, string filtro) where T : UnityEngine.Object
        {
            string ruta = string.IsNullOrEmpty(guid) ? "" : AssetDatabase.GUIDToAssetPath(guid);
            T a = string.IsNullOrEmpty(ruta) ? null : AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (a == null && !string.IsNullOrEmpty(nombre))
            {
                foreach (string g in AssetDatabase.FindAssets(nombre + " " + filtro))
                {
                    string r = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetFileNameWithoutExtension(r) != nombre) continue;
                    a = AssetDatabase.LoadAssetAtPath<T>(r);
                    if (a != null) break;
                }
            }
            return a;
        }

        static GameObject Instanciar(E3Objeto ob, Transform padre)
        {
            GameObject pf = Cargar<GameObject>(ob.guid, ob.prefab, "t:Prefab");
            if (pf == null) { avisos.Add("No encuentro el prefab " + ob.prefab); return null; }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pf, padre);
            go.name = ob.nombre;
            go.transform.localPosition = new Vector3(ob.x, ob.y, 0f);
            return go;
        }

        static GameObject Personaje(Transform padre, string nombre, Vector2 pos, E3Clip[] clips, int dir, Vector2 capsula, float anchoSombra)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (material != null) sr.sharedMaterial = material;
            E3Animador an = go.AddComponent<E3Animador>();
            an.clips = clips;
            an.actual = "Quieto";
            an.direccion = dir;
            foreach (E3Clip c in clips)
                if (c.nombre == "Quieto") { Sprite[] f = E3Animador.Frames(c, dir); if (f != null && f.Length > 0) sr.sprite = f[0]; }
            if (sr.sprite == null) avisos.Add("Sin sprites para " + nombre);
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None;
            CapsuleCollider2D cap = go.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = capsula;
            cap.offset = new Vector2(0f, capsula.y * 0.5f);
            go.AddComponent<E3Movedor>();
            go.AddComponent<E3Vida>();
            go.AddComponent<E3OrdenY>().dinamico = true;
            if (sombra != null)
            {
                GameObject sh = new GameObject("E3_Sombra");
                sh.transform.SetParent(go.transform, false);
                SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
                ssr.sprite = sombra;
                if (material != null) ssr.sharedMaterial = material;
                ssr.sortingOrder = -1500;
                float w = sombra.bounds.size.x > 0f ? sombra.bounds.size.x : 0.96f;
                sh.transform.localScale = new Vector3(anchoSombra / w, anchoSombra / w, 1f);
                sh.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            }
            return go;
        }

        /// <summary>Lee Personajes/&lt;nombre&gt;/&lt;Animacion&gt;/&lt;d&gt;_&lt;Direccion&gt;/*.png de tu proyecto.</summary>
        static E3Clip[] CargarClips(string guidCarpeta, string nombre)
        {
            string carpeta = AssetDatabase.GUIDToAssetPath(guidCarpeta);
            if (string.IsNullOrEmpty(carpeta) || !AssetDatabase.IsValidFolder(carpeta))
            {
                carpeta = null;
                foreach (string g in AssetDatabase.FindAssets(nombre + "_Quieto_Sur_01 t:Sprite"))
                {
                    string r = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetFileNameWithoutExtension(r) != nombre + "_Quieto_Sur_01") continue;
                    carpeta = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(r))).Replace('\\', '/');
                    break;
                }
            }
            List<E3Clip> lista = new List<E3Clip>();
            if (carpeta == null) { avisos.Add("No encuentro la carpeta de sprites de " + nombre); return lista.ToArray(); }
            foreach (string dirAnim in AssetDatabase.GetSubFolders(carpeta))
            {
                string an = Path.GetFileName(dirAnim);
                E3Clip c = new E3Clip();
                c.nombre = an;
                c.fps = Fps.ContainsKey(an) ? Fps[an] : 10f;
                c.bucle = Bucle.Contains(an);
                c.dirs = new E3Direccion[8];
                for (int k = 0; k < 8; k++) c.dirs[k] = new E3Direccion();
                foreach (string dirDir in AssetDatabase.GetSubFolders(dirAnim))
                {
                    string dn = Path.GetFileName(dirDir);
                    int us = dn.IndexOf('_');
                    int k;
                    if (us <= 0 || !int.TryParse(dn.Substring(0, us), out k) || k < 0 || k > 7) continue;
                    List<string> rutas = new List<string>();
                    foreach (string g in AssetDatabase.FindAssets("t:Sprite", new[] { dirDir })) rutas.Add(AssetDatabase.GUIDToAssetPath(g));
                    rutas.Sort(string.CompareOrdinal);
                    List<Sprite> fr = new List<Sprite>();
                    foreach (string r in rutas)
                    {
                        if (Path.GetDirectoryName(r).Replace('\\', '/') != dirDir) continue;
                        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(r);
                        if (s != null) fr.Add(s);
                    }
                    c.dirs[k].frames = fr.ToArray();
                }
                lista.Add(c);
            }
            return lista.ToArray();
        }

        /// <summary>Asegura que el arte del Escenario 3 se importe como sprite nítido (Point), sin compresión.</summary>
        static void ConfigurarTexturas()
        {
            List<string> rutas = new List<string>();
            foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { Raiz + "/Arte" })) rutas.Add(AssetDatabase.GUIDToAssetPath(g));
            foreach (string r in rutas)
            {
                TextureImporter ti = AssetImporter.GetAtPath(r) as TextureImporter;
                if (ti == null) continue;
                bool bala = r.EndsWith("E3_Bala.png");
                float ppu = bala ? 33.333332f : 100f;
                Vector2 piv = bala ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
                TextureImporterSettings st = new TextureImporterSettings();
                ti.ReadTextureSettings(st);
                bool ok = ti.textureType == TextureImporterType.Sprite && st.spriteMode == (int)SpriteImportMode.Single &&
                          Mathf.Approximately(ti.spritePixelsPerUnit, ppu) && ti.filterMode == FilterMode.Point &&
                          ti.textureCompression == TextureImporterCompression.Uncompressed && ti.maxTextureSize >= 2048 &&
                          st.spriteAlignment == (int)SpriteAlignment.Custom && (st.spritePivot - piv).sqrMagnitude < 0.0001f;
                if (ok) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 2048;
                ti.ReadTextureSettings(st);
                st.spriteAlignment = (int)SpriteAlignment.Custom;
                st.spritePivot = piv;
                ti.SetTextureSettings(st);
                ti.SaveAndReimport();
            }
        }

        static void AgregarABuild()
        {
            List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in lista) if (s.path == RutaEscena) return;
            lista.Add(new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
