using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ProximidadCompaneroFinal : MonoBehaviour
{
    public ComicFinal comic;
    private void OnTriggerEnter2D(Collider2D otro) { Detectar(otro); }
    private void OnTriggerStay2D(Collider2D otro) { Detectar(otro); }
    private void Detectar(Collider2D otro)
    {
        if (comic == null || LevelManager.Instance == null) return;
        var salud = otro.GetComponentInParent<PlayerHealth>();
        if (salud != null && !salud.IsDead && salud.gameObject == LevelManager.Instance.JugadorActual)
            comic.NotificarProximidad();
    }
}
