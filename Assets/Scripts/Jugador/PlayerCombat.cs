using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Configuración de Ataque")]
    public Transform attackPoint; 
    public float attackRange = 0.8f; 
    public float attackDamage = 15f; 
    public LayerMask enemyLayers; 
    public LayerMask capasObstaculos = ~0;

    [Header("Cadencia de Ataque")]
    public float attackRate = 2f; 
    private float nextAttackTime = 0f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoTajo;
    public AudioClip sonidoImpacto;

    // Cambiado de OnMeleeAttack a OnAttack
    public void OnAttack(InputValue value)
    {
        // Si el script está desactivado por el Manager, Unity no procesará esto
        if (!enabled || DeathScreenController.GameplayBlocked) return;

        if (value.isPressed && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    void Attack()
    {
        if (attackPoint == null) return;
        GetComponent<ArmaIndependiente>()?.ActualizarApuntado();
        GetComponent<MacheteAnimacion>()?.Reproducir();
        GetComponent<AnimacionJasonDinamica>()?.ReproducirAccion("Ataque");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        bool golpeoEnemigo = false; 
        var golpeados = new HashSet<Component>();

        foreach (Collider2D enemy in hitEnemies)
        {
            var salud = (Component)enemy.GetComponentInParent<EnemyHealth>() ?? enemy.GetComponentInParent<BossHealth>();
            if (salud == null) continue;
            var propio = GetComponent<Collider2D>(); var cuerpoObjetivo = salud.GetComponent<Collider2D>();
            Vector2 origen = propio != null ? (Vector2)propio.bounds.center : (Vector2)transform.position;
            Vector2 destino = cuerpoObjetivo != null ? cuerpoObjetivo.ClosestPoint(origen) : enemy.ClosestPoint(origen);
            if (!ColisionesCombate.LineaLibre(origen, destino, transform, salud.transform, capasObstaculos)) continue;
            // Intentar dañar enemigo normal
            EnemyHealth enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                if (golpeados.Add(enemyHealth)) enemyHealth.TakeDamage(attackDamage);
                golpeoEnemigo = true; 
            }
            else
            {
                // Intentar dañar al Jefe
                BossHealth bossHealth = enemy.GetComponentInParent<BossHealth>();
                if (bossHealth != null)
                {
                    if (golpeados.Add(bossHealth)) bossHealth.TakeDamage(attackDamage);
                    golpeoEnemigo = true;
                }
            }
        }

        if (sfxSource != null)
        {
            if (golpeoEnemigo && sonidoImpacto != null) sfxSource.PlayOneShot(sonidoImpacto);
            else if (!golpeoEnemigo && sonidoTajo != null) sfxSource.PlayOneShot(sonidoTajo);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
