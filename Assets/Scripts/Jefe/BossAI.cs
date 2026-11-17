using UnityEngine;
using System.Collections;

public class BossAI : MonoBehaviour
{
    public int CurrentPhase { get; private set; } = 1;

    [Header("Movimiento")]
    public float moveSpeed = 2.5f;
    public float attackRange = 2f; // Distancia para pegar un zarpazo
    [Min(.1f)] public float aceleracion = 12f;
    [Min(.1f)] public float desaceleracion = 18f;

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
    public AudioClip sfxRugidoInicio;
    [Min(.1f)] public float radioEscucha = 36f;
    [Range(0, 1)] public float volumenCercano = .8f;
    [Header("Inicio del combate")]
    [Min(.1f)] public float duracionInicio = 1.1f;
    [Min(0)] public float intensidadSacudida = .16f;
    [Min(.1f)] public float duracionSacudida = .7f;
    public int RugidosIniciales { get; private set; }
    public AudioClip UltimoSonido { get; private set; }

    private Transform player;
    private Rigidbody2D rb;
    private AnimacionJefeDinamica animacion;
    private NavegacionJefe navegacion;
    private SacudidaCamaraJefe sacudida;
    private bool inicioSolicitado, inicioAnunciado, rugiendo;
    private float finInicio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animacion = GetComponent<AnimacionJefeDinamica>();
        navegacion = GetComponent<NavegacionJefe>();
        if (navegacion == null) navegacion = gameObject.AddComponent<NavegacionJefe>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.Stop(); audioSource.enabled = true; audioSource.mute = false; audioSource.loop = false;
        audioSource.playOnAwake = false; audioSource.spatialBlend = 0; audioSource.priority = 64;
        audioSource.dopplerLevel = 0; audioSource.ignoreListenerPause = false;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        attackTimer = timeBetweenAttacks;

        ActualizarReferenciaJugador();
    }
    public void IniciarCombateDramatico()
    {
        if (inicioAnunciado) return;
        inicioAnunciado = true; inicioSolicitado = true;
    }
    private void Update() { ActualizarVolumenJefe(); }
    private void ActualizarVolumenJefe()
    {
        if (audioSource == null) return;
        if (player == null) ActualizarReferenciaJugador();
        float distancia = player != null ? Vector2.Distance(player.position, transform.position) : radioEscucha;
        float ganancia = distancia < radioEscucha ? Mathf.Lerp(1, .4f, Mathf.InverseLerp(4, radioEscucha, distancia)) : 0;
        float disparos = AudioEnemigosCercanos.Instancia != null ? Mathf.Max(.55f, AudioEnemigosCercanos.Instancia.GananciaEnemigos) : 1;
        audioSource.volume = DeathScreenController.GameplayBlocked || Time.timeScale <= 0 ? 0 : volumenCercano * ganancia * disparos;
    }

    private void ActualizarReferenciaJugador()
    {
        // El jefe SIEMPRE debe seguir al jugador real y nunca al ayudante
        if (LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null)
        {
            player = LevelManager.Instance.JugadorActual.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerObj.GetComponent<AyudanteJefeAI>() == null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
    if (DeathScreenController.GameplayBlocked || ComicFinal.EsperaCombate(this))
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }
    // Si el jugador aún no existe o cambió la referencia, asegurar que sea siempre el jugador real
    if (player == null || player.GetComponent<AyudanteJefeAI>() != null ||
        (LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null && player != LevelManager.Instance.JugadorActual.transform))
    {
        ActualizarReferenciaJugador();
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return; // Detenerse y esperar al siguiente frame
        }
    }

    if (inicioSolicitado)
    {
        inicioSolicitado = false; rugiendo = true; finInicio = Time.time + duracionInicio; RugidosIniciales++;
        ReproducirSFX(sfxRugidoInicio != null ? sfxRugidoInicio : sfxInvocacion);
        var camara = Camera.main;
        if (camara != null)
        {
            sacudida = camara.GetComponent<SacudidaCamaraJefe>();
            if (sacudida == null) sacudida = camara.gameObject.AddComponent<SacudidaCamaraJefe>();
            sacudida.Disparar(duracionSacudida, intensidadSacudida);
        }
        animacion?.ComenzarAccion("CampoEnergia", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down, duracionInicio);
        attackTimer = timeBetweenAttacks;
    }
    if (rugiendo)
    {
        if (Time.time < finInicio) { rb.linearVelocity = Vector2.zero; return; }
        rugiendo = false; animacion?.TerminarAccion();
    }

    // Si el jefe ya está atacando, detener su movimiento
    if (isAttacking) 
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }

    // Perseguir al jugador
    Vector2 delta = player.position - transform.position;
    Vector2 objetivoVelocidad = delta.magnitude > attackRange * .8f || !navegacion.PasoLibre(rb.position, player.position)
        ? navegacion.Direccion(player.position) * moveSpeed : Vector2.zero;
    Vector2 velocidad = Vector2.MoveTowards(rb.linearVelocity, objetivoVelocidad,
        (objetivoVelocidad.sqrMagnitude > 0 ? aceleracion : desaceleracion) * Time.fixedDeltaTime);
    if (velocidad.sqrMagnitude > .0001f && !navegacion.PasoFisicoLibre(rb.position, rb.position + velocidad * Time.fixedDeltaTime))
        velocidad = navegacion.PasoFisicoLibre(rb.position, rb.position + objetivoVelocidad * Time.fixedDeltaTime) ? objetivoVelocidad : Vector2.zero;
    rb.linearVelocity = velocidad;

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
        float distance = Vector2.Distance(transform.position, player.position);
        // Si no puede pegar, seguir caminando sin insertar un fotograma de Quieto cada 0.2 s.
        bool lineaLibre = navegacion.PasoLibre(rb.position, player.position);
        if ((CurrentPhase == 1 && distance > attackRange) || !lineaLibre) { attackTimer = .2f; return; }
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

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
        float duracion = animacion != null ? animacion.ComenzarAccion("Ataque", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down) : .4f;
        ReproducirSFX(sfxAtaquePesado != null ? sfxAtaquePesado : sfxInvocacion);

        // Feedback visual breve antes de pegar
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = telegraphColor; 
        
        yield return new WaitForSeconds(0.4f); // Tiempo de anticipación del golpe

        // Re-comprobar distancia para ver si el jugador lo esquivó
        if (player != null && !DeathScreenController.GameplayBlocked && navegacion.PasoLibre(rb.position, player.position) && Vector2.Distance(transform.position, player.position) <= attackRange * 1.5f)
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
        // Conservar los cuadros de impacto/recuperación, que antes se cortaban a los 0.4 s.
        if (duracion > .4f) yield return new WaitForSeconds(duracion - .4f);
        TerminarAtaque();
    }

    private IEnumerator EjecutarBulletHell()
    {
        animacion?.ComenzarAccion("BolaDePoder", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down, 1f);
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
        animacion?.ComenzarAccion("CampoEnergia", player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down, 1.5f);
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
        if (audioSource != null && clip != null && !DeathScreenController.GameplayBlocked)
        {
            // Una sola voz propia para Yiba: no se pierde en la cola limitada de Sorbos ni suma rugidos.
            audioSource.Stop(); audioSource.clip = clip; audioSource.pitch = 1; UltimoSonido = clip;
            ActualizarVolumenJefe(); audioSource.Play();
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
        if (audioSource != null) audioSource.Stop();
        if (sacudida != null) sacudida.Cancelar();
    }
}
