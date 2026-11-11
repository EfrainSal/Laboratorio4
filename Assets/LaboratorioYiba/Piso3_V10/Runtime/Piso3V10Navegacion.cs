using System.Collections.Generic;
using UnityEngine;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// Rejilla de navegación para los Zorbos, construida al iniciar a partir de los colliders REALES de la
    /// escena (muros, objetos, puertas). Así rodean paredes y muebles en vez de quedarse atorados.
    /// Celda de 0.16 u (más grande en mapas enormes, máx. 160 000 celdas). A* en 8 direcciones sin cortar esquinas.
    /// </summary>
    public static class Piso3V10Navegacion
    {
        private static float Celda = 0.16f;     // V15: crece en mapas grandes (escena modular de tilemaps)
        private const int MaxCeldas = 160000;
        private static bool[] libre;
        private static int ancho, alto;
        private static Vector2 origen;          // esquina inferior izquierda (centro de la celda 0,0)
        private static Vector2 tamCuerpo;
        public static bool Lista { get; private set; }

        /// <summary>Construye la rejilla una sola vez (la primera vez que un Zorbo la pide).</summary>
        public static void Asegurar(Vector2 cuerpo, Vector2 desfaseCuerpo)
        {
            if (Lista) return;
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            bool hay = false;
            Physics2D.SyncTransforms();
            Piso3V10Colisiones muros = Object.FindFirstObjectByType<Piso3V10Colisiones>();
            if (muros != null) hay = muros.Limites(out b);   // V16: cajas hijas
            if (!hay)
            {
                // V15 escena modular (tilemaps): el área es la de todos los colliders fijos (paredes, bordes, muebles).
                foreach (Collider2D c in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                {
                    if (!c.enabled || c.isTrigger) continue;
                    if (c.attachedRigidbody != null && c.attachedRigidbody.bodyType != RigidbodyType2D.Static) continue;
                    if (c.compositeOperation != Collider2D.CompositeOperation.None && c.composite != null) continue;
                    if (!hay) { b = c.bounds; hay = true; } else b.Encapsulate(c.bounds);
                }
            }
            if (!hay) { Debug.LogWarning("[Piso3] Navegación: no se encontraron paredes con collider en la escena."); return; }

            Celda = Mathf.Max(0.16f, Mathf.Sqrt(b.size.x * b.size.y / MaxCeldas));
            tamCuerpo = cuerpo;
            origen = new Vector2(b.min.x + Celda * 0.5f, b.min.y + Celda * 0.5f);
            ancho = Mathf.Max(1, Mathf.CeilToInt(b.size.x / Celda));
            alto = Mathf.Max(1, Mathf.CeilToInt(b.size.y / Celda));
            libre = new bool[ancho * alto];
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = false;
            Collider2D[] r = new Collider2D[8];
            int libres = 0;
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    Vector2 c = Centro(x, y) + desfaseCuerpo;
                    int n = Physics2D.OverlapBox(c, cuerpo, 0f, f, r);
                    bool ok = true;
                    for (int i = 0; i < n; i++)
                    {
                        Rigidbody2D rb = r[i].attachedRigidbody;
                        if (rb != null && rb.bodyType != RigidbodyType2D.Static) continue;   // personajes: no bloquean la rejilla
                        ok = false; break;
                    }
                    libre[y * ancho + x] = ok;
                    if (ok) libres++;
                }
            Lista = true;
            Debug.Log("[Piso3] Navegación de Zorbos lista: " + ancho + "x" + alto + " celdas, " + libres + " libres.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() { Lista = false; libre = null; }

        private static Vector2 Centro(int x, int y) { return origen + new Vector2(x * Celda, y * Celda); }

        private static bool Celda_(Vector2 p, out int cx, out int cy)
        {
            cx = Mathf.RoundToInt((p.x - origen.x) / Celda);
            cy = Mathf.RoundToInt((p.y - origen.y) / Celda);
            return cx >= 0 && cy >= 0 && cx < ancho && cy < alto;
        }

        private static bool Libre(int x, int y) { return x >= 0 && y >= 0 && x < ancho && y < alto && libre[y * ancho + x]; }

        /// <summary>Celda libre más cercana (por si el punto cae justo en un borde).</summary>
        private static bool CercanaLibre(ref int x, ref int y)
        {
            if (Libre(x, y)) return true;
            for (int r = 1; r <= 4; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (Libre(x + dx, y + dy)) { x += dx; y += dy; return true; }
            return false;
        }

        /// <summary>Calcula una ruta en el mundo de "desde" a "hasta". Devuelve false si no hay.</summary>
        public static bool Ruta(Vector2 desde, Vector2 hasta, List<Vector2> salida, int maxNodos = 14000)
        {
            salida.Clear();
            if (!Lista) return false;
            int sx, sy, tx, ty;
            if (!Celda_(desde, out sx, out sy) || !Celda_(hasta, out tx, out ty)) return false;
            if (!CercanaLibre(ref sx, ref sy) || !CercanaLibre(ref tx, ref ty)) return false;
            int inicio = sy * ancho + sx, fin = ty * ancho + tx;
            if (inicio == fin) { salida.Add(hasta); return true; }

            Dictionary<int, float> g = new Dictionary<int, float>();
            Dictionary<int, int> padre = new Dictionary<int, int>();
            List<KeyValuePair<float, int>> abiertos = new List<KeyValuePair<float, int>>();
            HashSet<int> cerrados = new HashSet<int>();
            g[inicio] = 0f;
            Empujar(abiertos, new KeyValuePair<float, int>(H(sx, sy, tx, ty), inicio));
            int expandidos = 0;
            while (abiertos.Count > 0 && expandidos < maxNodos)
            {
                int actual = Sacar(abiertos).Value;
                if (cerrados.Contains(actual)) continue;
                cerrados.Add(actual);
                expandidos++;
                if (actual == fin) break;
                int ax = actual % ancho, ay = actual / ancho;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = ax + dx, ny = ay + dy;
                        if (!Libre(nx, ny)) continue;
                        if (dx != 0 && dy != 0 && (!Libre(ax + dx, ay) || !Libre(ax, ay + dy))) continue;   // no cortar esquinas
                        int vecino = ny * ancho + nx;
                        if (cerrados.Contains(vecino)) continue;
                        float coste = g[actual] + ((dx != 0 && dy != 0) ? 1.4142f : 1f);
                        float previo;
                        if (g.TryGetValue(vecino, out previo) && previo <= coste) continue;
                        g[vecino] = coste;
                        padre[vecino] = actual;
                        Empujar(abiertos, new KeyValuePair<float, int>(coste + H(nx, ny, tx, ty), vecino));
                    }
            }
            if (!padre.ContainsKey(fin)) return false;
            List<Vector2> inversa = new List<Vector2>();
            int n = fin;
            while (n != inicio) { inversa.Add(Centro(n % ancho, n / ancho)); n = padre[n]; }
            for (int i = inversa.Count - 1; i >= 0; i--) salida.Add(inversa[i]);
            if (salida.Count > 0) salida[salida.Count - 1] = hasta;
            return true;
        }

        private static float H(int x, int y, int tx, int ty)
        {
            int dx = Mathf.Abs(x - tx), dy = Mathf.Abs(y - ty);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }

        // montículo binario mínimo
        private static void Empujar(List<KeyValuePair<float, int>> h, KeyValuePair<float, int> v)
        {
            h.Add(v);
            int i = h.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (h[p].Key <= h[i].Key) break;
                KeyValuePair<float, int> t = h[p]; h[p] = h[i]; h[i] = t; i = p;
            }
        }

        private static KeyValuePair<float, int> Sacar(List<KeyValuePair<float, int>> h)
        {
            KeyValuePair<float, int> raiz = h[0];
            int ultimo = h.Count - 1;
            h[0] = h[ultimo];
            h.RemoveAt(ultimo);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < h.Count && h[l].Key < h[m].Key) m = l;
                if (r < h.Count && h[r].Key < h[m].Key) m = r;
                if (m == i) break;
                KeyValuePair<float, int> t = h[m]; h[m] = h[i]; h[i] = t; i = m;
            }
            return raiz;
        }
    }
}
