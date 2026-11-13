#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Laboratorio4.EditorTools
{
    /// <summary>
    /// Respaldo: reconstruye la escena del Escenario 1 desde Datos/escenario1_layout.json usando los
    /// PREFABS de objetos del proyecto (por identificador y, si no aparece, por nombre).
    /// Menú: Laboratorio 4 > Reconstruir Escenario 1 (Entrada)
    /// </summary>
    public static class ConstruirEscenario1
    {
        [Serializable] class V2 { public float x, y; public Vector2 V => new Vector2(x, y); }
        [Serializable] class Pieza { public string sprite; public V2 pos; }
        [Serializable] class Obj { public string nombre, prefab, prefabGuid, sprite; public V2 pos, tam, huellaCentro, huellaTam; public bool plano, propio, colision; }
        [Serializable] class Puerta { public string nombre, orientacion; public V2 pos, tam; public int aberturaPx; }
        [Serializable] class Salida { public string prefabGuid; public float escala; public V2 pos, triggerCentro, triggerTam; }
        [Serializable] class Caja { public V2 centro, tam; }
        [Serializable] class Sorbo { public string variante, tipo; public int orientacion; public V2 pos; }
        [Serializable] class Anim { public string clave, dir; public string[] sprites; }
        [Serializable] class Layout
        {
            public float anchoUnidades, altoUnidades;
            public Pieza[] fondo, frente; public Obj[] objetos; public Puerta[] puertas; public Salida salida;
            public Caja[] muros; public Sorbo[] sorbos; public Anim[] animaciones; public V2 jason, milena;
            public string sombra, bala, impacto;
        }
        static readonly string[] Dirs = { "Norte", "Noreste", "Este", "Sureste", "Sur", "Suroeste", "Oeste", "Noroeste" };
        const int OrdenFondo = -32000, OrdenPlano = -2000, OrdenSombra = -1500, OrdenFrente = 20000;
        static string root;
        static Layout L;

        [MenuItem("Laboratorio 4/Reconstruir Escenario 1 (Entrada)")]
        public static void Construir()
        {
            string guid = AssetDatabase.FindAssets("escenario1_layout t:TextAsset").FirstOrDefault();
            if (guid == null) { Debug.LogError("No se encontró escenario1_layout.json"); return; }
            string jsonPath = AssetDatabase.GUIDToAssetPath(guid);
            root = "Assets";
            L = JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath).text);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Sprite sombra = S(L.sombra);

            var mapa = new GameObject("E1_Mapa").transform;
            foreach (var p in L.fondo) Render("E1_Fondo", S(p.sprite), p.pos.V, OrdenFondo, mapa);
            foreach (var p in L.frente) Render("E1_Cornisa_Delante", S(p.sprite), p.pos.V, OrdenFrente, mapa);
            var muros = new GameObject("E1_ColisionMuros"); muros.transform.SetParent(mapa); muros.isStatic = true; muros.AddComponent<MurosEscenario1>();
            foreach (var r in L.muros) { var b = muros.AddComponent<BoxCollider2D>(); b.offset = r.centro.V; b.size = r.tam.V; }

            var puertas = new GameObject("E1_Puertas").transform;
            foreach (var p in L.puertas)
            {
                var go = new GameObject(p.nombre); go.transform.SetParent(puertas); go.transform.position = p.pos.V;
                var i = go.AddComponent<PuertaInfo>(); i.orientacion = p.orientacion; i.aberturaPx = p.aberturaPx; i.tamano = p.tam.V;
            }
            var sal = Instanciar(L.salida.prefabGuid, "Puerta_Salida", puertas);
            if (sal != null) { sal.name = "Puerta_Salida_Piso2"; sal.transform.position = L.salida.pos.V; sal.transform.localScale = Vector3.one * L.salida.escala; }
            var trig = new GameObject("Trigger_Salida_Piso2"); trig.transform.SetParent(puertas); trig.transform.position = L.salida.triggerCentro.V;
            var tb = trig.AddComponent<BoxCollider2D>(); tb.isTrigger = true; tb.size = L.salida.triggerTam.V;
            trig.AddComponent<PuertaSalida>();

            var objetos = new GameObject("E1_Objetos").transform;
            int faltan = 0;
            foreach (var o in L.objetos)
            {
                GameObject go;
                if (o.propio)
                {
                    go = Render(o.nombre, S(o.sprite), o.pos.V, o.plano ? OrdenPlano : Orden(o.pos.y), objetos);
                    if (!o.plano) { go.AddComponent<OrdenPorY>(); Sombra(go, sombra, o.tam.x * 0.95f); }
                    if (o.colision) { var b = go.AddComponent<BoxCollider2D>(); b.offset = o.huellaCentro.V; b.size = o.huellaTam.V; }
                    continue;
                }
                go = Instanciar(o.prefabGuid, o.prefab, objetos);
                if (go == null) { faltan++; continue; }
                go.name = o.nombre;
                go.transform.position = o.pos.V;
                var box = go.GetComponent<BoxCollider2D>();
                if (box != null && o.colision) { box.offset = o.huellaCentro.V; box.size = o.huellaTam.V; }
            }

            var enemigos = new GameObject("E1_Sorbos").transform;
            int n = 1;
            foreach (var s in L.sorbos)
            {
                bool a = s.tipo == "A";
                var go = Render($"{(a ? "Sorbo" : "SorboAcorazado")}_{n++:00}", null, s.pos.V, Orden(s.pos.y), enemigos);
                go.AddComponent<OrdenPorY>().dinamico = true;
                var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0;
                var c = go.AddComponent<CapsuleCollider2D>(); c.direction = CapsuleDirection2D.Horizontal;
                c.size = a ? new Vector2(0.42f, 0.18f) : new Vector2(0.5f, 0.2f); c.offset = new Vector2(0, c.size.y * 0.5f);
                var se = go.AddComponent<SorboEnemigo>();
                se.variante = s.variante; se.orientacion = s.orientacion;
                string k = a ? "SorboA" : "SorboB";
                if (a) se.quieto8 = Dirs.Select(d => A(k + "/Quieto", d).FirstOrDefault()).ToArray();
                else se.quieto4 = new[] { "Norte", "Este", "Sur", "Oeste" }.Select(d => A(k + "/Correr", d).FirstOrDefault()).ToArray();
                se.ataqueNorte = A(k + "/Ataque", "Norte"); se.ataqueEste = A(k + "/Ataque", "Este"); se.ataqueSur = A(k + "/Ataque", "Sur"); se.ataqueOeste = A(k + "/Ataque", "Oeste");
                se.muerteNorte = A(k + "/Muerte", "Norte"); se.muerteEste = A(k + "/Muerte", "Este"); se.muerteSur = A(k + "/Muerte", "Sur"); se.muerteOeste = A(k + "/Muerte", "Oeste");
                Sombra(go, sombra, a ? 0.62f : 0.72f);
            }

            var pj = new GameObject("E1_Personajes").transform;
            var jason = Jugador("JasonSmith", "Jason Smith", L.jason.V, pj, "Jason", sombra);
            var cj = jason.GetComponent<ControladorJugador>();
            cj.caminarNorte = A("Jason/Caminar", "Norte"); cj.caminarEste = A("Jason/Caminar", "Este");
            cj.caminarOeste = A("Jason/Caminar", "Oeste"); cj.caminarSur = A("Jason/Caminar", "Sur");
            cj.disparoFrente = A("Jason/Disparo", "Sur"); cj.disparoDerecha = A("Jason/Disparo", "Este"); cj.disparoIzquierda = A("Jason/Disparo", "Oeste");
            cj.escudoNorte = A("Jason/Escudo", "Norte"); cj.escudoEste = A("Jason/Escudo", "Este"); cj.escudoSur = A("Jason/Escudo", "Sur"); cj.escudoOeste = A("Jason/Escudo", "Oeste");
            cj.muerteNorte = A("Jason/Muerte", "Norte"); cj.muerteEste = A("Jason/Muerte", "Este"); cj.muerteSur = A("Jason/Muerte", "Sur"); cj.muerteOeste = A("Jason/Muerte", "Oeste");
            cj.spriteBala = S(L.bala); cj.spriteImpacto = S(L.impacto);
            var milena = Jugador("MilenaRhea", "Milena Rhea", L.milena.V, pj, "Milena", sombra);

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.027f, 0.035f);
            camGo.transform.position = new Vector3(L.jason.x, L.jason.y, -10);
            camGo.AddComponent<AudioListener>();
            var seg = camGo.AddComponent<CamaraSeguimiento>(); seg.limites = new Rect(0, -L.altoUnidades, L.anchoUnidades, L.altoUnidades); seg.objetivo = jason.transform;

            var sel = new GameObject("SeleccionPersonaje").AddComponent<SeleccionPersonaje>();
            sel.jasonSmith = jason; sel.milenaRhea = milena; sel.camara = seg;
            sel.gameObject.AddComponent<Depuracion>();
            sel.gameObject.AddComponent<GuardianEscenario1>().layout = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            milena.SetActive(false);

            EditorSceneManager.SaveScene(scene, root + "/Scenes/Escenario1_Entrada.unity");
            Debug.Log("[Laboratorio 4] Escena del Escenario 1 reconstruida." + (faltan > 0 ? " Faltaron " + faltan + " prefabs (revisa la Console)." : ""));
        }

        static Sprite S(string rel) => string.IsNullOrEmpty(rel) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(root + "/" + rel);

        static Sprite[] A(string clave, string dir)
        {
            var a = L.animaciones.FirstOrDefault(x => x.clave == clave && x.dir == dir);
            return a == null ? new Sprite[0] : a.sprites.Select(S).Where(s => s != null).ToArray();
        }

        static GameObject Instanciar(string guid, string nombre, Transform padre)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(ruta) || AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
                ruta = AssetDatabase.FindAssets(nombre + " t:Prefab").Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == nombre);
            var pf = ruta == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (pf == null) { Debug.LogWarning("[Laboratorio 4] No se encontró el prefab " + nombre); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, padre);
            return go;
        }

        static int Orden(float y) => Mathf.RoundToInt(-y * 100f);

        static GameObject Render(string nombre, Sprite sp, Vector2 pos, int orden, Transform padre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = orden;
            return go;
        }

        static void Sombra(GameObject padre, Sprite sombra, float ancho)
        {
            if (sombra == null) return;
            var s = new GameObject("Sombra"); s.transform.SetParent(padre.transform, false);
            var sr = s.AddComponent<SpriteRenderer>(); sr.sprite = sombra; sr.sortingOrder = OrdenSombra;
            s.transform.localScale = new Vector3(ancho / 1.28f, ancho * 0.3f / 0.4f, 1f);
        }

        static GameObject Jugador(string nombre, string visible, Vector2 pos, Transform padre, string clave, Sprite sombra)
        {
            var go = Render(nombre, null, pos, Orden(pos.y), padre);
            go.tag = "Player";
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var cap = go.AddComponent<CapsuleCollider2D>(); cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = new Vector2(0.34f, 0.16f); cap.offset = new Vector2(0, 0.08f);
            go.AddComponent<OrdenPorY>().dinamico = true;
            var c = go.AddComponent<ControladorJugador>();
            c.nombrePersonaje = visible; c.render = go.GetComponent<SpriteRenderer>();
            c.spritesDireccion = Dirs.Select(d => A(clave + "/Quieto", d).FirstOrDefault()).ToArray();
            c.render.sprite = c.spritesDireccion[2];
            Sombra(go, sombra, 0.5f);
            return go;
        }
    }
}
#endif
