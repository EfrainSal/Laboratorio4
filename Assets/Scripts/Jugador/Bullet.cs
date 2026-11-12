using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración")]
    public float damage = 15f;
    public float lifetime = 3f; // Tiempo de vida si no choca con nada

    private Vector2 previousPosition;

    void Start()
    {
        // Autodestrucción de seguridad
        Destroy(gameObject, lifetime);
        // Guardamos la posición inicial
        previousPosition = transform.position;
    }

    void Update()
    {
        Vector2 currentPosition = transform.position;
        Vector2 direction = currentPosition - previousPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
            // Trazamos una línea desde la posición anterior hasta la actual
            RaycastHit2D hit = Physics2D.Raycast(previousPosition, direction.normalized, distance);

            // Si la línea choca con algo que no sea la propia bala
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                // Ignorar al jugador y los drops (la bala los atraviesa)
                if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Drop")) 
                {
                    // No hacemos nada, la bala sigue su curso
                }
                else
                {
                    // Daño a enemigos normales
                    if (hit.collider.CompareTag("Enemy"))
                    {
                        EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                        if (enemyHealth != null) enemyHealth.TakeDamage(damage);
                    }
                    // Daño al Jefe
                    else if (hit.collider.CompareTag("Boss"))
                    {
                        BossHealth bossHealth = hit.collider.GetComponent<BossHealth>();
                        if (bossHealth != null) bossHealth.TakeDamage(damage);
                    }
                    
                    // La bala choca contra un muro u otro objeto válido y se destruye
                    Destroy(gameObject);
                }
            }
        }
        
        // Actualizamos la posición para el siguiente frame
        previousPosition = currentPosition;
    }

    // Mantenemos OnTriggerEnter2D como método de respaldo
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Drop")) return;

        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null) enemyHealth.TakeDamage(damage);
        }
        else if (collision.CompareTag("Boss"))
        {
            BossHealth bossHealth = collision.GetComponent<BossHealth>();
            if (bossHealth != null) bossHealth.TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}