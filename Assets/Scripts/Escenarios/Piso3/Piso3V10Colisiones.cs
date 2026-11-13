using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Va en "01_ColisionMuros" (hijo del fondo, así los muros SIEMPRE coinciden con el dibujo aunque el
    /// fondo se mueva o se escale).
    ///  - Al dar Play comprueba la alineación: 24 puntos que deben ser suelo y 24 que deben ser muro.
    ///    El resultado sale en la Consola ("[Piso3] Colisiones alineadas 48/48").
    ///  - En la vista Scene dibuja el contorno de los muros (rojo) para verlo sin dar Play.
    ///  - V16: los muros son cajas hijas ("Muro_###"); también acepta el PolygonCollider2D antiguo.
    /// </summary>
    public class Piso3V10Colisiones : MonoBehaviour
    {
        [Tooltip("Puntos (en local) que deben ser suelo libre.")]
        public Vector2[] pruebaSuelo = new Vector2[0];
        [Tooltip("Puntos (en local) que deben estar dentro de un muro.")]
        public Vector2[] pruebaMuro = new Vector2[0];
        [Tooltip("Dibujar el contorno de los muros en la vista Scene.")]
        public bool verEnEscena = true;
        [HideInInspector] public string firma = "";

        public int Aciertos { get; private set; }
        public int Total { get; private set; }

        private void Start()
        {
            Physics2D.SyncTransforms();
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = false;
            Collider2D[] r = new Collider2D[8];
            int ok = 0, total = 0, malSuelo = 0, malMuro = 0;
            foreach (Vector2 p in pruebaSuelo)
            {
                total++;
                int n = Physics2D.OverlapPoint(transform.TransformPoint(p), f, r);
                bool esMuro = false;
                for (int i = 0; i < n; i++) if (EsMio(r[i])) esMuro = true;
                if (!esMuro) ok++; else malSuelo++;
            }
            foreach (Vector2 p in pruebaMuro)
            {
                total++;
                int n = Physics2D.OverlapPoint(transform.TransformPoint(p), f, r);
                bool esMuro = false;
                for (int i = 0; i < n; i++) if (EsMio(r[i])) esMuro = true;
                if (esMuro) ok++; else malMuro++;
            }
            Aciertos = ok; Total = total;
            if (total == 0) return;
            if (ok == total)
                Debug.Log("[Piso3] Colisiones alineadas con el dibujo: " + ok + "/" + total + " puntos correctos.", this);
            else
                Debug.LogError("[Piso3] Colisiones DESALINEADAS: " + ok + "/" + total + " (" + malSuelo + " puntos de suelo bloqueados, " +
                               malMuro + " puntos de muro libres). Usa 'Laboratorio Yiba > Piso 3 V10 > 0 - Revisar y arreglar escena ahora' " +
                               "y manda captura de la Consola.", this);
        }

        private bool EsMio(Collider2D c)
        {
            return c != null && (c.transform == transform || c.transform.IsChildOf(transform));
        }

        /// <summary>Límites de todos los muros (cajas hijas o polígono antiguo).</summary>
        public bool Limites(out Bounds b)
        {
            b = new Bounds();
            bool hay = false;
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            {
                if (!c.enabled) continue;
                if (!hay) { b = c.bounds; hay = true; } else b.Encapsulate(c.bounds);
            }
            return hay;
        }

        private void OnDrawGizmos()
        {
            if (!verEnEscena) return;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            PolygonCollider2D pc = GetComponent<PolygonCollider2D>();
            if (pc != null)
                for (int i = 0; i < pc.pathCount; i++)
                {
                    Vector2[] p = pc.GetPath(i);
                    for (int k = 0; k < p.Length; k++)
                        Gizmos.DrawLine(transform.TransformPoint(p[k] + pc.offset), transform.TransformPoint(p[(k + 1) % p.Length] + pc.offset));
                }
            foreach (BoxCollider2D bc in GetComponentsInChildren<BoxCollider2D>())
            {
                Vector2 h = bc.size * 0.5f;
                Transform t = bc.transform;
                Vector3 a = t.TransformPoint(bc.offset + new Vector2(-h.x, -h.y)), b = t.TransformPoint(bc.offset + new Vector2(-h.x, h.y));
                Vector3 c = t.TransformPoint(bc.offset + new Vector2(h.x, h.y)), d = t.TransformPoint(bc.offset + new Vector2(h.x, -h.y));
                Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
            }
            Gizmos.color = Color.green;
            foreach (Vector2 q in pruebaSuelo) Gizmos.DrawWireSphere(transform.TransformPoint(q), 0.06f);
            Gizmos.color = Color.magenta;
            foreach (Vector2 q in pruebaMuro) Gizmos.DrawWireSphere(transform.TransformPoint(q), 0.06f);
        }
    }
}
