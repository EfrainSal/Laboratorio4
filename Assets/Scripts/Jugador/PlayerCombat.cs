using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Configuración de Ataque")]
    public Transform attackPoint; 
    public float attackRange = 0.8f; 
    public float attackDamage = 15f; 
    public LayerMask enemyLayers; 

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
        if (!enabled) return; 

        if (value.isPressed && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        bool golpeoEnemigo = false; // Variable para saber si le dimos a alguien

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
                golpeoEnemigo = true; // Confirmamos el impacto
            }
        }

        // ---> REPRODUCIR SONIDO SEGÚN EL RESULTADO <---
        if (sfxSource != null)
        {
            if (golpeoEnemigo && sonidoImpacto != null)
            {
                sfxSource.PlayOneShot(sonidoImpacto);
            }
            else if (!golpeoEnemigo && sonidoTajo != null)
            {
                sfxSource.PlayOneShot(sonidoTajo);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}