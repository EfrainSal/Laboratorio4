using UnityEngine;

public class AmmoDrop : MonoBehaviour
{
    [Header("Configuración del Drop")]
    public int minAmmo = 2; // Mínimo de balas que da
    public int maxAmmo = 5; // Máximo de balas que da
    [Header("Audio de recogida (canal SFX del jugador)")]
    public AudioClip sonidoRecogida;
    [Range(0, 1)] public float volumenRecogida = 1f;
    private bool recogido;
    public string IdPersistencia { get; set; }

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

    private void OnTriggerEnter2D(Collider2D collision)
    { IntentarRecoger(collision); }
    private void OnTriggerStay2D(Collider2D collision)
    { IntentarRecoger(collision); }
    private void IntentarRecoger(Collider2D collision)
    {
        if (recogido || DeathScreenController.GameplayBlocked) return;

        // Si el jugador lo toca
        var playerShooting = collision.GetComponentInParent<PlayerShooting>();
        if (playerShooting != null && playerShooting.GetComponent<AyudanteJefeAI>() == null &&
            (LevelManager.Instance == null || LevelManager.Instance.JugadorActual == playerShooting.gameObject))
        {
            // Buscamos el script de disparo del jugador
            if (playerShooting != null)
            {
                recogido = true;
                // Calculamos balas aleatorias (maxAmmo + 1 porque Random.Range con enteros excluye el máximo)
                int ammoToGive = Random.Range(minAmmo, maxAmmo + 1);
                playerShooting.AddAmmo(ammoToGive);
                var inventario = playerShooting.GetComponent<PlayerInventory>();
                var clip = sonidoRecogida != null ? sonidoRecogida : inventario != null ? inventario.sfxRecogerObjeto : null;
                var fuente = inventario != null && inventario.audioSource != null ? inventario.audioSource : playerShooting.GetComponent<AudioSource>();
                // La fuente pertenece al jugador: destruir el drop no corta el sonido ni evita su mezcla SFX.
                if (fuente != null && clip != null) fuente.PlayOneShot(clip, volumenRecogida);
                
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.RegistrarObjetoRecogido(IdPersistencia);
                }

                // Destruimos el cuadro gris
                Destroy(gameObject);
            }
        }
    }
}
