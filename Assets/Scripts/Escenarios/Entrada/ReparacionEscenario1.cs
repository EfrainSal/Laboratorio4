using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Protege el Escenario 1 de herramientas de otras escenas (p. ej. las del Piso 3):
    /// - Si faltan las colisiones de las paredes (las borró otra herramienta), las vuelve a crear
    ///   desde Datos/escenario1_layout.json.
    /// - Quita los restos del Piso 3 que se hayan metido en esta escena (su fondo, sus muros, su cornisa).
    /// - Deja a Jason con SU controlador (ControladorJugador) y desactiva scripts "Piso3..." que le hayan
    ///   añadido, porque mover al personaje con dos scripts a la vez rompe las colisiones.
    /// Se ejecuta solo al dar Play (componente GuardianEscenario1) o con el menú
    /// Laboratorio 4 > Reparar Escenario 1 (escena abierta).
    /// </summary>
    public static class ReparacionEscenario1
    {
        [Serializable] class V2 { public float x, y; }
        [Serializable] class Caja { public V2 centro, tam; }
        [Serializable] class Layout { public Caja[] muros; }

        static readonly string[] RestosPiso3 = { "00_Fondo", "01_ColisionMuros", "90_MurosFrente (cornisa delante)", "90_MurosFrente" };

        /// <summary>Devuelve el informe. 'enEditor' = true usa DestroyImmediate; en Play desactiva.</summary>
        public static string Reparar(TextAsset layout, bool enEditor, List<UnityEngine.Object> modificados = null)
        {
            var informe = new List<string>();
            var L = layout != null ? JsonUtility.FromJson<Layout>(layout.text) : null;
            int esperadas = L != null && L.muros != null ? L.muros.Length : 0;

            // 1) contenedor de muros propio (o el de la v5, que se llamaba 01_ColisionMuros dentro de 00_Mapa)
            MurosEscenario1 muros = UnityEngine.Object.FindFirstObjectByType<MurosEscenario1>(FindObjectsInactive.Include);
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.name != "01_ColisionMuros") continue;
                bool mio = t.parent != null && (t.parent.name == "00_Mapa" || t.parent.name == "E1_Mapa") && t.GetComponent<EdgeCollider2D>() == null;
                if (mio && muros == null)
                {
                    t.name = "E1_ColisionMuros";
                    muros = t.gameObject.AddComponent<MurosEscenario1>();
                    Marcar(modificados, t.gameObject);
                    informe.Add("muros de la v5 renombrados a E1_ColisionMuros");
                }
                else if (!mio || muros != null && t.gameObject != muros.gameObject)
                {
                    Quitar(t.gameObject, enEditor, modificados);
                    informe.Add("quitado resto del Piso 3: " + t.name);
                }
            }
            // 2) otros restos del Piso 3 (fondo y cornisa), solo en la raíz o con su sprite
            foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr == null || sr.sprite == null || !sr.sprite.name.StartsWith("Piso3_V10_")) continue;
                informe.Add("quitado resto del Piso 3: " + sr.gameObject.name);
                Quitar(sr.gameObject, enEditor, modificados);
            }
            foreach (var go in RaicesConNombre(RestosPiso3))
            {
                if (go.GetComponent<MurosEscenario1>() != null) continue;
                informe.Add("quitado resto del Piso 3: " + go.name);
                Quitar(go, enEditor, modificados);
            }

            // 3) colisiones de las paredes
            if (muros == null)
            {
                var mapa = GameObject.Find("E1_Mapa") ?? GameObject.Find("00_Mapa");
                var go = new GameObject("E1_ColisionMuros");
                if (mapa != null) go.transform.SetParent(mapa.transform, false);
                muros = go.AddComponent<MurosEscenario1>();
                Marcar(modificados, go);
                informe.Add("creado E1_ColisionMuros (las paredes no tenían colisión)");
            }
            if (!muros.gameObject.activeSelf) { muros.gameObject.SetActive(true); informe.Add("paredes reactivadas"); }
            var cajas = muros.GetComponents<BoxCollider2D>();
            int activas = 0; foreach (var b in cajas) if (b.enabled && !b.isTrigger) activas++;
            if (L != null && activas != esperadas)
            {
                foreach (var b in cajas) UnityEngine.Object.DestroyImmediate(b);
                foreach (var m in L.muros)
                {
                    var b = muros.gameObject.AddComponent<BoxCollider2D>();
                    b.offset = new Vector2(m.centro.x, m.centro.y);
                    b.size = new Vector2(m.tam.x, m.tam.y);
                }
                muros.transform.position = Vector3.zero; muros.transform.rotation = Quaternion.identity; muros.transform.localScale = Vector3.one;
                if (muros.transform.parent != null) { muros.transform.parent.position = Vector3.zero; muros.transform.parent.localScale = Vector3.one; }
                Marcar(modificados, muros.gameObject);
                informe.Add("paredes reconstruidas: " + esperadas + " piezas (había " + activas + ")");
            }

            // 4) jugadores: su propio controlador, sin scripts del Piso 3
            foreach (var c in UnityEngine.Object.FindObjectsByType<ControladorJugador>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = c.gameObject;
                if (!c.enabled) { c.enabled = true; informe.Add("reactivado ControladorJugador en " + go.name); }
                foreach (var mb in go.GetComponents<MonoBehaviour>())
                {
                    if (mb == null || !mb.enabled) continue;
                    string n = mb.GetType().Name;
                    if (n.StartsWith("Piso3")) { mb.enabled = false; informe.Add("desactivado " + n + " en " + go.name); }
                }
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic) { rb.bodyType = RigidbodyType2D.Kinematic; informe.Add("Rigidbody2D Kinematic en " + go.name); }
                var cap = go.GetComponent<CapsuleCollider2D>();
                if (cap != null && (Mathf.Abs(cap.size.x - 0.34f) > 0.001f || Mathf.Abs(cap.size.y - 0.16f) > 0.001f))
                {
                    cap.direction = CapsuleDirection2D.Horizontal; cap.size = new Vector2(0.34f, 0.16f); cap.offset = new Vector2(0f, 0.08f);
                    informe.Add("cápsula de " + go.name + " restaurada (0.34 x 0.16)");
                }
                Marcar(modificados, go);
            }

            int final = 0; foreach (var b in muros.GetComponents<BoxCollider2D>()) if (b.enabled && !b.isTrigger) final++;
            string res = "[Escenario 1] Paredes con colisión: " + final + "/" + esperadas + " piezas." +
                         (informe.Count > 0 ? " Arreglado: " + string.Join("; ", informe) + "." : " Todo en orden.");
            return res;
        }

        static IEnumerable<GameObject> RaicesConNombre(string[] nombres)
        {
            var lista = new List<GameObject>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.parent == null && Array.IndexOf(nombres, t.name) >= 0) lista.Add(t.gameObject);
            return lista;
        }

        static void Quitar(GameObject go, bool enEditor, List<UnityEngine.Object> modificados)
        {
            if (go == null) return;
            if (enEditor) UnityEngine.Object.DestroyImmediate(go); else go.SetActive(false);
        }

        static void Marcar(List<UnityEngine.Object> lista, UnityEngine.Object o) { if (lista != null && o != null) lista.Add(o); }
    }
}
