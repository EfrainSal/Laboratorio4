using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración")]
    public float damage = 15f;
    public float lifetime = 3f; // Tiempo de vida si no choca con nada

    private Vector2 previousPosition;
    private bool impactoAplicado;

    void Start()
    {
        // Autodestrucción de seguridad
        Destroy(gameObject, lifetime);
        // Guardamos la posición inicial
        previousPosition = transform.position;
    }

    void Update()
    {
        if (impactoAplicado) return;
        Vector2 currentPosition = transform.position;
        Vector2 direction = currentPosition - previousPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
            // Trazamos una línea desde la posición anterior hasta la actual
            RaycastHit2D[] hits = Physics2D.RaycastAll(previousPosition, direction.normalized, distance);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;

                // Ignorar al jugador, drops y balas del jefe o de otros proyectiles
                if (hit.collider.CompareTag("Player") ||
                    hit.collider.CompareTag("Drop") ||
                    hit.collider.GetComponentInParent<BossBullet>() != null ||
                    hit.collider.GetComponentInParent<Bullet>() != null ||
                    hit.collider.GetComponentInParent<EnemyBullet>() != null)
                {
                    continue;
                }

                impactoAplicado = true;
                // Daño a enemigos normales
                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (enemyHealth != null) enemyHealth.TakeDamage(damage);
                }
                // Daño al Jefe
                else if (hit.collider.CompareTag("Boss"))
                {
                    BossHealth bossHealth = hit.collider.GetComponentInParent<BossHealth>();
                    if (bossHealth != null) bossHealth.TakeDamage(damage);
                }

                // La bala choca contra un enemigo, jefe o muro válido y se destruye
                Destroy(gameObject);
                break;
            }
        }
        
        // Actualizamos la posición para el siguiente frame
        previousPosition = currentPosition;
    }

    // Mantenemos OnTriggerEnter2D como método de respaldo
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (impactoAplicado ||
            collision.CompareTag("Player") ||
            collision.CompareTag("Drop") ||
            collision.GetComponentInParent<BossBullet>() != null ||
            collision.GetComponentInParent<Bullet>() != null ||
            collision.GetComponentInParent<EnemyBullet>() != null) return;
        impactoAplicado = true;

        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null) enemyHealth.TakeDamage(damage);
        }
        else if (collision.CompareTag("Boss"))
        {
            BossHealth bossHealth = collision.GetComponentInParent<BossHealth>();
            if (bossHealth != null) bossHealth.TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}