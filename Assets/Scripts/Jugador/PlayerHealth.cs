using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isInvulnerable = false;
    [HideInInspector] public bool cargadoDesdeGuardado = false;

    private bool isDead = false;
    private DeathCause ultimaCausa = DeathCause.Desconocida;

    public bool IsDead => isDead;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoDanio;

    [Header("Defensa")]
    [Tooltip("Porcentaje de daño que se bloquea (Ej: 10 = 10% menos de daño)")]
    public float defensaPorcentual = 0f;
    public float defensaMaxima = 80f;

    [Header("UI Interface")]
    public Image healthBarFill;
    public Image damageTrailFill;
    public Image damageFlash;
    public TextMeshProUGUI healthText;

    [Header("Efectos de Daño")]
    public float trailSpeed = 3f;
    [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
    public float flashSpeed = 1.5f;
    public float maxFlashAlpha = 0.4f;

    [Header("Al morir")]
    [Tooltip("Activo: los enemigos se destruyen al morir. Desactivado: se quedan congelados en la escena y se ven detrás del desenfoque de la pantalla de muerte.")]
    public bool destruirEnemigosAlMorir = false;
    [Tooltip("Oculta el sprite del jugador al morir.")]
    public bool ocultarSpriteAlMorir = true;

    void Awake()
    {

    }

    void Start()
    {
        // ELIMINAMOS la línea "currentHealth = maxHealth;" para que no sobrescriba el guardado.
        // La inicialización por defecto (nueva partida) ya la hace el Awake().
        if (currentHealth == 0f)
        {
            currentHealth = maxHealth;
        }

        UpdateHealthUI();

        if (damageTrailFill != null)
        {
            damageTrailFill.fillAmount = currentHealth / maxHealth;
        }
    }

    void Update()
    {
        // 1. Animación de la barra roja bajando suavemente (funciona incluso si estamos muertos)
        if (damageTrailFill != null && healthBarFill != null &&
            damageTrailFill.fillAmount > healthBarFill.fillAmount)
        {
            damageTrailFill.fillAmount = Mathf.Lerp(damageTrailFill.fillAmount, healthBarFill.fillAmount, Time.deltaTime * trailSpeed);
        }

        // Si estamos muertos, no calculamos el parpadeo
        if (isDead) return;

        // 2. Efecto de respiración en los bordes (viñeta)
        if (damageFlash != null)
        {
            float healthPercentage = currentHealth / maxHealth;

            if (healthPercentage <= lowHealthThreshold && currentHealth > 0)
            {
                float wave = (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f;
                float alpha = wave * maxFlashAlpha;
                damageFlash.color = new Color(1f, 0f, 0f, alpha);
            }
            else
            {
                damageFlash.color = new Color(1f, 0f, 0f, 0f);
            }
        }
    }

    /// <param name="causa">Qué te está golpeando; define el mensaje de la pantalla de muerte.</param>
    public void TakeDamage(float damageAmount, DeathCause causa = DeathCause.Desconocida)
    {
        if (isDead || isInvulnerable) return;   // muerto o con escudo: ignoramos el daño

        ultimaCausa = causa;

        // El daño se aplica UNA sola vez, ya reducido por la defensa (con su tope máximo)
        float defensa = Mathf.Clamp(defensaPorcentual, 0f, defensaMaxima);
        float dañoFinal = damageAmount * (1f - defensa / 100f);

        currentHealth = Mathf.Clamp(currentHealth - dañoFinal, 0f, maxHealth);
        UpdateHealthUI();

        // ---> REPRODUCIR SONIDO DE DAÑO <---
        if (sfxSource != null && sonidoDanio != null && dañoFinal > 0)
        {
            sfxSource.PlayOneShot(sonidoDanio);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        if (damageTrailFill != null)
        {
            damageTrailFill.fillAmount = currentHealth / maxHealth;
        }
    }

    public void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = Mathf.FloorToInt(currentHealth) + " / " + Mathf.FloorToInt(maxHealth);
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"¡El jugador ha muerto! Causa: {ultimaCausa}");

        // 1. Limpiar pantalla roja
        if (damageFlash != null)
            damageFlash.color = new Color(1f, 0f, 0f, 0f);

        // 2. Detener al jugador: sin movimiento, disparo ni input
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Desactivamos el resto de scripts del jugador, pero NO este (su Update anima la barra roja)
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour != this) behaviour.enabled = false;
        }

        // 3. Enemigos: destruirlos o congelarlos
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (destruirEnemigosAlMorir)
            {
                Destroy(enemy);
                continue;
            }

            foreach (MonoBehaviour behaviour in enemy.GetComponents<MonoBehaviour>())
                behaviour.enabled = false;

            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null) enemyRb.linearVelocity = Vector2.zero;
        }

        // 4. Desactivar el spawner
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

        // 5. Simular la muerte sin desactivar el GameObject (así sigue el Update de la barra roja)
        if (ocultarSpriteAlMorir)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        // 6. Terminar la run: esto dispara la pantalla de muerte
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TerminarRunPorMuerte(ultimaCausa);
        }
        else
        {
            Debug.LogWarning("No hay GameManager en la escena: no se mostrará la pantalla de muerte. Inicia el juego desde el Menú Principal.");
        }
    }
}