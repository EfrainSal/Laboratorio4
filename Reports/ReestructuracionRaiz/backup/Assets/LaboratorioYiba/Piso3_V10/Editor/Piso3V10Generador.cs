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
    /// Menú: Laboratorio Yiba > Piso 3 V10 > ...   (versión V14)
    ///  1  Verificar archivos
    ///  2  Generar escena completa
    ///  3  Reparar escena abierta (muros sólidos, cornisa por tramos, objetos, Jason, Zorbos) SIN mover nada
    ///  4  Integrar Jason y Zorbos en la escena abierta
    ///  5  Crear prefabs de personajes (Jason, Zorbo, Zorbo2, Milena, DoctorYiba, Arma)
    ///  6  Generar galería de personajes (todas las animaciones y direcciones)
    ///  7  Solo reconfigurar sprites
    ///  8  Borrar sprites antiguos de V10/V11
    /// </summary>
    public static class Piso3V10Generador
    {
        private const string Raiz = "Assets/LaboratorioYiba/Piso3_V10";
        private const string RutaDatos = Raiz + "/Data/Piso3_V10_Datos.json";
        private const string RutaFondo = Raiz + "/Art/Fondo/Piso3_V10_Fondo.png";
        private const string CarpetaCornisa = Raiz + "/Art/Fondo/Cornisa";
        private const string CarpetaObjetos = Raiz + "/Art/Objetos";
        private const string CarpetaPuertas = Raiz + "/Art/Puertas";
        private const string RutaSombra = Raiz + "/Art/Objetos/Extra/SombraObjeto.png";
        private const string CarpetaPersonajes = Raiz + "/Art/Personajes";
        private const string CarpetaArmas = Raiz + "/Art/Armas";
        private const string RutaAnimaciones = CarpetaPersonajes + "/Personajes_Animaciones.json";
        private const string RutaBala = Raiz + "/Art/Efectos/Bala.png";
        private const string RutaImpacto = Raiz + "/Art/Efectos/Impacto.png";
        private const string CarpetaPrefabObjetos = Raiz + "/Prefabs/Objetos";
        private const string CarpetaPrefabPuertas = Raiz + "/Prefabs/Puertas";
        private const string CarpetaPrefabPersonajes = Raiz + "/Prefabs/Personajes";
        private const string RutaEscena = Raiz + "/Scenes/Piso3_V10.unity";
        private const string RutaGaleria = Raiz + "/Scenes/Galeria_Personajes.unity";
        internal const string NombreMuros = "01_ColisionMuros";
        internal const string NombreCornisa = "90_Cornisa (tramos ordenados por profundidad)";
        private const float PPU = 100f;
        private const float PPUPersonajes = 100f / 3f;   // pixel art mostrado a x3

        internal const int OrdenFondo = -32000;
        internal const int OrdenDecal = -31000;
        internal const int OrdenSombra = -30000;

        private static readonly string[] Direcciones = { "Sur", "Sureste", "Este", "Noreste", "Norte", "Noroeste", "Oeste", "Suroeste" };
        private static readonly string[] Personajes = { "Jason", "Zorbo", "Zorbo2", "Milena", "DoctorYiba" };

        // ------------------------------------------------------------------ datos (JsonUtility)
        [Serializable] internal class Punto { public float x; public float y; }
        [Serializable] internal class Poligono { public Punto[] puntos; }
        [Serializable] internal class Caja { public float x; public float y; public float w; public float h; }
        [Serializable] internal class Objeto
        {
            public string nombre; public string sprite; public float x; public float y; public float escala;
            public bool solido; public float colAlto; public float colAncho; public bool decal;
            public int anchoPx; public int altoPx;
        }
        [Serializable] internal class Puerta
        {
            public string nombre; public string sprite; public float x; public float y; public float escala; public Caja bloqueo;
        }
        [Serializable] internal class ZorboDato { public float x; public float y; public int orientacion; }
        [Serializable] internal class Huella { public string sprite; public float alto; public float ancho; }
        [Serializable] internal class Tramo { public string archivo; public int x; public int y; public int w; public int h; public int ordenY; }
        [Serializable] internal class Datos
        {
            public string version; public int ppu; public int ancho; public int alto;
            public Poligono[] solidos; public Objeto[] objetos; public Puerta[] puertas; public Punto jugador;
            public ZorboDato[] zorbos; public Huella[] huellasLista; public float huecoMinimoPx; public Tramo[] cornisas;
            public Punto[] pruebasSuelo; public Punto[] pruebasMuro;
            public Caja[] murosCajas;   // V16: muros = cajas simples (px del fondo, arriba-izquierda)
        }
        [Serializable] internal class AnimInfo { public string ruta; public float fps; public bool loop; public int[] direcciones; }
        [Serializable] internal class PivoteInfo { public string ruta; public float x; public float y; }
        [Serializable] internal class AnimLista { public AnimInfo[] animaciones; public PivoteInfo[] pivotes; }

        private static Vector2 Mundo(float px, float py) { return new Vector2(px / PPU, -py / PPU); }

        // ================================================================== 1. verificar
        [MenuItem("Laboratorio Yiba/Piso 3 V10/1 - Verificar archivos")]
        public static void Verificar()
        {
            List<string> faltan = new List<string>();
            foreach (string r in new[] { RutaDatos, RutaFondo, RutaSombra, RutaBala, RutaImpacto, RutaAnimaciones })
                if (!File.Exists(r)) faltan.Add(r);
            foreach (string p in Personajes)
                if (!Directory.Exists(CarpetaPersonajes + "/" + p)) faltan.Add(CarpetaPersonajes + "/" + p);
            if (!Directory.Exists(CarpetaCornisa)) faltan.Add(CarpetaCornisa);
            Datos d = CargarDatos();
            if (d != null)
            {
                foreach (Objeto o in d.objetos)
                    if (!File.Exists(CarpetaObjetos + "/" + o.sprite + ".png")) faltan.Add(o.sprite);
                foreach (Puerta p in d.puertas)
                    if (!File.Exists(CarpetaPuertas + "/" + p.sprite + ".png")) faltan.Add(p.sprite);
                if (d.cornisas != null)
                    foreach (Tramo t in d.cornisas)
                        if (!File.Exists(CarpetaCornisa + "/" + t.archivo)) { faltan.Add(t.archivo); break; }
            }
            bool antiguos = Directory.Exists(CarpetaPersonajes + "/Jason/CaminarDerecha") || File.Exists(CarpetaPersonajes + "/Zorbo/Zorbo_01.png");
            if (faltan.Count == 0)
                Debug.Log("[Piso3 V14] Todo presente. Escena nueva: '2'. Tu escena acomodada: ábrela y usa '3'. Ver animaciones: '6'." +
                          (antiguos ? "  (Quedan sprites antiguos de V10/V11: puedes borrarlos con '8' DESPUÉS de usar '2' o '3'.)" : ""));
            else
                Debug.LogError("[Piso3 V14] Faltan archivos: " + string.Join(", ", faltan));
        }

        // ================================================================== 2. escena completa
        [MenuItem("Laboratorio Yiba/Piso 3 V10/2 - Generar escena completa")]
        public static void GenerarEscena()
        {
            Datos d = CargarDatos();
            if (d == null) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AsegurarCarpeta(CarpetaPrefabObjetos);
            AsegurarCarpeta(CarpetaPrefabPuertas);
            AsegurarCarpeta(CarpetaPrefabPersonajes);
            AsegurarCarpeta(Raiz + "/Scenes");
            ConfigurarSprites();

            Sprite fondo = CargarSprite(RutaFondo);
            Sprite sombra = CargarSprite(RutaSombra);
            if (fondo == null || sombra == null) return;

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject goFondo = new GameObject("00_Fondo");
            SpriteRenderer srF = goFondo.AddComponent<SpriteRenderer>();
            srF.sprite = fondo; srF.sortingOrder = OrdenFondo;

            ConstruirCornisa(d);
            ConstruirMuros(d);

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
                    if (bc != null) AjustarCollider(bc, o.anchoPx, o.altoPx, o.colAncho, o.colAlto);
                    Transform sh = inst.transform.Find("Sombra");
                    if (sh != null) AjustarSombra(sh, o.anchoPx, o.escala, o.colAncho);
                }
                Piso3V10OrdenVisual ov = inst.GetComponent<Piso3V10OrdenVisual>();
                if (ov != null) ov.Aplicar();
            }

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

            GameObject jason = CrearJason(Mundo(d.jugador.x, d.jugador.y));
            GameObject goCam = new GameObject("Main Camera");
            goCam.tag = "MainCamera";
            Camera cam = goCam.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            goCam.AddComponent<AudioListener>();
            goCam.transform.position = new Vector3(jason.transform.position.x, jason.transform.position.y, -10f);
            Piso3V10Camara camScript = goCam.AddComponent<Piso3V10Camara>();
            camScript.objetivo = jason.transform;
            camScript.limites = new Rect(0f, -d.alto / PPU, d.ancho / PPU, d.alto / PPU);

            GameObject goDep = new GameObject("06_Depuracion (C colliders, R reinicio)");
            goDep.AddComponent<Piso3V10Depuracion>().jugador = jason.GetComponent<Piso3V10Movimiento>();

            CrearZorbos(d);
            CrearPrefabsPersonajes();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AgregarABuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Piso3 V14] Escena generada: " + RutaEscena + " (" + d.objetos.Length + " objetos, " + d.puertas.Length +
                      " puertas, " + d.solidos.Length + " piezas de muro, " + (d.cornisas != null ? d.cornisas.Length : 0) +
                      " tramos de cornisa, " + (d.zorbos != null ? d.zorbos.Length : 0) + " Zorbos)");
        }

        // ================================================================== 3. reparar escena abierta
        [MenuItem("Laboratorio Yiba/Piso 3 V10/3 - Reparar escena abierta (sin mover nada)")]
        public static void RepararEscenaAbierta()
        {
            if (!EsEscenaPiso3()) { Debug.LogWarning("[Piso3] La escena abierta no es del Piso 3 (su nombre no empieza por 'Piso3'): no se toca."); return; }
            if (Piso3ModularReparar.EsModular()) { Piso3ModularReparar.Menu(); return; }   // V15: escena de tilemaps
            Datos d = CargarDatos();
            if (d == null) return;
            ConfigurarSprites();
            Scene escena = SceneManager.GetActiveScene();

            ConstruirCornisa(d);
            ConstruirMuros(d);
            ActualizarOrdenes(BuscarFondo());
            AjustarAccesoPiso4(BuscarFondo());

            Dictionary<string, Huella> tabla = new Dictionary<string, Huella>();
            if (d.huellasLista != null) foreach (Huella h in d.huellasLista) tabla[h.sprite] = h;
            int ajustados = 0;
            List<BoxCollider2D> cajas = new List<BoxCollider2D>();
            foreach (BoxCollider2D bc in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                if (bc.GetComponentInParent<Piso3V10Puerta>() != null) continue;
                SpriteRenderer sr = bc.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) continue;
                string clave = sr.sprite.name;
                if (!tabla.ContainsKey(clave)) continue;
                Huella h = tabla[clave];
                Undo.RecordObject(bc, "Ajustar huella");
                AjustarCollider(bc, sr.sprite.rect.width, sr.sprite.rect.height, h.ancho, h.alto);
                cajas.Add(bc);
                ajustados++;
            }
            Physics2D.SyncTransforms();
            int cerrados = CerrarHuecos(cajas, d.huecoMinimoPx > 0 ? d.huecoMinimoPx / PPU : 0.30f);

            Piso3V10Movimiento mov = UnityEngine.Object.FindFirstObjectByType<Piso3V10Movimiento>();
            if (mov != null) PrepararJason(mov.gameObject);
            int zorbos = 0;
            foreach (Piso3V10Zorbo z in UnityEngine.Object.FindObjectsByType<Piso3V10Zorbo>(FindObjectsSortMode.None))
            {
                PrepararZorbo(z.gameObject, "Zorbo", z.orientacion);
                zorbos++;
            }

            EditorSceneManager.MarkSceneDirty(escena);
            if (!string.IsNullOrEmpty(escena.path)) EditorSceneManager.SaveScene(escena);
            Debug.Log("[Piso3 V14] Escena reparada: cornisa por tramos, muros sólidos, " + ajustados + " objetos (" + cerrados +
                      " huecos cerrados), " + zorbos + " Zorbos animados" +
                      (mov != null ? ", Jason con todas sus animaciones." : ". No se encontró Piso3V10Movimiento (usa '4')."));
        }

        // ================================================================== 4. Jason + Zorbos
        [MenuItem("Laboratorio Yiba/Piso 3 V10/4 - Integrar Jason y Zorbos (escena abierta)")]
        public static void MenuIntegrarPersonajes()
        {
            if (!EsEscenaPiso3()) { Debug.LogWarning("[Piso3] La escena abierta no es del Piso 3 (su nombre no empieza por 'Piso3'): no se toca."); return; }
            if (Piso3ModularReparar.EsModular()) { Piso3ModularReparar.Menu(); return; }   // V15: escena de tilemaps
            Datos d = CargarDatos();
            if (d == null) return;
            ConfigurarSprites();
            Scene escena = SceneManager.GetActiveScene();
            Piso3V10Movimiento mov = UnityEngine.Object.FindFirstObjectByType<Piso3V10Movimiento>();
            if (mov != null) PrepararJason(mov.gameObject);
            else CrearJason(PosDatos(d, d.jugador.x, d.jugador.y));
            if (GameObject.Find("07_Zorbos") == null) CrearZorbos(d);
            else foreach (Piso3V10Zorbo z in UnityEngine.Object.FindObjectsByType<Piso3V10Zorbo>(FindObjectsSortMode.None))
                    PrepararZorbo(z.gameObject, "Zorbo", z.orientacion);
            EditorSceneManager.MarkSceneDirty(escena);
            if (!string.IsNullOrEmpty(escena.path)) EditorSceneManager.SaveScene(escena);
            Debug.Log("[Piso3 V14] Jason y Zorbos actualizados con todas sus animaciones (posiciones conservadas).");
        }

        // ================================================================== 5. prefabs de personajes
        [MenuItem("Laboratorio Yiba/Piso 3 V10/5 - Crear prefabs de personajes")]
        public static void MenuCrearPrefabsPersonajes()
        {
            ConfigurarSprites();
            CrearPrefabsPersonajes();
            Debug.Log("[Piso3 V14] Prefabs en " + CarpetaPrefabPersonajes + ": Jason, Zorbo, Zorbo2, Milena, DoctorYiba, ArmaLaboratorio4.");
        }

        private static void CrearPrefabsPersonajes()
        {
            AsegurarCarpeta(CarpetaPrefabPersonajes);
            GuardarPrefab(CrearJason(Vector2.zero, false), "Jason");
            GuardarPrefab(CrearZorbo("Zorbo", Vector2.zero, 0), "Zorbo");
            GuardarPrefab(CrearZorbo("Zorbo2", Vector2.zero, 0), "Zorbo2");
            GuardarPrefab(CrearPersonajeSimple("Milena", new Vector2(0.34f, 0.16f), 44f), "Milena");
            GuardarPrefab(CrearPersonajeSimple("DoctorYiba", new Vector2(0.50f, 0.22f), 70f), "DoctorYiba");
            GuardarPrefab(CrearArma(), "ArmaLaboratorio4");
            AssetDatabase.SaveAssets();
        }

        private static void GuardarPrefab(GameObject go, string nombre)
        {
            PrefabUtility.SaveAsPrefabAsset(go, CarpetaPrefabPersonajes + "/" + nombre + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        // ================================================================== 6. galería
        [MenuItem("Laboratorio Yiba/Piso 3 V10/6 - Generar galería de personajes")]
        public static void GenerarGaleria()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConfigurarSprites();
            AsegurarCarpeta(Raiz + "/Scenes");
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float y = 0f;
            int filas = 0;
            foreach (string p in Personajes)
            {
                Piso3V10Animacion[] anims = CargarAnimacionesDe(p);
                float alto = p == "DoctorYiba" ? 2.4f : 1.5f;
                foreach (Piso3V10Animacion a in anims)
                {
                    Etiqueta(p + " / " + a.nombre, new Vector3(-1.2f, y + 0.4f, 0f), TextAnchor.MiddleRight);
                    for (int dir = 0; dir < 8; dir++)
                    {
                        bool tiene = a.direcciones[dir] != null && a.direcciones[dir].frames.Length > 0;
                        if (!tiene) continue;
                        GameObject go = new GameObject(p + "_" + a.nombre + "_" + Direcciones[dir]);
                        go.transform.position = new Vector3(dir * 1.6f, y, 0f);
                        go.AddComponent<SpriteRenderer>();
                        Piso3V10Personaje8Dir pa = go.AddComponent<Piso3V10Personaje8Dir>();
                        pa.animaciones = anims;
                        pa.animacionInicial = a.nombre;
                        pa.direccion = dir;
                        pa.forzarBucle = true;
                        go.GetComponent<SpriteRenderer>().sprite = a.direcciones[dir].frames[0];
                    }
                    y -= alto;
                    filas++;
                }
                y -= 0.6f;
            }
            for (int dir = 0; dir < 8; dir++) Etiqueta(Direcciones[dir], new Vector3(dir * 1.6f, 1.6f, 0f), TextAnchor.MiddleCenter);
            GameObject goCam = new GameObject("Main Camera");
            goCam.tag = "MainCamera";
            Camera cam = goCam.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(6f, -y * 0.52f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            goCam.transform.position = new Vector3(5.6f, y * 0.5f + 0.8f, -10f);
            EditorSceneManager.SaveScene(escena, RutaGaleria);
            Debug.Log("[Piso3 V14] Galería: " + RutaGaleria + " (" + filas + " animaciones, cada columna una dirección). Dale Play.");
        }

        private static void Etiqueta(string texto, Vector3 pos, TextAnchor ancla)
        {
            GameObject g = new GameObject("Texto_" + texto);
            g.transform.position = pos;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.text = texto;
            tm.anchor = ancla;
            tm.characterSize = 0.06f;
            tm.fontSize = 48;
            tm.color = Color.white;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fuente != null)
            {
                tm.font = fuente;
                g.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
        }

        // ================================================================== 8. limpieza
        [MenuItem("Laboratorio Yiba/Piso 3 V10/8 - Borrar sprites antiguos de V10-V11")]
        public static void BorrarAntiguos()
        {
            List<string> rutas = new List<string>();
            foreach (string n in new[] { "CaminarDerecha", "CaminarIzquierda", "CaminarArriba", "CaminarAbajo", "DisparoDerecha", "DisparoIzquierda", "DisparoFrente" })
                rutas.Add(CarpetaPersonajes + "/Jason/" + n);
            if (Directory.Exists(CarpetaPersonajes + "/Jason/Quieto"))
                foreach (string f in Directory.GetFiles(CarpetaPersonajes + "/Jason/Quieto", "*.png", SearchOption.TopDirectoryOnly)) rutas.Add(f.Replace('\\', '/'));
            if (Directory.Exists(CarpetaPersonajes + "/Zorbo"))
                foreach (string f in Directory.GetFiles(CarpetaPersonajes + "/Zorbo", "Zorbo_*.png", SearchOption.TopDirectoryOnly)) rutas.Add(f.Replace('\\', '/'));
            rutas.Add(Raiz + "/Art/Fondo/Piso3_V10_MurosFrente.png");
            rutas.Add(Raiz + "/Art/Jugador");
            int borrados = 0;
            foreach (string r in rutas)
                if (AssetDatabase.LoadMainAssetAtPath(r) != null && AssetDatabase.DeleteAsset(r)) borrados++;
            Debug.Log("[Piso3 V14] Sprites antiguos borrados: " + borrados + ". Si algún objeto quedó sin imagen, usa '3' o '4' para reasignarlas.");
        }

        // ================================================================== anclaje al fondo
        /// <summary>
        /// V14: busca el fondo REAL de la escena (sprite "Piso3_V10_Fondo"). Muros y cornisa se crean como
        /// hijos suyos, en sus coordenadas locales, así coinciden con el dibujo aunque el fondo esté movido,
        /// escalado, con otro pivote/PPU o importado a menor tamaño.
        /// </summary>
        /// <summary>
        /// V16.1: ¿la escena ABIERTA es del Piso 3? Solo por el NOMBRE de la escena ("Piso3...").
        /// Antes bastaba con que el jugador tuviera Piso3V10Movimiento, y por eso al dar Play en otras
        /// escenas (Escenario1_Entrada) se les metía el fondo y los muros invisibles del Piso 3.
        /// </summary>
        internal static bool EsEscenaPiso3()
        {
            string n = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
            return n.Contains("piso3");
        }

        /// <summary>
        /// V16.1: quita de una escena que NO es del Piso 3 lo que la auto-reparación le metió por error
        /// (fondo del Piso 3, muros invisibles "01_ColisionMuros", cornisa). Devuelve lo que quitó.
        /// </summary>
        internal static List<string> QuitarRestosPiso3()
        {
            List<string> quitado = new List<string>();
            HashSet<GameObject> borrar = new HashSet<GameObject>();
            foreach (SpriteRenderer sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (sr.sprite != null && sr.sprite.name == "Piso3_V10_Fondo") borrar.Add(sr.gameObject);
            foreach (Piso3V10Colisiones c in UnityEngine.Object.FindObjectsByType<Piso3V10Colisiones>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                borrar.Add(c.gameObject);
            foreach (string n in new[] { NombreMuros, NombreCornisa, "90_MurosFrente (cornisa delante)" })
            {
                GameObject g = GameObject.Find(n);
                if (g != null) borrar.Add(g);
            }
            foreach (GameObject g in borrar)
            {
                if (g == null) continue;            // ya borrado como hijo de otro
                quitado.Add("'" + g.name + "'" + (g.transform.childCount > 0 ? " (con " + g.transform.childCount + " hijos)" : ""));
                Undo.DestroyObjectImmediate(g);
            }
            return quitado;
        }

        [MenuItem("Laboratorio Yiba/Piso 3 V10/Quitar restos del Piso 3 de esta escena")]
        private static void MenuQuitarRestos()
        {
            if (EsEscenaPiso3())
            {
                Debug.LogWarning("[Piso3] Esta escena ES del Piso 3 ('" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "'): no se quita nada.");
                return;
            }
            List<string> q = QuitarRestosPiso3();
            if (q.Count > 0) EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log(q.Count > 0 ? "[Piso3] Quitado de esta escena: " + string.Join(", ", q) + ". Guarda la escena (Ctrl+S)." : "[Piso3] Esta escena no tenía nada del Piso 3.");
        }

        /// <summary>
        /// V17: puerta de salida al Piso 4. Antes su caja "BloqueoAcceso" tapaba toda el área de franjas
        /// amarillas; ahora solo la base de la puerta (92 x 27 px del fondo): se puede entrar al área hasta
        /// la puerta y detrás de ella sigue la pared. Respeta dónde y a qué escala esté tu puerta.
        /// Devuelve true si cambió algo.
        /// </summary>
        internal static bool AjustarAccesoPiso4(SpriteRenderer fondo)
        {
            float f = 0.01f;                               // px del fondo -> unidades de mundo
            if (fondo != null && fondo.sprite != null)
                f = fondo.transform.lossyScale.y / fondo.sprite.pixelsPerUnit * (fondo.sprite.rect.width / 3888f);
            bool cambio = false;
            foreach (Piso3V10Puerta pu in UnityEngine.Object.FindObjectsByType<Piso3V10Puerta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pu.tipo != Piso3V10Puerta.TipoPuerta.SalidaAPiso4 && !pu.name.Contains("Salida")) continue;
                BoxCollider2D bc = null;
                if (pu.bloqueos != null) foreach (Collider2D c in pu.bloqueos) if (c is BoxCollider2D) { bc = (BoxCollider2D)c; break; }
                if (bc == null) { Transform t = pu.transform.Find("BloqueoAcceso"); if (t != null) bc = t.GetComponent<BoxCollider2D>(); }
                if (bc == null) continue;
                SpriteRenderer sr = pu.GetComponent<SpriteRenderer>();
                float cx = sr != null ? sr.bounds.center.x : pu.transform.position.x;
                float ancho = sr != null ? sr.bounds.size.x * 0.95f : 92f * f;
                Vector3 centro = new Vector3(cx, pu.transform.position.y + 3.5f * f, bc.transform.position.z);
                Vector2 tam = new Vector2(ancho, 27f * f);
                Bounds actual = bc.bounds;
                if (Mathf.Abs(actual.center.x - centro.x) < 0.005f && Mathf.Abs(actual.center.y - centro.y) < 0.005f &&
                    Mathf.Abs(actual.size.x - tam.x) < 0.005f && Mathf.Abs(actual.size.y - tam.y) < 0.005f) continue;
                Undo.RecordObject(bc.transform, "Acceso Piso 4");
                Undo.RecordObject(bc, "Acceso Piso 4");
                bc.transform.position = centro;
                Vector3 ls = bc.transform.lossyScale;
                bc.offset = Vector2.zero;
                bc.size = new Vector2(tam.x / Mathf.Abs(ls.x), tam.y / Mathf.Abs(ls.y));
                cambio = true;
            }
            return cambio;
        }

        internal static SpriteRenderer BuscarFondo()
        {
            SpriteRenderer mejor = null;
            foreach (SpriteRenderer sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr.sprite == null || sr.sprite.name != "Piso3_V10_Fondo") continue;
                if (mejor == null || sr.bounds.size.sqrMagnitude > mejor.bounds.size.sqrMagnitude) mejor = sr;
            }
            return mejor;
        }

        /// <summary>Píxel del diseño (origen arriba-izquierda, 3888x1872) -> coordenadas locales del fondo.</summary>
        internal static Vector2 LocalFondo(SpriteRenderer fondo, Datos d, float px, float py)
        {
            Sprite sp = fondo.sprite;
            float k = sp.rect.width / Mathf.Max(1f, d.ancho);       // por si se importó más pequeño
            float ppu = sp.pixelsPerUnit;
            Vector2 piv = sp.pivot;                                   // en píxeles desde abajo-izquierda
            return new Vector2((px * k - piv.x) / ppu, ((sp.rect.height - py * k) - piv.y) / ppu);
        }

        internal static Vector2 PosDatos(Datos d, float px, float py)
        {
            SpriteRenderer fondo = BuscarFondo();
            if (fondo == null) return Mundo(px, py);
            return fondo.transform.TransformPoint(LocalFondo(fondo, d, px, py));
        }

        private static SpriteRenderer FondoOCrear()
        {
            SpriteRenderer fondo = BuscarFondo();
            if (fondo != null) return fondo;
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondo);
            if (sp == null) return null;
            GameObject go = new GameObject("00_Fondo");
            Undo.RegisterCreatedObjectUndo(go, "Fondo");
            fondo = go.AddComponent<SpriteRenderer>();
            fondo.sprite = sp;
            fondo.sortingOrder = OrdenFondo;
            Debug.LogWarning("[Piso3 V14] No había fondo del Piso 3 en la escena: se creó '00_Fondo' en el origen.");
            return fondo;
        }

        internal static string Firma(SpriteRenderer fondo)
        {
            if (fondo == null) return "";
            Sprite sp = fondo.sprite;
            return sp.rect.width + "x" + sp.rect.height + "|" + sp.pixelsPerUnit + "|" + sp.pivot.x + "," + sp.pivot.y;
        }

        // ================================================================== cornisa por tramos
        internal static void ConstruirCornisa(Datos d)
        {
            foreach (string n in new[] { NombreCornisa, "90_MurosFrente (cornisa delante)" })
            {
                GameObject viejo = GameObject.Find(n);
                if (viejo != null) Undo.DestroyObjectImmediate(viejo);
            }
            if (d.cornisas == null) return;
            SpriteRenderer fondo = FondoOCrear();
            if (fondo == null) return;
            float k = fondo.sprite.rect.width / Mathf.Max(1f, d.ancho);
            float escala = 100f * k / fondo.sprite.pixelsPerUnit;     // los tramos se importan a 100 PPU
            GameObject raiz = new GameObject(NombreCornisa);
            Undo.RegisterCreatedObjectUndo(raiz, "Cornisa");
            raiz.transform.SetParent(fondo.transform, false);
            foreach (Tramo t in d.cornisas)
            {
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaCornisa + "/" + t.archivo);
                if (sp == null) continue;
                GameObject g = new GameObject(Path.GetFileNameWithoutExtension(t.archivo));
                g.transform.SetParent(raiz.transform, false);
                g.transform.localPosition = LocalFondo(fondo, d, t.x, t.y);     // pivote arriba-izquierda
                g.transform.localScale = new Vector3(escala, escala, 1f);
                SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                // mismo criterio que Piso3V10OrdenVisual (-Y del mundo x 100), medido en el borde inferior del tramo:
                // la sala de abajo se dibuja delante de la pared; la de arriba queda detrás (pies tapados).
                Vector3 borde = fondo.transform.TransformPoint(LocalFondo(fondo, d, t.x + t.w * 0.5f, t.ordenY));
                sr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-borde.y * 100f), -29000, 29000);
            }
            raiz.isStatic = true;
        }

        // ================================================================== muros sólidos
        /// <summary>Marca de la versión de los muros (si cambia, la auto-reparación los rehace al dar Play).</summary>
        internal const string VersionMuros = "cajasV16";

        internal static string FirmaMuros(SpriteRenderer fondo) { return Firma(fondo) + "|" + VersionMuros; }

        /// <summary>
        /// V16 – Muros sólidos = BoxCollider2D simples (242 cajas, una por hijo "Muro_###"), hijos del fondo y en
        /// sus coordenadas locales. Antes era UN PolygonCollider2D con 176 trazos (55 de ellos de 1 px de alto):
        /// Unity los triangula juntos y eso podía dejar barreras invisibles en pasillos abiertos.
        /// Las cajas no se triangulan: lo que se ve en rojo con la tecla C es exactamente lo que choca.
        /// Incluye Piso3V10Colisiones (prueba de alineación al dar Play).
        /// </summary>
        internal static void ConstruirMuros(Datos d)
        {
            GameObject viejo = GameObject.Find(NombreMuros);
            if (viejo != null) Undo.DestroyObjectImmediate(viejo);
            SpriteRenderer fondo = FondoOCrear();
            if (fondo == null) return;

            GameObject go = new GameObject(NombreMuros);
            Undo.RegisterCreatedObjectUndo(go, "Muros sólidos");
            go.transform.SetParent(fondo.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            if (d.murosCajas != null && d.murosCajas.Length > 0)
            {
                for (int i = 0; i < d.murosCajas.Length; i++)
                {
                    Caja c = d.murosCajas[i];
                    Vector2 a = LocalFondo(fondo, d, c.x, c.y);
                    Vector2 b = LocalFondo(fondo, d, c.x + c.w, c.y + c.h);
                    GameObject m = new GameObject("Muro_" + i.ToString("000"));
                    m.transform.SetParent(go.transform, false);
                    m.isStatic = true;
                    BoxCollider2D bc = m.AddComponent<BoxCollider2D>();
                    bc.offset = (a + b) * 0.5f;
                    bc.size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                }
            }
            else
            {
                PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();
                poly.pathCount = d.solidos.Length;
                for (int i = 0; i < d.solidos.Length; i++)
                {
                    Punto[] p = d.solidos[i].puntos;
                    List<Vector2> pts = new List<Vector2>();
                    for (int k = 0; k < p.Length; k++)
                    {
                        Vector2 v = LocalFondo(fondo, d, p[k].x, p[k].y);
                        if (pts.Count == 0 || (pts[pts.Count - 1] - v).sqrMagnitude > 1e-8f) pts.Add(v);
                    }
                    if (pts.Count > 1 && (pts[0] - pts[pts.Count - 1]).sqrMagnitude <= 1e-8f) pts.RemoveAt(pts.Count - 1);
                    poly.SetPath(i, pts.ToArray());
                }
            }
            Piso3V10Colisiones col = go.AddComponent<Piso3V10Colisiones>();
            col.pruebaSuelo = Locales(fondo, d, d.pruebasSuelo);
            col.pruebaMuro = Locales(fondo, d, d.pruebasMuro);
            col.firma = FirmaMuros(fondo);
            go.isStatic = true;
        }

        /// <summary>Fondo, decals y sombras al fondo del orden de dibujo (V14). Devuelve cuántos cambió.</summary>
        internal static int ActualizarOrdenes(SpriteRenderer fondo)
        {
            int n = 0;
            if (fondo != null && fondo.sortingOrder != OrdenFondo) { Undo.RecordObject(fondo, "Orden"); fondo.sortingOrder = OrdenFondo; n++; }
            HashSet<string> decals = new HashSet<string> { "Papeles", "DocumentosRotos", "ManchaSangre", "SangreSeca" };
            foreach (SpriteRenderer sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr == fondo || sr.sprite == null) continue;
                int objetivo = int.MinValue;
                if (sr.gameObject.name == "Sombra") objetivo = OrdenSombra;
                else if (decals.Contains(sr.sprite.name) || (sr.transform.parent != null && sr.transform.parent.name == "03_Decals")) objetivo = OrdenDecal;
                if (objetivo != int.MinValue && sr.sortingOrder != objetivo) { Undo.RecordObject(sr, "Orden"); sr.sortingOrder = objetivo; n++; }
            }
            return n;
        }

        private static Vector2[] Locales(SpriteRenderer fondo, Datos d, Punto[] p)
        {
            if (p == null) return new Vector2[0];
            Vector2[] r = new Vector2[p.Length];
            for (int i = 0; i < p.Length; i++) r[i] = LocalFondo(fondo, d, p[i].x, p[i].y);
            return r;
        }

        /// <summary>¿Los muros actuales son los de cajas V16, cuelgan del fondo y fueron hechos con su importación actual?</summary>
        internal static bool MurosAlDia(GameObject muros, SpriteRenderer fondo)
        {
            if (muros == null || fondo == null) return false;
            Piso3V10Colisiones c = muros.GetComponent<Piso3V10Colisiones>();
            return c != null && muros.GetComponent<PolygonCollider2D>() == null && muros.GetComponent<CompositeCollider2D>() == null
                   && muros.GetComponent<EdgeCollider2D>() == null && muros.transform.parent == fondo.transform
                   && muros.transform.localPosition == Vector3.zero && muros.transform.localScale == Vector3.one
                   && muros.transform.childCount > 0 && c.firma == FirmaMuros(fondo);
        }

        private static int CerrarHuecos(List<BoxCollider2D> cajas, float huecoMinimo)
        {
            int cerrados = 0;
            ContactFilter2D filtro = new ContactFilter2D();
            filtro.useTriggers = false;
            RaycastHit2D[] hits = new RaycastHit2D[8];
            foreach (BoxCollider2D bc in cajas)
            {
                Bounds b = bc.bounds;
                Vector2 origen = new Vector2(b.center.x, b.max.y + 0.006f);
                int n = Physics2D.BoxCast(origen, new Vector2(b.size.x * 0.98f, 0.004f), 0f, Vector2.up, filtro, hits, huecoMinimo);
                float mejor = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (hits[i].collider == null || hits[i].collider == bc) continue;
                    if (hits[i].collider.gameObject.name != NombreMuros &&
                        (hits[i].collider.transform.parent == null || hits[i].collider.transform.parent.name != NombreMuros)) continue;
                    mejor = Mathf.Min(mejor, hits[i].distance);
                }
                if (mejor == float.MaxValue || mejor <= 0f) continue;
                Vector3 esc = bc.transform.lossyScale;
                float extra = (mejor + 0.006f) / Mathf.Max(0.0001f, Mathf.Abs(esc.y));
                Vector2 s = bc.size; Vector2 o = bc.offset;
                s.y += extra; o.y += extra * 0.5f;
                bc.size = s; bc.offset = o;
                cerrados++;
            }
            return cerrados;
        }

        // ================================================================== objetos y puertas
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
                    AjustarCollider(bc, o.anchoPx, o.altoPx, o.colAncho, o.colAlto);
                    GameObject sh = new GameObject("Sombra");
                    sh.transform.SetParent(go.transform, false);
                    SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
                    ssr.sprite = sombra;
                    ssr.sortingOrder = OrdenSombra;
                    AjustarSombra(sh.transform, o.anchoPx, 1f, o.colAncho);
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

        private static void AjustarCollider(BoxCollider2D bc, float anchoPx, float altoPx, float fracAncho, float fracAlto)
        {
            float w = anchoPx * fracAncho / PPU;
            float h = altoPx * fracAlto / PPU;
            bc.size = new Vector2(w, h);
            bc.offset = new Vector2(0f, h * 0.5f);
        }

        private static void AjustarSombra(Transform sh, float anchoPx, float escala, float fracAncho)
        {
            float anchoMundoPx = anchoPx * escala * fracAncho * 1.08f;
            float altoMundoPx = Mathf.Min(anchoMundoPx * 0.22f, 26f);
            float esc = Mathf.Max(escala, 0.01f);
            sh.localScale = new Vector3(anchoMundoPx / 96f / esc, altoMundoPx / 24f / esc, 1f);
            sh.localPosition = new Vector3(0f, altoMundoPx * 0.15f / PPU / esc, 0f);
        }

        // ================================================================== Jason
        internal static GameObject CrearJason(Vector2 pos, bool enEscena = true)
        {
            GameObject jason = new GameObject("05_Jason");
            jason.tag = "Player";
            jason.transform.position = pos;
            jason.AddComponent<SpriteRenderer>();
            jason.AddComponent<Rigidbody2D>();
            jason.AddComponent<CapsuleCollider2D>();
            jason.AddComponent<Piso3V10Movimiento>();
            PrepararJason(jason);
            if (enEscena)
            {
                Piso3V10Camara camara = UnityEngine.Object.FindFirstObjectByType<Piso3V10Camara>();
                if (camara != null) camara.objetivo = jason.transform;
                Piso3V10Depuracion dep = UnityEngine.Object.FindFirstObjectByType<Piso3V10Depuracion>();
                if (dep != null) dep.jugador = jason.GetComponent<Piso3V10Movimiento>();
            }
            return jason;
        }

        /// <summary>Jason: Kinematic con barrido, cápsula en los pies, orden por Y, animador 8 dir con TODAS sus animaciones y balas.</summary>
        internal static void PrepararJason(GameObject jason)
        {
            jason.transform.localScale = Vector3.one;
            Rigidbody2D rb = jason.GetComponent<Rigidbody2D>();
            if (rb == null) rb = jason.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D cap = jason.GetComponent<CapsuleCollider2D>();
            if (cap == null) cap = jason.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = new Vector2(0.44f, 0.16f);
            cap.offset = new Vector2(0f, 0.08f);
            if (jason.GetComponent<Piso3V10Movimiento>() == null) jason.AddComponent<Piso3V10Movimiento>();

            Piso3V10OrdenVisual ov = jason.GetComponent<Piso3V10OrdenVisual>();
            if (ov == null) ov = jason.AddComponent<Piso3V10OrdenVisual>();
            ov.dinamico = true;

            Piso3V10Personaje8Dir pa = jason.GetComponent<Piso3V10Personaje8Dir>();
            if (pa == null) pa = jason.AddComponent<Piso3V10Personaje8Dir>();
            pa.animaciones = CargarAnimacionesDe("Jason");
            pa.animacionInicial = "Quieto";
            pa.direccion = 0;

            Piso3V10JasonAnimacion ctrl = jason.GetComponent<Piso3V10JasonAnimacion>();
            if (ctrl == null) ctrl = jason.AddComponent<Piso3V10JasonAnimacion>();
            ctrl.spriteBala = AssetDatabase.LoadAssetAtPath<Sprite>(RutaBala);
            ctrl.spriteImpacto = AssetDatabase.LoadAssetAtPath<Sprite>(RutaImpacto);

            PrimerFotograma(jason, pa);
            AgregarSombra(jason, 44f, 12f);
            ov.Aplicar();
            EditorUtility.SetDirty(jason);
        }

        // ================================================================== Zorbos
        private static void CrearZorbos(Datos d)
        {
            if (d.zorbos == null) return;
            GameObject viejo = GameObject.Find("07_Zorbos");
            if (viejo != null) UnityEngine.Object.DestroyImmediate(viejo);
            GameObject raiz = new GameObject("07_Zorbos");
            for (int i = 0; i < d.zorbos.Length; i++)
            {
                ZorboDato zd = d.zorbos[i];
                GameObject z = CrearZorbo("Zorbo", PosDatos(d, zd.x, zd.y), zd.orientacion);
                z.name = "Zorbo_" + (i + 1).ToString("00");
                z.transform.SetParent(raiz.transform, true);
            }
        }

        internal static GameObject CrearZorbo(string tipo, Vector2 pos, int orientacion)
        {
            GameObject z = new GameObject(tipo);
            z.transform.position = pos;
            z.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            z.AddComponent<SpriteRenderer>();
            PrepararZorbo(z, tipo, orientacion);
            return z;
        }

        internal static void PrepararZorbo(GameObject z, string tipo, int orientacion)
        {
            SpriteRenderer sr = z.GetComponent<SpriteRenderer>();
            if (sr == null) z.AddComponent<SpriteRenderer>();
            Piso3V10Personaje8Dir pa = z.GetComponent<Piso3V10Personaje8Dir>();
            if (pa == null) pa = z.AddComponent<Piso3V10Personaje8Dir>();
            pa.animaciones = CargarAnimacionesDe(tipo);
            pa.animacionInicial = "Quieto";
            pa.direccion = Mathf.Clamp(orientacion, 0, 7);
            Piso3V10Zorbo zc = z.GetComponent<Piso3V10Zorbo>();
            if (zc == null) zc = z.AddComponent<Piso3V10Zorbo>();
            zc.orientacion = Mathf.Clamp(orientacion, 0, 7);
            Rigidbody2D rb = z.GetComponent<Rigidbody2D>();
            if (rb == null) rb = z.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            CapsuleCollider2D cz = z.GetComponent<CapsuleCollider2D>();
            if (cz == null) cz = z.AddComponent<CapsuleCollider2D>();
            cz.direction = CapsuleDirection2D.Horizontal;
            cz.size = new Vector2(0.48f, 0.18f);
            cz.offset = new Vector2(0f, 0.09f);
            Piso3V10OrdenVisual ov = z.GetComponent<Piso3V10OrdenVisual>();
            if (ov == null) ov = z.AddComponent<Piso3V10OrdenVisual>();
            ov.dinamico = true;
            PrimerFotograma(z, pa);
            AgregarSombra(z, 56f, 14f);
            ov.Aplicar();
            EditorUtility.SetDirty(z);
        }

        // ================================================================== Milena / Doctor Yiba / Arma
        private static GameObject CrearPersonajeSimple(string nombre, Vector2 capsula, float anchoSombra)
        {
            GameObject go = new GameObject(nombre);
            go.AddComponent<SpriteRenderer>();
            Piso3V10Personaje8Dir p = go.AddComponent<Piso3V10Personaje8Dir>();
            p.animaciones = CargarAnimacionesDe(nombre);
            p.animacionInicial = "Quieto";
            p.direccion = 0;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            CapsuleCollider2D cap = go.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = capsula;
            cap.offset = new Vector2(0f, capsula.y * 0.5f);
            go.AddComponent<Piso3V10OrdenVisual>().dinamico = true;
            PrimerFotograma(go, p);
            AgregarSombra(go, anchoSombra, anchoSombra * 0.28f);
            return go;
        }

        /// <summary>Arma: "Girar" (las 8 direcciones en bucle, para recogerla), "Apuntar" y "Disparar" (1 fotograma por dirección).</summary>
        private static GameObject CrearArma()
        {
            GameObject go = new GameObject("ArmaLaboratorio4");
            go.AddComponent<SpriteRenderer>();
            Piso3V10Personaje8Dir p = go.AddComponent<Piso3V10Personaje8Dir>();
            List<Piso3V10Animacion> lista = new List<Piso3V10Animacion>();
            Sprite[] todas = new Sprite[8];
            foreach (string modo in new[] { "Apuntar", "Disparar" })
            {
                Piso3V10Animacion a = new Piso3V10Animacion { nombre = modo, fps = 1f, bucle = true, direcciones = new Piso3V10FramesDireccion[8] };
                for (int k = 0; k < 8; k++)
                {
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaArmas + "/ArmaLaboratorio4/" + modo + "/Arma_" + modo + "_" + k + "_" + Direcciones[k] + ".png");
                    a.direcciones[k] = new Piso3V10FramesDireccion { frames = s != null ? new[] { s } : new Sprite[0] };
                    if (modo == "Apuntar") todas[k] = s;
                }
                lista.Add(a);
            }
            Piso3V10Animacion girar = new Piso3V10Animacion { nombre = "Girar", fps = 8f, bucle = true, direcciones = new Piso3V10FramesDireccion[8] };
            for (int k = 0; k < 8; k++) girar.direcciones[k] = new Piso3V10FramesDireccion { frames = k == 0 ? Array.FindAll(todas, s => s != null) : new Sprite[0] };
            lista.Insert(0, girar);
            p.animaciones = lista.ToArray();
            p.animacionInicial = "Girar";
            go.AddComponent<Piso3V10OrdenVisual>().dinamico = true;
            PrimerFotograma(go, p);
            AgregarSombra(go, 40f, 10f);
            return go;
        }

        private static void PrimerFotograma(GameObject go, Piso3V10Personaje8Dir p)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            foreach (Piso3V10Animacion a in p.animaciones)
            {
                if (a.nombre != p.animacionInicial) continue;
                Sprite[] f = Piso3V10Personaje8Dir.Frames(a, p.direccion);
                if (f != null && f.Length > 0) sr.sprite = f[0];
            }
        }

        /// <summary>Lee Art/Personajes/&lt;personaje&gt;/&lt;Animacion&gt;/&lt;d&gt;_&lt;Direccion&gt;/ y arma el animador.</summary>
        private static Piso3V10Animacion[] CargarAnimacionesDe(string personaje)
        {
            string carpeta = CarpetaPersonajes + "/" + personaje;
            List<Piso3V10Animacion> lista = new List<Piso3V10Animacion>();
            if (!Directory.Exists(carpeta)) { Debug.LogError("[Piso3 V14] Falta " + carpeta); return lista.ToArray(); }
            AnimLista info = CargarInfo();
            string[] dirsAnim = Directory.GetDirectories(carpeta);
            Array.Sort(dirsAnim, StringComparer.Ordinal);
            foreach (string dirAnim in dirsAnim)
            {
                string nombreAnim = Path.GetFileName(dirAnim);
                Piso3V10Animacion a = new Piso3V10Animacion();
                a.nombre = nombreAnim;
                AnimInfo ai = BuscarAnim(info, personaje + "/" + nombreAnim);
                a.fps = ai != null ? ai.fps : 10f;
                a.bucle = ai == null || ai.loop;
                a.direcciones = new Piso3V10FramesDireccion[8];
                int total = 0;
                for (int k = 0; k < 8; k++)
                {
                    a.direcciones[k] = new Piso3V10FramesDireccion();
                    a.direcciones[k].frames = CargarCarpeta(dirAnim + "/" + k + "_" + Direcciones[k]);
                    total += a.direcciones[k].frames.Length;
                }
                if (total > 0) lista.Add(a);
            }
            lista.Sort((x, y) =>
            {
                if (x.nombre == y.nombre) return 0;
                if (x.nombre == "Quieto") return -1;
                if (y.nombre == "Quieto") return 1;
                return string.CompareOrdinal(x.nombre, y.nombre);
            });
            return lista.ToArray();
        }

        // ================================================================== importación de sprites
        /// <summary>Configura los sprites de personajes solo si aún no lo están (PPU 33.33). Devuelve true si lo hizo.</summary>
        internal static bool ConfigurarSpritesSiHaceFalta()
        {
            string carpeta = CarpetaPersonajes + "/Jason/Quieto/0_Sur";
            string[] muestra = Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, "*.png") : new string[0];
            if (muestra.Length == 0) return false;
            TextureImporter ti = AssetImporter.GetAtPath(muestra[0].Replace('\\', '/')) as TextureImporter;
            if (ti != null && ti.textureType == TextureImporterType.Sprite && Mathf.Abs(ti.spritePixelsPerUnit - PPUPersonajes) <= 0.01f) return false;
            ConfigurarSprites();
            return true;
        }

        [MenuItem("Laboratorio Yiba/Piso 3 V10/7 - Solo reconfigurar sprites")]
        public static void ConfigurarSprites()
        {
            AnimLista info = CargarInfo();
            Dictionary<string, Vector2> pivotes = new Dictionary<string, Vector2>();
            if (info.pivotes != null) foreach (PivoteInfo p in info.pivotes) pivotes[p.ruta] = new Vector2(p.x, p.y);

            AssetDatabase.StartAssetEditing();
            try
            {
                ConfigurarTextura(RutaFondo, new Vector2(0f, 1f), 4096, PPU);
                ConfigurarTextura(RutaSombra, new Vector2(0.5f, 0.5f), 256, PPU);
                ConfigurarTextura(RutaBala, new Vector2(0.5f, 0.5f), 64, PPUPersonajes);
                ConfigurarTextura(RutaImpacto, new Vector2(0.5f, 0.5f), 64, PPUPersonajes);
                if (Directory.Exists(CarpetaCornisa))
                    foreach (string f in Directory.GetFiles(CarpetaCornisa, "*.png", SearchOption.TopDirectoryOnly))
                        ConfigurarTextura(f.Replace('\\', '/'), new Vector2(0f, 1f), 1024, PPU);
                foreach (string carpeta in new[] { CarpetaObjetos, CarpetaPuertas })
                    foreach (string f in Directory.GetFiles(carpeta, "*.png", SearchOption.TopDirectoryOnly))
                        ConfigurarTextura(f.Replace('\\', '/'), new Vector2(0.5f, 0f), 1024, PPU);

                // personajes: pivote en los pies por animación y dirección (Personajes_Animaciones.json)
                foreach (string personaje in Personajes)
                {
                    string carpeta = CarpetaPersonajes + "/" + personaje;
                    if (!Directory.Exists(carpeta)) continue;
                    foreach (string dirAnim in Directory.GetDirectories(carpeta))
                        foreach (string dirDir in Directory.GetDirectories(dirAnim))
                        {
                            string clave = personaje + "/" + Path.GetFileName(dirAnim) + "/" + Path.GetFileName(dirDir);
                            Vector2 piv = pivotes.ContainsKey(clave) ? pivotes[clave] : new Vector2(0.5f, 0.1f);
                            foreach (string f in Directory.GetFiles(dirDir, "*.png", SearchOption.TopDirectoryOnly))
                                ConfigurarTextura(f.Replace('\\', '/'), piv, 256, PPUPersonajes);
                        }
                }
                if (Directory.Exists(CarpetaArmas))
                    foreach (string f in Directory.GetFiles(CarpetaArmas, "*.png", SearchOption.AllDirectories))
                        ConfigurarTextura(f.Replace('\\', '/'), new Vector2(0.5f, 0.3f), 256, PPUPersonajes);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
        }

        private static void ConfigurarTextura(string ruta, Vector2 pivote, int tamMax, float ppu)
        {
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null)
            {
                Debug.LogWarning("[Piso3 V14] No se encontró la textura: " + ruta);
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

            TextureImporterSettings ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
            ajustes.spritePivot = pivote;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }

        // ================================================================== utilidades
        internal static Datos CargarDatos()
        {
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(RutaDatos);
            if (ta == null)
            {
                Debug.LogError("[Piso3 V14] No se encontró " + RutaDatos);
                return null;
            }
            Datos d = JsonUtility.FromJson<Datos>(ta.text);
            if (d == null || d.solidos == null || d.solidos.Length == 0 || d.objetos == null || d.puertas == null)
            {
                Debug.LogError("[Piso3 V14] El JSON de datos está incompleto o es de una versión anterior. Copia de nuevo la carpeta Data.");
                return null;
            }
            return d;
        }

        private static AnimLista CargarInfo()
        {
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(RutaAnimaciones);
            AnimLista l = ta != null ? JsonUtility.FromJson<AnimLista>(ta.text) : null;
            if (l == null) l = new AnimLista();
            if (l.animaciones == null) l.animaciones = new AnimInfo[0];
            if (l.pivotes == null) l.pivotes = new PivoteInfo[0];
            return l;
        }

        private static AnimInfo BuscarAnim(AnimLista l, string ruta)
        {
            foreach (AnimInfo a in l.animaciones) if (a.ruta == ruta) return a;
            return null;
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

        private static Sprite CargarSprite(string ruta)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (s == null) Debug.LogError("[Piso3 V14] No se pudo cargar el sprite: " + ruta + " (¿está importado como Sprite?)");
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
