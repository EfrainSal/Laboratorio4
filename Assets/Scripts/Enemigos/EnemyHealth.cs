using UnityEngine;
using UnityEngine.UI; 
using System.Collections; 

public class EnemyHealth : MonoBehaviour
{
    [Header("Salud")]
    public float maxHealth = 30f;
    private float currentHealth;

    [Header("UI y Feedback")]
    public Image healthBarFill; 
    public float knockbackForce = 5f; 
    public float knockbackTime = 0.2f; 

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private Transform player;

    [Header("Dropeo")]
    public GameObject ammoDropPrefab; 
    [Range(0f, 100f)] public float dropChance = 40f; 
    
    [Header("Experiencia")]
    public float experienciaAlMorir = 25f; // Cuánta XP da este enemigo al morir

    [Header("Audio y Sonidos")]
    public AudioSource audioSource;
    public AudioClip sfxDanio;      // Asigna: Monstruo_Chillido.mp3
    public AudioClip sfxSpawnOcio;  // Asigna: Monstruo_Grunido.mp3
    public AudioClip sfxMuerte;     // Asigna: Muerte_Enemigo.ogg

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Evita que 7 enemigos suenen en el mismo milisegundo al spawnear
        StartCoroutine(ReproducirSonidoSpawnConRetraso());
        
        // Inicia los gruñidos aleatorios para darles vida
        StartCoroutine(RutinaGruñidosAleatorios());
    }

    public void TakeDamage(float damageAmount)
    {
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
            StartCoroutine(ApplyKnockback());
        }
    }

    private IEnumerator ApplyKnockback()
    {
        if (enemyAI != null && rb != null && player != null)
        {
            enemyAI.isKnockedBack = true;
            Vector2 knockbackDirection = (transform.position - player.position).normalized;
            rb.linearVelocity = knockbackDirection * knockbackForce;
            yield return new WaitForSeconds(knockbackTime);
            enemyAI.isKnockedBack = false;
        }
    }

    private void Die()
        {
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
                if (randomValue <= dropChance)
                {
                    Instantiate(ammoDropPrefab, transform.position, Quaternion.identity);
                }
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegistrarEnemigoDerrotado(transform.position);
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

            Destroy(gameObject); 
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