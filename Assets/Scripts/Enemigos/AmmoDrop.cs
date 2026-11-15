using UnityEngine;

public class AmmoDrop : MonoBehaviour
{
    [Header("Configuración del Drop")]
    public int minAmmo = 2; // Mínimo de balas que da
    public int maxAmmo = 5; // Máximo de balas que da
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
    {
        if (recogido) return;

        // Si el jugador lo toca
        if (collision.CompareTag("Player"))
        {
            // Buscamos el script de disparo del jugador
            PlayerShooting playerShooting = collision.GetComponent<PlayerShooting>();
            
            if (playerShooting != null)
            {
                recogido = true;
                // Calculamos balas aleatorias (maxAmmo + 1 porque Random.Range con enteros excluye el máximo)
                int ammoToGive = Random.Range(minAmmo, maxAmmo + 1);
                playerShooting.AddAmmo(ammoToGive);
                
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