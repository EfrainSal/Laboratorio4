using UnityEngine;

public static class ColisionesCombate
{
    public static bool EsNota(Collider2D c) => c != null && c.GetComponentInParent<NotaInteractable>() != null;
    public static bool LineaLibre(Vector2 desde, Vector2 hasta, Transform atacante, Transform objetivo, LayerMask capas)
    {
        float distanciaObjetivo = Vector2.Distance(desde, hasta);
        foreach (var hit in Physics2D.LinecastAll(desde, hasta, capas))
        {
            var c = hit.collider;
            if (c == null || c.isTrigger || c.transform.IsChildOf(atacante) || c.transform.IsChildOf(objetivo) || EsNota(c)) continue;
            if (c.GetComponentInParent<PlayerHealth>() != null || c.GetComponentInParent<EnemyHealth>() != null ||
                c.GetComponentInParent<BossHealth>() != null || c.GetComponentInParent<AyudanteJefeAI>() != null || c.CompareTag("Drop")) continue;
            // Un muro tangente al extremo del corte o detrás del enemigo no tapa su cara expuesta.
            if (distanciaObjetivo > .005f && hit.distance >= distanciaObjetivo - .005f) continue;
            return false;
        }
        return true;
    }
}
