using System.Collections.Generic;
using UnityEngine;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Al dar Play revisa el Escenario 3 antes que nada:
    ///  - que existan todas las colisiones de los muros (si faltan, las vuelve a crear desde E3_Nivel.json);
    ///  - que Jason solo use su propio movimiento (desactiva scripts ajenos como PlayerMovement o los del Piso 3);
    ///  - prepara la rejilla de rutas de los Sorbos.
    /// En la Consola escribe "[Escenario 3] Paredes con colisión: N/N piezas".
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class E3Guardian : MonoBehaviour
    {
        public TextAsset datos;
        public Transform raiz;

        private E3NivelDatos d;

        private void Awake()
        {
            List<string> informe = new List<string>();
            if (datos != null) d = JsonUtility.FromJson<E3NivelDatos>(datos.text);
            if (raiz == null) raiz = transform.parent;

            // --- muros
            E3Muros muros = raiz != null ? raiz.GetComponentInChildren<E3Muros>(true) : FindAnyObjectByType<E3Muros>(FindObjectsInactive.Include);
            int esperadas = d != null && d.muros != null ? d.muros.Length : (muros != null ? muros.esperadas : 0);
            if (muros == null && raiz != null)
            {
                GameObject go = new GameObject("E3_ColisionMuros");
                go.transform.SetParent(raiz, false);
                muros = go.AddComponent<E3Muros>();
                informe.Add("faltaba E3_ColisionMuros: creado");
            }
            if (muros != null)
            {
                if (!muros.gameObject.activeSelf) { muros.gameObject.SetActive(true); informe.Add("E3_ColisionMuros estaba desactivado"); }
                int activas = Contar(muros);
                if (activas < esperadas && d != null)
                {
                    foreach (BoxCollider2D b in muros.GetComponents<BoxCollider2D>()) DestroyImmediate(b);
                    foreach (E3Rect r in d.muros)
                    {
                        BoxCollider2D b = muros.gameObject.AddComponent<BoxCollider2D>();
                        b.offset = new Vector2(r.cx, r.cy);
                        b.size = new Vector2(r.w, r.h);
                    }
                    informe.Add("colisiones de muro repuestas (" + activas + " -> " + esperadas + ")");
                }
                muros.esperadas = esperadas;
            }

            // --- Jason: solo su propio movimiento
            E3Jugador j = FindAnyObjectByType<E3Jugador>();
            if (j != null)
            {
                foreach (MonoBehaviour mb in j.GetComponents<MonoBehaviour>())
                {
                    if (mb == null || !mb.enabled) continue;
                    string n = mb.GetType().Name;
                    if (n == "PlayerMovement" || n.StartsWith("Piso3"))
                    {
                        mb.enabled = false;
                        informe.Add("desactivado " + n + " en Jason");
                    }
                }
            }

            int final = muros != null ? Contar(muros) : 0;
            Debug.Log("[Escenario 3] Paredes con colisión: " + final + "/" + esperadas + " piezas." +
                      (informe.Count > 0 ? " Arreglado: " + string.Join("; ", informe.ToArray()) + "." : " Todo en orden."));
        }

        private void Start()
        {
            if (d == null) return;
            Vector2 org = raiz != null ? (Vector2)raiz.position : Vector2.zero;
            E3Rejilla.Preparar(org, d.columnas, d.filas, d.celda);
        }

        private static int Contar(E3Muros m)
        {
            int c = 0;
            foreach (BoxCollider2D b in m.GetComponents<BoxCollider2D>()) if (b.enabled && !b.isTrigger) c++;
            return c;
        }
    }
}
