using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    [Header("Salud del Jefe")]
    public float maxHealth = 1500f;
    private float currentHealth;

    [Header("Efectos de Daño")]
    public float trailSpeed = 3f;

    // Referencias privadas que el script buscará automáticamente
    private GameObject bossHealthUIContainer;
    private Image healthBarFill;
    private Image damageTrailFill;
    private BossAI bossAI;

    void Start()
    {
        currentHealth = maxHealth;
        bossAI = GetComponent<BossAI>();

        // Encontrar el contenedor usando FindObjectOfType (busca incluso inactivos si le pasamos 'true')
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform containerTransform = canvas.transform.Find("BossHealthContainer");
            if (containerTransform != null)
            {
                bossHealthUIContainer = containerTransform.gameObject;
                healthBarFill = containerTransform.Find("Fill").GetComponent<Image>();
                damageTrailFill = containerTransform.Find("DamageTrail").GetComponent<Image>();
                
                bossHealthUIContainer.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No se encontró 'BossHealthContainer' dentro del Canvas.");
            }
        }

        ActualizarUI();
        if (damageTrailFill != null) damageTrailFill.fillAmount = currentHealth / maxHealth;
    }

    void Update()
    {
        // Animación del rastro de daño (bajando suavemente como en el Player)
        if (damageTrailFill != null && healthBarFill != null && damageTrailFill.fillAmount > healthBarFill.fillAmount)
        {
            damageTrailFill.fillAmount = Mathf.Lerp(damageTrailFill.fillAmount, healthBarFill.fillAmount, Time.deltaTime * trailSpeed);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        ActualizarUI();
        EvaluarFase();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void EvaluarFase()
    {
        float porcentajeVida = currentHealth / maxHealth;

        if (porcentajeVida <= 0.33f && bossAI.CurrentPhase != 3)
        {
            bossAI.CambiarFase(3);
        }
        else if (porcentajeVida <= 0.66f && porcentajeVida > 0.33f && bossAI.CurrentPhase != 2)
        {
            bossAI.CambiarFase(2);
        }
    }

    private void ActualizarUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    private void Die()
    {
        // Ocultar la barra de vida al morir
        if (bossHealthUIContainer != null)
        {
            bossHealthUIContainer.SetActive(false);
        }

        if (GameManager.Instance != null && GameManager.Instance.RunActiva)
        {
            GameManager.Instance.RegistrarBajaEnemiga(500); 
            GameManager.Instance.CompletarRunVictoria();    
        }

        Destroy(gameObject);
    }
}