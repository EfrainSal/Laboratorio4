using UnityEngine;

public class NotaInteractable : MonoBehaviour
{
    [Header("Contenido")]
    [Tooltip("La imagen de alta resolución que se mostrará en pantalla al leer esta nota")]
    public Sprite spriteNotaAltaResolucion;

    private void Awake()
    {
        if (GetComponent<BrilloObjeto>() == null)
        {
            gameObject.AddComponent<BrilloObjeto>();
        }
    }

    // Esta función la llamará el jugador
    public void Interactuar()
    {
        if (UIManagerNotas.Instance == null) return;

        // Si la nota ya está en pantalla, la cerramos. Si no, la abrimos.
        if (UIManagerNotas.Instance.NotaActiva)
        {
            UIManagerNotas.Instance.OcultarNota();
        }
        else
        {
            UIManagerNotas.Instance.MostrarNota(spriteNotaAltaResolucion);
        }
    }
}