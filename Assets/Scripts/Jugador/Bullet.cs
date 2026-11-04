using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración")]
    public float damage = 15f;
    public float lifetime = 3f; // Tiempo de vida si no choca con nada

    void Start()
    {
        // Autodestrucción de seguridad
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignorar al jugador para que no se dispare a sí mismo
        if (collision.CompareTag("Player")) return;

        if (collision.CompareTag("Drop")) return;

        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
        
        // Destruimos la bala al impactar con cualquier cosa (enemigos, paredes, etc.)
        Destroy(gameObject);
    }
}