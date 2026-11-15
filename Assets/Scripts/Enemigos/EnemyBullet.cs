using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Configuración")]
    public float damage = 15f;
    public float lifetime = 3f;

    [Header("Muerte")]
    [Tooltip("Qué mensaje de muerte se muestra si esta bala mata al jugador")]
    public DeathCause deathCause = DeathCause.Disparo;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignorar a otros enemigos y al ayudante inmortal para que no bloquee balas
        if (collision.CompareTag("Enemy") ||
            collision.CompareTag("Drop") ||
            collision.GetComponentInParent<AyudanteJefeAI>() != null) return;

        // Si choca con el jugador, le hace daño
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, deathCause);
            }
        }

        // Destruimos la bala (sea que chocó con el jugador o con una pared)
        Destroy(gameObject);
    }
}