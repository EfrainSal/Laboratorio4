using UnityEngine;
using UnityEngine.Tilemaps;

namespace LaboratorioYiba.Piso3V10
{
    /// <summary>
    /// V15 – Va en el tilemap de paredes de la escena MODULAR (Grid + tilemaps, p. ej. "03_Paredes_Colision").
    /// Al dar Play comprueba que TODAS las celdas de pared son sólidas y lo escribe en la Consola:
    /// "[Piso3 Modular] Paredes sólidas: 497/497 celdas". Si falta alguna, la nombra.
    /// En la vista Scene marca en rojo las celdas de pared (Gizmos activado).
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class Piso3ModularParedes : MonoBehaviour
    {
        [Tooltip("Dibujar las celdas de pared en la vista Scene.")]
        public bool verEnEscena = true;

        [HideInInspector] public string firmaFilas = "";
        [HideInInspector] public string firmaBordes = "";

        public int Solidas { get; private set; }
        public int Total { get; private set; }

        private void Start()
        {
            Physics2D.SyncTransforms();
            Tilemap tm = GetComponent<Tilemap>();
            ContactFilter2D f = new ContactFilter2D();
            f.useTriggers = false;
            Collider2D[] r = new Collider2D[8];
            int ok = 0, total = 0;
            string primeraMala = null;
            foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(p)) continue;
                total++;
                Vector3 c = tm.GetCellCenterWorld(p);
                int n = Physics2D.OverlapPoint(c, f, r);
                bool solida = false;
                for (int i = 0; i < n; i++)
                {
                    Rigidbody2D rb = r[i].attachedRigidbody;
                    if (rb == null || rb.bodyType == RigidbodyType2D.Static) { solida = true; break; }
                }
                if (solida) ok++;
                else if (primeraMala == null) primeraMala = p.x + "," + p.y;
            }
            Solidas = ok;
            Total = total;
            if (ok == total)
                Debug.Log("[Piso3 Modular] Paredes sólidas: " + ok + "/" + total + " celdas.", this);
            else
                Debug.LogError("[Piso3 Modular] Hay celdas de pared SIN colisión: " + (total - ok) + " de " + total +
                               " (primera en la celda " + primeraMala + "). Usa el menú Laboratorio Yiba > Piso 3 V10 > " +
                               "9 - Arreglar escena modular (tilemaps).", this);
        }

        private void OnDrawGizmos()
        {
            if (!verEnEscena) return;
            Tilemap tm = GetComponent<Tilemap>();
            if (tm == null) return;
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.55f);
            Vector3 tam = Vector3.Scale(tm.layoutGrid != null ? tm.layoutGrid.cellSize : Vector3.one, transform.lossyScale);
            foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
                if (tm.HasTile(p)) Gizmos.DrawWireCube(tm.GetCellCenterWorld(p), tam);
        }
    }
}
