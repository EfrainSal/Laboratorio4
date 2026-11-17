using UnityEngine;
using System.Collections.Generic;

public class InteraccionJugador : MonoBehaviour
{
    private BotiquinInteractable botiquinCercano;
    private NotaInteractable notaCercana;
    private NotaInteractable notaAbierta;
    private readonly HashSet<Collider2D> contactos = new HashSet<Collider2D>();
    public BotiquinInteractable BotiquinCercano => botiquinCercano;
    public NotaInteractable NotaCercana => notaCercana;
    public bool TieneObjetivo => notaCercana != null || (botiquinCercano != null && !botiquinCercano.YaAbierto);
    public int UltimoFotogramaInteraccion { get; private set; } = -1;
    private void Update() { ActualizarCercanos(); }

    public void OnInteract()
    {
        if (DeathScreenController.GameplayBlocked ||
            (LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null && LevelManager.Instance.JugadorActual != gameObject)) return;
        ActualizarCercanos();
        // Una nota abierta consume F; no abre también una puerta o un contenedor.
        if (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva)
        {
            UltimoFotogramaInteraccion = Time.frameCount;
            UIManagerNotas.Instance.OcultarNota();
            notaAbierta = null;
            return; // Salimos para no abrir botiquines accidentalmente al cerrar la nota
        }

        // 2. Si no hay notas en pantalla, intentamos interactuar con lo que tengamos enfrente
        if (notaCercana != null)
        {
            UltimoFotogramaInteraccion = Time.frameCount;
            notaCercana.Interactuar();
            notaAbierta = notaCercana;
        }
        else if (botiquinCercano != null && !botiquinCercano.YaAbierto)
        {
            UltimoFotogramaInteraccion = Time.frameCount;
            botiquinCercano.Abrir();
            ActualizarCercanos();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<BotiquinInteractable>() == null && collision.GetComponentInParent<NotaInteractable>() == null) return;
        contactos.Add(collision); ActualizarCercanos();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // El jugador tiene más de un collider: salir con uno no termina el otro contacto.
        if (collision != null && collision.enabled && collision.gameObject.activeInHierarchy)
            foreach (var propio in GetComponents<Collider2D>())
                if (propio.enabled && Physics2D.Distance(propio, collision).distance <= .001f) return;
        contactos.Remove(collision); ActualizarCercanos();
    }
    private void ActualizarCercanos()
    {
        contactos.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        notaCercana = null; botiquinCercano = null;
        float distanciaNota = float.MaxValue, distanciaBotiquin = float.MaxValue; bool notaAbiertaEnRango = false;
        foreach (var col in contactos)
        {
            var nota = col.GetComponentInParent<NotaInteractable>();
            float d = ((Vector2)col.bounds.ClosestPoint(transform.position) - (Vector2)transform.position).sqrMagnitude;
            if (nota != null && nota.isActiveAndEnabled)
            {
                if (nota == notaAbierta) notaAbiertaEnRango = true;
                if (d < distanciaNota) { notaCercana = nota; distanciaNota = d; }
            }
            var botiquin = col.GetComponentInParent<BotiquinInteractable>();
            if (botiquin != null && botiquin.isActiveAndEnabled && !botiquin.YaAbierto && d < distanciaBotiquin)
            { botiquinCercano = botiquin; distanciaBotiquin = d; }
        }
        if (notaAbierta != null && !notaAbiertaEnRango)
        {
            if (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva) UIManagerNotas.Instance.OcultarNota();
            notaAbierta = null;
        }
    }
    private void OnDisable()
    {
        contactos.Clear(); notaCercana = null; botiquinCercano = null;
        if (notaAbierta != null && UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva) UIManagerNotas.Instance.OcultarNota();
        notaAbierta = null;
    }
}
