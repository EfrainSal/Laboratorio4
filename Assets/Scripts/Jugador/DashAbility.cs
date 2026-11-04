using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DashAbility : MonoBehaviour
{
    [Header("Configuración del Dash")]
    public float fuerzaDash = 25f;
    public float duracionDash = 0.2f;
    public float cooldown = 3f;

    private bool habilidadLista = true;
    private bool haciendoDash = false;
    private Vector2 direccionInput;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoDash;

    [Header("Colisiones")]
    [Tooltip("La capa original de tu jugador (ej. 'Player')")]
    public string capaJugador = "Player";
    [Tooltip("Una capa que ignora a los enemigos, pero choca con paredes (debes crearla en Unity)")]
    public string capaDash = "PlayerDash";

    private Rigidbody2D rb;
    private MovimientoPlayer movPlayer; // Tu script de movimiento actual

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movPlayer = GetComponent<MovimientoPlayer>();
    }

    // Leemos el movimiento para saber hacia dónde hacer el dash
    public void OnMove(InputValue value)
    {
        direccionInput = value.Get<Vector2>();
    }

    public void OnAbility(InputValue value)
    {
        // Solo podemos hacer dash si está listo, no estamos dasheando ya, y nos estamos moviendo
        if (value.isPressed && habilidadLista && !haciendoDash && direccionInput != Vector2.zero)
        {
            StartCoroutine(ActivarDash());
        }
    }

    private IEnumerator ActivarDash()
    {
        habilidadLista = false;
        haciendoDash = true;

        // Reproducir sonido
        if (sfxSource != null && sonidoDash != null)
        {
            sfxSource.PlayOneShot(sonidoDash);
        }

        if (movPlayer != null) movPlayer.enabled = false;
        
        // Cambiar la capa para atravesar enemigos
        gameObject.layer = LayerMask.NameToLayer(capaDash);

        rb.linearVelocity = direccionInput.normalized * fuerzaDash;

        yield return new WaitForSeconds(duracionDash);

        haciendoDash = false;
        if (movPlayer != null) movPlayer.enabled = true;
        
        // Restaurar la capa original para volver a colisionar con enemigos
        gameObject.layer = LayerMask.NameToLayer(capaJugador);

        yield return new WaitForSeconds(cooldown);
        habilidadLista = true;
    }
}