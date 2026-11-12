using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario4.EditorTools
{
    /// <summary>
    /// Menú: Laboratorio 4 > Escenario 4 (Jefe) > ...
    ///  - Construir escena: crea Scenes/Escenario4_Jefe.unity con fondo, muros, cornisa, objetos, puertas,
    ///    Jason (jugable), Milena (compañera) y el Doctor Yiba (jefe).
    ///  - Verificar archivos: revisa que estén todos los sprites del JSON.
    /// Todo lo que crea lleva el prefijo E4_ y vive en Assets/Laboratorio4_Escenario4: no toca otros escenarios.
    /// </summary>
    [InitializeOnLoad]
    public static class E4Constructor
    {
        private const string Raiz = "Assets/Laboratorio4_Escenario4";
        private const string RutaDatos = Raiz + "/Data/E4_Datos.json";
        private const string CarpetaEscenas = Raiz + "/Scenes";
        private const string RutaEscena = CarpetaEscenas + "/Escenario4_Jefe.unity";
        private const int OrdenFondo = -32000;
        private const int OrdenSombras = -31000;

        static E4Constructor()
        {
            EditorApplication.delayCall += AlCargar;
            EditorSceneManager.sceneOpened += (escena, modo) => ActualizarSiHaceFalta(escena, "al abrir la escena");
            EditorApplication.playModeStateChanged += estado =>
            {
                if (estado == PlayModeStateChange.ExitingEditMode)
                    ActualizarSiHaceFalta(SceneManager.GetActiveScene(), "antes de dar Play");
            };
        }

        private static int VersionDatos()
        {
            if (!File.Exists(RutaDatos)) return -1;
            E4Datos d = JsonUtility.FromJson<E4Datos>(File.ReadAllText(RutaDatos));
            return d != null ? d.version : -1;
        }

        /// <summary>Es una escena del Escenario 4 si tiene E4_Sistema o E4_Mapa en la raíz (aunque la hayas movido o copiado).</summary>
        private static bool EsEscenaE4(Scene escena)
        {
            if (!escena.IsValid() || !escena.isLoaded) return false;
            foreach (GameObject g in escena.GetRootGameObjects())
                if (g.name == "E4_Sistema" || g.name == "E4_Mapa") return true;
            return false;
        }

        private static int VersionDe(Scene escena)
        {
            foreach (GameObject g in escena.GetRootGameObjects())
            {
                E4Version v = g.GetComponent<E4Version>();
                if (v != null) return v.version;
            }
            return 0;
        }

        /// <summary>Si la escena abierta es del Escenario 4 y es de un paquete anterior, la reconstruye en su sitio y la guarda.</summary>
        private static void ActualizarSiHaceFalta(Scene escena, string cuando)
        {
            if (!EsEscenaE4(escena)) return;
            int vd = VersionDatos();
            if (vd <= 0 || VersionDe(escena) >= vd) return;
            E4Datos d = Cargar();
            if (d == null) return;
            ReconstruirEn(escena, d);
            Debug.Log("[Escenario 4] La escena '" + escena.name + "' era de una versión anterior y se actualizó sola " + cuando +
                      " (versión " + vd + "): muros de cobertura, cornisa, colisiones y objetos.");
        }

        private static void AlCargar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RutaDatos)) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) ActualizarSiHaceFalta(SceneManager.GetSceneAt(i), "al importar el paquete");
            if (File.Exists(RutaEscena)) return;
            string clave = "Laboratorio4_E4Jefe_Preguntado_" + Application.dataPath.GetHashCode();
            if (EditorPrefs.GetBool(clave, false)) return;
            EditorPrefs.SetBool(clave, true);
            if (EditorUtility.DisplayDialog("Escenario 4 · Doctor Yiba",
                    "¿Construir la escena del jefe final y abrirla ahora?\n\nTambién puedes hacerlo luego en:\nLaboratorio 4 > Escenario 4 (Jefe) > Construir escena",
                    "Construir", "Más tarde"))
                Construir();
        }

        private static E4Datos Cargar()
        {
            if (!File.Exists(RutaDatos))
            {
                EditorUtility.DisplayDialog("Escenario 4", "No se encontró " + RutaDatos, "OK");
                return null;
            }
            return JsonUtility.FromJson<E4Datos>(File.ReadAllText(RutaDatos));
        }

        private static readonly List<string> faltan = new List<string>();

        private static Sprite S(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return null;
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (s == null && !faltan.Contains(ruta)) faltan.Add(ruta);
            return s;
        }

        // =====================================================================
        [MenuItem("Laboratorio 4/Escenario 4 (Jefe)/Verificar archivos")]
        public static void Verificar()
        {
            E4Datos d = Cargar();
            if (d == null) return;
            faltan.Clear();
            S(d.fondo);
            foreach (E4CornisaDato c in d.cornisas) S(c.sprite);
            foreach (E4ObjetoDato o in d.objetos) S(o.sprite);
            S(d.puertaEntrada.sprite); S(d.puertaSalida.sprite);
            S(d.spriteSombra); S(d.spriteBala); S(d.spriteImpacto); S(d.spriteBola); S(d.spriteImpactoBola);
            S(d.spriteEscudo); S(d.spriteLuzRoja); S(d.spriteLuzVerde);
            int frames = 0;
            foreach (E4AnimDato a in d.animaciones)
                foreach (E4DirDato dd in a.dirs)
                    foreach (string f in dd.frames) { S(f); frames++; }
            if (faltan.Count == 0)
                Debug.Log("[Escenario 4] Archivos completos: fondo, " + d.cornisas.Length + " tramos de cornisa, " + d.muros.Length +
                          " piezas de muro, " + d.objetos.Length + " objetos, 2 puertas y " + frames + " fotogramas de personajes.");
            else
                Debug.LogError("[Escenario 4] Faltan " + faltan.Count + " archivos. Primero: " + faltan[0]);
        }

        // =====================================================================
        [MenuItem("Laboratorio 4/Escenario 4 (Jefe)/Construir escena")]
        public static void Construir()
        {
            E4Datos d = Cargar();
            if (d == null) return;
            Scene activa = SceneManager.GetActiveScene();
            if (EsEscenaE4(activa))
            {
                ReconstruirEn(activa, d);   // actualiza la escena que tienes abierta, esté donde esté
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Poblar(d);
            if (!AssetDatabase.IsValidFolder(CarpetaEscenas)) AssetDatabase.CreateFolder(Raiz, "Scenes");
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AgregarABuild(RutaEscena);
            Informe(d, RutaEscena);
        }

        /// <summary>Borra lo que creó el constructor (raíces E4_ y la cámara E4) y lo vuelve a crear con los datos actuales.</summary>
        private static void ReconstruirEn(Scene escena, E4Datos d)
        {
            SceneManager.SetActiveScene(escena);
            foreach (GameObject g in escena.GetRootGameObjects())
                if (g.name.StartsWith("E4_") || g.GetComponent<E4Camara>() != null) UnityEngine.Object.DestroyImmediate(g);
            Poblar(d);
            EditorSceneManager.MarkSceneDirty(escena);
            if (!string.IsNullOrEmpty(escena.path))
            {
                EditorSceneManager.SaveScene(escena);
                AgregarABuild(escena.path);
            }
            Informe(d, string.IsNullOrEmpty(escena.path) ? escena.name : escena.path);
        }

        private static void Informe(E4Datos d, string ruta)
        {
            if (faltan.Count > 0)
                Debug.LogError("[Escenario 4] Escena creada, pero faltan " + faltan.Count + " sprites. Primero: " + faltan[0] + "  (usa Verificar archivos)");
            else
                Debug.Log("[Escenario 4] Escena lista (versión " + d.version + "): " + ruta + " · Paredes con colisión: " + d.muros.Length + "/" + d.muros.Length +
                          " piezas · 4 muros de cobertura · " + d.objetos.Length + " objetos · Jason, Milena y Doctor Yiba.");
        }

        /// <summary>Crea todo el escenario en la escena activa.</summary>
        private static void Poblar(E4Datos d)
        {
            faltan.Clear();

            // ---------------- mapa
            GameObject mapa = new GameObject("E4_Mapa");
            SpriteRenderer fondo = Hijo("E4_Fondo", mapa.transform, Vector2.zero).AddComponent<SpriteRenderer>();
            fondo.sprite = S(d.fondo);
            fondo.sortingOrder = OrdenFondo;

            GameObject muros = new GameObject("E4_ColisionMuros");
            muros.isStatic = true;
            foreach (E4Rect r in d.muros)
            {
                BoxCollider2D b = muros.AddComponent<BoxCollider2D>();
                b.offset = new Vector2(r.x, r.y);
                b.size = new Vector2(r.w, r.h);
            }
            muros.AddComponent<E4Muros>().piezas = d.muros.Length;

            GameObject cornisa = new GameObject("E4_CornisaYMurosCobertura");
            for (int i = 0; i < d.cornisas.Length; i++)
            {
                E4CornisaDato c = d.cornisas[i];
                SpriteRenderer sr = Hijo("E4_Cornisa_" + (i + 1).ToString("00"), cornisa.transform, new Vector2(c.x, c.y)).AddComponent<SpriteRenderer>();
                sr.sprite = S(c.sprite);
                sr.sortingOrder = c.orden;   // misma escala que -y*100: tapa a quien está detrás (más arriba) del muro
                if (c.sprite.Contains("E4_Columna")) { sr.gameObject.name = "E4_MuroCobertura_" + (i + 1).ToString("00"); sr.gameObject.AddComponent<E4Columna>(); }
            }

            // ---------------- objetos
            Sprite sombra = S(d.spriteSombra);
            GameObject objetos = new GameObject("E4_Objetos");
            foreach (E4ObjetoDato o in d.objetos) CrearObjeto(o, objetos.transform, sombra);

            // ---------------- puertas
            GameObject puertas = new GameObject("E4_Puertas");
            CrearObjeto(d.puertaEntrada, puertas.transform, null);
            GameObject salidaGO = CrearObjeto(d.puertaSalida, puertas.transform, null);
            E4PuertaSalida puerta = salidaGO.AddComponent<E4PuertaSalida>();
            Rect zs = R(d.zonaSalida);
            puerta.zonaSalida = zs;
            puerta.luzRoja = S(d.spriteLuzRoja);
            puerta.luzVerde = S(d.spriteLuzVerde);
            GameObject luz = new GameObject("E4_LuzPuerta");
            luz.transform.SetParent(salidaGO.transform, false);
            luz.transform.position = new Vector3(d.luzSalida.x, d.luzSalida.y, 0f);
            puerta.luz = luz.AddComponent<SpriteRenderer>();
            puerta.luz.sprite = puerta.luzRoja;
            puerta.luz.sortingOrder = Mathf.RoundToInt(-d.puertaSalida.y * 100f) + 2;

            // ---------------- personajes
            GameObject personajes = new GameObject("E4_Personajes");
            Sprite bala = S(d.spriteBala), impacto = S(d.spriteImpacto);

            GameObject jason = CrearPersonaje("E4_Jason", "Jason", d, personajes.transform, new Vector2(d.jason.x, d.jason.y),
                new Vector2(0.34f, 0.16f), new Vector2(0.5f, 0.4f), 0.55f, sombra, true);
            jason.tag = "Player";
            E4Vida vJason = jason.GetComponent<E4Vida>();
            vJason.bando = E4Bando.Jugadores; vJason.maximo = 10; vJason.actual = 10;
            E4Jugador jug = jason.AddComponent<E4Jugador>();
            jug.spriteBala = bala; jug.spriteImpacto = impacto;
            GameObject esc = new GameObject("E4_Escudo");
            esc.transform.SetParent(jason.transform, false);
            esc.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            SpriteRenderer escSr = esc.AddComponent<SpriteRenderer>();
            escSr.sprite = S(d.spriteEscudo);
            E4OrdenY escOrden = esc.AddComponent<E4OrdenY>();
            escOrden.referencia = jason.transform; escOrden.desfase = 1;
            escSr.enabled = false;
            jug.escudoVisual = escSr;

            GameObject milena = CrearPersonaje("E4_Milena", "Milena", d, personajes.transform, new Vector2(d.milena.x, d.milena.y),
                new Vector2(0.34f, 0.16f), new Vector2(0.5f, 0.4f), 0.55f, sombra, true);
            E4Vida vMilena = milena.GetComponent<E4Vida>();
            vMilena.bando = E4Bando.Jugadores; vMilena.maximo = 8; vMilena.actual = 8;
            E4Companera comp = milena.AddComponent<E4Companera>();
            comp.lider = jason.transform; comp.spriteBala = bala; comp.spriteImpacto = impacto;

            GameObject doctor = CrearPersonaje("E4_DoctorYiba", "DoctorYiba", d, personajes.transform, new Vector2(d.doctor.x, d.doctor.y),
                new Vector2(0.5f, 0.22f), new Vector2(1.0f, 0.6f), 0.9f, sombra, false);
            E4Vida vDoc = doctor.GetComponent<E4Vida>();
            vDoc.bando = E4Bando.Jefe; vDoc.maximo = 80; vDoc.actual = 80;
            E4Jefe jefe = doctor.AddComponent<E4Jefe>();
            jefe.zona = R(d.zonaJefe);
            jefe.objetivos = new Transform[] { jason.transform, milena.transform };
            jefe.spriteBola = S(d.spriteBola);
            jefe.spriteImpactoBola = S(d.spriteImpactoBola);
            jefe.puerta = puerta;
            doctor.GetComponent<E4Anim>().direccion = 0;

            comp.jefe = jefe;
            jug.objetivoAsistido = doctor.transform;
            puerta.jugador = jason.transform;

            // ---------------- cámara y sistema
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.027f, 0.035f, 1f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f;
            camGO.transform.position = new Vector3(d.jason.x, d.jason.y, -10f);
            camGO.AddComponent<AudioListener>();
            E4Camara ec = camGO.AddComponent<E4Camara>();
            ec.objetivo = jason.transform;
            ec.limites = new Rect(0f, -d.alto, d.ancho, d.alto);

            GameObject sistema = new GameObject("E4_Sistema");
            E4HUD hud = sistema.AddComponent<E4HUD>();
            hud.jefe = jefe; hud.jason = jug; hud.vidaJason = vJason; hud.vidaMilena = vMilena; hud.puerta = puerta;
            sistema.AddComponent<E4Depuracion>();

            sistema.AddComponent<E4Version>().version = d.version;
        }

        // =====================================================================
        private static Rect R(E4Rect r) { return new Rect(r.x - r.w * 0.5f, r.y - r.h * 0.5f, r.w, r.h); }

        private static GameObject Hijo(string nombre, Transform padre, Vector2 pos)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            return go;
        }

        private static void Sombra(Transform padre, Sprite sombra, float ancho)
        {
            if (sombra == null) return;
            GameObject s = new GameObject("Sombra");
            s.transform.SetParent(padre, false);
            float w = sombra.bounds.size.x;
            float k = w > 0f ? ancho / w : 1f;
            s.transform.localScale = new Vector3(k, k, 1f);
            // el centro de la sombra queda en los pies, sea cual sea el pivote del sprite
            s.transform.localPosition = new Vector3(-sombra.bounds.center.x * k, -sombra.bounds.center.y * k, 0f);
            SpriteRenderer sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = sombra;
            sr.sortingOrder = OrdenSombras;
        }

        private static GameObject CrearObjeto(E4ObjetoDato o, Transform padre, Sprite sombra)
        {
            GameObject go = Hijo(o.nombre, padre, new Vector2(o.x, o.y));
            go.isStatic = true;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(o.sprite);
            E4OrdenY orden = go.AddComponent<E4OrdenY>();
            orden.dinamico = false;
            orden.Aplicar();
            if (o.colision)
            {
                BoxCollider2D b = go.AddComponent<BoxCollider2D>();   // solo la base: se puede pasar por detrás
                b.size = new Vector2(o.colW, o.colH);
                b.offset = new Vector2(0f, o.colY + o.colH * 0.5f);
            }
            if (o.sombra && sr.sprite != null) Sombra(go.transform, sombra, sr.sprite.bounds.size.x * 0.95f);
            return go;
        }

        private static GameObject CrearPersonaje(string nombre, string personaje, E4Datos d, Transform padre, Vector2 pos,
            Vector2 capsula, Vector2 zonaDanio, float anchoSombra, Sprite sombra, bool dinamico)
        {
            GameObject go = Hijo(nombre, padre, pos);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            E4Anim anim = go.AddComponent<E4Anim>();
            List<E4Animacion> lista = new List<E4Animacion>();
            foreach (E4AnimDato a in d.animaciones)
            {
                if (a.personaje != personaje) continue;
                E4Animacion an = new E4Animacion { nombre = a.nombre, fps = a.fps, bucle = a.bucle, dirs = new E4FramesDir[8] };
                for (int k = 0; k < 8; k++) an.dirs[k] = new E4FramesDir();
                foreach (E4DirDato dd in a.dirs)
                {
                    if (dd.d < 0 || dd.d > 7) continue;
                    List<Sprite> fs = new List<Sprite>();
                    foreach (string f in dd.frames) { Sprite s = S(f); if (s != null) fs.Add(s); }
                    an.dirs[dd.d].frames = fs.ToArray();
                }
                lista.Add(an);
            }
            anim.animaciones = lista.ToArray();
            anim.inicial = "Quieto";
            anim.direccion = personaje == "DoctorYiba" ? 0 : 2;
            E4Animacion quieto = anim.Buscar("Quieto");
            if (quieto != null && quieto.dirs[anim.direccion].frames.Length > 0) sr.sprite = quieto.dirs[anim.direccion].frames[0];

            E4OrdenY orden = go.AddComponent<E4OrdenY>();
            orden.dinamico = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = dinamico ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            if (dinamico) rb.mass = 1f;

            CapsuleCollider2D cap = go.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = capsula;
            cap.offset = new Vector2(0f, capsula.y * 0.5f);

            E4Vida vida = go.AddComponent<E4Vida>();

            GameObject hb = new GameObject("E4_ZonaDanio");
            hb.transform.SetParent(go.transform, false);
            BoxCollider2D hbc = hb.AddComponent<BoxCollider2D>();
            hbc.isTrigger = true;
            hbc.size = zonaDanio;
            hbc.offset = new Vector2(0f, zonaDanio.y * 0.4f);
            hb.AddComponent<E4Hurtbox>().vida = vida;

            Sombra(go.transform, sombra, anchoSombra);
            return go;
        }

        private static void Silueta(GameObject personaje)
        {
            GameObject s = new GameObject("E4_Silueta");
            s.transform.SetParent(personaje.transform, false);
            SpriteRenderer sr = s.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 31000;
            sr.enabled = false;
            s.AddComponent<E4Silueta>().origen = personaje.GetComponent<SpriteRenderer>();
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
