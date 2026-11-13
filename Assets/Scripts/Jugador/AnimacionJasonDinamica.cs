using UnityEngine;
using Laboratorio4.Animacion;

// Conserva clase y GUID para las referencias anteriores al merge.
[RequireComponent(typeof(SpriteRenderer), typeof(MovimientoPlayer), typeof(AnimadorSprites8Direcciones))]
public class AnimacionJasonDinamica : MonoBehaviour
{
    [HideInInspector] public string rutaBaseSprites = "Art/Personajes/Jason";
    [HideInInspector] public float fpsCaminar = 10f;
    [HideInInspector] public float fpsDisparo = 14f;
    private AnimadorSprites8Direcciones anim;
    private MovimientoPlayer movimiento;
    private PlayerHealth salud;
    private string accion;
    private int direccion;
    private bool muerto;

    private void Awake()
    {
        anim = GetComponent<AnimadorSprites8Direcciones>();
        movimiento = GetComponent<MovimientoPlayer>();
        salud = GetComponent<PlayerHealth>();
    }

    private void LateUpdate()
    {
        if (muerto || (salud != null && salud.IsDead)) return;
        Vector2 mirada = movimiento.DireccionMirada;
        if (mirada.sqrMagnitude > 0.001f) direccion = Direccion8(mirada);
        if (accion != null && !anim.Terminada) return;
        accion = null;
        anim.Reproducir(movimiento.DireccionEntrada.sqrMagnitude > 0.01f ? "Caminar" : "Quieto", direccion);
        GetComponent<SpriteRenderer>().flipX = false;
    }

    // Solo acciones confirmadas: respeta municion y cooldowns.
    public void ReproducirAccion(string nombre)
    {
        if (nombre == "Disparo") return;
        if (muerto || !isActiveAndEnabled) return;
        if (anim == null) anim = GetComponent<AnimadorSprites8Direcciones>();
        if (!anim.Tiene(nombre)) return;
        Vector2 mirada = movimiento != null ? movimiento.DireccionMirada : Vector2.down;
        if (mirada.sqrMagnitude > 0.001f) direccion = Direccion8(mirada);
        accion = nombre;
        anim.Reproducir(nombre, direccion, true);
    }

    public void Morir()
    {
        muerto = true;
        anim.Reproducir("Muerte", direccion, true);
    }

    public static int Direccion8(Vector2 direccion)
    {
        return (Mathf.RoundToInt(Mathf.Atan2(direccion.x, -direccion.y) * Mathf.Rad2Deg / 45f) + 8) % 8;
    }
}
