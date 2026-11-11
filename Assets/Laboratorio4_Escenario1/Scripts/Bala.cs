using UnityEngine;

namespace Laboratorio4
{
    /// <summary>
    /// Bala con barrido por rayo: cada frame lanza un rayo desde su posición anterior hasta la nueva,
    /// así nunca atraviesa un muro aunque vaya rápido. Viaja por el suelo (para chocar con las bases)
    /// y se dibuja a la altura del arma (perspectiva 3/4).
    /// </summary>
    public class Bala : MonoBehaviour
    {
        Vector2 dir;
        float velocidad, recorrido;
        Collider2D tirador;
        Sprite impacto;
        Transform visual;
        ContactFilter2D filtro;
        readonly RaycastHit2D[] golpes = new RaycastHit2D[8];
        const float Alcance = 18f;

        public static void Disparar(Vector2 pies, Vector2 boca, Vector2 dir, float velocidad, Sprite sprite, Sprite impacto, Collider2D tirador)
        {
            var go = new GameObject("Bala");
            go.transform.position = new Vector3(pies.x + boca.x, pies.y + (dir.y < 0f ? 0f : 0.02f), 0f);
            var b = go.AddComponent<Bala>();
            b.dir = dir; b.velocidad = velocidad; b.tirador = tirador; b.impacto = impacto;
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            v.transform.localPosition = new Vector3(0f, boca.y, 0f);
            if (dir.y < 0f) v.transform.localRotation = Quaternion.Euler(0, 0, -90f);
            else if (dir.x < 0f) v.transform.localRotation = Quaternion.Euler(0, 0, 180f);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 21000;
            b.visual = v.transform;
            b.filtro = new ContactFilter2D(); b.filtro.useTriggers = false; b.filtro.useLayerMask = false;
        }

        void Update()
        {
            float paso = velocidad * Time.deltaTime;
            Vector2 p = transform.position;
            int n = Physics2D.Raycast(p, dir, filtro, golpes, paso);
            float mejor = float.MaxValue; RaycastHit2D? golpe = null;
            for (int i = 0; i < n; i++)
            {
                var g = golpes[i];
                if (g.collider == null || g.collider == tirador || g.collider.isTrigger) continue;
                if (g.distance < mejor) { mejor = g.distance; golpe = g; }
            }
            if (golpe.HasValue)
            {
                var g = golpe.Value;
                var sorbo = g.collider.GetComponentInParent<SorboEnemigo>();
                if (sorbo != null) sorbo.Golpear();
                var viejo = g.collider.GetComponentInParent<SorboEstatico>();
                if (viejo != null) viejo.Golpear();
                Impacto(g.point);
                Destroy(gameObject);
                return;
            }
            transform.position = p + dir * paso;
            recorrido += paso;
            if (recorrido > Alcance) Destroy(gameObject);
        }

        void Impacto(Vector2 punto)
        {
            if (impacto == null) return;
            var go = new GameObject("Impacto");
            go.transform.position = new Vector3(punto.x, punto.y + (visual != null ? visual.localPosition.y : 0f), 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = impacto; sr.sortingOrder = 21001;
            Destroy(go, 0.12f);
        }
    }
}
