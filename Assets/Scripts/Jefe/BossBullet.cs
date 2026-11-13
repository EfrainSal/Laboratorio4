using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [Header("Configuración")]
    public float speed = 8f;
    public float damage = 20f;
    public float lifetime = 4f;

    [Header("Muerte")]
    public DeathCause deathCause = DeathCause.Jefe;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Mueve la bala hacia donde está mirando (su transform.up)
        rb.linearVelocity = transform.up * speed; 
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.CompareTag("Boss") || collision.CompareTag("Drop")) return;

        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, deathCause);
            }
        }
        
        Destroy(gameObject);
    }
}
