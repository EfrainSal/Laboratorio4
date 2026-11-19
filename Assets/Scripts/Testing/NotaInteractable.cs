using UnityEngine;

public class NotaInteractable : MonoBehaviour
{
    [Header("Contenido")]
    [Tooltip("La imagen de alta resolución que se mostrará en pantalla al leer esta nota")]
    public Sprite spriteNotaAltaResolucion;
    public string tituloNota;
    [TextArea(5, 16)] public string textoNota;

    private bool yaLeida;
    public bool YaLeida => yaLeida;
    public string IdPersistencia { get; private set; }

    private void Awake()
    {
        foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.isTrigger = true;
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
        GetComponent<BrilloObjeto>()?.DesactivarBrillo();
    }

    // Esta función la llamará el jugador
    public void Interactuar()
    {
        if (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva)
        {
            UIManagerNotas.Instance.OcultarNota();
            return;
        }

        if (UIManagerNotas.Instance == null || !UIManagerNotas.Instance.MostrarNota(this)) return;

        if (!yaLeida)
        {
            yaLeida = true;
            GetComponent<BrilloObjeto>()?.DesactivarBrillo();
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegistrarNotaLeida(this);
            }
        }
    }
}
