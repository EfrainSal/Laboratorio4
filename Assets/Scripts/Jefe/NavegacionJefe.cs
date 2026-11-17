using System.Collections.Generic;
using UnityEngine;

// A* de ocho vecinos, como la navegación de los Sorbos, pero con rejilla propia por jefe/escena
// y despeje calculado con su collider de pies. No comparte la caché de los enemigos pequeños.
[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
public class NavegacionJefe : MonoBehaviour
{
    [Min(.15f)] public float celda = .3f;
    [Min(0)] public float margen = .07f;
    [Min(.1f)] public float intervaloRuta = .45f;
    public int RutasCalculadas { get; private set; }
    public bool TieneRuta => ruta.Count > 0;
    private readonly List<Vector2> ruta = new List<Vector2>();
    private readonly Collider2D[] solapes = new Collider2D[32];
    private readonly RaycastHit2D[] impactos = new RaycastHit2D[32];
    private ContactFilter2D filtro;
    private Vector2 origen, cuerpo, offset, ultimoObjetivo;
    private int ancho, alto, siguiente;
    private bool[] libre;
    private float proximaRuta, vigilarAtasco;
    private Vector2 ultimaPosicion;
    private Rigidbody2D rb;
    private void Awake() { rb = GetComponent<Rigidbody2D>(); filtro = new ContactFilter2D { useTriggers = false }; }
    private bool Obstaculo(Collider2D c) => c != null && c.enabled && !c.isTrigger &&
        c.gameObject.scene == gameObject.scene && (c.attachedRigidbody == null || c.attachedRigidbody.bodyType == RigidbodyType2D.Static);
    public void Reconstruir()
    {
        Physics2D.SyncTransforms();
        var col = GetComponent<Collider2D>();
        cuerpo = (Vector2)col.bounds.size + Vector2.one * margen * 2;
        offset = (Vector2)col.bounds.center - (Vector2)transform.position;
        Bounds bounds = default; bool hay = false;
        foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            if (Obstaculo(c)) { if (!hay) { bounds = c.bounds; hay = true; } else bounds.Encapsulate(c.bounds); }
        if (!hay) { libre = null; return; }
        float paso = Mathf.Max(celda, Mathf.Sqrt(bounds.size.x * bounds.size.y / 60000));
        celda = paso; origen = (Vector2)bounds.min + Vector2.one * paso * .5f;
        ancho = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / paso)); alto = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / paso));
        libre = new bool[ancho * alto];
        for (int y = 0; y < alto; y++) for (int x = 0; x < ancho; x++)
        {
            int n = Physics2D.OverlapBox(Centro(y * ancho + x) + offset, cuerpo, 0, filtro, solapes);
            bool despejado = n < solapes.Length;
            for (int i = 0; i < n && despejado; i++) if (Obstaculo(solapes[i])) despejado = false;
            libre[y * ancho + x] = despejado;
        }
        ruta.Clear(); siguiente = 0; proximaRuta = 0; ultimaPosicion = rb.position;
    }
    private Vector2 Centro(int indice) => origen + new Vector2(indice % ancho, indice / ancho) * celda;
    private bool Libre(int x, int y) => x >= 0 && y >= 0 && x < ancho && y < alto && libre[y * ancho + x];
    private int Cercana(Vector2 punto)
    {
        int x = Mathf.RoundToInt((punto.x - origen.x) / celda), y = Mathf.RoundToInt((punto.y - origen.y) / celda);
        for (int radio = 0; radio <= 8; radio++)
        {
            int mejor = -1; float distancia = float.PositiveInfinity;
            for (int dy = -radio; dy <= radio; dy++) for (int dx = -radio; dx <= radio; dx++)
            {
                if (!Libre(x + dx, y + dy)) continue;
                int i = (y + dy) * ancho + x + dx; float d = (Centro(i) - punto).sqrMagnitude;
                if (d < distancia) { mejor = i; distancia = d; }
            }
            if (mejor >= 0) return mejor;
        }
        return -1;
    }
    public bool PasoLibre(Vector2 desde, Vector2 hasta)
    {
        if (cuerpo == Vector2.zero)
        {
            var col = GetComponent<Collider2D>(); cuerpo = (Vector2)col.bounds.size + Vector2.one * margen * 2;
            offset = (Vector2)col.bounds.center - (Vector2)transform.position;
        }
        var delta = hasta - desde;
        if (delta.sqrMagnitude < .00001f) return true;
        int n = Physics2D.BoxCast(desde + offset, cuerpo, 0, delta.normalized, filtro, impactos, delta.magnitude);
        if (n == impactos.Length) return false;
        for (int i = 0; i < n; i++) if (Obstaculo(impactos[i].collider)) return false;
        return true;
    }
    public bool PasoFisicoLibre(Vector2 desde, Vector2 hasta)
    {
        Vector2 delta = hasta - desde; if (delta.sqrMagnitude < .00001f) return true;
        // Para salir de un roce se usa el cuerpo real: el margen de A* no debe convertirse en una pared invisible.
        Vector2 tamano = cuerpo - Vector2.one * margen * 2;
        int n = Physics2D.BoxCast(desde + offset, tamano, 0, delta.normalized, filtro, impactos, delta.magnitude);
        for (int i = 0; i < n; i++)
        {
            var c = impactos[i].collider; if (!Obstaculo(c)) continue;
            Vector2 afuera = desde + offset - c.ClosestPoint(desde + offset);
            if (impactos[i].distance <= .001f && afuera.sqrMagnitude > .000001f && Vector2.Dot(delta, afuera) >= -.000001f) continue;
            return false;
        }
        return n < impactos.Length;
    }
    public Vector2 Direccion(Vector2 destino)
    {
        Vector2 actual = rb.position;
        if (PasoLibre(actual, destino)) { ruta.Clear(); return (destino - actual).normalized; }
        if (libre == null) Reconstruir();
        if (libre == null) return Vector2.zero;
        vigilarAtasco += Time.fixedDeltaTime;
        if (vigilarAtasco >= .55f)
        {
            if ((actual - ultimaPosicion).sqrMagnitude < .01f) proximaRuta = 0;
            ultimaPosicion = actual; vigilarAtasco = 0;
        }
        if (Time.time >= proximaRuta || (destino - ultimoObjetivo).sqrMagnitude > .64f)
        { Calcular(actual, destino); proximaRuta = Time.time + intervaloRuta; ultimoObjetivo = destino; }
        while (siguiente < ruta.Count && (ruta[siguiente] - actual).sqrMagnitude < .04f) siguiente++;
        if (siguiente >= ruta.Count) return Vector2.zero;
        // Mirar adelante sólo por segmentos que admiten el cuerpo completo; nunca cortar una esquina.
        int visible = siguiente;
        for (int i = siguiente + 1; i < Mathf.Min(ruta.Count, siguiente + 24); i++)
            if (PasoLibre(actual, ruta[i])) visible = i;
        siguiente = visible;
        return (ruta[siguiente] - actual).normalized;
    }
    public bool Calcular(Vector2 desde, Vector2 hasta)
    {
        if (libre == null) Reconstruir();
        ruta.Clear(); siguiente = 0; RutasCalculadas++;
        if (libre == null) return false;
        int inicio = Cercana(desde), fin = Cercana(hasta);
        if (inicio < 0 || fin < 0) return false;
        var coste = new Dictionary<int, float> { [inicio] = 0 };
        var padre = new Dictionary<int, int>(); var cerrados = new HashSet<int>();
        var heap = new List<KeyValuePair<float, int>>(); Push(heap, H(inicio, fin), inicio);
        int mejor = inicio, expandidos = 0;
        while (heap.Count > 0 && expandidos++ < 24000)
        {
            int actual = Pop(heap); if (!cerrados.Add(actual)) continue;
            if (H(actual, fin) < H(mejor, fin)) mejor = actual;
            if (actual == fin) { mejor = fin; break; }
            int x = actual % ancho, y = actual / ancho;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if ((dx == 0 && dy == 0) || !Libre(x + dx, y + dy)) continue;
                if (dx != 0 && dy != 0 && (!Libre(x + dx, y) || !Libre(x, y + dy))) continue;
                int vecino = (y + dy) * ancho + x + dx;
                float g = coste[actual] + (dx != 0 && dy != 0 ? 1.4142f : 1);
                if (cerrados.Contains(vecino) || (coste.TryGetValue(vecino, out float previo) && previo <= g)) continue;
                coste[vecino] = g; padre[vecino] = actual; Push(heap, g + H(vecino, fin), vecino);
            }
        }
        if (mejor == inicio) { if (inicio == fin) ruta.Add(Centro(fin)); return ruta.Count > 0; }
        for (int n = mejor; n != inicio; n = padre[n]) ruta.Add(Centro(n));
        ruta.Reverse(); return true;
    }
    private float H(int a, int b)
    {
        int dx = Mathf.Abs(a % ancho - b % ancho), dy = Mathf.Abs(a / ancho - b / ancho);
        return dx + dy + (1.4142f - 2) * Mathf.Min(dx, dy);
    }
    private static void Push(List<KeyValuePair<float, int>> heap, float prioridad, int nodo)
    {
        var item = new KeyValuePair<float, int>(prioridad, nodo); heap.Add(item); int i = heap.Count - 1;
        while (i > 0) { int p = (i - 1) / 2; if (heap[p].Key <= item.Key) break; heap[i] = heap[p]; i = p; } heap[i] = item;
    }
    private static int Pop(List<KeyValuePair<float, int>> heap)
    {
        int resultado = heap[0].Value; var ultimo = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
        if (heap.Count == 0) return resultado;
        int i = 0;
        while (i * 2 + 1 < heap.Count)
        {
            int h = i * 2 + 1;
            if (h + 1 < heap.Count && heap[h + 1].Key < heap[h].Key) h++;
            if (ultimo.Key <= heap[h].Key) break;
            heap[i] = heap[h]; i = h;
        }
        heap[i] = ultimo; return resultado;
    }
}
