using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RangedEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float detectionRadius = 8f;
    public float stoppingDistance = 4f; // Distancia ideal para disparar
    public float retreatDistance = 2.5f; // Huye si el jugador se acerca a esta distancia

    [Header("Combat")]
    public GameObject bulletPrefab; // Asigna tu prefab 'Bullet' aquí
    public Transform firePoint; // Crea un GameObject vacío hijo del enemigo y asígnalo aquí
    public float bulletForce = 10f;
    public float chargeTime = 1.5f; // Tiempo para cargar el disparo

    [Header("Audio de Alerta")]
    public AudioClip sfxAlerta; // Asigna: Monstruo_Rugido_Alerta.mp3
    private AudioSource audioSource;
    private bool jugadorDetectado = false;
    
    [Header("UI Indicador")]
    public Image chargeIndicatorFill; // Imagen de UI en el Canvas del enemigo

    [HideInInspector] public bool isKnockedBack = false;
    private bool isCharging = false;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        
        if (chargeIndicatorFill != null) chargeIndicatorFill.fillAmount = 0f;
    }

    void FixedUpdate()
    {
        if (player == null || isKnockedBack || isCharging) 
        {
            if (isCharging) rb.linearVelocity = Vector2.zero; // No moverse mientras dispara
            return; 
        }

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

            if (distanceToPlayer > stoppingDistance)
            {
                // Acercarse
                Vector2 direction = (player.position - transform.position).normalized;
                rb.linearVelocity = direction * speed;
            }
            else if (distanceToPlayer < retreatDistance)
            {
                // Alejarse (Jugador muy cerca)
                Vector2 direction = (transform.position - player.position).normalized;
                rb.linearVelocity = direction * speed;
            }
            else
            {
                jugadorDetectado = false;
                // En posición, iniciar ataque
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(ChargeAndShoot());
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator ChargeAndShoot()
    {
        isCharging = true;
        float timer = 0f;

        // Llenar el círculo visual
        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            if (chargeIndicatorFill != null)
            {
                chargeIndicatorFill.fillAmount = timer / chargeTime;
            }
            yield return null;
        }

        // Disparar
        if (bulletPrefab != null && player != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            
            if (bulletRb != null)
            {
                Vector2 direction = (player.position - firePoint.position).normalized;
                bulletRb.AddForce(direction * bulletForce, ForceMode2D.Impulse);
            }
        }

        // Vaciar indicador y esperar antes de volver a moverse
        if (chargeIndicatorFill != null) chargeIndicatorFill.fillAmount = 0f;
        yield return new WaitForSeconds(0.5f);
        isCharging = false;
    }
}