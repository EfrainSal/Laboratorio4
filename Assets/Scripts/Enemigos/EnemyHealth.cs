using UnityEngine;
using UnityEngine.UI; 
using System.Collections; 

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner OrigenSpawner { get; set; }
    [Header("Salud")]
    public float maxHealth = 30f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    [Header("UI y Feedback")]
    public Image healthBarFill; 
    public float knockbackForce = 5f; 
    public float knockbackTime = 0.2f; 
    public float healthBarVisibleDuration = 1f;
    public float healthBarFadeDuration = 0.5f;

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private Transform player;
    private MeleeEnemyAI meleeAI;
    private RangedEnemyAI rangedAI;
    private bool muerto;
    private CanvasGroup healthBarCanvasGroup;
    private Image[] healthBarImages;
    private float[] healthBarBaseAlphas;
    private Coroutine healthBarFadeCoroutine;

    [Header("Dropeo")]
    public GameObject ammoDropPrefab; 
    [Range(0f, 100f)] public float dropChance = 40f; 
    [Tooltip("Prefab de vendas que se puede soltar además de la munición.")]
    public GameObject vendaDropPrefab;
    [Range(0f, 100f)] public float vendaDropChance = 20f;
    
    [Header("Experiencia")]
    public float experienciaAlMorir = 25f; // Cuánta XP da este enemigo al morir

    [Header("Audio y Sonidos")]
    public AudioSource audioSource;
    public AudioClip sfxDanio;      // Asigna: Monstruo_Chillido.mp3
    public AudioClip sfxSpawnOcio;  // Asigna: Monstruo_Grunido.mp3
    public AudioClip sfxMuerte;     // Asigna: Muerte_Enemigo.ogg

    void Awake()
    {
        InicializarBarraVida();
    }

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
        meleeAI = GetComponent<MeleeEnemyAI>();
        rangedAI = GetComponent<RangedEnemyAI>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        EstablecerOpacidadBarraVida(0f);
        
        // Evita que 7 enemigos suenen en el mismo milisegundo al spawnear
        StartCoroutine(ReproducirSonidoSpawnConRetraso());
        
        // Inicia los gruñidos aleatorios para darles vida
        StartCoroutine(RutinaGruñidosAleatorios());
    }

    private void InicializarBarraVida()
    {
        if (healthBarFill == null) return;

        Transform parent = healthBarFill.transform.parent;
        if (parent != null)
        {
            Image chargeFill = rangedAI != null ? rangedAI.chargeIndicatorFill : GetComponent<RangedEnemyAI>()?.chargeIndicatorFill;
            var lista = new System.Collections.Generic.List<Image>();
            foreach (var img in parent.GetComponentsInChildren<Image>(true))
            {
                if (img != null && img != chargeFill && img.gameObject.name != "Image")
                {
                    lista.Add(img);
                }
            }
            healthBarImages = lista.ToArray();
            if (chargeFill == null && parent.GetComponent<Canvas>() != null)
            {
                healthBarCanvasGroup = parent.GetComponent<CanvasGroup>();
            }
        }
        else
        {
            healthBarImages = new[] { healthBarFill };
        }

        healthBarBaseAlphas = new float[healthBarImages.Length];
        for (int i = 0; i < healthBarImages.Length; i++)
        {
            float a = healthBarImages[i].color.a;
            healthBarBaseAlphas[i] = a > 0.01f ? a : 1f;
        }

        EstablecerOpacidadBarraVida(0f);
    }

    private void EstablecerOpacidadBarraVida(float opacidad)
    {
        float alpha = Mathf.Clamp01(opacidad);
        if (healthBarCanvasGroup != null)
        {
            healthBarCanvasGroup.alpha = alpha;
        }
        if (healthBarImages != null && healthBarBaseAlphas != null)
        {
            for (int i = 0; i < healthBarImages.Length; i++)
            {
                if (healthBarImages[i] != null)
                {
                    Color c = healthBarImages[i].color;
                    c.a = (healthBarCanvasGroup != null ? healthBarBaseAlphas[i] : healthBarBaseAlphas[i] * alpha);
                    healthBarImages[i].color = c;
                }
            }
        }
    }

    private void MostrarBarraVidaTemporalmente()
    {
        if (healthBarFill == null || muerto) return;
        if (healthBarImages == null) InicializarBarraVida();

        if (healthBarFadeCoroutine != null)
        {
            StopCoroutine(healthBarFadeCoroutine);
        }
        healthBarFadeCoroutine = StartCoroutine(RutinaMostrarYDesvanecerBarraVida());
    }

    private IEnumerator RutinaMostrarYDesvanecerBarraVida()
    {
        EstablecerOpacidadBarraVida(1f);
        yield return new WaitForSeconds(healthBarVisibleDuration);

        float tiempo = 0f;
        float duracion = Mathf.Max(0.05f, healthBarFadeDuration);
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            EstablecerOpacidadBarraVida(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        EstablecerOpacidadBarraVida(0f);
        healthBarFadeCoroutine = null;
    }

    public void TakeDamage(float damageAmount)
    {
        if (muerto || damageAmount <= 0f) return;
        GetComponent<RetroalimentacionDano>()?.Mostrar(Mathf.Min(currentHealth, damageAmount), false);
        currentHealth -= damageAmount;

        ReproducirSFX(sfxDanio, true);
        
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            MostrarBarraVidaTemporalmente();
            StartCoroutine(ApplyKnockback());
        }
    }

    private IEnumerator ApplyKnockback()
    {
        if (rb != null && player != null)
        {
            if (enemyAI != null) enemyAI.isKnockedBack = true;
            if (meleeAI != null) meleeAI.isKnockedBack = true;
            if (rangedAI != null) rangedAI.isKnockedBack = true;
            Vector2 knockbackDirection = (transform.position - player.position).normalized;
            rb.linearVelocity = knockbackDirection * knockbackForce;
            yield return new WaitForSeconds(knockbackTime);
            if (enemyAI != null) enemyAI.isKnockedBack = false;
            if (meleeAI != null) meleeAI.isKnockedBack = false;
            if (rangedAI != null) rangedAI.isKnockedBack = false;
        }
    }

    private void Die()
        {
            if (muerto) return;
            muerto = true;
            StopAllCoroutines();
            if (healthBarFill != null)
            {
                Canvas healthCanvas = healthBarFill.GetComponentInParent<Canvas>();
                if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);
                else healthBarFill.gameObject.SetActive(false);
            }
            if (enemyAI != null) enemyAI.enabled = false;
            if (meleeAI != null) meleeAI.enabled = false;
            if (rangedAI != null) rangedAI.enabled = false;
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            var animacion = GetComponent<AnimacionEnemigoDinamica>();
            float duracionMuerte = animacion != null ? animacion.Morir() : 0f;
            // Otorgar experiencia al jugador antes de morir
            if (player != null)
            {
                PlayerExperience playerXP = player.GetComponent<PlayerExperience>();
                if (playerXP != null)
                {
                    playerXP.GanarExperiencia(experienciaAlMorir);
                }
            }

            // Comprobamos la probabilidad de dropeo de balas
            if (ammoDropPrefab != null)
            {
                float randomValue = Random.Range(0f, 100f);
                if (dropChance >= 100f || randomValue < dropChance)
                {
                    Instantiate(ammoDropPrefab, transform.position + Vector3.left * .18f, Quaternion.identity);
                }
            }
            // Tiradas independientes: puede soltar munición, vendas, ambas o ninguna.
            if (vendaDropPrefab != null && (vendaDropChance >= 100f || Random.Range(0f, 100f) < vendaDropChance))
                Instantiate(vendaDropPrefab, transform.position + Vector3.right * .18f, Quaternion.identity);

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegistrarEnemigoDerrotado(transform.position, OrigenSpawner);
            }

    // ---> CORRECCIÓN: Avisar al GameManager Global para las estadísticas <---
            if (GameManager.Instance != null && GameManager.Instance.RunActiva)
            {
                // Casteamos a (int) porque GameManager recibe un int
                GameManager.Instance.RegistrarBajaEnemiga((int)experienciaAlMorir);
            }

            // ---> AVISAR AL AUDIOMANAGER <---
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.RegistrarEnemigoDerrotado();
            }

            if (sfxMuerte != null)
            {
                // Usamos PlayClipAtPoint porque el enemigo se va a destruir, 
                // si usáramos su propio AudioSource, el sonido se cortaría de golpe al morir.
                AudioSource.PlayClipAtPoint(sfxMuerte, transform.position, 1f);
            }

            Destroy(gameObject, duracionMuerte);
        }

    private IEnumerator ReproducirSonidoSpawnConRetraso()
    {
        // Retraso al azar entre 0 y 0.3 segundos para que no suenen todos a la vez
        yield return new WaitForSeconds(Random.Range(0f, 0.3f)); 
        ReproducirSFX(sfxSpawnOcio, true);
    }

    private IEnumerator RutinaGruñidosAleatorios()
    {
        while (true)
        {
            // Espera un tiempo aleatorio entre 5 y 12 segundos
            yield return new WaitForSeconds(Random.Range(5f, 12f)); 
            if (currentHealth > 0)
            {
                ReproducirSFX(sfxSpawnOcio, true);
            }
        }
    }

    private void ReproducirSFX(AudioClip clip, bool variarPitch = false)
    {
        if (audioSource != null && clip != null)
        {
            // Variar el pitch hace que cada monstruo suene de distinto tamaño
            audioSource.pitch = variarPitch ? Random.Range(0.85f, 1.15f) : 1f;
            audioSource.PlayOneShot(clip);
        }
    }
}
