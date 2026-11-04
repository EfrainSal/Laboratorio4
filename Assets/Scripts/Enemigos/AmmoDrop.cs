using UnityEngine;

public class AmmoDrop : MonoBehaviour
{
    [Header("Configuración del Drop")]
    public int minAmmo = 2; // Mínimo de balas que da
    public int maxAmmo = 5; // Máximo de balas que da

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si el jugador lo toca
        if (collision.CompareTag("Player"))
        {
            // Buscamos el script de disparo del jugador
            PlayerShooting playerShooting = collision.GetComponent<PlayerShooting>();
            
            if (playerShooting != null)
            {
                // Calculamos balas aleatorias (maxAmmo + 1 porque Random.Range con enteros excluye el máximo)
                int ammoToGive = Random.Range(minAmmo, maxAmmo + 1);
                playerShooting.AddAmmo(ammoToGive);
                
                // Destruimos el cuadro gris
                Destroy(gameObject);
            }
        }
    }
}