using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem; // Necesario para leer el nuevo input system
using System.Collections.Generic;

public class PuertaNivel : MonoBehaviour
{
    [Header("Configuración de Nivel")]
    [Tooltip("Nombre exacto de la escena del siguiente piso")]
    public string nombreSiguienteEscena;

    [Header("UI Feedback")]
    public TextMeshProUGUI textoMensajeUI;
    public string mensajeFaltaLlave = "Debo buscar la llave primero";
    public float duracionMensaje = 2.5f;

    [Header("Input alternativo (opcional)")]
    [Tooltip("Se usa Gameplay/Player/Interact del PlayerInput en rango. Esta referencia sólo sirve si no hay PlayerInput.")]
    public InputActionReference accionInteractuar;

    private Coroutine rutinaMensajeActiva;
    private bool jugadorEnRango = false;
    private PlayerInventory inventarioJugador;
    private PlayerInput entradaJugador;
    private bool cargando;
    private readonly HashSet<Collider2D> contactos = new HashSet<Collider2D>();

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

        if (GameManager.Instance != null && inventarioJugador != null)
        {
            GameManager.Instance.AvanzarPiso();
            
            // Activamos la bandera para avisarle al siguiente piso que debe usar estos datos
            GameManager.Instance.usarDatosDeTransicion = true;

            // Extraer y guardar Puntos de Mejora
            PlayerExperience exp = inventarioJugador.GetComponent<PlayerExperience>();
            if (exp != null)
            {
                GameManager.Instance.puntosDeMejoraPendientes = exp.puntosDeMejora;
                GameManager.Instance.expActualPendiente = exp.experienciaActual;
                GameManager.Instance.nivelActualPendiente = exp.nivelActual;
            }

            // Extraer y guardar Inventario
            GameManager.Instance.vendasPendientes = inventarioJugador.vendasCount;
            GameManager.Instance.botiquinesPendientes = inventarioJugador.botiquinesCount;
            GameManager.Instance.estimulantesPendientes = inventarioJugador.estimulantesCount;

            // Extraer y guardar Salud
            PlayerHealth health = inventarioJugador.GetComponent<PlayerHealth>();
            if (health != null)
            {
                GameManager.Instance.vidaPendiente = health.currentHealth;
            }

            // Extraer y guardar Munición
            PlayerShooting shooting = inventarioJugador.GetComponent<PlayerShooting>();
            if (shooting != null)
            {
                GameManager.Instance.municionCargadorPendiente = shooting.currentAmmoInClip;
                GameManager.Instance.municionTotalPendiente = shooting.totalAmmo;
            }
        }

        SceneManager.LoadScene(nombreSiguienteEscena);
    }

    private void MostrarMensaje()
    {
        if (textoMensajeUI != null)
        {
            if (rutinaMensajeActiva != null)
            {
                StopCoroutine(rutinaMensajeActiva);
            }
            rutinaMensajeActiva = StartCoroutine(RutinaMostrarMensaje());
        }
    }

    private IEnumerator RutinaMostrarMensaje()
    {
        textoMensajeUI.text = mensajeFaltaLlave;
        textoMensajeUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(duracionMensaje);

        textoMensajeUI.gameObject.SetActive(false);
    }
}
