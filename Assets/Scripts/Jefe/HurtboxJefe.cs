using UnityEngine;

// Sólo recepción de daño. La navegación continúa utilizando exclusivamente el collider de pies.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public class HurtboxJefe : MonoBehaviour
{
    public BossHealth salud;
    private BoxCollider2D caja;
    private SpriteRenderer cuerpo;
    public static HurtboxJefe Asegurar(BossHealth jefe)
    {
        var h = jefe.GetComponentInChildren<HurtboxJefe>(true);
        if (h == null)
        {
            var go = new GameObject("HurtboxCuerpoCompleto"); go.transform.SetParent(jefe.transform, false);
            go.layer = jefe.gameObject.layer; go.tag = jefe.gameObject.tag;
            h = go.AddComponent<HurtboxJefe>();
        }
        h.salud = jefe; h.Ajustar(); return h;
    }
    private void Awake() { Ajustar(); }
    private void LateUpdate() { Ajustar(); }
    public void Ajustar()
    {
        if (salud == null) salud = GetComponentInParent<BossHealth>();
        if (salud == null) return;
        if (caja == null) caja = GetComponent<BoxCollider2D>();
        if (cuerpo == null) cuerpo = salud.GetComponent<SpriteRenderer>();
        caja.isTrigger = true;
        if (cuerpo != null && cuerpo.sprite != null)
        {
            var limites = cuerpo.sprite.bounds;
            caja.offset = limites.center;
            caja.size = new Vector2(limites.size.x * .78f, limites.size.y * .98f);
        }
        caja.enabled = !salud.IsDead;
    }
}
