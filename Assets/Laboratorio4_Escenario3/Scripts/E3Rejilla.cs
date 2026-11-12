using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Rejilla de rutas para los Sorbos (celdas de 0.24 u). Se arma al empezar a partir de las colisiones reales
    /// de la escena (muros, objetos y puertas), dejando un margen de una celda para que el cuerpo no roce.
    /// Ruta A* en 8 direcciones sin cortar esquinas.
    /// </summary>
    public static class E3Rejilla
    {
        private static bool[] libre;
        private static int cols, filas;
        private static Scene escena;
        private static float celda = 0.24f;
        private static Vector2 origen;

        public static bool Lista { get { return libre != null && escena == SceneManager.GetActiveScene(); } }

        public static void Preparar(Vector2 org, int c, int f, float cel)
        {
            cols = c; filas = f; celda = cel; origen = org;
            escena = SceneManager.GetActiveScene();
            bool[] bloq = new bool[c * f];
            Collider2D[] buf = new Collider2D[8];
            ContactFilter2D cf = new ContactFilter2D();
            cf.useTriggers = false;
            Vector2 tam = new Vector2(cel * 0.9f, cel * 0.9f);
            for (int r = 0; r < f; r++)
                for (int k = 0; k < c; k++)
                {
                    int n = Physics2D.OverlapBox(Centro(k, r), tam, 0f, cf, buf);
                    for (int i = 0; i < n; i++)
                        if (buf[i] != null && buf[i].GetComponentInParent<E3Movedor>() == null) { bloq[r * c + k] = true; break; }
                }
            libre = new bool[c * f];
            for (int r = 0; r < f; r++)
                for (int k = 0; k < c; k++)
                {
                    bool ok = true;
                    for (int dr = -1; dr <= 1 && ok; dr++)
                        for (int dk = -1; dk <= 1 && ok; dk++)
                        {
                            int rr = r + dr, kk = k + dk;
                            if (rr < 0 || kk < 0 || rr >= f || kk >= c || bloq[rr * c + kk]) ok = false;
                        }
                    libre[r * c + k] = ok;
                }
        }

        private static Vector2 Centro(int k, int r) { return origen + new Vector2((k + 0.5f) * celda, -(r + 0.5f) * celda); }

        private static bool Celda(Vector2 p, out int k, out int r)
        {
            k = Mathf.FloorToInt((p.x - origen.x) / celda);
            r = Mathf.FloorToInt((origen.y - p.y) / celda);
            return k >= 0 && r >= 0 && k < cols && r < filas;
        }

        private static int MasCercana(Vector2 p)
        {
            int k, r;
            Celda(p, out k, out r);
            for (int rad = 0; rad <= 4; rad++)
                for (int dr = -rad; dr <= rad; dr++)
                    for (int dk = -rad; dk <= rad; dk++)
                    {
                        int rr = r + dr, kk = k + dk;
                        if (rr < 0 || kk < 0 || rr >= filas || kk >= cols) continue;
                        if (libre[rr * cols + kk]) return rr * cols + kk;
                    }
            return -1;
        }

        // montículo binario simple para A*
        private static int[] heap = new int[0];
        private static float[] prio;
        private static int heapN;

        private static void Push(int nodo, float p)
        {
            if (heapN >= heap.Length) System.Array.Resize(ref heap, Mathf.Max(256, heap.Length * 2));
            int i = heapN++;
            heap[i] = nodo;
            while (i > 0)
            {
                int pa = (i - 1) / 2;
                if (prio[heap[pa]] <= p) break;
                heap[i] = heap[pa]; i = pa;
            }
            heap[i] = nodo;
        }

        private static int Pop()
        {
            int top = heap[0];
            int last = heap[--heapN];
            float p = prio[last];
            int i = 0;
            while (true)
            {
                int a = i * 2 + 1;
                if (a >= heapN) break;
                int b = a + 1;
                int m = (b < heapN && prio[heap[b]] < prio[heap[a]]) ? b : a;
                if (prio[heap[m]] >= p) break;
                heap[i] = heap[m]; i = m;
            }
            if (heapN > 0) heap[i] = last;
            return top;
        }

        private static float[] g;
        private static int[] padre;
        private static int[] visita;
        private static int sello;

        /// <summary>Calcula la ruta y la deja en 'ruta' (puntos de mundo). Devuelve false si no hay.</summary>
        public static bool Ruta(Vector2 desde, Vector2 hasta, List<Vector2> ruta)
        {
            ruta.Clear();
            if (!Lista) return false;
            int n = cols * filas;
            if (g == null || g.Length != n) { g = new float[n]; padre = new int[n]; visita = new int[n]; prio = new float[n]; sello = 0; }
            int ini = MasCercana(desde), fin = MasCercana(hasta);
            if (ini < 0 || fin < 0) return false;
            sello++;
            heapN = 0;
            int fk = fin % cols, fr = fin / cols;
            g[ini] = 0f; padre[ini] = -1; visita[ini] = sello;
            prio[ini] = Heur(ini % cols, ini / cols, fk, fr);
            Push(ini, prio[ini]);
            int expandidos = 0;
            bool hallado = false;
            while (heapN > 0 && expandidos < 9000)
            {
                int u = Pop();
                expandidos++;
                if (u == fin) { hallado = true; break; }
                int uk = u % cols, ur = u / cols;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dk = -1; dk <= 1; dk++)
                    {
                        if (dr == 0 && dk == 0) continue;
                        int vk = uk + dk, vr = ur + dr;
                        if (vk < 0 || vr < 0 || vk >= cols || vr >= filas) continue;
                        int v = vr * cols + vk;
                        if (!libre[v]) continue;
                        if (dr != 0 && dk != 0 && (!libre[ur * cols + vk] || !libre[vr * cols + uk])) continue;
                        float coste = g[u] + ((dr != 0 && dk != 0) ? 1.4142f : 1f);
                        if (visita[v] == sello && coste >= g[v]) continue;
                        visita[v] = sello; g[v] = coste; padre[v] = u;
                        prio[v] = coste + Heur(vk, vr, fk, fr);
                        Push(v, prio[v]);
                    }
            }
            if (!hallado) return false;
            for (int c = fin; c != -1; c = padre[c]) ruta.Add(Centro(c % cols, c / cols));
            ruta.Reverse();
            if (ruta.Count > 0) ruta.RemoveAt(0);
            ruta.Add(hasta);
            return true;
        }

        private static float Heur(int ak, int ar, int bk, int br)
        {
            int dx = Mathf.Abs(ak - bk), dy = Mathf.Abs(ar - br);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }
    }
}
