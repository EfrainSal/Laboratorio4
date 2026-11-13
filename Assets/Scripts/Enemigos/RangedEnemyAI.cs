using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RangedEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float detectionRadius = 8f;
    public float stoppingDistance = 4f; 
    public float retreatDistance = 2.5f; 
    
    [Header("Camino y Tambaleo")]
    [Tooltip("Asigna SOLO la capa de los muros.")]
    public LayerMask capaMuros;
    public float wobbleSpeed = 5f; 
    public float wobbleAmount = 0.4f;

    [Header("Patrulla (Idle)")]
    public float patrolSpeed = 1f;
    public float patrolRadius = 4f;
    public float patrolWaitTime = 2f;

    [Header("Combat")]
    public GameObject bulletPrefab; 
    public Transform firePoint; 
    public float bulletForce = 10f;
    public float chargeTime = 1.5f; 

    [Header("Audio")]
    public AudioClip sfxAlerta; 
    private AudioSource audioSource;
    private bool jugadorDetectado = false;
    
    [Header("UI Indicador")]
    public Image chargeIndicatorFill; 

    [HideInInspector] public bool isKnockedBack = false;
    private bool isCharging = false;
    public int DisparosRealizados { get; private set; }

    private Transform player;
    private Rigidbody2D rb;
    private float zPosition;
    
    private Vector2 startPosition;
    private Vector2 patrolTarget;
    private float patrolTimer;
    
    private int currentPathIndex = -1;
    private float randomWobbleOffset;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        zPosition = transform.position.z;
        startPosition = transform.position;
        randomWobbleOffset = Random.Range(0f, 100f);
        GetNewPatrolTarget();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        
        if (chargeIndicatorFill != null) chargeIndicatorFill.fillAmount = 0f;
    }

    void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, zPosition);

        if (player == null || isKnockedBack || isCharging) 
        {
            if (isCharging) rb.linearVelocity = Vector2.zero; 
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
                // Acercarse usando el rastro
                Vector2 targetPos = ObtenerObjetivoCamino();
                MoverConTambaleo(targetPos, speed);
            }
            else if (distanceToPlayer < retreatDistance)
            {
                // Alejarse (huir)
                Vector2 dirAlejarse = (transform.position - player.position).normalized;
                Vector2 retreatTarget = (Vector2)transform.position + dirAlejarse;
                MoverConTambaleo(retreatTarget, speed);
            }
            else
            {
                // Distancia ideal. Verificar si te puede ver para disparar.
                if (TieneLineaDeVision(player.position))
                {
                    rb.linearVelocity = Vector2.zero;
                    StartCoroutine(ChargeAndShoot());
                }
                else
                {
                    // Si estás escondido detrás de una caja, usará el rastro para buscarte
                    Vector2 targetPos = ObtenerObjetivoCamino();
                    MoverConTambaleo(targetPos, speed);
                }
            }
        }
        else
        {
            jugadorDetectado = false;
            currentPathIndex = -1;
            Patrol();
        }
    }

    private void MoverConTambaleo(Vector2 target, float vel)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        dir += perpendicular * Mathf.Sin(Time.time * wobbleSpeed + randomWobbleOffset) * wobbleAmount;
        rb.linearVelocity = dir.normalized * vel;
    }

    private Vector2 ObtenerObjetivoCamino()
    {
        if (TieneLineaDeVision(player.position))
        {
            currentPathIndex = -1; 
            return player.position;
        }

        if (MovimientoPlayer.historialPosiciones.Count == 0) return player.position;

        if (currentPathIndex == -1 || currentPathIndex >= MovimientoPlayer.historialPosiciones.Count)
        {
            currentPathIndex = BuscarIndiceMasCercanoYVisible();
        }

        Vector2 nodoDestino = MovimientoPlayer.historialPosiciones[currentPathIndex];

        if (Vector2.Distance(transform.position, nodoDestino) < 0.5f)
        {
            currentPathIndex++;
            if (currentPathIndex >= MovimientoPlayer.historialPosiciones.Count)
            {
                currentPathIndex = MovimientoPlayer.historialPosiciones.Count - 1;
            }
            nodoDestino = MovimientoPlayer.historialPosiciones[currentPathIndex];
        }

        return nodoDestino;
    }

    private int BuscarIndiceMasCercanoYVisible()
    {
        int mejorIndice = MovimientoPlayer.historialPosiciones.Count - 1;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < MovimientoPlayer.historialPosiciones.Count; i++)
        {
            float dist = Vector2.Distance(transform.position, MovimientoPlayer.historialPosiciones[i]);
            if (dist < mejorDistancia && TieneLineaDeVision(MovimientoPlayer.historialPosiciones[i]))
            {
                mejorDistancia = dist;
                mejorIndice = i;
            }
        }
        return mejorIndice;
    }

    private bool TieneLineaDeVision(Vector2 targetPos)
    {
        Vector2 dir = targetPos - (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, dir.magnitude, capaMuros);
        return hit.collider == null;
    }

    private void Patrol()
    {
        if (Vector2.Distance(transform.position, patrolTarget) < 0.5f)
        {
            rb.linearVelocity = Vector2.zero;
            patrolTimer -= Time.fixedDeltaTime;

            if (patrolTimer <= 0f)
            {
                GetNewPatrolTarget();
                patrolTimer = patrolWaitTime;
            }
        }
        else
        {
            MoverConTambaleo(patrolTarget, patrolSpeed);
        }
    }

    private void GetNewPatrolTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle * patrolRadius;
        patrolTarget = startPosition + randomDirection;
    }

    private IEnumerator ChargeAndShoot()
    {
        isCharging = true;
        float timer = 0f;

        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            if (chargeIndicatorFill != null)
            {
                chargeIndicatorFill.fillAmount = timer / chargeTime;
            }
            yield return null;
        }

        if (!isKnockedBack && player != null)
        {
            var animacion = GetComponent<AnimacionEnemigoDinamica>();
            float espera = animacion != null ? animacion.Atacar(player.position) : 0f;
            if (espera > 0f) yield return new WaitForSeconds(espera);
        }
        if (!isKnockedBack && bulletPrefab != null && player != null && firePoint != null && TieneLineaDeVision(player.position))
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            DisparosRealizados++;
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            
            if (bulletRb != null)
            {
                Vector2 direction = (player.position - firePoint.position).normalized;
                bulletRb.AddForce(direction * bulletForce, ForceMode2D.Impulse);
            }
        }

        if (chargeIndicatorFill != null) chargeIndicatorFill.fillAmount = 0f;
        yield return new WaitForSeconds(0.5f);
        isCharging = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isCharging = false;
        if (chargeIndicatorFill != null) chargeIndicatorFill.fillAmount = 0f;
    }
}
