using UnityEngine;

public class MeleeEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float detectionRadius = 5f;

    [Header("Combat")]
    public float damage = 10f;
    public float attackCooldown = 1.2f; // Segundos entre cada golpe
    public float knockbackForce = 5f; // Fuerza de empuje al jugador
    private float lastAttackTime;

    [Header("Audio de Alerta")]
    public AudioClip sfxAlerta; // Asigna: Monstruo_Rugido_Alerta.mp3
    private AudioSource audioSource;
    private bool jugadorDetectado = false;

    [HideInInspector] public bool isKnockedBack = false; 

    private Transform player;
    private Rigidbody2D rb;
    private float zPosition;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>(); // Requiere el Rigidbody2D que ya tienes asignado
        zPosition = transform.position.z;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, zPosition);

        if (player == null || isKnockedBack) return; 

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (!jugadorDetectado)
            {
                jugadorDetectado = true;
                if (audioSource != null && sfxAlerta != null)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(sfxAlerta);
                }
            }

            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed; 
        }
        else
        {
            jugadorDetectado = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Cambiamos a Stay2D para detectar contacto continuo contra paredes
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Solo atacamos si ha pasado el tiempo de enfriamiento (Cooldown)
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage, DeathCause.Mutante);
                    lastAttackTime = Time.time; // Reiniciamos el temporizador

                    // Aplicamos el empuje al Rigidbody del jugador
                    if (playerRb != null)
                    {
                        Vector2 pushDirection = (collision.transform.position - transform.position).normalized;
                        playerRb.AddForce(pushDirection * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
}