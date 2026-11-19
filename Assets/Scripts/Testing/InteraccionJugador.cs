using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InteraccionJugador : MonoBehaviour
{
    private BotiquinInteractable botiquinCercano;
    private NotaInteractable notaCercana;
    private NotaInteractable notaAbierta;
    private readonly HashSet<Collider2D> contactos = new HashSet<Collider2D>();
    private readonly HashSet<NotaInteractable> notasCerradasEnRango = new HashSet<NotaInteractable>();
    public Object currentInteractable { get; private set; }
    public BotiquinInteractable BotiquinCercano => botiquinCercano;
    public NotaInteractable NotaCercana => notaCercana;
    public bool TieneObjetivo => notaCercana != null || (botiquinCercano != null && !botiquinCercano.YaAbierto);
    public int UltimoFotogramaInteraccion { get; private set; } = -1;
    private void Update() { ActualizarCercanos(); }
    private void OnEnable() { UIManagerNotas.AlCerrarNota += AlCerrarNota; }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed) IntentarInteractuar();
    }
    public void IntentarInteractuar()
    {
        if (UltimoFotogramaInteraccion == Time.frameCount || DeathScreenController.GameplayBlocked ||
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
            var nota = notaCercana;
            nota.Interactuar();
            notaAbierta = UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva ? nota : null;
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
        if (!EnContactoReal(collision)) contactos.Remove(collision);
        ActualizarCercanos();
    }
    private bool EnContactoReal(Collider2D otro)
    {
        if (otro == null || !otro.enabled || !otro.gameObject.activeInHierarchy) return false;
        foreach (var propio in GetComponents<Collider2D>())
            if (propio.enabled && propio.gameObject.activeInHierarchy && Physics2D.Distance(propio, otro).distance <= .001f) return true;
        return false;
    }
    private void AlCerrarNota(NotaInteractable nota)
    {
        if (notaAbierta == null && (nota == null || notaCercana != nota)) return;
        var cerrada = nota != null ? nota : notaAbierta;
        if (cerrada != null) notasCerradasEnRango.Add(cerrada);
        notaAbierta = null; notaCercana = null; currentInteractable = null;
        UltimoFotogramaInteraccion = Time.frameCount;
    }
    private void ActualizarCercanos()
    {
        // También comprobar geometría: un Exit perdido o varios colliders no pueden retener una nota lejana.
        contactos.RemoveWhere(c => !EnContactoReal(c));
        notasCerradasEnRango.RemoveWhere(n => {
            if (n == null) return true;
            foreach (var c in contactos) if (c.GetComponentInParent<NotaInteractable>() == n) return false;
            return true;
        });
        notaCercana = null; botiquinCercano = null;
        float distanciaNota = float.MaxValue, distanciaBotiquin = float.MaxValue; bool notaAbiertaEnRango = false;
        foreach (var col in contactos)
        {
            var nota = col.GetComponentInParent<NotaInteractable>();
            float d = ((Vector2)col.bounds.ClosestPoint(transform.position) - (Vector2)transform.position).sqrMagnitude;
            if (nota != null && nota.isActiveAndEnabled)
            {
                if (nota == notaAbierta) notaAbiertaEnRango = true;
                if (!notasCerradasEnRango.Contains(nota) && d < distanciaNota) { notaCercana = nota; distanciaNota = d; }
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
        currentInteractable = notaCercana != null ? (Object)notaCercana : botiquinCercano;
    }
    private void OnDisable()
    {
        UIManagerNotas.AlCerrarNota -= AlCerrarNota;
        contactos.Clear(); notasCerradasEnRango.Clear(); notaCercana = null; botiquinCercano = null; currentInteractable = null;
        if (notaAbierta != null && UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva) UIManagerNotas.Instance.OcultarNota();
        notaAbierta = null;
    }
}
