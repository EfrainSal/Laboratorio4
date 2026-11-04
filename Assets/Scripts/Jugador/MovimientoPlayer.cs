using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoPlayer : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    
    [Header("Audio de Pasos")]
    [Tooltip("Arrastra aquí los audios Paso_1, Paso_2 y Paso_3")]
    public AudioClip[] sonidosPasos;
    public float tiempoEntrePasos = 0.35f; 
    
    private Rigidbody2D rb;
    private Camera cam;
    private AudioSource sfxSource;
    
    private Vector2 direccionMovimiento;
    private Vector2 posicionRaton;
    private Vector2 joystickApuntado;
    
    private float temporizadorPasos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main; 
        sfxSource = GetComponent<AudioSource>(); 
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
        ManejarSonidoPasos();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direccionMovimiento * velocidad; 
        RotarJugador();
    }

    private void ManejarSonidoPasos()
    {
        // Si hay movimiento (magnitud mayor a 0)
        if (direccionMovimiento.sqrMagnitude > 0.01f)
        {
            temporizadorPasos -= Time.deltaTime;

            if (temporizadorPasos <= 0f)
            {
                ReproducirPasoAleatorio();
                temporizadorPasos = tiempoEntrePasos;
            }
        }
        else
        {
            // Si nos detenemos, reiniciamos el temporizador para que el primer paso suene de inmediato al reanudar
            temporizadorPasos = 0f; 
        }
    }

    private void ReproducirPasoAleatorio()
    {
        if (sonidosPasos.Length > 0 && sfxSource != null)
        {
            int indexAleatorio = Random.Range(0, sonidosPasos.Length);
            // Usamos PlayOneShot para que los sonidos puedan sobreponerse ligeramente si corres muy rápido
            sfxSource.PlayOneShot(sonidosPasos[indexAleatorio]);
        }
    }

    private void RotarJugador()
    {
        if (joystickApuntado.sqrMagnitude > 0.1f)
        {
            Vector2 direccionMirada = joystickApuntado.normalized;
            float angulo = Mathf.Atan2(direccionMirada.y, direccionMirada.x) * Mathf.Rad2Deg;
            rb.rotation = angulo;
        }
        else
        {
            Vector3 posicionRatonMundo = cam.ScreenToWorldPoint(posicionRaton);
            Vector2 direccionMirada = (posicionRatonMundo - transform.position).normalized;
            float angulo = Mathf.Atan2(direccionMirada.y, direccionMirada.x) * Mathf.Rad2Deg;
            rb.rotation = angulo;
        }
    }
}