using UnityEngine;
using System.Collections;

public class BossAI : MonoBehaviour
{
    public int CurrentPhase { get; private set; } = 1;

    [Header("Movimiento")]
    public float moveSpeed = 2.5f;
    public float attackRange = 2f; // Distancia para pegar un zarpazo

    [Header("Cooldowns")]
    public float timeBetweenAttacks = 2f;
    private float attackTimer;
    private bool isAttacking = false;

    [Header("Ataques Básicos")]
    public float damageZarpazo = 20f;
    public float knockbackZarpazo = 8f;

    [Header("Ataque: Bullet Hell")]
    public GameObject bossBulletPrefab;
    public Transform firePoint; // Centro del jefe para disparar
    public int bulletsAmount = 12; // Cantidad de proyectiles en el círculo
    public int burstCount = 3; // Número de ráfagas por ataque
    public float timeBetweenBursts = 0.25f; // Tiempo entre cada ráfaga
    public float burstRotationOffset = 10f; // Rotación ligera entre ráfagas para que no vayan en fila

    [Header("Ataque: Zarpazo Letal (Fase 3)")]
    public float damageZarpazoLetal = 45f;
    public float knockbackLetal = 15f;

    [Header("Ataque: Invocación (Fase 3)")]
    public GameObject[] basicEnemiesToSummon; // Arrastra aquí tus prefabs Enemy_Melee y Ranged
    public int enemiesToSpawn = 3;

    [Header("Audio y Sonidos")]
    public AudioSource audioSource;
    public AudioClip sfxRecargaEnergia; // Jefe_Recarga_Energia
    public AudioClip sfxDisparo;        // Jefe_Disparo
    public AudioClip sfxInvocacion;     // Jefe_Rugido_Largo_1
    public AudioClip sfxAtaquePesado;   // Jefe_Rugido_Largo_2

    private Transform player;
    private Rigidbody2D rb;
    private float zPosition;
    private AnimacionJefeDinamica animacion;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animacion = GetComponent<AnimacionJefeDinamica>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        zPosition = transform.position.z;
        attackTimer = timeBetweenAttacks;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void FixedUpdate()
    {
    if (DeathScreenController.GameplayBlocked)
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }
    // Congelar posición en Z
    transform.position = new Vector3(transform.position.x, transform.position.y, zPosition);

    // Si el jugador aún no existe o no se ha encontrado, buscarlo dinámicamente
    if (player == null)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            player = playerObj.transform;
        }
        else 
        {
            rb.linearVelocity = Vector2.zero;
            return; // Detenerse y esperar al siguiente frame
        }
    }

    // Si el jefe ya está atacando, detener su movimiento
    if (isAttacking) 
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }

    // Perseguir al jugador
    Vector2 direction = (player.position - transform.position).normalized;
    rb.linearVelocity = direction * moveSpeed;

    // Lógica de ataque
    attackTimer -= Time.fixedDeltaTime;
    if (attackTimer <= 0f)
    {
        DecidirAtaque();
    }
}

    public void CambiarFase(int nuevaFase)
    {
        CurrentPhase = Mathf.Clamp(nuevaFase, 1, 3);
        // Opcional: Añadir un rugido o cambio de color aquí
        Debug.Log("¡El Jefe ha entrado en la Fase " + nuevaFase + "!");
    }

    private void DecidirAtaque()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero; // El jefe se detiene para atacar

        float distance = Vector2.Distance(transform.position, player.position);

        if (CurrentPhase == 1)
        {
            // FASE 1: Solo persigue y da zarpazos (requiere estar cerca)
            if (distance <= attackRange) StartCoroutine(EjecutarZarpazo(damageZarpazo, knockbackZarpazo, Color.white));
            else { isAttacking = false; attackTimer = 0.2f; } // Si no llega, sigue caminando
        }
        else if (CurrentPhase == 2)
        {
            // FASE 2: Alterna 50/50 entre Zarpazo y Bullet Hell
            int rand = Random.Range(0, 2);
            if (rand == 0 && distance <= attackRange) StartCoroutine(EjecutarZarpazo(damageZarpazo, knockbackZarpazo, Color.white));
            else StartCoroutine(EjecutarBulletHell());
        }
        else if (CurrentPhase == 3)
        {
            // FASE 3: Probabilidades Ponderadas
            int rand = Random.Range(1, 101); // Número del 1 al 100
            
            // 40% Zarpazo Normal
            if (rand <= 40) 
            {
                if (distance <= attackRange) StartCoroutine(EjecutarZarpazo(damageZarpazo, knockbackZarpazo, Color.white));
                else StartCoroutine(EjecutarBulletHell()); // Si está muy lejos para el zarpazo, dispara
            }
            // 35% Proyectiles (Bullet Hell)
            else if (rand <= 75) 
            {
                StartCoroutine(EjecutarBulletHell());
            }
            // 15% Invocación
            else if (rand <= 90) 
            {
                StartCoroutine(EjecutarInvocacion());
            }
            // 10% Zarpazo Pesado Especial
            else 
            {
                if (distance <= attackRange * 1.5f) StartCoroutine(EjecutarZarpazo(damageZarpazoLetal, knockbackLetal, Color.red, true));
                else StartCoroutine(EjecutarBulletHell()); // Si está muy lejos, dispara
            }
        }
    }

    // --- CORRUTINAS DE ATAQUES ---

    private IEnumerator EjecutarZarpazo(float damage, float knockback, Color telegraphColor, bool esAtaquePesado = false)
    {
        animacion?.ComenzarAccion("Ataque", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down);
        if (esAtaquePesado) ReproducirSFX(sfxAtaquePesado);

        // Feedback visual breve antes de pegar
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = telegraphColor; 
        
        yield return new WaitForSeconds(0.4f); // Tiempo de anticipación del golpe

        // Re-comprobar distancia para ver si el jugador lo esquivó
        if (player != null && !DeathScreenController.GameplayBlocked && Vector2.Distance(transform.position, player.position) <= attackRange * 1.5f)
        {
            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            Rigidbody2D pRb = player.GetComponent<Rigidbody2D>();

            if (pHealth != null)
            {
                pHealth.TakeDamage(damage, DeathCause.Jefe);
                if (pRb != null)
                {
                    Vector2 pushDir = (player.position - transform.position).normalized;
                    pRb.AddForce(pushDir * knockback, ForceMode2D.Impulse);
                }
            }
        }

        sr.color = originalColor;
        TerminarAtaque();
    }

    private IEnumerator EjecutarBulletHell()
    {
        animacion?.ComenzarAccion("BolaDePoder", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down);
        ReproducirSFX(sfxRecargaEnergia);
        // Carga energía por 1 segundo
        yield return new WaitForSeconds(1f);

        if (DeathScreenController.GameplayBlocked || bossBulletPrefab == null || firePoint == null)
        {
            TerminarAtaque();
            yield break;
        }

        float angleStep = 360f / Mathf.Max(1, bulletsAmount);
        int rafagas = Mathf.Max(1, burstCount);

        for (int b = 0; b < rafagas; b++)
        {
            if (DeathScreenController.GameplayBlocked) break;

            ReproducirSFX(sfxDisparo);

            float currentAngle = b * burstRotationOffset;
            for (int i = 0; i < bulletsAmount; i++)
            {
                // Calcular rotación en Z para cada bala
                Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);
                Instantiate(bossBulletPrefab, firePoint.position, rotation);
                currentAngle += angleStep;
            }

            if (b < rafagas - 1)
            {
                yield return new WaitForSeconds(timeBetweenBursts);
            }
        }

        yield return new WaitForSeconds(0.5f); // Pausa tras disparar
        TerminarAtaque();
    }

    private IEnumerator EjecutarInvocacion()
    {
        animacion?.ComenzarAccion("CampoEnergia", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down);
        ReproducirSFX(sfxInvocacion);
        // Rugido largo
        yield return new WaitForSeconds(1.5f);

        if (DeathScreenController.GameplayBlocked)
        {
            TerminarAtaque();
            yield break;
        }

        if (basicEnemiesToSummon.Length > 0)
        {
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 3f;
                Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
                
                int randomEnemy = Random.Range(0, basicEnemiesToSummon.Length);
                Instantiate(basicEnemiesToSummon[randomEnemy], spawnPos, Quaternion.identity);
            }
        }

        TerminarAtaque(3.5f);
    }

    private void ReproducirSFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void TerminarAtaque(float cooldownExtra = 0f)
    {
        animacion?.TerminarAccion();
        isAttacking = false;
        attackTimer = timeBetweenAttacks + cooldownExtra;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        animacion?.TerminarAccion();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
