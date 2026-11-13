using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    [Header("Salud del Jefe")]
    public float maxHealth = 1500f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    [Header("Efectos de Daño")]
    public float trailSpeed = 3f;
    [Header("HUD de la escena de combate")]
    public GameObject bossHealthUIContainer;
    public Image healthBarFill;
    public Image damageTrailFill;
    private BossAI bossAI;

    private void Awake()
    {
        currentHealth = maxHealth;
        bossAI = GetComponent<BossAI>();
    }
    private void Start()
    {
        // Compatibilidad con escenas anteriores; Nivel1 guarda referencias explícitas.
        if (bossHealthUIContainer == null)
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.gameObject.scene != gameObject.scene) continue;
                var contenedor = canvas.transform.Find("BossHealthContainer");
                if (contenedor == null) continue;
                bossHealthUIContainer = contenedor.gameObject;
                healthBarFill = contenedor.Find("Fill")?.GetComponent<Image>();
                damageTrailFill = contenedor.Find("DamageTrail")?.GetComponent<Image>();
                break;
            }
        }
        if (bossHealthUIContainer != null) bossHealthUIContainer.SetActive(true);
        ActualizarUI();
        if (damageTrailFill != null) damageTrailFill.fillAmount = 1;
    }
    private void Update()
    {
        if (damageTrailFill != null && healthBarFill != null && damageTrailFill.fillAmount > healthBarFill.fillAmount)
            damageTrailFill.fillAmount = Mathf.Lerp(damageTrailFill.fillAmount, healthBarFill.fillAmount, Time.deltaTime * trailSpeed);
    }
    public void TakeDamage(float damageAmount)
    {
        if (IsDead || damageAmount <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        ActualizarUI();
        if (currentHealth <= 0) Die();
        else EvaluarFase();
    }
    private void EvaluarFase()
    {
        if (bossAI == null) return;
        float porcentaje = currentHealth / maxHealth;
        int fase = porcentaje <= .33f ? 3 : porcentaje <= .66f ? 2 : 1;
        if (fase != bossAI.CurrentPhase) bossAI.CambiarFase(fase);
    }
    private void ActualizarUI()
    {
        if (healthBarFill != null) healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }
    private void Die()
    {
        IsDead = true;
        if (bossAI != null) bossAI.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
        float duracion = GetComponent<AnimacionJefeDinamica>()?.Morir() ?? 0f;
        if (bossHealthUIContainer != null) bossHealthUIContainer.SetActive(false);
        foreach (var bala in FindObjectsByType<BossBullet>(FindObjectsSortMode.None))
            if (bala.gameObject.scene == gameObject.scene) Destroy(bala.gameObject);
        // La victoria detiene el combate: los ataques pendientes no pueden matar al ganador.
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent<PlayerHealth>(out var salud)) salud.isInvulnerable = true;
        if (GameManager.Instance != null && GameManager.Instance.RunActiva)
        {
            GameManager.Instance.RegistrarBajaEnemiga(500);
            GameManager.Instance.CompletarRunVictoria();
        }
        StartCoroutine(RetirarTrasAnimacion(duracion));
    }
    private IEnumerator RetirarTrasAnimacion(float duracion)
    {
        yield return new WaitForSecondsRealtime(duracion);
        Destroy(gameObject);
    }
}
