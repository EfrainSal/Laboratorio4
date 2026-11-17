using UnityEngine;

public class NotaInteractable : MonoBehaviour
{
    [Header("Contenido")]
    [Tooltip("La imagen de alta resolución que se mostrará en pantalla al leer esta nota")]
    public Sprite spriteNotaAltaResolucion;

    private bool yaLeida;
    public bool YaLeida => yaLeida;
    public string IdPersistencia { get; private set; }

    private void Awake()
    {
        if (string.IsNullOrEmpty(IdPersistencia))
        {
            IdPersistencia = LevelManager.GenerarIdObjetoEscena(gameObject);
        }

        if (GetComponent<BrilloObjeto>() == null)
        {
            gameObject.AddComponent<BrilloObjeto>();
        }
    }

    public void MarcarComoLeida()
    {
        yaLeida = true;
    }

    // Esta función la llamará el jugador
    public void Interactuar()
    {
        if (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva)
        {
            UIManagerNotas.Instance.OcultarNota();
            return;
        }

        if (UIManagerNotas.Instance != null)
        {
            UIManagerNotas.Instance.MostrarNota(spriteNotaAltaResolucion);
        }

        if (!yaLeida)
        {
            yaLeida = true;
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegistrarNotaLeida(this);
            }
        }
    }
}
