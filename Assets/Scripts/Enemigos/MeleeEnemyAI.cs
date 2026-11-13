using UnityEngine;
using System.Collections;

public class MeleeEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float detectionRadius = 5f;
    
    [Header("Camino y Tambaleo")]
    [Tooltip("Asigna SOLO la capa de los muros. Los drops no deben tener esta capa.")]
    public LayerMask capaMuros; 
    public float wobbleSpeed = 5f; // Qué tan rápido se tambalea
    public float wobbleAmount = 0.4f; // Qué tanto se abre hacia los lados

    [Header("Patrulla (Idle)")]
    public float patrolSpeed = 1f; 
    public float patrolRadius = 4f; 
    public float patrolWaitTime = 2f; 

    [Header("Combat")]
    public float damage = 10f;
    public float attackCooldown = 1.2f;
    public float knockbackForce = 5f;
    private float lastAttackTime;
    private bool atacando;

    [Header("Audio")]
    public AudioClip sfxAlerta;
    private AudioSource audioSource;
    private bool jugadorDetectado = false;

    [HideInInspector] public bool isKnockedBack = false; 

    private Transform player;
    private Rigidbody2D rb;
    private float zPosition;
    
    private Vector2 startPosition;
    private Vector2 patrolTarget;
    private float patrolTimer;
    
    // Variables para seguir rastro
    private int currentPathIndex = -1;
    private float randomWobbleOffset;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        zPosition = transform.position.z;
        startPosition = transform.position; 
        randomWobbleOffset = Random.Range(0f, 100f); // Para que cada enemigo se tambalee distinto
        GetNewPatrolTarget();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, zPosition);

        if (player == null) { rb.linearVelocity = Vector2.zero; return; }
        if (isKnockedBack) return;
        if (atacando) { rb.linearVelocity = Vector2.zero; return; }

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

            Vector2 targetPos = ObtenerObjetivoCamino();
            MoverConTambaleo(targetPos, speed);
        }
        else
        {
            jugadorDetectado = false;
            currentPathIndex = -1; // Pierde el rastro si te alejas mucho
            Patrol();
        }
    }

    private void MoverConTambaleo(Vector2 target, float vel)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        
        // Calcular tambaleo (Vector perpendicular)
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        dir += perpendicular * Mathf.Sin(Time.time * wobbleSpeed + randomWobbleOffset) * wobbleAmount;

        rb.linearVelocity = dir.normalized * vel;
    }

    private Vector2 ObtenerObjetivoCamino()
    {
        // 1. Si no hay pared entre el enemigo y el jugador, va directo a él
        if (TieneLineaDeVision(player.position))
        {
            currentPathIndex = -1; 
            return player.position;
        }

        // 2. Si hay pared, sigue las migas de pan
        if (MovimientoPlayer.historialPosiciones.Count == 0) return player.position;

        // Si perdimos el índice, buscamos la miga más cercana
        if (currentPathIndex == -1 || currentPathIndex >= MovimientoPlayer.historialPosiciones.Count)
        {
            currentPathIndex = BuscarIndiceMasCercanoYVisible();
        }

        Vector2 nodoDestino = MovimientoPlayer.historialPosiciones[currentPathIndex];

        // Cuando llegue a esa miga, cambia el objetivo a la siguiente
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
            // Solo elige migas que pueda ver sin atravesar paredes
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!atacando && Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(AtacarJugador(collision.gameObject));
            }
        }
    }

    private IEnumerator AtacarJugador(GameObject objetivo)
    {
        atacando = true;
        var visual = GetComponent<AnimacionEnemigoDinamica>();
        float espera = visual != null ? visual.Atacar(objetivo.transform.position) : 0f;
        if (espera > 0f) yield return new WaitForSeconds(espera);
        if (objetivo != null && objetivo.activeInHierarchy && !isKnockedBack
            && Vector2.Distance(objetivo.transform.position, transform.position) <= 0.95f)
        {
            objetivo.GetComponent<PlayerHealth>()?.TakeDamage(damage, DeathCause.Mutante);
            var cuerpoJugador = objetivo.GetComponent<Rigidbody2D>();
            if (cuerpoJugador != null)
                cuerpoJugador.AddForce((objetivo.transform.position - transform.position).normalized * knockbackForce, ForceMode2D.Impulse);
        }
        atacando = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        atacando = false;
    }
}
