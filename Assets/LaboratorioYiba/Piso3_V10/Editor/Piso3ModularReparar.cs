using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using LaboratorioYiba.Piso3V10;

namespace LaboratorioYiba.Piso3V10.EditorTools
{
    /// <summary>
    /// V15 – Arreglo para la escena MODULAR del Piso 3 (Grid + Tilemaps: "01_Suelo", "02_Detalles...",
    /// "03_Paredes_Colision"), p. ej. Piso3_Final_Modular / Piso3_Modular.
    ///
    /// Qué estaba mal en esas escenas
    ///  - El jugador de prueba ("Jugador_Prueba" con Piso3TestMovement / Piso3TestPlayer) se mueve sin
    ///    barrido de física: atraviesa las paredes y, al empujarlo la física, se queda vibrando/atorado.
    ///  - Jason y los Zorbos (Piso3_Modular) estaban en coordenadas del fondo dibujado V10 (y negativa),
    ///    FUERA del mapa de tiles: ahí no hay paredes; y al dar Play la auto-reparación V14 creaba el
    ///    fondo V10 encima. Dos mapas mezclados.
    ///  - Paredes: una caja por tile sin unir (costuras en las que se engancha) y un hueco en el borde.
    ///  - Perspectiva: paredes con un solo orden de dibujo (10000 = tapan siempre a todos, o 5 = nunca),
    ///    objetos todos en orden 10 y colliders del tamaño del sprite completo (no se pasa por detrás).
    ///
    /// Qué hace (no mueve objetos, no borra nada; el jugador de prueba solo se desactiva)
    ///  1. Paredes: todas las celdas con collider "Grid" (cuadro completo), TilemapCollider2D unido en un
    ///     CompositeCollider2D (Polygons) con Rigidbody2D Static = paredes macizas sin costuras.
    ///  2. Bordes invisibles donde el suelo toca el vacío sin pared (no se puede salir del mapa).
    ///  3. Dibujo de paredes por filas: cada fila de tiles se ordena por su Y, igual que personajes y
    ///     objetos (-Y*100). Quien está detrás de una pared queda tapado; quien está delante, la tapa.
    ///  4. Objetos: collider solo en la base (se pasa por detrás) y orden por la base.
    ///  5. Jason (movimiento con barrido) dentro del mapa, cámara siguiéndolo, jugador de prueba apagado,
    ///     Zorbos dentro del mapa en los Spawn_Zorbo (crea 7 si no hay ninguno).
    /// </summary>
    public static class Piso3ModularReparar
    {
        internal const string NombreFilas = "03c_Paredes_Dibujo (filas por profundidad)";
        internal const string NombreBordes = "03b_Bordes_Invisibles";
        private const int OrdenParedOriginal = -30500;     // copia original, bajo todo (las filas van encima)
        private const int ZorbosACrear = 7;

        // fracción de la altura del sprite que es "base" (lo que pisa el suelo)
        private static readonly string[] ClavesBase = { "caja", "computadora", "sillon", "silla", "mesa", "escritorio", "camilla", "cama",
                                                        "maquina", "generador", "consola", "planta", "barril", "tanque", "tuberia",
                                                        "estanteria", "estante", "armario", "librero", "panel", "botiquin", "nevera", "refrigerador" };
        private static readonly float[] FraccionBase = { 0.60f, 0.55f, 0.55f, 0.55f, 0.50f, 0.50f, 0.50f, 0.50f,
                                                         0.45f, 0.45f, 0.45f, 0.30f, 0.35f, 0.35f, 0.35f,
                                                         0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f, 0.35f };
        // si no tienen collider se les pone uno en la base (están en el suelo)
        private static readonly string[] SolidosSinCollider = { "planta", "barril", "caja", "tanque", "estanteria", "maquina" };

        // ------------------------------------------------------------------ detección
        private static bool EsNombrePared(string n)
        {
            n = n.ToLowerInvariant();
            return n.Contains("pared") || n.Contains("muro") || n.Contains("wall");
        }

        private static bool EsGenerado(Tilemap tm)
        {
            Transform p = tm.transform.parent;
            return p != null && p.name.StartsWith(NombreFilas);
        }

        private static bool EsTilemapPared(Tilemap tm)
        {
            if (EsGenerado(tm)) return false;
            TilemapCollider2D tc = tm.GetComponent<TilemapCollider2D>();
            return (tc != null && tc.enabled) || EsNombrePared(tm.name);
        }

        /// <summary>Escena hecha con tilemaps (sin el fondo dibujado V10) y con paredes.</summary>
        public static bool EsModular()
        {
            if (Piso3V10Generador.BuscarFondo() != null) return false;
            foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                if (EsTilemapPared(tm)) return true;
            return false;
        }

        /// <summary>Escena modular que además es del Piso 3 (nombre de escena/Grid o personajes del paquete).</summary>
        public static bool EsPiso3Modular()
        {
            // V16.1: solo por el nombre de la escena (antes bastaba con tener Piso3V10Movimiento y se
            // "arreglaban" escenas de otros pisos).
            return EsModular() && Piso3V10Generador.EsEscenaPiso3();
        }

        // ------------------------------------------------------------------ menú
        [MenuItem("Laboratorio Yiba/Piso 3 V10/9 - Arreglar escena modular (tilemaps)")]
        public static void Menu()
        {
            if (!Piso3V10Generador.EsEscenaPiso3())
            {
                Debug.LogWarning("[Piso3 Modular] La escena abierta no es del Piso 3 (su nombre no empieza por 'Piso3'): no se toca.");
                return;
            }
            if (!EsModular())
            {
                Debug.LogWarning("[Piso3 Modular] La escena abierta no tiene tilemap de paredes (o tiene el fondo dibujado V10). " +
                                 "Para la escena del fondo dibujado usa '3 - Reparar escena abierta'.");
                return;
            }
            Piso3V10Generador.ConfigurarSpritesSiHaceFalta();
            string informe = Reparar();
            Scene escena = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            if (!string.IsNullOrEmpty(escena.path)) EditorSceneManager.SaveScene(escena);
            Debug.Log("[Piso3 Modular] " + informe);
        }

        // ------------------------------------------------------------------ datos de trabajo
        private class Mapa
        {
            public Grid grid;
            public HashSet<Vector2Int> suelo = new HashSet<Vector2Int>();
            public HashSet<Vector2Int> pared = new HashSet<Vector2Int>();
            public HashSet<Vector2Int> borde = new HashSet<Vector2Int>();
            public List<Rect> huellas = new List<Rect>();        // bases de objetos (mundo)
            public Rect limites;

            public Vector2Int Celda(Vector3 mundo)
            {
                Vector3Int c = grid.WorldToCell(mundo);
                return new Vector2Int(c.x, c.y);
            }

            public bool Bloqueada(Vector2Int c) { return pared.Contains(c) || borde.Contains(c) || !suelo.Contains(c); }

            /// <summary>Punto donde cabe un personaje (0.3 de margen con paredes y muebles).</summary>
            public bool Libre(Vector2 p)
            {
                const float m = 0.3f;
                Vector3 tam = grid.cellSize;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Vector2 q = p + new Vector2(dx * m, dy * m);
                        if (Bloqueada(Celda(q))) return false;
                    }
                foreach (Rect r in huellas)
                {
                    Rect g = new Rect(r.xMin - m, r.yMin - m, r.width + 2 * m, r.height + 2 * m);
                    if (g.Contains(p)) return false;
                }
                return tam.x > 0f;
            }

            public bool LibreCercano(Vector2 desde, out Vector2 res, List<Vector2> evitar = null, float sep = 0.9f)
            {
                for (float r = 0f; r <= 40f; r += 0.5f)
                {
                    int pasos = r <= 0f ? 1 : Mathf.CeilToInt(2f * Mathf.PI * r / 0.5f);
                    for (int i = 0; i < pasos; i++)
                    {
                        float a = i * 2f * Mathf.PI / pasos;
                        Vector2 p = desde + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        if (!Libre(p)) continue;
                        bool lejos = true;
                        if (evitar != null) foreach (Vector2 e in evitar) if ((e - p).sqrMagnitude < sep * sep) { lejos = false; break; }
                        if (!lejos) continue;
                        res = p;
                        return true;
                    }
                }
                res = desde;
                return false;
            }
        }

        private static void Celdas(Tilemap tm, Grid grid, HashSet<Vector2Int> salida)
        {
            foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(p)) continue;
                Vector3Int c = grid.WorldToCell(tm.GetCellCenterWorld(p));
                salida.Add(new Vector2Int(c.x, c.y));
            }
        }

        // ================================================================== reparación completa
        /// <summary>Repara la escena modular abierta. Idempotente: si ya está bien no cambia nada. Devuelve el informe.</summary>
        public static string Reparar()
        {
            List<string> hecho = new List<string>();
            StringBuilder inf = new StringBuilder();

            List<Tilemap> paredes = new List<Tilemap>();
            List<Tilemap> suelos = new List<Tilemap>();
            foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (EsGenerado(tm)) continue;
                if (EsTilemapPared(tm)) paredes.Add(tm); else suelos.Add(tm);
            }
            if (paredes.Count == 0) return "No hay tilemap de paredes.";
            Mapa mapa = new Mapa();
            mapa.grid = paredes[0].layoutGrid;
            if (mapa.grid == null) return "El tilemap de paredes no está dentro de un Grid.";
            foreach (Tilemap tm in suelos) Celdas(tm, mapa.grid, mapa.suelo);
            foreach (Tilemap tm in paredes) Celdas(tm, mapa.grid, mapa.pared);
            mapa.suelo.ExceptWith(mapa.pared);
            inf.Append("Mapa modular: ").Append(mapa.suelo.Count).Append(" celdas de suelo, ").Append(mapa.pared.Count).Append(" de pared. ");

            // límites del mapa (para la cámara)
            bool hay = false;
            Rect lim = new Rect();
            foreach (Vector2Int c in mapa.suelo)
            {
                Vector3 a = mapa.grid.CellToWorld(new Vector3Int(c.x, c.y, 0));
                Vector3 b = mapa.grid.CellToWorld(new Vector3Int(c.x + 1, c.y + 1, 0));
                Rect r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                if (!hay) { lim = r; hay = true; }
                else lim = Rect.MinMaxRect(Mathf.Min(lim.xMin, r.xMin), Mathf.Min(lim.yMin, r.yMin), Mathf.Max(lim.xMax, r.xMax), Mathf.Max(lim.yMax, r.yMax));
            }
            mapa.limites = Rect.MinMaxRect(lim.xMin - 1f, lim.yMin - 1f, lim.xMax + 1f, lim.yMax + 1f);

            // 1. paredes macizas
            foreach (Tilemap tm in paredes) ParedesSolidas(tm, hecho);

            // 2. bordes invisibles
            foreach (Vector2Int c in mapa.suelo)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Vector2Int v = new Vector2Int(c.x + dx, c.y + dy);
                        if (!mapa.suelo.Contains(v) && !mapa.pared.Contains(v)) mapa.borde.Add(v);
                    }
            Bordes(mapa, paredes[0].GetComponent<Piso3ModularParedes>(), hecho);

            // 3. dibujo de paredes por filas + orden del suelo
            foreach (Tilemap tm in paredes) FilasDeDibujo(tm, hecho);
            OrdenSuelos(suelos, hecho);

            // 4. objetos
            Objetos(mapa, hecho);

            // 5. personajes
            Personajes(mapa, hecho, inf);

            if (hecho.Count > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            inf.Append(hecho.Count > 0 ? " Arreglado: " + string.Join(" · ", hecho) + ". Guarda la escena (Ctrl+S)." : " Todo al día.");
            return inf.ToString();
        }

        // ------------------------------------------------------------------ 1. paredes
        private static void ParedesSolidas(Tilemap tm, List<string> hecho)
        {
            GameObject go = tm.gameObject;
            int celdas = 0, assets = 0;
            HashSet<TileBase> vistos = new HashSet<TileBase>();
            Undo.RegisterCompleteObjectUndo(tm, "Paredes macizas");
            foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(p)) continue;
                TileBase tb = tm.GetTile(p);
                if (tb != null && vistos.Add(tb))
                {
                    Tile t = tb as Tile;
                    if (t != null && t.colliderType != Tile.ColliderType.Grid)
                    {
                        Undo.RecordObject(t, "Tile Grid");
                        t.colliderType = Tile.ColliderType.Grid;
                        EditorUtility.SetDirty(t);
                        assets++;
                    }
                }
                if (tm.GetColliderType(p) != Tile.ColliderType.Grid)
                {
                    tm.SetColliderType(p, Tile.ColliderType.Grid);
                    celdas++;
                }
            }
            if (assets > 0) { AssetDatabase.SaveAssets(); hecho.Add(assets + " tile(s) de pared con collider 'Grid'"); }
            if (celdas > 0) hecho.Add(celdas + " celdas de pared con collider de cuadro completo");

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) { rb = Undo.AddComponent<Rigidbody2D>(go); hecho.Add("Rigidbody2D Static en " + go.name); }
            if (rb.bodyType != RigidbodyType2D.Static) { Undo.RecordObject(rb, "Static"); rb.bodyType = RigidbodyType2D.Static; }

            TilemapCollider2D tc = go.GetComponent<TilemapCollider2D>();
            if (tc == null) { tc = Undo.AddComponent<TilemapCollider2D>(go); hecho.Add("TilemapCollider2D en " + go.name); }
            if (!tc.enabled || tc.isTrigger || tc.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                Undo.RecordObject(tc, "Unir paredes");
                tc.enabled = true;
                tc.isTrigger = false;
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
                hecho.Add("paredes unidas sin costuras (CompositeCollider2D)");
            }
            CompositeCollider2D cc = go.GetComponent<CompositeCollider2D>();
            if (cc == null) cc = Undo.AddComponent<CompositeCollider2D>(go);
            if (cc.geometryType != CompositeCollider2D.GeometryType.Polygons || cc.generationType != CompositeCollider2D.GenerationType.Synchronous || cc.isTrigger)
            {
                Undo.RecordObject(cc, "Composite");
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
                cc.generationType = CompositeCollider2D.GenerationType.Synchronous;
                cc.isTrigger = false;
            }
            cc.GenerateGeometry();
            if (go.GetComponent<Piso3ModularParedes>() == null) Undo.AddComponent<Piso3ModularParedes>(go);
            if (!go.isStatic) go.isStatic = true;
        }

        // ------------------------------------------------------------------ 2. bordes
        private static string Firma(IEnumerable<Vector2Int> celdas)
        {
            long h = 17; int n = 0;
            foreach (Vector2Int c in celdas) { h += (c.x * 73856093L) ^ (c.y * 19349663L); n++; }
            return n + ":" + h;
        }

        private static void Bordes(Mapa mapa, Piso3ModularParedes marca, List<string> hecho)
        {
            Transform raiz = mapa.grid.transform;
            Transform viejo = raiz.Find(NombreBordes);
            string firma = Firma(mapa.borde);
            bool alDia = mapa.borde.Count == 0 ? viejo == null : (viejo != null && marca != null && marca.firmaBordes == firma);
            if (alDia) return;
            if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
            if (marca != null) { Undo.RecordObject(marca, "Firma"); marca.firmaBordes = firma; }
            if (mapa.borde.Count == 0) return;

            GameObject go = new GameObject(NombreBordes);
            Undo.RegisterCreatedObjectUndo(go, "Bordes invisibles");
            go.transform.SetParent(raiz, false);
            go.isStatic = true;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            CompositeCollider2D cc = go.AddComponent<CompositeCollider2D>();
            cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
            cc.generationType = CompositeCollider2D.GenerationType.Synchronous;

            // tramos horizontales de celdas seguidas
            Dictionary<int, List<int>> filas = new Dictionary<int, List<int>>();
            foreach (Vector2Int c in mapa.borde)
            {
                if (!filas.ContainsKey(c.y)) filas[c.y] = new List<int>();
                filas[c.y].Add(c.x);
            }
            int cajas = 0, huecosAlVacio = 0;
            foreach (KeyValuePair<int, List<int>> fila in filas)
            {
                fila.Value.Sort();
                int ini = fila.Value[0], prev = ini;
                for (int i = 1; i <= fila.Value.Count; i++)
                {
                    if (i < fila.Value.Count && fila.Value[i] == prev + 1) { prev = fila.Value[i]; continue; }
                    Vector3 a = go.transform.InverseTransformPoint(mapa.grid.CellToWorld(new Vector3Int(ini, fila.Key, 0)));
                    Vector3 b = go.transform.InverseTransformPoint(mapa.grid.CellToWorld(new Vector3Int(prev + 1, fila.Key + 1, 0)));
                    BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
                    bc.offset = (a + b) * 0.5f;
                    bc.size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                    bc.compositeOperation = Collider2D.CompositeOperation.Merge;
                    cajas++;
                    if (i < fila.Value.Count) { ini = fila.Value[i]; prev = ini; }
                }
            }
            foreach (Vector2Int c in mapa.borde)
                if (mapa.suelo.Contains(c + Vector2Int.left) || mapa.suelo.Contains(c + Vector2Int.right) ||
                    mapa.suelo.Contains(c + Vector2Int.up) || mapa.suelo.Contains(c + Vector2Int.down)) huecosAlVacio++;
            cc.GenerateGeometry();
            hecho.Add(huecosAlVacio > 0
                ? "bordes invisibles: " + huecosAlVacio + " celdas donde el suelo daba al vacío sin pared"
                : "bordes invisibles en " + mapa.borde.Count + " esquinas diagonales");
        }

        // ------------------------------------------------------------------ 3. dibujo por filas
        private static void FilasDeDibujo(Tilemap tm, List<string> hecho)
        {
            Transform padre = tm.transform.parent;
            string nombre = NombreFilas + " " + tm.name;
            Transform viejo = padre != null ? padre.Find(nombre) : null;
            List<Vector3Int> celdas = new List<Vector3Int>();
            foreach (Vector3Int p in tm.cellBounds.allPositionsWithin) if (tm.HasTile(p)) celdas.Add(p);
            long h = 17;
            foreach (Vector3Int p in celdas)
            {
                TileBase tb = tm.GetTile(p);
                h += (p.x * 73856093L) ^ (p.y * 19349663L) ^ (tb != null ? tb.name.GetHashCode() : 0);
            }
            string firma = celdas.Count + ":" + h + ":" + tm.transform.localPosition + tm.transform.localScale;

            TilemapRenderer original = tm.GetComponent<TilemapRenderer>();
            Piso3ModularParedes marca = tm.GetComponent<Piso3ModularParedes>();
            if (viejo != null && marca != null && marca.firmaFilas == firma)
            {
                if (original != null && original.sortingOrder != OrdenParedOriginal) { Undo.RecordObject(original, "Orden"); original.sortingOrder = OrdenParedOriginal; }
                return;
            }
            if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
            if (original == null || celdas.Count == 0) return;

            GameObject cont = new GameObject(nombre);
            Undo.RegisterCreatedObjectUndo(cont, "Paredes por filas");
            cont.transform.SetParent(padre, false);
            cont.transform.localPosition = tm.transform.localPosition;
            cont.transform.localRotation = tm.transform.localRotation;
            cont.transform.localScale = tm.transform.localScale;

            Dictionary<int, List<Vector3Int>> filas = new Dictionary<int, List<Vector3Int>>();
            foreach (Vector3Int p in celdas)
            {
                if (!filas.ContainsKey(p.y)) filas[p.y] = new List<Vector3Int>();
                filas[p.y].Add(p);
            }
            foreach (KeyValuePair<int, List<Vector3Int>> fila in filas)
            {
                GameObject f = new GameObject("Fila_" + fila.Key);
                f.transform.SetParent(cont.transform, false);
                f.isStatic = true;
                Tilemap t = f.AddComponent<Tilemap>();
                t.tileAnchor = tm.tileAnchor;
                t.orientation = tm.orientation;
                if (tm.orientation == Tilemap.Orientation.Custom) t.orientationMatrix = tm.orientationMatrix;
                t.color = tm.color;
                foreach (Vector3Int p in fila.Value)
                {
                    t.SetTile(p, tm.GetTile(p));
                    t.SetTileFlags(p, TileFlags.None);
                    t.SetTransformMatrix(p, tm.GetTransformMatrix(p));
                    t.SetColor(p, tm.GetColor(p));
                    t.SetTileFlags(p, tm.GetTileFlags(p));
                    t.SetColliderType(p, Tile.ColliderType.None);
                }
                TilemapRenderer r = f.AddComponent<TilemapRenderer>();
                r.sharedMaterial = original.sharedMaterial;
                r.sortingLayerID = original.sortingLayerID;
                r.mode = original.mode == TilemapRenderer.Mode.Individual ? TilemapRenderer.Mode.Chunk : original.mode;
                r.sortOrder = original.sortOrder;
                r.maskInteraction = original.maskInteraction;
                float yBase = t.CellToWorld(new Vector3Int(fila.Value[0].x, fila.Key, 0)).y;
                r.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-yBase * 100f), -29000, 29000);
            }
            Undo.RecordObject(original, "Orden");
            original.sortingOrder = OrdenParedOriginal;
            if (marca == null) marca = Undo.AddComponent<Piso3ModularParedes>(tm.gameObject);
            Undo.RecordObject(marca, "Firma");
            marca.firmaFilas = firma;
            hecho.Add("paredes dibujadas en " + filas.Count + " filas ordenadas por profundidad");
        }

        private static void OrdenSuelos(List<Tilemap> suelos, List<string> hecho)
        {
            Tilemap mayor = null;
            int max = -1;
            foreach (Tilemap tm in suelos)
            {
                int n = 0;
                foreach (Vector3Int p in tm.cellBounds.allPositionsWithin) if (tm.HasTile(p)) n++;
                string nom = tm.name.ToLowerInvariant();
                if (nom.Contains("suelo") || nom.Contains("piso") || nom.Contains("floor") || nom.Contains("ground")) n += 1000000;
                if (n > max) { max = n; mayor = tm; }
            }
            int cambios = 0, k = 0;
            foreach (Tilemap tm in suelos)
            {
                TilemapRenderer r = tm.GetComponent<TilemapRenderer>();
                if (r == null) continue;
                int orden = tm == mayor ? Piso3V10Generador.OrdenFondo : Piso3V10Generador.OrdenDecal + (k++);
                if (r.sortingOrder != orden) { Undo.RecordObject(r, "Orden suelo"); r.sortingOrder = orden; cambios++; }
            }
            if (cambios > 0) hecho.Add("suelo y detalles al fondo del orden de dibujo");
        }

        // ------------------------------------------------------------------ 4. objetos
        private static bool EsPersonaje(GameObject go)
        {
            if (go.GetComponent<Piso3V10Personaje8Dir>() != null || go.GetComponent<Piso3V10Zorbo>() != null ||
                go.GetComponent<Piso3V10JasonAnimacion>() != null || go.GetComponent<Piso3V10Movimiento>() != null) return true;
            if (go.CompareTag("Player")) return true;
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Static) return true;
            foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                if (Piso3V10Movimiento.EsMovimientoAjeno(mb)) return true;
            return false;
        }

        private static bool EsOrdenAjeno(MonoBehaviour mb)
        {
            if (mb == null) return false;
            string ns = mb.GetType().Namespace ?? "";
            if (ns.StartsWith("LaboratorioYiba")) return false;
            string n = mb.GetType().Name;
            return n.Contains("YSort") || n.Contains("SortY") || n.Contains("YOrder") || n.Contains("OrdenY") || n.Contains("DepthSort") || n.Contains("SortByY");
        }

        private static void Objetos(Mapa mapa, List<string> hecho)
        {
            int ajustados = 0, ordenados = 0, nuevos = 0, scripts = 0;
            foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                GameObject go = sr.gameObject;
                if (sr.sprite == null || go.name == "Sombra" || go.GetComponentInParent<Grid>() != null) continue;
                if (go.GetComponent<Camera>() != null || EsPersonaje(go)) continue;
                if (go.transform.parent != null && EsPersonaje(go.transform.parent.gameObject)) continue;

                foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                    if (EsOrdenAjeno(mb) && mb.enabled) { Undo.RecordObject(mb, "Orden ajeno"); mb.enabled = false; scripts++; }

                Bounds b = sr.sprite.bounds;                       // local (con pivote y PPU del sprite)
                Vector3 esc = go.transform.lossyScale;
                string nom = go.name.ToLowerInvariant();
                float frac = 0.40f;
                bool conocido = false;
                for (int i = 0; i < ClavesBase.Length; i++)
                    if (nom.Contains(ClavesBase[i])) { frac = FraccionBase[i]; conocido = true; break; }

                BoxCollider2D bc = go.GetComponent<BoxCollider2D>();
                if (bc == null && conocido)
                    foreach (string s in SolidosSinCollider)
                        if (nom.Contains(s)) { bc = Undo.AddComponent<BoxCollider2D>(go); nuevos++; break; }

                float ySort;
                if (bc != null && !bc.isTrigger)
                {
                    float hBase = b.size.y * frac;
                    Vector2 tam = new Vector2(b.size.x * 0.9f, hBase);
                    Vector2 off = new Vector2(b.center.x, b.min.y + hBase * 0.5f);
                    if ((bc.size - tam).sqrMagnitude > 1e-6f || (bc.offset - off).sqrMagnitude > 1e-6f)
                    {
                        Undo.RecordObject(bc, "Base del objeto");
                        bc.size = tam;
                        bc.offset = off;
                        ajustados++;
                    }
                    ySort = off.y;
                    Vector3 w0 = go.transform.TransformPoint(new Vector3(off.x - tam.x * 0.5f, off.y - tam.y * 0.5f, 0f));
                    Vector3 w1 = go.transform.TransformPoint(new Vector3(off.x + tam.x * 0.5f, off.y + tam.y * 0.5f, 0f));
                    mapa.huellas.Add(Rect.MinMaxRect(Mathf.Min(w0.x, w1.x), Mathf.Min(w0.y, w1.y), Mathf.Max(w0.x, w1.x), Mathf.Max(w0.y, w1.y)));
                }
                else ySort = b.min.y;

                Piso3V10OrdenVisual ov = go.GetComponent<Piso3V10OrdenVisual>();
                if (ov == null) ov = Undo.AddComponent<Piso3V10OrdenVisual>(go);
                int desfase = Mathf.RoundToInt(-ySort * esc.y * 100f);
                if (ov.dinamico || ov.desfase != desfase)
                {
                    Undo.RecordObject(ov, "Orden por la base");
                    ov.dinamico = false;
                    ov.desfase = desfase;
                    ordenados++;
                }
                ov.Aplicar();
            }
            if (scripts > 0) hecho.Add(scripts + " scripts de orden ajenos (YSort) desactivados");
            if (ajustados + nuevos > 0) hecho.Add((ajustados + nuevos) + " objetos con collider solo en la base (" + nuevos + " nuevos)");
            if (ordenados > 0) hecho.Add(ordenados + " objetos ordenados por su base");
        }

        // ------------------------------------------------------------------ 5. personajes
        private static Transform Marcador(string contiene)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.Contains(contiene)) return t;
            return null;
        }

        private static void Personajes(Mapa mapa, List<string> hecho, StringBuilder inf)
        {
            // --- Jason
            Piso3V10JasonAnimacion jasonCtrl = Object.FindFirstObjectByType<Piso3V10JasonAnimacion>();
            Transform entrada = Marcador("Entrada");
            Vector2 inicio = entrada != null ? (Vector2)entrada.position : mapa.limites.center;
            GameObject jason;
            if (jasonCtrl == null)
            {
                Piso3V10Movimiento m = Object.FindFirstObjectByType<Piso3V10Movimiento>();
                if (m != null) { jason = m.gameObject; Piso3V10Generador.PrepararJason(jason); }
                else
                {
                    Vector2 p;
                    mapa.LibreCercano(inicio, out p);
                    jason = Piso3V10Generador.CrearJason(p);
                    Undo.RegisterCreatedObjectUndo(jason, "Jason");
                    hecho.Add("Jason creado en la entrada " + p.ToString("F1"));
                }
            }
            else
            {
                jason = jasonCtrl.gameObject;
                Piso3V10Personaje8Dir pa = jason.GetComponent<Piso3V10Personaje8Dir>();
                Rigidbody2D rb0 = jason.GetComponent<Rigidbody2D>();
                if (pa == null || !pa.Tiene("Quieto") || !pa.Tiene("Caminar") || jason.GetComponent<Piso3V10Movimiento>() == null ||
                    rb0 == null || rb0.bodyType != RigidbodyType2D.Kinematic)
                {
                    Piso3V10Generador.PrepararJason(jason);
                    hecho.Add("Jason completado (Kinematic con barrido + animaciones)");
                }
            }
            if (!jason.activeSelf) { Undo.RecordObject(jason, "Activar"); jason.SetActive(true); }
            foreach (MonoBehaviour mb in jason.GetComponents<MonoBehaviour>())
                if (Piso3V10Movimiento.EsMovimientoAjeno(mb) && mb.enabled)
                {
                    Undo.RecordObject(mb, "Movimiento ajeno"); mb.enabled = false;
                    hecho.Add("desactivado '" + mb.GetType().Name + "' en Jason");
                }
            Vector2 pj = jason.transform.position;
            if (!mapa.Libre(pj))
            {
                Vector2 p;
                mapa.LibreCercano(mapa.Libre(inicio) ? inicio : (mapa.suelo.Contains(mapa.Celda(pj)) ? pj : inicio), out p);
                Undo.RecordObject(jason.transform, "Mover Jason");
                jason.transform.position = new Vector3(p.x, p.y, jason.transform.position.z);
                hecho.Add("Jason estaba en " + pj.ToString("F1") + (mapa.suelo.Contains(mapa.Celda(pj)) ? " (dentro de una pared/mueble)" : " (FUERA del mapa de tiles)") +
                          " → movido a " + p.ToString("F1"));
            }
            inf.Append("Jason en ").Append(((Vector2)jason.transform.position).ToString("F1")).Append(". ");

            // --- otros jugadores (de prueba)
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null || mb.gameObject == jason) continue;
                string n = mb.GetType().Name;
                bool jugadorPrueba = Piso3V10Movimiento.EsMovimientoAjeno(mb) || (n.Contains("Player") && n.Contains("Test"));
                if (!jugadorPrueba || !mb.gameObject.activeSelf) continue;
                if (mb.GetComponent<Piso3V10Zorbo>() != null) continue;
                Undo.RecordObject(mb.gameObject, "Apagar jugador de prueba");
                mb.gameObject.SetActive(false);
                hecho.Add("'" + mb.gameObject.name + "' (" + n + ", se movía sin colisiones) desactivado");
            }
            foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
                if (p != jason && p.activeSelf && p.GetComponent<Piso3V10Zorbo>() == null)
                {
                    Undo.RecordObject(p, "Apagar jugador"); p.SetActive(false);
                    hecho.Add("segundo jugador '" + p.name + "' desactivado");
                }

            // --- cámara
            Camara(jason, mapa, hecho);

            // --- depuración (tecla C)
            if (Object.FindFirstObjectByType<Piso3V10Depuracion>() == null)
            {
                GameObject dep = new GameObject("99_Depuracion (tecla C)");
                Undo.RegisterCreatedObjectUndo(dep, "Depuracion");
                dep.AddComponent<Piso3V10Depuracion>().jugador = jason.GetComponent<Piso3V10Movimiento>();
            }

            // --- Zorbos
            List<Transform> spawns = new List<Transform>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("Spawn_Zorbo")) spawns.Add(t);
            spawns.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            List<Vector2> ocupados = new List<Vector2> { (Vector2)jason.transform.position };
            Piso3V10Zorbo[] zorbos = Object.FindObjectsByType<Piso3V10Zorbo>(FindObjectsSortMode.None);
            System.Array.Sort(zorbos, (a, b) => string.CompareOrdinal(a.name, b.name));
            int i0 = 0;
            if (zorbos.Length == 0 && spawns.Count > 0)
            {
                GameObject raiz = GameObject.Find("07_Zorbos");
                if (raiz == null) { raiz = new GameObject("07_Zorbos"); Undo.RegisterCreatedObjectUndo(raiz, "Zorbos"); }
                for (int i = 0; i < ZorbosACrear; i++)
                {
                    Vector2 p;
                    if (!mapa.LibreCercano(PuntoSpawn(spawns, i), out p, ocupados)) continue;
                    ocupados.Add(p);
                    GameObject z = Piso3V10Generador.CrearZorbo("Zorbo", p, (i * 3) % 8);
                    z.name = "Zorbo_" + (i + 1).ToString("00");
                    z.transform.SetParent(raiz.transform, true);
                    Undo.RegisterCreatedObjectUndo(z, "Zorbo");
                }
                hecho.Add(ZorbosACrear + " Zorbos creados en los Spawn_Zorbo");
            }
            else
            {
                int movidos = 0, preparados = 0;
                foreach (Piso3V10Zorbo zo in zorbos)
                {
                    Piso3V10Personaje8Dir pa = zo.GetComponent<Piso3V10Personaje8Dir>();
                    CapsuleCollider2D cap = zo.GetComponent<CapsuleCollider2D>();
                    if (pa == null || !pa.Tiene("Quieto") || !(pa.Tiene("Caminar") || pa.Tiene("Correr")) || cap == null || Mathf.Abs(cap.size.x - 0.48f) > 0.001f)
                    {
                        Piso3V10Generador.PrepararZorbo(zo.gameObject, zo.gameObject.name.Contains("Zorbo2") ? "Zorbo2" : "Zorbo", zo.orientacion);
                        preparados++;
                    }
                    Vector2 pz = zo.transform.position;
                    if (mapa.Libre(pz) && (pz - (Vector2)jason.transform.position).sqrMagnitude > 9f) { ocupados.Add(pz); continue; }
                    Vector2 destino = spawns.Count > 0 ? PuntoSpawn(spawns, i0++) : mapa.limites.center;
                    Vector2 p;
                    if (!mapa.LibreCercano(destino, out p, ocupados)) continue;
                    ocupados.Add(p);
                    Undo.RecordObject(zo.transform, "Mover Zorbo");
                    zo.transform.position = new Vector3(p.x, p.y, zo.transform.position.z);
                    movidos++;
                }
                if (preparados > 0) hecho.Add(preparados + " Zorbos con animaciones nuevas");
                if (movidos > 0) hecho.Add(movidos + " Zorbos que estaban FUERA del mapa movidos a los Spawn_Zorbo");
            }
            inf.Append("Zorbos: ").Append(Object.FindObjectsByType<Piso3V10Zorbo>(FindObjectsSortMode.None).Length).Append(". ");
        }

        private static Vector2 PuntoSpawn(List<Transform> spawns, int i)
        {
            Transform s = spawns[i % spawns.Count];
            int vuelta = i / spawns.Count;
            Vector2[] anillo = { Vector2.zero, new Vector2(1.4f, 0.6f), new Vector2(-1.4f, -0.6f), new Vector2(0.6f, -1.4f) };
            return (Vector2)s.position + anillo[vuelta % anillo.Length];
        }

        private static readonly string[] CamposObjetivo = { "target", "objetivo", "player", "jugador", "follow", "seguir" };

        private static void Camara(GameObject jason, Mapa mapa, List<string> hecho)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;
            bool sigue = false;
            Piso3V10Camara propia = cam.GetComponent<Piso3V10Camara>();
            if (propia != null)
            {
                if (propia.objetivo != jason.transform || propia.limites != mapa.limites)
                {
                    Undo.RecordObject(propia, "Cámara");
                    propia.objetivo = jason.transform;
                    propia.limites = mapa.limites;
                    hecho.Add("cámara sigue a Jason");
                }
                sigue = true;
            }
            foreach (MonoBehaviour mb in cam.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb is Piso3V10Camara) continue;
                SerializedObject so = new SerializedObject(mb);
                SerializedProperty it = so.GetIterator();
                bool entrar = true;
                while (it.NextVisible(entrar))
                {
                    entrar = false;
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    string n = it.name.ToLowerInvariant().TrimStart('m', '_');
                    bool coincide = false;
                    foreach (string c in CamposObjetivo) if (n.Contains(c)) coincide = true;
                    if (!coincide) continue;
                    Object actual = it.objectReferenceValue;
                    if (actual == jason.transform || actual == jason) { sigue = true; continue; }
                    it.objectReferenceValue = jason.transform;
                    if (it.objectReferenceValue == null) it.objectReferenceValue = jason;
                    if (it.objectReferenceValue != null)
                    {
                        so.ApplyModifiedProperties();
                        sigue = true;
                        hecho.Add("cámara (" + mb.GetType().Name + "." + it.name + ") sigue a Jason");
                    }
                }
            }
            if (!sigue)
            {
                propia = Undo.AddComponent<Piso3V10Camara>(cam.gameObject);
                propia.objetivo = jason.transform;
                propia.limites = mapa.limites;
                hecho.Add("cámara con seguimiento a Jason");
            }
            Vector3 cp = cam.transform.position;
            Vector3 jp = jason.transform.position;
            if ((new Vector2(cp.x - jp.x, cp.y - jp.y)).sqrMagnitude > 0.01f)
            {
                Undo.RecordObject(cam.transform, "Cámara");
                cam.transform.position = new Vector3(jp.x, jp.y, cp.z);
            }
        }
    }
}
