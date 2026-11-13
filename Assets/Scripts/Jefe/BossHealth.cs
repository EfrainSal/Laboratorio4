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
    [Header("Posición de la barra del jefe")]
    public Vector2 margenBarra = new Vector2(40f, 20f);
    public float anchoMaximoBarra = 853.398f;
    public float separacionDelJugador = 24f;
    private BossAI bossAI;
    private Vector2 tamanoPantalla;
    private float anchoOriginal;

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
        if (bossHealthUIContainer != null)
        {
            bossHealthUIContainer.SetActive(true);
            Canvas.ForceUpdateCanvases();
            AcomodarBarraVida();
        }
        ActualizarUI();
        if (damageTrailFill != null) damageTrailFill.fillAmount = 1;
    }
    private void Update()
    {
        if (damageTrailFill != null && healthBarFill != null && damageTrailFill.fillAmount > healthBarFill.fillAmount)
            damageTrailFill.fillAmount = Mathf.Lerp(damageTrailFill.fillAmount, healthBarFill.fillAmount, Time.deltaTime * trailSpeed);
    }
    private void LateUpdate()
    {
        if (tamanoPantalla != new Vector2(Screen.width, Screen.height)) AcomodarBarraVida();
    }
    public void AcomodarBarraVida()
    {
        if (bossHealthUIContainer == null) return;
        var barra = bossHealthUIContainer.GetComponent<RectTransform>();
        var padre = barra != null ? barra.parent as RectTransform : null;
        if (padre == null) return;
        if (anchoOriginal <= 0) anchoOriginal = Mathf.Max(1, barra.rect.width);
        var jugador = padre.Find("HealthBarContainer") as RectTransform;
        float disponible = padre.rect.width - margenBarra.x * 2;
        float distanciaSuperior = margenBarra.y;
        if (jugador != null)
        {
            var limitesJugador = RectTransformUtility.CalculateRelativeRectTransformBounds(padre, jugador);
            float espacioDerecho = padre.rect.xMax - margenBarra.x - limitesJugador.max.x - separacionDelJugador;
            if (espacioDerecho >= 320) disponible = Mathf.Min(disponible, espacioDerecho);
            else
                // En ventanas estrechas, colocar la barra debajo del HUD en vez de encimarla.
                distanciaSuperior = Mathf.Max(distanciaSuperior, padre.rect.yMax - limitesJugador.min.y + separacionDelJugador);
        }
        barra.anchorMin = barra.anchorMax = barra.pivot = Vector2.one;
        barra.anchoredPosition = new Vector2(-margenBarra.x, -distanciaSuperior);
        float escala = Mathf.Min(anchoMaximoBarra, Mathf.Max(1, disponible)) / anchoOriginal;
        barra.localScale = new Vector3(escala, escala, 1);
        tamanoPantalla = new Vector2(Screen.width, Screen.height);
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
