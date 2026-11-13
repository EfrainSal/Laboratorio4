using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BotiquinInteractable : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("El sprite del botiquín abierto")]
    public Sprite spriteAbierto;
    
    [Header("Recompensas")]
    [Tooltip("El prefab del objeto que va a soltar (ej. una cura o un arma)")]
    public GameObject objetoASoltar;
    [Tooltip("Distancia hacia abajo donde aparecerá el objeto")]
    public float distanciaSoltar = 0.5f;

    private SpriteRenderer spriteRenderer;
    private bool yaAbierto = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Este método será llamado por el jugador cuando presione la 'E'
    public void Abrir()
    {
        // Si ya está abierto, no hacemos nada
        if (yaAbierto) return;

        yaAbierto = true;

        // 1. Cambiar el sprite rápidamente
        if (spriteAbierto != null)
        {
            spriteRenderer.sprite = spriteAbierto;
        }

        // 2. Soltar el objeto cerca por delante (hacia abajo en top-down 2D)
        if (objetoASoltar != null)
        {
            Vector3 posicionCaida = transform.position + new Vector3(0, -distanciaSoltar, 0);
            Instantiate(objetoASoltar, posicionCaida, Quaternion.identity);
        }
        
        // Opcional: Desactivar el collider para que el jugador no siga detectándolo
        GetComponent<BoxCollider2D>().enabled = false;
    }
}