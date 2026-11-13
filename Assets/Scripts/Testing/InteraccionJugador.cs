using UnityEngine;
using UnityEngine.InputSystem;

public class InteraccionJugador : MonoBehaviour
{
    private BotiquinInteractable botiquinCercano;
    private NotaInteractable notaCercana;

    public void OnInteract()
    {
        // 1. Si hay una nota en pantalla, la prioridad absoluta de la tecla 'E' es cerrarla.
        if (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva)
        {
            UIManagerNotas.Instance.OcultarNota();
            return; // Salimos para no abrir botiquines accidentalmente al cerrar la nota
        }

        // 2. Si no hay notas en pantalla, intentamos interactuar con lo que tengamos enfrente
        if (notaCercana != null)
        {
            notaCercana.Interactuar();
        }
        else if (botiquinCercano != null)
        {
            botiquinCercano.Abrir();
            // botiquinCercano = null; // Descomenta si el botiquín se destruye al abrirlo
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactuable"))
        {
            // Verificamos qué tipo de objeto es
            BotiquinInteractable botiquin = collision.GetComponent<BotiquinInteractable>();
            if (botiquin != null)
            {
                botiquinCercano = botiquin;
            }

            NotaInteractable nota = collision.GetComponent<NotaInteractable>();
            if (nota != null)
            {
                notaCercana = nota;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactuable"))
        {
            if (collision.GetComponent<BotiquinInteractable>() != null)
            {
                botiquinCercano = null;
            }

            if (collision.GetComponent<NotaInteractable>() != null)
            {
                notaCercana = null;
                // Si el jugador se aleja mientras la nota estaba abierta, se cierra automáticamente
                if (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva)
                {
                    UIManagerNotas.Instance.OcultarNota();
                }
            }
        }
    }
}