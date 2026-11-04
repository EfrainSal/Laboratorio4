using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LaboratorioYiba.Piso3V10;

namespace LaboratorioYiba.Piso3V10.EditorTools
{
    /// <summary>
    /// Menú: Laboratorio Yiba > Piso 3 V10 > ...
    /// 1) Configura la importación de todos los PNG (Sprite, 100 PPU, Point, sin compresión, pivotes).
    /// 2) Crea prefabs de objetos y puertas (las carpetas se crean ANTES de guardar).
    /// 3) Construye la escena con fondo, cornisa delantera, colisiones de muros, objetos, puertas y jugador.
    /// </summary>
    public static class Piso3V10Generador
    {
        private const string Raiz = "Assets/LaboratorioYiba/Piso3_V10";
        private const string RutaDatos = Raiz + "/Data/Piso3_V10_Datos.json";
        private const string RutaFondo = Raiz + "/Art/Fondo/Piso3_V10_Fondo.png";
        private const string RutaFrente = Raiz + "/Art/Fondo/Piso3_V10_MurosFrente.png";
        private const string CarpetaObjetos = Raiz + "/Art/Objetos";
        private const string CarpetaPuertas = Raiz + "/Art/Puertas";
        private const string RutaJugador = Raiz + "/Art/Jugador/JugadorPrueba.png";
        private const string RutaSombra = Raiz + "/Art/Objetos/Extra/SombraObjeto.png";
        private const string CarpetaPrefabObjetos = Raiz + "/Prefabs/Objetos";
        private const string CarpetaPrefabPuertas = Raiz + "/Prefabs/Puertas";
        private const string RutaEscena = Raiz + "/Scenes/Piso3_V10.unity";
        private const string CarpetaJason = Raiz + "/Art/Personajes/Jason";
        private const string CarpetaZorbo = Raiz + "/Art/Personajes/Zorbo";
        private const float PPUPersonajes = 100f / 3f;   // pixel art 32/40 px mostrado a x3
        private const int CantidadZorbos = 7;
        private const float PPU = 100f;

        private const int OrdenFondo = -3000;
        private const int OrdenDecal = -2000;
        private const int OrdenSombra = -1500;
        private const int OrdenFrente = 20000;

        // ------------------------------------------------------------------ datos (JsonUtility)
        [Serializable] private class Punto { public float x; public float y; }
        [Serializable] private class Muro { public Punto[] puntos; }
        [Serializable] private class Caja { public float x; public float y; public float w; public float h; }
        [Serializable] private class Objeto
        {
            public string nombre; public string sprite; public float x; public float y; public float escala;
            public bool solido; public float colAlto; public float colAncho; public bool decal;
            public int anchoPx; public int altoPx;
        }
        [Serializable] private class Puerta
        {
            public string nombre; public string sprite; public float x; public float y; public float escala; public Caja bloqueo;
        }
        [Serializable] private class Datos
        {
            public int ppu; public int ancho; public int alto;
            public Muro[] muros; public Objeto[] objetos; public Puerta[] puertas; public Punto jugador;
            public Punto[] zorbosCandidatos;
        }

        private static Vector2 Mundo(float px, float py) { return new Vector2(px / PPU, -py / PPU); }

        // ------------------------------------------------------------------ menú
        [MenuItem("Laboratorio Yiba/Piso 3 V10/1 - Verificar archivos")]
        public static void Verificar()
        {
            List<string> faltan = new List<string>();
            foreach (string r in new[] { RutaDatos, RutaFondo, RutaFrente, RutaJugador, RutaSombra })
                if (!File.Exists(r)) faltan.Add(r);
            Datos d = CargarDatos();
            if (d != null)
            {
                foreach (Objeto o in d.objetos)
                    if (!File.Exists(CarpetaObjetos + "/" + o.sprite + ".png")) faltan.Add(o.sprite);
                foreach (Puerta p in d.puertas)
                    if (!File.Exists(CarpetaPuertas + "/" + p.sprite + ".png")) faltan.Add(p.sprite);
            }
            if (faltan.Count == 0)
                Debug.Log("[Piso3 V10] Todos los archivos están presentes. Siguiente paso: '2 - Generar escena'.");
            else
                Debug.LogError("[Piso3 V10] Faltan archivos: " + string.Join(", ", faltan));
        }

        [MenuItem("Laboratorio Yiba/Piso 3 V10/2 - Generar escena")]
        public static void GenerarEscena()
        {
            Datos d = CargarDatos();
            if (d == null) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AsegurarCarpeta(CarpetaPrefabObjetos);
            AsegurarCarpeta(CarpetaPrefabPuertas);
            AsegurarCarpeta(Raiz + "/Scenes");

            ConfigurarSprites();

            Sprite fondo = CargarSprite(RutaFondo);
            Sprite frente = CargarSprite(RutaFrente);
            Sprite jugadorSp = CargarSprite(RutaJugador);
            Sprite sombra = CargarSprite(RutaSombra);
            if (fondo == null || frente == null || jugadorSp == null || sombra == null) return;

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Fondo (suelo + paredes vistas desde arriba + caras frontales)
            GameObject goFondo = new GameObject("00_Fondo");
            SpriteRenderer srF = goFondo.AddComponent<SpriteRenderer>();
            srF.sprite = fondo; srF.sortingOrder = OrdenFondo;

            // Cornisa sur: se dibuja SIEMPRE delante del jugador y de los objetos
            GameObject goFrente = new GameObject("90_MurosFrente (cornisa delante)");
            SpriteRenderer srM = goFrente.AddComponent<SpriteRenderer>();
            srM.sprite = frente; srM.sortingOrder = OrdenFrente;

            // Colisiones de muros
            GameObject goMuros = new GameObject("01_ColisionMuros");
            goMuros.isStatic = true;
            foreach (Muro m in d.muros)
            {
                if (m.puntos == null || m.puntos.Length < 3) continue;
                EdgeCollider2D e = goMuros.AddComponent<EdgeCollider2D>();
                Vector2[] pts = new Vector2[m.puntos.Length + 1];
                for (int i = 0; i < m.puntos.Length; i++) pts[i] = Mundo(m.puntos[i].x, m.puntos[i].y);
                pts[pts.Length - 1] = pts[0];
                e.points = pts;
            }

            // Objetos
            GameObject goObjetos = new GameObject("02_Objetos");
            GameObject goDecals = new GameObject("03_Decals");
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
            foreach (Objeto o in d.objetos)
            {
                if (!prefabs.ContainsKey(o.sprite))
                {
                    GameObject pf = CrearPrefabObjeto(o, sombra);
                    if (pf == null) continue;
                    prefabs[o.sprite] = pf;
                }
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[o.sprite]);
                inst.name = o.nombre;
                inst.transform.SetParent(o.decal ? goDecals.transform : goObjetos.transform, false);
                inst.transform.position = Mundo(o.x, o.y);
                inst.transform.localScale = new Vector3(o.escala, o.escala, 1f);
                if (o.solido)
                {
                    BoxCollider2D bc = inst.GetComponent<BoxCollider2D>();
                    if (bc != null) AjustarCollider(bc, o);
                    Transform sh = inst.transform.Find("Sombra");
                    if (sh != null) AjustarSombra(sh, o);
                }
                Piso3V10OrdenVisual ov = inst.GetComponent<Piso3V10OrdenVisual>();
                if (ov != null) ov.Aplicar();
            }

            // Puertas
            GameObject goPuertas = new GameObject("04_Puertas");
            foreach (Puerta p in d.puertas)
            {
                GameObject pf = CrearPrefabPuerta(p);
                if (pf == null) continue;
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                inst.name = p.nombre;
                inst.transform.SetParent(goPuertas.transform, false);
                inst.transform.position = Mundo(p.x, p.y);
                inst.transform.localScale = new Vector3(p.escala, p.escala, 1f);

                GameObject bloqueo = new GameObject("BloqueoAcceso");
                bloqueo.transform.position = Mundo(p.bloqueo.x + p.bloqueo.w * 0.5f, p.bloqueo.y + p.bloqueo.h * 0.5f);
                bloqueo.transform.SetParent(inst.transform, true);
                BoxCollider2D bc = bloqueo.AddComponent<BoxCollider2D>();
                Vector3 ls = bloqueo.transform.lossyScale;
                bc.size = new Vector2(p.bloqueo.w / PPU / ls.x, p.bloqueo.h / PPU / ls.y);

                Piso3V10Puerta puerta = inst.GetComponent<Piso3V10Puerta>();
                puerta.tipo = p.sprite.Contains("Salida") ? Piso3V10Puerta.TipoPuerta.SalidaAPiso4 : Piso3V10Puerta.TipoPuerta.EntradaDesdePiso2;
                puerta.cerrada = true;
                puerta.bloqueos = new Collider2D[] { bc };
                inst.GetComponent<Piso3V10OrdenVisual>().Aplicar();
            }

            // Jugador provisional
            GameObject jugador = new GameObject("05_JugadorPrueba");
            jugador.tag = "Player";
            jugador.transform.position = Mundo(d.jugador.x, d.jugador.y);
            SpriteRenderer srJ = jugador.AddComponent<SpriteRenderer>();
            srJ.sprite = jugadorSp;
            Rigidbody2D rb = jugador.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D cap = jugador.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = new Vector2(0.34f, 0.16f);
            cap.offset = new Vector2(0f, 0.08f);
            Piso3V10Movimiento mov = jugador.AddComponent<Piso3V10Movimiento>();
            Piso3V10OrdenVisual ovJ = jugador.AddComponent<Piso3V10OrdenVisual>();
            ovJ.dinamico = true;
            ovJ.Aplicar();

            // Cámara
            GameObject goCam = new GameObject("Main Camera");
            goCam.tag = "MainCamera";
            Camera cam = goCam.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            goCam.AddComponent<AudioListener>();
            goCam.transform.position = new Vector3(jugador.transform.position.x, jugador.transform.position.y, -10f);
            Piso3V10Camara camScript = goCam.AddComponent<Piso3V10Camara>();
            camScript.objetivo = jugador.transform;
            camScript.limites = new Rect(0f, -d.alto / PPU, d.ancho / PPU, d.alto / PPU);

            // Depuración (C / R)
            GameObject goDep = new GameObject("06_Depuracion (C colliders, R reinicio)");
            Piso3V10Depuracion dep = goDep.AddComponent<Piso3V10Depuracion>();
            dep.jugador = mov;

            ConfigurarPersonajes();
            IntegrarPersonajes(d);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AgregarABuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Piso3 V10] Escena generada: " + RutaEscena + "  (" + d.objetos.Length + " objetos, " + d.puertas.Length + " puertas, " + d.muros.Length + " contornos de muro)");
        }

        // ------------------------------------------------------------------ prefabs
        private static GameObject CrearPrefabObjeto(Objeto o, Sprite sombra)
        {
            Sprite sp = CargarSprite(CarpetaObjetos + "/" + o.sprite + ".png");
            if (sp == null) return null;
            GameObject go = new GameObject(o.sprite);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            if (o.decal)
            {
                sr.sortingOrder = OrdenDecal;
            }
            else
            {
                go.AddComponent<Piso3V10OrdenVisual>();
                if (o.solido)
                {
                    BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
                    AjustarCollider(bc, o);
                    GameObject sh = new GameObject("Sombra");
                    sh.transform.SetParent(go.transform, false);
                    SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
                    ssr.sprite = sombra;
                    ssr.sortingOrder = OrdenSombra;
                    AjustarSombra(sh.transform, o);
                }
            }
            string ruta = CarpetaPrefabObjetos + "/" + o.sprite + ".prefab";
            GameObject pf = PrefabUtility.SaveAsPrefabAsset(go, ruta);
            UnityEngine.Object.DestroyImmediate(go);
            return pf;
        }

        private static GameObject CrearPrefabPuerta(Puerta p)
        {
            Sprite sp = CargarSprite(CarpetaPuertas + "/" + p.sprite + ".png");
            if (sp == null) return null;
            GameObject go = new GameObject(p.sprite);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            go.AddComponent<Piso3V10OrdenVisual>();
            go.AddComponent<Piso3V10Puerta>();
            string ruta = CarpetaPrefabPuertas + "/" + p.sprite + ".prefab";
            GameObject pf = PrefabUtility.SaveAsPrefabAsset(go, ruta);
            UnityEngine.Object.DestroyImmediate(go);
            return pf;
        }

        // Collider en espacio local del sprite (pivote abajo-centro, sin escala)
        private static void AjustarCollider(BoxCollider2D bc, Objeto o)
        {
            float w = o.anchoPx * o.colAncho / PPU;
            float h = o.altoPx * o.colAlto / PPU;
            bc.size = new Vector2(w, h);
            bc.offset = new Vector2(0f, h * 0.5f);
        }

        private static void AjustarSombra(Transform sh, Objeto o)
        {
            float anchoMundoPx = o.anchoPx * o.escala * o.colAncho * 1.08f;
            float altoMundoPx = Mathf.Min(anchoMundoPx * 0.22f, 26f);
            float esc = Mathf.Max(o.escala, 0.01f);
            sh.localScale = new Vector3(anchoMundoPx / 96f / esc, altoMundoPx / 24f / esc, 1f);
            sh.localPosition = new Vector3(0f, altoMundoPx * 0.15f / PPU / esc, 0f);
        }

        // ------------------------------------------------------------------ importación
        [MenuItem("Laboratorio Yiba/Piso 3 V10/3 - Solo reconfigurar sprites")]
        public static void ConfigurarSprites()
        {
            ConfigurarTextura(RutaFondo, SpriteAlignment.TopLeft, 4096);
            ConfigurarTextura(RutaFrente, SpriteAlignment.TopLeft, 4096);
            ConfigurarTextura(RutaJugador, SpriteAlignment.BottomCenter, 256);
            ConfigurarTextura(RutaSombra, SpriteAlignment.Center, 256);
            foreach (string carpeta in new[] { CarpetaObjetos, CarpetaPuertas })
            {
                foreach (string f in Directory.GetFiles(carpeta, "*.png", SearchOption.TopDirectoryOnly))
                    ConfigurarTextura(f.Replace('\\', '/'), SpriteAlignment.BottomCenter, 1024);
            }
            AssetDatabase.Refresh();
        }

        private static void ConfigurarTextura(string ruta, SpriteAlignment alineacion, int tamMax)
        {
            ConfigurarTextura(ruta, alineacion, tamMax, PPU, Pivote(alineacion));
        }

        private static void ConfigurarTextura(string ruta, SpriteAlignment alineacion, int tamMax, float ppu, Vector2 pivote)
        {
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null)
            {
                Debug.LogError("[Piso3 V10] No se encontró la textura: " + ruta);
                return;
            }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = tamMax;

            // spriteAlignment/spritePivot se configuran vía TextureImporterSettings
            // (TextureImporter.spriteAlignment NO existe en Unity 6: era el error CS1061 de la V7).
            TextureImporterSettings ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)alineacion;
            ajustes.spritePivot = pivote;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }

        private static Vector2 Pivote(SpriteAlignment a)
        {
            switch (a)
            {
                case SpriteAlignment.TopLeft: return new Vector2(0f, 1f);
                case SpriteAlignment.BottomCenter: return new Vector2(0.5f, 0f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        // ------------------------------------------------------------------ Jason + Zorbos
        [MenuItem("Laboratorio Yiba/Piso 3 V10/4 - Integrar Jason y Zorbos (escena abierta)")]
        public static void MenuIntegrarPersonajes()
        {
            Datos d = CargarDatos();
            if (d == null) return;
            ConfigurarPersonajes();
            Scene escena = SceneManager.GetActiveScene();
            if (!IntegrarPersonajes(d)) return;
            EditorSceneManager.MarkSceneDirty(escena);
            if (!string.IsNullOrEmpty(escena.path)) EditorSceneManager.SaveScene(escena);
            Debug.Log("[Piso3 V10] Jason y Zorbos integrados en la escena abierta. El resto de la escena no se tocó.");
        }

        /// <summary>Importa los fotogramas de personajes: 33.33 PPU (x3), Point, pivote en los pies.</summary>
        public static void ConfigurarPersonajes()
        {
            foreach (string carpeta in new[] { CarpetaJason, CarpetaZorbo })
            {
                if (!Directory.Exists(carpeta)) { Debug.LogError("[Piso3 V10] Falta la carpeta " + carpeta); continue; }
                foreach (string f in Directory.GetFiles(carpeta, "*.png", SearchOption.AllDirectories))
                {
                    string r = f.Replace('\\', '/');
                    Vector2 piv;
                    if (r.Contains("/Zorbo/")) piv = new Vector2(0.5f, 1f / 32f);       // pies en la fila 31 de 32
                    else if (r.Contains("/Quieto/")) piv = new Vector2(0.5f, 2f / 32f); // pies en la fila 30 de 32
                    else piv = new Vector2(0.5f, 6f / 40f);                            // caminar/disparo: 40x40
                    ConfigurarTextura(r, SpriteAlignment.Custom, 256, PPUPersonajes, piv);
                }
            }
            AssetDatabase.Refresh();
        }

        private static Sprite[] CargarCarpeta(string carpeta)
        {
            if (!Directory.Exists(carpeta)) return new Sprite[0];
            string[] archivos = Directory.GetFiles(carpeta, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(archivos, StringComparer.Ordinal);
            List<Sprite> lista = new List<Sprite>();
            foreach (string f in archivos)
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'));
                if (s != null) lista.Add(s);
            }
            return lista.ToArray();
        }

        private static void AgregarSombra(GameObject padre, float anchoPx, float altoPx)
        {
            if (padre.transform.Find("Sombra") != null) return;
            Sprite sombra = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSombra);
            if (sombra == null) return;
            GameObject sh = new GameObject("Sombra");
            sh.transform.SetParent(padre.transform, false);
            SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = sombra;
            ssr.sortingOrder = OrdenSombra;
            Vector3 ls = padre.transform.lossyScale;
            sh.transform.localScale = new Vector3(anchoPx / 96f / ls.x, altoPx / 24f / ls.y, 1f);
            sh.transform.localPosition = new Vector3(0f, 0.02f / ls.y, 0f);
        }

        private static bool IntegrarPersonajes(Datos d)
        {
            Sprite[] quieto = CargarCarpeta(CarpetaJason + "/Quieto");
            Sprite[] zorbos = CargarCarpeta(CarpetaZorbo);
            if (quieto.Length != 8 || zorbos.Length == 0)
            {
                Debug.LogError("[Piso3 V10] No se cargaron los sprites de Jason (8 quieto) o del Zorbo. Revisa Art/Personajes.");
                return false;
            }

            // ---------- Jason: reutiliza el jugador existente (conserva su posición) o lo crea
            Piso3V10Movimiento mov = UnityEngine.Object.FindFirstObjectByType<Piso3V10Movimiento>();
            GameObject jason;
            if (mov != null)
            {
                jason = mov.gameObject;
            }
            else
            {
                jason = new GameObject("05_Jason");
                jason.tag = "Player";
                jason.transform.position = Mundo(d.jugador.x, d.jugador.y);
                jason.AddComponent<SpriteRenderer>();
                Rigidbody2D rb = jason.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                mov = jason.AddComponent<Piso3V10Movimiento>();
                jason.AddComponent<Piso3V10OrdenVisual>().dinamico = true;
                Piso3V10Camara camara = UnityEngine.Object.FindFirstObjectByType<Piso3V10Camara>();
                if (camara != null) camara.objetivo = jason.transform;
                Piso3V10Depuracion dep = UnityEngine.Object.FindFirstObjectByType<Piso3V10Depuracion>();
                if (dep != null) dep.jugador = mov;
            }
            jason.name = "05_Jason";
            jason.transform.localScale = Vector3.one;
            SpriteRenderer srJ = jason.GetComponent<SpriteRenderer>();
            if (srJ == null) srJ = jason.AddComponent<SpriteRenderer>();
            srJ.sprite = quieto[0];
            CapsuleCollider2D cap = jason.GetComponent<CapsuleCollider2D>();
            if (cap == null) cap = jason.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = new Vector2(0.34f, 0.16f);
            cap.offset = new Vector2(0f, 0.08f);
            Piso3V10OrdenVisual ovJ = jason.GetComponent<Piso3V10OrdenVisual>();
            if (ovJ == null) ovJ = jason.AddComponent<Piso3V10OrdenVisual>();
            ovJ.dinamico = true;

            Piso3V10JasonAnimacion anim = jason.GetComponent<Piso3V10JasonAnimacion>();
            if (anim == null) anim = jason.AddComponent<Piso3V10JasonAnimacion>();
            anim.quieto = quieto;
            anim.caminarDerecha = CargarCarpeta(CarpetaJason + "/CaminarDerecha");
            anim.caminarIzquierda = CargarCarpeta(CarpetaJason + "/CaminarIzquierda");
            anim.caminarArriba = CargarCarpeta(CarpetaJason + "/CaminarArriba");
            anim.caminarAbajo = CargarCarpeta(CarpetaJason + "/CaminarAbajo");
            anim.disparoFrente = CargarCarpeta(CarpetaJason + "/DisparoFrente");
            anim.disparoDerecha = CargarCarpeta(CarpetaJason + "/DisparoDerecha");
            anim.disparoIzquierda = CargarCarpeta(CarpetaJason + "/DisparoIzquierda");
            AgregarSombra(jason, 44f, 12f);
            EditorUtility.SetDirty(jason);

            // ---------- 7 Zorbos estáticos repartidos por el mapa
            GameObject viejo = GameObject.Find("07_Zorbos");
            if (viejo != null) UnityEngine.Object.DestroyImmediate(viejo);
            GameObject raiz = new GameObject("07_Zorbos");

            Physics2D.SyncTransforms();
            List<Bounds> ocupados = new List<Bounds>();
            foreach (Piso3V10OrdenVisual ov in UnityEngine.Object.FindObjectsByType<Piso3V10OrdenVisual>(FindObjectsSortMode.None))
            {
                if (ov.gameObject == jason) continue;
                SpriteRenderer r = ov.GetComponent<SpriteRenderer>();
                if (r != null && r.sprite != null) ocupados.Add(r.bounds);
            }

            int[] orientaciones = { 0, 1, 7, 0, 2, 6, 0 };
            List<Vector2> puestos = new List<Vector2>();
            Vector2 posJason = jason.transform.position;
            if (d.zorbosCandidatos != null)
            {
                foreach (Punto c in d.zorbosCandidatos)
                {
                    if (puestos.Count >= CantidadZorbos) break;
                    Vector2 p = Mundo(c.x, c.y);
                    if (Vector2.Distance(p, posJason) < 5f) continue;
                    bool lejos = true;
                    foreach (Vector2 q in puestos) if (Vector2.Distance(p, q) < 3f) lejos = false;
                    if (!lejos) continue;
                    if (Physics2D.OverlapBox(p + new Vector2(0f, 0.12f), new Vector2(0.9f, 0.5f), 0f) != null) continue;
                    Bounds bz = new Bounds(new Vector3(p.x, p.y + 0.55f, 0f), new Vector3(1.1f, 1.2f, 10f));
                    bool choca = false;
                    foreach (Bounds b in ocupados)
                    {
                        Bounds b2 = b; b2.size = new Vector3(b.size.x, b.size.y, 10f);
                        if (b2.Intersects(bz)) { choca = true; break; }
                    }
                    if (choca) continue;
                    puestos.Add(p);
                }
            }

            for (int i = 0; i < puestos.Count; i++)
            {
                GameObject z = new GameObject("Zorbo_" + (i + 1).ToString("00"));
                z.transform.SetParent(raiz.transform, false);
                z.transform.position = puestos[i];
                z.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                z.AddComponent<SpriteRenderer>();
                Piso3V10Zorbo zc = z.AddComponent<Piso3V10Zorbo>();
                zc.orientaciones = zorbos;
                zc.orientacion = orientaciones[i % orientaciones.Length] % zorbos.Length;
                zc.Aplicar();
                Rigidbody2D rbz = z.AddComponent<Rigidbody2D>();
                rbz.bodyType = RigidbodyType2D.Kinematic;
                rbz.gravityScale = 0f;
                CapsuleCollider2D cz = z.AddComponent<CapsuleCollider2D>();
                cz.direction = CapsuleDirection2D.Horizontal;
                cz.size = new Vector2(0.42f, 0.18f);
                cz.offset = new Vector2(0f, 0.09f);
                Piso3V10OrdenVisual ovz = z.AddComponent<Piso3V10OrdenVisual>();
                ovz.Aplicar();
                AgregarSombra(z, 56f, 14f);
            }
            if (puestos.Count < CantidadZorbos)
                Debug.LogWarning("[Piso3 V10] Solo se colocaron " + puestos.Count + " Zorbos: el resto de posiciones candidatas estaban ocupadas por objetos.");
            return true;
        }

        // ------------------------------------------------------------------ utilidades
        private static Datos CargarDatos()
        {
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(RutaDatos);
            if (ta == null)
            {
                Debug.LogError("[Piso3 V10] No se encontró " + RutaDatos);
                return null;
            }
            Datos d = JsonUtility.FromJson<Datos>(ta.text);
            if (d == null || d.muros == null || d.objetos == null || d.puertas == null)
            {
                Debug.LogError("[Piso3 V10] El JSON de datos está incompleto.");
                return null;
            }
            return d;
        }

        private static Sprite CargarSprite(string ruta)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (s == null) Debug.LogError("[Piso3 V10] No se pudo cargar el sprite: " + ruta + " (¿está importado como Sprite?)");
            return s;
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            string nombre = Path.GetFileName(ruta);
            AsegurarCarpeta(padre);
            AssetDatabase.CreateFolder(padre, nombre);
        }

        private static void AgregarABuild(string ruta)
        {
            List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in lista) if (s.path == ruta) return;
            lista.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
