using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem; // Necesario para leer el nuevo input system

public class PuertaNivel : MonoBehaviour
{
    [Header("Configuración de Nivel")]
    [Tooltip("Nombre exacto de la escena del siguiente piso")]
    public string nombreSiguienteEscena;

    [Header("UI Feedback")]
    public TextMeshProUGUI textoMensajeUI;
    public string mensajeFaltaLlave = "Debo buscar la llave primero";
    public float duracionMensaje = 2.5f;

    [Header("Input (Asignar desde el Inspector)")]
    [Tooltip("Arrastra aquí la acción 'Interactuar' de tus PlayerControls")]
    public InputActionReference accionInteractuar;

    private Coroutine rutinaMensajeActiva;
    private bool jugadorEnRango = false;
    private PlayerInventory inventarioJugador;

    private void Start()
    {
        if (textoMensajeUI != null)
        {
            textoMensajeUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Si el jugador está cerca y presiona el botón configurado (E o Botón A)
        if (jugadorEnRango && accionInteractuar != null && accionInteractuar.action.WasPressedThisFrame())
        {
            IntentarAbrirPuerta();
        }
    }

    private void IntentarAbrirPuerta()
    {
        if (inventarioJugador != null)
        {
            // IntentarUsarLlave devuelve true si había llave y ya la restó
            if (inventarioJugador.IntentarUsarLlave())
            {
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
        if (collision.CompareTag("Player"))
        {
            jugadorEnRango = true;
            inventarioJugador = collision.GetComponent<PlayerInventory>();
        }
    }

    // Detectar cuando el jugador se aleja de la puerta
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorEnRango = false;
            inventarioJugador = null;
        }
    }

    private void AvanzarNivel()
    {
        Debug.Log("Llave aceptada. Cargando el siguiente piso...");
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