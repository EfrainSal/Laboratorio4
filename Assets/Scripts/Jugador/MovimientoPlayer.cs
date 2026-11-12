using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Necesario para usar List<>

public class MovimientoPlayer : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    [Range(0.1f, 1f)]
    public float factorVelocidadReversa = 0.6f; 

    [Header("Apuntado y Visuales")]
    public Transform pivoteArma; 
    public SpriteRenderer spriteCuerpo; 

    [Header("Audio de Pasos")]
    public AudioClip[] sonidosPasos;
    public float tiempoEntrePasos = 0.35f; 

    [Header("Rastro para Enemigos")]
    public float distanciaEntreHuellas = 0.5f; // Cada cuánta distancia deja una miga
    public static List<Vector2> historialPosiciones = new List<Vector2>();
    private Vector2 ultimaPosicionGuardada;
    
    private Rigidbody2D rb;
    private Camera cam;
    private AudioSource sfxSource;
    
    private Vector2 direccionMovimiento;
    private Vector2 posicionRaton;
    private Vector2 joystickApuntado;
    private Vector2 direccionMirada;
    private float temporizadorPasos;

    public Vector2 DireccionEntrada => direccionMovimiento;
    public Vector2 DireccionMirada => direccionMirada;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main; 
        sfxSource = GetComponent<AudioSource>(); 
        rb.freezeRotation = true; 

        // Inicializar el rastro
        historialPosiciones.Clear();
        ultimaPosicionGuardada = transform.position;
        historialPosiciones.Add(ultimaPosicionGuardada);
    }

    public void OnMove(InputValue value)
    {
        direccionMovimiento = value.Get<Vector2>();
    }

    public void OnLookMouse(InputValue value)
    {
        posicionRaton = value.Get<Vector2>();
    }

    public void OnAimGamepad(InputValue value)
    {
        joystickApuntado = value.Get<Vector2>();
    }

    void Update()
    {
        ActualizarDireccionMirada();
        ManejarSonidoPasos();
    }

    void FixedUpdate()
    {
        MoverJugador();
        RotarJugador();
        ActualizarRastro();
    }

    private void ActualizarRastro()
    {
        // Si el jugador se movió lo suficiente, guardamos su posición
        if (Vector2.Distance(transform.position, ultimaPosicionGuardada) >= distanciaEntreHuellas)
        {
            historialPosiciones.Add(transform.position);
            
            // Limitamos a 150 posiciones (aprox 75 metros de rastro) para no saturar la memoria
            if (historialPosiciones.Count > 150)
            {
                historialPosiciones.RemoveAt(0);
            }
            ultimaPosicionGuardada = transform.position;
        }
    }

    private void ActualizarDireccionMirada()
    {
        if (joystickApuntado.sqrMagnitude > 0.1f)
            direccionMirada = joystickApuntado.normalized;
        else
        {
            Vector3 posRatonMundo = cam.ScreenToWorldPoint(posicionRaton);
            direccionMirada = ((Vector2)posRatonMundo - (Vector2)transform.position).normalized;
        }
    }

    private void MoverJugador()
    {
        float dotProduct = Vector2.Dot(direccionMovimiento.normalized, direccionMirada);
        bool enReversa = dotProduct < -0.2f; 
        float velocidadActual = enReversa ? velocidad * factorVelocidadReversa : velocidad;
        rb.linearVelocity = direccionMovimiento * velocidadActual; 
    }

    private void ManejarSonidoPasos()
    {
        if (direccionMovimiento.sqrMagnitude > 0.01f)
        {
            float dotProduct = Vector2.Dot(direccionMovimiento.normalized, direccionMirada);
            float ajusteTiempo = (dotProduct < -0.2f) ? tiempoEntrePasos / factorVelocidadReversa : tiempoEntrePasos;

            temporizadorPasos -= Time.deltaTime;
            if (temporizadorPasos <= 0f)
            {
                if (sonidosPasos.Length > 0 && sfxSource != null)
                {
                    sfxSource.PlayOneShot(sonidosPasos[Random.Range(0, sonidosPasos.Length)]);
                }
                temporizadorPasos = ajusteTiempo;
            }
        }
        else
        {
            temporizadorPasos = 0f; 
        }
    }

    private void RotarJugador()
    {
        if (pivoteArma != null)
        {
            float angulo = Mathf.Atan2(direccionMirada.y, direccionMirada.x) * Mathf.Rad2Deg;
            pivoteArma.rotation = Quaternion.Euler(0, 0, angulo);
            pivoteArma.localScale = (angulo > 90 || angulo < -90) ? new Vector3(1, -1, 1) : new Vector3(1, 1, 1);
        }

        if (spriteCuerpo != null)
        {
            if (direccionMirada.x < 0) spriteCuerpo.flipX = true; 
            else if (direccionMirada.x > 0) spriteCuerpo.flipX = false; 
        }
    }
}