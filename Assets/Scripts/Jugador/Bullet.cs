using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración")]
    public float damage = 15f;
    public float lifetime = 3f; // Tiempo de vida si no choca con nada
    [HideInInspector] public bool esDeAyudante = false;
    [HideInInspector] public float umbralMinimoVidaJefe = 0.05f;

    private Vector2 previousPosition;
    private bool impactoAplicado;
    private bool Ignorar(Collider2D c)
    {
        if (c == null || c.gameObject == gameObject || ColisionesCombate.EsNota(c) || c.CompareTag("Player") || c.CompareTag("Drop") ||
            c.GetComponentInParent<AyudanteJefeAI>() != null || c.GetComponentInParent<BossBullet>() != null ||
            c.GetComponentInParent<Bullet>() != null || c.GetComponentInParent<EnemyBullet>() != null) return true;
        var jefe = c.GetComponentInParent<BossHealth>();
        if (jefe != null) return c.GetComponentInParent<HurtboxJefe>() == null;
        return c.isTrigger && c.GetComponentInParent<EnemyHealth>() == null;
    }

    private void AplicarDanioJefe(BossHealth bossHealth)
    {
        if (bossHealth == null || bossHealth.IsDead) return;

        if (esDeAyudante)
        {
            float vidaMinimaPermitida = bossHealth.maxHealth * umbralMinimoVidaJefe;
            float margenDisponible = bossHealth.CurrentHealth - vidaMinimaPermitida;
            if (margenDisponible <= 0f) return;

            float danioLimitado = Mathf.Min(damage, margenDisponible);
            if (danioLimitado > 0f)
            {
                bossHealth.TakeDamage(danioLimitado);
            }
            return;
        }

        bossHealth.TakeDamage(damage);
    }

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
                if (Ignorar(hit.collider)) continue;

                impactoAplicado = true;
                // Daño a enemigos normales
                if (hit.collider.GetComponentInParent<EnemyHealth>() != null)
                {
                    EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (enemyHealth != null && !esDeAyudante) enemyHealth.TakeDamage(damage);
                }
                // Daño al Jefe
                else if (hit.collider.GetComponentInParent<HurtboxJefe>() != null)
                {
                    BossHealth bossHealth = hit.collider.GetComponentInParent<BossHealth>();
                    AplicarDanioJefe(bossHealth);
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
        if (impactoAplicado || Ignorar(collision)) return;
        impactoAplicado = true;

        if (collision.GetComponentInParent<EnemyHealth>() != null)
        {
            EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && !esDeAyudante) enemyHealth.TakeDamage(damage);
        }
        else if (collision.GetComponentInParent<HurtboxJefe>() != null)
        {
            BossHealth bossHealth = collision.GetComponentInParent<BossHealth>();
            AplicarDanioJefe(bossHealth);
        }
        
        Destroy(gameObject);
    }
}
