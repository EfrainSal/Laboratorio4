using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Laboratorio4.Animacion;

/// <summary>
/// Controla al personaje compañero (el personaje no elegido por el jugador) durante la batalla
/// contra el jefe final en Escenario4_Jefe.
/// Reglas de diseño:
/// 1. El jefe siempre persigue al jugador real (el ayudante no tiene tag "Player").
/// 2. El ayudante ataca de forma lenta y táctica para brindar apoyo sin trivializar el combate.
/// 3. El ayudante es inmortal y no utiliza su habilidad especial.
/// 4. Se mueve libremente por la arena de forma autónoma (no sigue pegado al jugador).
/// 5. Cuando el jefe tiene <= 5% de vida, el ayudante deja de atacarlo para nunca robar el golpe final.
/// </summary>
public class AyudanteJefeAI : MonoBehaviour
{
    public const float UmbralAltoElFuegoJefe = 0.05f;

    [Header("Movimiento Autónomo en la Arena")]
    public float velocidadMovimiento = 3.7f;
    public Vector2 limiteArenaMin = new Vector2(9.2f, -24.2f);
    public Vector2 limiteArenaMax = new Vector2(33.5f, -9.2f);
    public float distanciaMinimaAlJefe = 4.8f;
    public float distanciaOptimaMinJefe = 6.0f;
    public float distanciaOptimaMaxJefe = 11.2f;
    public float distanciaMinimaAlJugador = 2.8f;

    [Header("Combate Táctico y Lento")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fuerzaBala = 18f;
    public float danioPorBala = 8f;
    public float tiempoMinEntreAtaques = 2.6f;
    public float tiempoMaxEntreAtaques = 3.8f;
    public float retardoEntreDisparosRafaga = 0.22f;
    public float dispersionGrados = 3.5f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoDisparo;
    public AudioClip[] sonidosPasos;
    public float tiempoEntrePasos = 0.42f;
    [Range(0.1f, 1f)] public float volumenDisparo = 0.42f;
    [Range(0.05f, 1f)] public float volumenPasos = 0.35f;

    private Rigidbody2D rb;
    private CapsuleCollider2D colisionadorPropio;
    private SpriteRenderer spriteCuerpo;
    private AnimadorSprites8Direcciones animador;
    private ArmaIndependiente arma;

    private Transform jugador;
    private Transform jefe;
    private BossHealth saludJefe;

    private Vector2 destinoActual;
    private bool tieneDestino;
    private float temporizadorPausaDestino;
    private float temporizadorCambioDestino;
    private float temporizadorAtasco;
    private Vector2 ultimaPosicionControl;

    private float temporizadorAtaque;
    private bool disparandoRafaga;
    private float temporizadorPasos;
    private Vector2 direccionMirada = Vector2.right;
    private int direccion8Actual;

    public bool EnAltoElFuego =>
        saludJefe != null && (saludJefe.IsDead || saludJefe.CurrentHealth <= saludJefe.maxHealth * UmbralAltoElFuegoJefe);

    /// <summary>
    /// Convierte una instancia inactiva de un prefab de jugador (Jason o Milena) en el ayudante de IA
    /// antes de que se ejecuten los Awake/Start de los componentes exclusivos del jugador.
    /// </summary>
    public static AyudanteJefeAI ConfigurarInstanciaComoAyudante(GameObject instancia)
    {
        if (instancia == null) return null;

        // 1. Cambiar tag y nombre para que el Jefe, enemigos, cámara y balas enemigas ignoren al ayudante
        instancia.tag = "Untagged";
        instancia.name = instancia.name.Replace("(Clone)", "").Trim() + "_Ayudante";

        // 2. Extraer referencias de disparo y audio antes de remover los scripts de jugador
        PlayerShooting shooting = instancia.GetComponent<PlayerShooting>();
        GameObject prefabBala = shooting != null ? shooting.bulletPrefab : null;
        Transform puntoDisparo = shooting != null ? shooting.firePoint : null;
        AudioClip clipDisparo = shooting != null ? shooting.sonidoDisparo : null;
        AudioSource fuenteAudio = shooting != null && shooting.sfxSource != null
            ? shooting.sfxSource
            : instancia.GetComponent<AudioSource>();

        MovimientoPlayer movPlayer = instancia.GetComponent<MovimientoPlayer>();
        AudioClip[] clipsPasos = movPlayer != null ? movPlayer.sonidosPasos : null;

        // 3. Desactivar visual del escudo si es Jason (no usa habilidades especiales)
        ShieldAbility shield = instancia.GetComponent<ShieldAbility>();
        if (shield != null && shield.escudoVisual != null)
        {
            shield.escudoVisual.SetActive(false);
        }
        Transform shieldChild = instancia.transform.Find("Shield");
        if (shieldChild != null)
        {
            shieldChild.gameObject.SetActive(false);
        }

        // 4. Eliminar componentes exclusivos del jugador humano (orden respetando RequireComponent)
        DestruirComponenteInmediato<AnimacionJasonDinamica>(instancia);
        DestruirComponenteInmediato<MovimientoPlayer>(instancia);
        DestruirComponenteInmediato<MiraPersonalizada>(instancia);
        DestruirComponenteInmediato<InteraccionJugador>(instancia);
        DestruirComponenteInmediato<PlayerInventory>(instancia);
        DestruirComponenteInmediato<PlayerExperience>(instancia);
        DestruirComponenteInmediato<ShieldAbility>(instancia);
        DestruirComponenteInmediato<DashAbility>(instancia);
        DestruirComponenteInmediato<PlayerCombat>(instancia);
        DestruirComponenteInmediato<MacheteAnimacion>(instancia);
        DestruirComponenteInmediato<PlayerShooting>(instancia);
        DestruirComponenteInmediato<WeaponManager>(instancia);
        DestruirComponenteInmediato<RetroalimentacionDano>(instancia);
        DestruirComponenteInmediato<PlayerHealth>(instancia);
        DestruirComponenteInmediato<PlayerInput>(instancia);

        // Si quedó algún indicador de recarga creado por edición o prefab, removerlo
        Transform indicadorRecarga = instancia.transform.Find("IndicadorRecarga");
        if (indicadorRecarga != null)
        {
            DestroyImmediate(indicadorRecarga.gameObject);
        }

        // 5. Configurar ArmaIndependiente en modo manual para que apunte hacia donde indique la IA
        ArmaIndependiente armaInd = instancia.GetComponent<ArmaIndependiente>();
        if (armaInd != null)
        {
            armaInd.controlManual = true;
            if (puntoDisparo == null) puntoDisparo = armaInd.salidaDisparo;
        }

        // 6. Añadir y configurar el componente AyudanteJefeAI
        AyudanteJefeAI ia = instancia.GetComponent<AyudanteJefeAI>();
        if (ia == null) ia = instancia.AddComponent<AyudanteJefeAI>();

        ia.bulletPrefab = prefabBala;
        ia.firePoint = puntoDisparo;
        ia.sonidoDisparo = clipDisparo;
        ia.sfxSource = fuenteAudio;
        ia.sonidosPasos = clipsPasos;

        return ia;
    }

    private static void DestruirComponenteInmediato<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp != null)
        {
            DestroyImmediate(comp);
        }
    }

    private void Awake()
    {
        gameObject.tag = "Untagged";
        rb = GetComponent<Rigidbody2D>();
        colisionadorPropio = GetComponent<CapsuleCollider2D>();
        spriteCuerpo = GetComponent<SpriteRenderer>();
        animador = GetComponent<AnimadorSprites8Direcciones>();
        arma = GetComponent<ArmaIndependiente>();
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        if (arma != null)
        {
            arma.controlManual = true;
            if (firePoint == null) firePoint = arma.salidaDisparo;
        }

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.gravityScale = 0f;
        }
    }

    private void Start()
    {
        ActualizarReferenciasCombate();
        IgnorarColisionesConActores();

        ultimaPosicionControl = transform.position;
        temporizadorAtaque = Random.Range(1.4f, 2.4f);

        // Si empieza en el pasillo de entrada, su primer destino será adentrarse en la arena
        ElegirNuevoDestinoLibre(true);
    }

    private void Update()
    {
        if (DeathScreenController.GameplayBlocked || ComicFinal.EsperaCombate(this))
        {
            return;
        }

        ActualizarReferenciasCombate();
        ActualizarMiradaYApuntado();
        ManejarCombateTactico();
        ManejarSonidoPasos();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (DeathScreenController.GameplayBlocked || ComicFinal.EsperaCombate(this))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        MoverDeFormaAutonoma();
    }

    private void LateUpdate()
    {
        ActualizarAnimacion8Direcciones();
        if (arma != null)
        {
            Vector2 puntoApuntado = ObtenerPuntoApuntadoActual();
            arma.Apuntar(puntoApuntado);
        }
    }

    private void ActualizarReferenciasCombate()
    {
        if (jugador == null)
        {
            if (LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null)
            {
                jugador = LevelManager.Instance.JugadorActual.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null && playerObj != gameObject)
                {
                    jugador = playerObj.transform;
                }
            }
            if (jugador != null) IgnorarColisionesConActores();
        }

        if (jefe == null || saludJefe == null)
        {
            BossAI bossAI = FindAnyObjectByType<BossAI>();
            if (bossAI != null)
            {
                jefe = bossAI.transform;
                saludJefe = bossAI.GetComponent<BossHealth>();
                IgnorarColisionesConActores();
            }
        }
    }

    private void IgnorarColisionesConActores()
    {
        if (colisionadorPropio == null) return;

        if (jugador != null)
        {
            foreach (Collider2D col in jugador.GetComponentsInChildren<Collider2D>())
            {
                if (col != null) Physics2D.IgnoreCollision(colisionadorPropio, col, true);
            }
        }

        if (jefe != null)
        {
            foreach (Collider2D col in jefe.GetComponentsInChildren<Collider2D>())
            {
                if (col != null) Physics2D.IgnoreCollision(colisionadorPropio, col, true);
            }
        }
    }

    private void MoverDeFormaAutonoma()
    {
        Vector2 posActual = rb.position;

        // 1. Si el jefe se acerca demasiado al ayudante, reposicionarse tácticamente lejos del jefe
        if (jefe != null && (saludJefe == null || !saludJefe.IsDead))
        {
            float distAlJefe = Vector2.Distance(posActual, jefe.position);
            if (distAlJefe < distanciaMinimaAlJefe && temporizadorCambioDestino <= 2.2f)
            {
                ElegirDestinoEvasivoRespectoAlJefe();
                temporizadorPausaDestino = 0f;
            }
        }

        // 2. Si está haciendo una breve pausa táctica en su posición, mantenerse quieto observando/apuntando
        if (temporizadorPausaDestino > 0f)
        {
            temporizadorPausaDestino -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;
            if (temporizadorPausaDestino <= 0f)
            {
                ElegirNuevoDestinoLibre(false);
            }
            return;
        }

        temporizadorCambioDestino -= Time.fixedDeltaTime;
        if (!tieneDestino || temporizadorCambioDestino <= 0f)
        {
            ElegirNuevoDestinoLibre(false);
        }

        Vector2 haciaDestino = destinoActual - posActual;
        float distanciaRestante = haciaDestino.magnitude;

        if (distanciaRestante <= 0.45f)
        {
            rb.linearVelocity = Vector2.zero;
            // Pausa orgánica de 0.5 a 1.4 segundos antes de decidir otro movimiento libre
            temporizadorPausaDestino = Random.Range(0.55f, 1.45f);
            return;
        }

        Vector2 dirDeseada = ObtenerDireccionLibreDeObstaculos(posActual, haciaDestino.normalized);

        // Reducir ligeramente la velocidad cuando camina de espaldas a donde apunta (como un jugador real)
        float dotMirada = Vector2.Dot(dirDeseada, direccionMirada);
        float factorVel = dotMirada < -0.2f ? 0.78f : 1f;

        rb.linearVelocity = dirDeseada * (velocidadMovimiento * factorVel);

        // Detector de atasco contra muros o columnas de cobertura
        temporizadorAtasco += Time.fixedDeltaTime;
        if (temporizadorAtasco >= 0.45f)
        {
            if (Vector2.Distance(posActual, ultimaPosicionControl) < 0.18f)
            {
                ElegirNuevoDestinoLibre(false);
            }
            ultimaPosicionControl = posActual;
            temporizadorAtasco = 0f;
        }
    }

    private Vector2 ObtenerDireccionLibreDeObstaculos(Vector2 origen, Vector2 dirObjetivo)
    {
        if (!HayObstaculoSolidoEnDireccion(origen, dirObjetivo, 1.05f))
        {
            return dirObjetivo;
        }

        // Probar ángulos alternativos para rodear muros de cobertura con naturalidad
        float[] angulosPrueba = { 35f, -35f, 65f, -65f, 95f, -95f, 130f, -130f };
        foreach (float angulo in angulosPrueba)
        {
            Vector2 dirAlternativa = RotarVector(dirObjetivo, angulo);
            if (!HayObstaculoSolidoEnDireccion(origen, dirAlternativa, 1.0f))
            {
                return dirAlternativa;
            }
        }

        return -dirObjetivo;
    }

    private bool HayObstaculoSolidoEnDireccion(Vector2 origen, Vector2 direccion, float distancia)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origen, 0.28f, direccion, distancia);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Boss") || hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Drop"))
                continue;
            if (hit.collider.GetComponentInParent<AyudanteJefeAI>() != null) continue;

            return true;
        }
        return false;
    }

    private void ElegirNuevoDestinoLibre(bool entradaInicial)
    {
        Vector2 posActual = rb != null ? rb.position : (Vector2)transform.position;

        // Si el ayudante aún está en el pasillo izquierdo (X < 8.5), avanzar hacia la puerta de la arena primero
        if (posActual.x < 8.5f || entradaInicial && posActual.x < 9.5f)
        {
            destinoActual = new Vector2(Random.Range(11.0f, 15.5f), Random.Range(-16.5f, -12.0f));
            tieneDestino = true;
            temporizadorCambioDestino = Random.Range(3.2f, 4.8f);
            return;
        }

        Vector2 mejorCandidato = posActual;
        float mejorPuntaje = float.NegativeInfinity;

        for (int i = 0; i < 14; i++)
        {
            Vector2 candidato = new Vector2(
                Random.Range(limiteArenaMin.x, limiteArenaMax.x),
                Random.Range(limiteArenaMin.y, limiteArenaMax.y)
            );

            // Evitar puntos dentro de un muro de cobertura
            if (PuntoOcupadoPorMuro(candidato)) continue;

            float puntaje = 0f;
            float distPropia = Vector2.Distance(posActual, candidato);

            // Preferir desplazamientos medianos (de 3.5 a 10 unidades) para que recorra la sala con vida propia
            if (distPropia >= 3.5f && distPropia <= 10.5f) puntaje += 3.0f;
            else if (distPropia < 2.0f) puntaje -= 4.0f;

            // Evaluar posición táctica respecto al jefe
            if (jefe != null)
            {
                float distJefe = Vector2.Distance(candidato, jefe.position);
                if (distJefe >= distanciaOptimaMinJefe && distJefe <= distanciaOptimaMaxJefe)
                {
                    puntaje += 4.0f;
                }
                else if (distJefe < distanciaMinimaAlJefe)
                {
                    puntaje -= 6.0f;
                }
                else
                {
                    puntaje -= Mathf.Abs(distJefe - distanciaOptimaMaxJefe) * 0.35f;
                }
            }

            // No seguir pegado al jugador: mantener su propio espacio y buscar ángulos distintos (flanqueo)
            if (jugador != null)
            {
                float distJugador = Vector2.Distance(candidato, jugador.position);
                if (distJugador < distanciaMinimaAlJugador)
                {
                    puntaje -= 5.0f;
                }
                else if (distJugador >= 4.0f && distJugador <= 14.0f)
                {
                    puntaje += 2.0f;
                }

                if (jefe != null)
                {
                    Vector2 dirJefeAJugador = ((Vector2)jugador.position - (Vector2)jefe.position).normalized;
                    Vector2 dirJefeACandidato = (candidato - (Vector2)jefe.position).normalized;
                    // Premiar ángulos laterales o de flanqueo respecto al jugador
                    float alineacion = Vector2.Dot(dirJefeAJugador, dirJefeACandidato);
                    if (alineacion < 0.5f) puntaje += 1.8f;
                }
            }

            // Ligera variación aleatoria para comportamiento orgánico
            puntaje += Random.Range(-0.8f, 0.8f);

            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejorCandidato = candidato;
            }
        }

        destinoActual = mejorCandidato;
        tieneDestino = true;
        temporizadorCambioDestino = Random.Range(2.8f, 5.2f);
    }

    private void ElegirDestinoEvasivoRespectoAlJefe()
    {
        if (jefe == null)
        {
            ElegirNuevoDestinoLibre(false);
            return;
        }

        Vector2 posActual = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 dirAlejarse = (posActual - (Vector2)jefe.position).normalized;
        if (dirAlejarse.sqrMagnitude < 0.01f) dirAlejarse = Vector2.right;

        for (int i = 0; i < 8; i++)
        {
            float angulo = Random.Range(-55f, 55f);
            Vector2 dir = RotarVector(dirAlejarse, angulo);
            Vector2 candidato = posActual + dir * Random.Range(5.0f, 8.0f);
            candidato.x = Mathf.Clamp(candidato.x, limiteArenaMin.x, limiteArenaMax.x);
            candidato.y = Mathf.Clamp(candidato.y, limiteArenaMin.y, limiteArenaMax.y);

            if (!PuntoOcupadoPorMuro(candidato))
            {
                destinoActual = candidato;
                tieneDestino = true;
                temporizadorCambioDestino = Random.Range(2.4f, 3.8f);
                return;
            }
        }

        ElegirNuevoDestinoLibre(false);
    }

    private bool PuntoOcupadoPorMuro(Vector2 punto)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(punto, 0.55f);
        foreach (Collider2D col in cols)
        {
            if (col == null || col.isTrigger) continue;
            if (col.gameObject == gameObject) continue;
            if (col.CompareTag("Player") || col.CompareTag("Boss") || col.CompareTag("Enemy") || col.CompareTag("Drop"))
                continue;
            return true;
        }
        return false;
    }

    private void ActualizarMiradaYApuntado()
    {
        if (jefe != null && (saludJefe == null || !saludJefe.IsDead))
        {
            Vector2 haciaJefe = (Vector2)jefe.position - (Vector2)transform.position;
            if (haciaJefe.sqrMagnitude > 0.001f)
            {
                direccionMirada = haciaJefe.normalized;
                return;
            }
        }

        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            direccionMirada = rb.linearVelocity.normalized;
        }
    }

    private Vector2 ObtenerPuntoApuntadoActual()
    {
        if (jefe != null && (saludJefe == null || !saludJefe.IsDead))
        {
            return jefe.position;
        }

        return (Vector2)transform.position + direccionMirada * 3f;
    }

    private void ManejarCombateTactico()
    {
        // Si el jefe no existe, murió o llegó al 5% de vida (o menos), el ayudante detiene todo ataque
        if (jefe == null || saludJefe == null || EnAltoElFuego)
        {
            if (disparandoRafaga)
            {
                StopAllCoroutines();
                disparandoRafaga = false;
            }
            return;
        }

        if (disparandoRafaga) return;

        temporizadorAtaque -= Time.deltaTime;
        if (temporizadorAtaque > 0f) return;

        float distanciaAlJefe = Vector2.Distance(transform.position, jefe.position);
        if (distanciaAlJefe > 16.5f || !TieneLineaDeTiroAlJefe())
        {
            // Reintentar pronto mientras busca línea de tiro
            temporizadorAtaque = 0.65f;
            return;
        }

        StartCoroutine(EjecutarAtaqueTactico());
    }

    private bool TieneLineaDeTiroAlJefe()
    {
        if (jefe == null) return false;
        Vector2 origen = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 destino = jefe.position;
        Vector2 dir = destino - origen;
        float dist = dir.magnitude;
        if (dist <= 0.1f) return true;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir.normalized, dist);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Drop"))
                continue;
            if (hit.collider.CompareTag("Boss") || hit.collider.GetComponentInParent<BossHealth>() != null)
                return true;

            // Si antes del jefe hay un muro sólido de cobertura, no gastar el disparo contra el muro
            return false;
        }

        return true;
    }

    private IEnumerator EjecutarAtaqueTactico()
    {
        disparandoRafaga = true;

        // Disparo táctico pausado: 1 o 2 disparos espaciados, nunca ráfagas masivas
        int disparos = Random.value < 0.55f ? 1 : 2;

        for (int i = 0; i < disparos; i++)
        {
            if (DeathScreenController.GameplayBlocked || EnAltoElFuego)
            {
                break;
            }

            DispararBalaTactica();

            if (i < disparos - 1)
            {
                yield return new WaitForSeconds(retardoEntreDisparosRafaga);
            }
        }

        temporizadorAtaque = Random.Range(tiempoMinEntreAtaques, tiempoMaxEntreAtaques);
        disparandoRafaga = false;
    }

    private void DispararBalaTactica()
    {
        if (bulletPrefab == null || firePoint == null || jefe == null || EnAltoElFuego) return;

        // Asegurar que el arma apunte al jefe en el instante del disparo
        if (arma != null)
        {
            arma.Apuntar(jefe.position);
            arma.MostrarDisparo();
        }

        Vector2 dirAlJefe = ((Vector2)jefe.position - (Vector2)firePoint.position).normalized;
        float anguloBase = Mathf.Atan2(dirAlJefe.y, dirAlJefe.x) * Mathf.Rad2Deg;
        float anguloFinal = anguloBase + Random.Range(-dispersionGrados, dispersionGrados);
        Quaternion rotacionDisparo = Quaternion.Euler(0f, 0f, anguloFinal);

        GameObject balaObj = Instantiate(bulletPrefab, firePoint.position, rotacionDisparo);

        // Ignorar colisión física con el propio ayudante y con el jugador
        Collider2D colBala = balaObj.GetComponent<Collider2D>();
        if (colBala != null && colisionadorPropio != null)
        {
            Physics2D.IgnoreCollision(colBala, colisionadorPropio, true);
        }

        Bullet scriptBala = balaObj.GetComponent<Bullet>();
        if (scriptBala != null)
        {
            scriptBala.damage = danioPorBala;
            scriptBala.esDeAyudante = true;
            scriptBala.umbralMinimoVidaJefe = UmbralAltoElFuegoJefe;
        }

        Rigidbody2D rbBala = balaObj.GetComponent<Rigidbody2D>();
        if (rbBala != null)
        {
            Vector2 dirImpulso = rotacionDisparo * Vector3.right;
            rbBala.AddForce(dirImpulso * fuerzaBala, ForceMode2D.Impulse);
        }

        if (sfxSource != null && sonidoDisparo != null)
        {
            sfxSource.PlayOneShot(sonidoDisparo, volumenDisparo);
        }
    }

    private void ActualizarAnimacion8Direcciones()
    {
        if (animador == null) return;

        if (direccionMirada.sqrMagnitude > 0.001f)
        {
            direccion8Actual = AnimacionJasonDinamica.Direccion8(direccionMirada);
        }

        bool enMovimiento = rb != null && rb.linearVelocity.sqrMagnitude > 0.04f;
        animador.Reproducir(enMovimiento ? "Caminar" : "Quieto", direccion8Actual);

        if (spriteCuerpo != null)
        {
            spriteCuerpo.flipX = false;
        }
    }

    private void ManejarSonidoPasos()
    {
        if (rb == null || sonidosPasos == null || sonidosPasos.Length == 0 || sfxSource == null) return;

        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            temporizadorPasos -= Time.deltaTime;
            if (temporizadorPasos <= 0f)
            {
                AudioClip clip = sonidosPasos[Random.Range(0, sonidosPasos.Length)];
                if (clip != null) sfxSource.PlayOneShot(clip, volumenPasos);
                temporizadorPasos = tiempoEntrePasos;
            }
        }
        else
        {
            temporizadorPasos = 0f;
        }
    }

    private static Vector2 RotarVector(Vector2 v, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
