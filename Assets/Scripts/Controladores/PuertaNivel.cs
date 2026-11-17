using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem; // Necesario para leer el nuevo input system
using System.Collections.Generic;

public class PuertaNivel : MonoBehaviour
{
    [Header("Configuración de Nivel")]
    [Tooltip("Nombre exacto de la escena del siguiente piso")]
    public string nombreSiguienteEscena;
    [Tooltip("Desactivar en la recepción/prólogo para conservar el índice del primer piso.")]
    public bool avanzarContadorPiso = true;
    public bool JugadorEnRango => jugadorEnRango;
    public bool Cargando => cargando;
    private static readonly HashSet<PuertaNivel> activas = new HashSet<PuertaNivel>();
    public static IEnumerable<PuertaNivel> Activas => activas;

    [Header("UI Feedback")]
    public TextMeshProUGUI textoMensajeUI;
    public string mensajeFaltaLlave = "Debo buscar la llave primero";
    public float duracionMensaje = 2.5f;

    [Header("Input alternativo (opcional)")]
    [Tooltip("Se usa Gameplay/Player/Interact del PlayerInput en rango. Esta referencia sólo sirve si no hay PlayerInput.")]
    public InputActionReference accionInteractuar;

    private float finAviso;
    public bool AvisoVisible => Time.unscaledTime < finAviso && !DeathScreenController.GameplayBlocked;
    private bool jugadorEnRango = false;
    private PlayerInventory inventarioJugador;
    private PlayerInput entradaJugador;
    private bool cargando;
    private readonly HashSet<Collider2D> contactos = new HashSet<Collider2D>();
    private void OnEnable() { activas.Add(this); }
    private void OnDisable()
    {
        activas.Remove(this); contactos.Clear(); jugadorEnRango = false;
        inventarioJugador = null; entradaJugador = null;
    }

    private void Start()
    {
        if (textoMensajeUI != null)
        {
            textoMensajeUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Leer la copia de Gameplay que está habilitada y emparejada con este jugador.
        if (cargando || DeathScreenController.GameplayBlocked || !jugadorEnRango) return;
        var interaccion = entradaJugador != null ? entradaJugador.GetComponent<InteraccionJugador>() : null;
        if ((UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva) ||
            (interaccion != null && (interaccion.TieneObjetivo || interaccion.UltimoFotogramaInteraccion == Time.frameCount))) return;
        var accion = entradaJugador != null ? entradaJugador.currentActionMap?.FindAction("Interact", false) : accionInteractuar?.action;
        if (accion != null && accion.WasPressedThisFrame())
        {
            IntentarAbrirPuerta();
        }
    }

    private void IntentarAbrirPuerta()
    {
        if (cargando || !Application.CanStreamedLevelBeLoaded(nombreSiguienteEscena)) return;
        if (inventarioJugador != null)
        {
            // IntentarUsarLlave devuelve true si había llave y ya la restó
            if (inventarioJugador.IntentarUsarLlave())
            {
                cargando = true;
                AvanzarNivel();
            }
            else
            {
                MostrarMensaje();
            }
        }
    }

    // Detectar cuando el jugador entra en la zona de la puerta
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var inventario = collision.GetComponentInParent<PlayerInventory>();
        if (inventario != null && inventario.isActiveAndEnabled)
        {
            if (LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null &&
                inventario.gameObject != LevelManager.Instance.JugadorActual) return;
            contactos.Add(collision);
            jugadorEnRango = true;
            inventarioJugador = inventario;
            entradaJugador = inventario.GetComponent<PlayerInput>();
        }
    }

    // Detectar cuando el jugador se aleja de la puerta
    private void OnTriggerExit2D(Collider2D collision)
    {
        contactos.Remove(collision);
        if (contactos.Count == 0)
        {
            jugadorEnRango = false;
            inventarioJugador = null;
            entradaJugador = null;
        }
    }

    private void AvanzarNivel()
    {
        Debug.Log("Llave aceptada. Cargando el siguiente piso...");

        if (GameManager.Instance != null && avanzarContadorPiso) GameManager.Instance.AvanzarPiso();

        TransicionEntrePisos.CargarEscena(nombreSiguienteEscena);
    }

    private void MostrarMensaje()
    {
        // Conservar la referencia de TMP para las escenas antiguas, sin mostrar su franja gris.
        if (textoMensajeUI != null)
            textoMensajeUI.gameObject.SetActive(false);
        var avisoInteraccion = inventarioJugador != null ? inventarioJugador.GetComponent<AvisoInteraccionUI>() : null;
        // Sustituir el texto en el mismo panel de F; el aviso antiguo sólo cubre escenas sin ese panel.
        if ((avisoInteraccion != null && avisoInteraccion.PuedeMostrarAvisoPuerta(this)) ||
            AvisoPuertaUI.Mostrar(this, mensajeFaltaLlave, duracionMensaje))
            finAviso = Time.unscaledTime + Mathf.Max(.1f, duracionMensaje);
    }
}
