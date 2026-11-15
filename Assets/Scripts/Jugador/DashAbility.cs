using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI; // Asegúrate de incluir esto

public class DashAbility : MonoBehaviour
{
    [Header("Configuración del Dash")]
    public float fuerzaDash = 25f;
    public float duracionDash = 0.2f;
    public float cooldown = 3f;
    public float reduccionCooldownPorBaja = 1f;

    [Header("Configuración de UI")]
    public Sprite iconoHabilidad; // Arrastra aquí el sprite de la habilidad (icono_desliz...)
    public Color colorListo = new Color(0, 1, 1, 1); // Cian brillante
    public Color colorEnCooldown = new Color(0, 0.5f, 0.5f, 1); // Cian oscuro
    
    // El LevelManager nos inyectará esto
    [HideInInspector] public Image skillBarFill;
    [HideInInspector] public Image skillIconImage;

    private bool habilidadLista = true;
    private bool haciendoDash = false;
    private bool enCooldown = false;
    private float tiempoCooldownActual = 0f;
    private Vector2 direccionInput;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoDash;

    [Header("Colisiones")]
    public string capaJugador = "Player";
    public string capaDash = "PlayerDash";

    private Rigidbody2D rb;
    private MovimientoPlayer movPlayer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movPlayer = GetComponent<MovimientoPlayer>();
    }

    // Se llama desde el LevelManager después de instanciar al jugador
    public void InicializarUI()
    {
        if (skillIconImage != null && iconoHabilidad != null)
        {
            skillIconImage.sprite = iconoHabilidad;
        }

        if (skillBarFill != null)
        {
            skillBarFill.color = colorListo;
            skillBarFill.fillAmount = 1f;
        }
    }

    public void OnMove(InputValue value)
    {
        direccionInput = value.Get<Vector2>();
    }

    public void OnAbility(InputValue value)
    {
        if (!enabled || DeathScreenController.GameplayBlocked) return;
        if (value.isPressed && habilidadLista && !haciendoDash && direccionInput != Vector2.zero)
        {
            StartCoroutine(ActivarDash());
        }
    }

    private IEnumerator ActivarDash()
    {
        habilidadLista = false;
        haciendoDash = true;
        GetComponent<AnimacionJasonDinamica>()?.ReproducirAccion("Habilidad");

        if (sfxSource != null && sonidoDash != null) sfxSource.PlayOneShot(sonidoDash);
        if (movPlayer != null) movPlayer.enabled = false;
        
        gameObject.layer = LayerMask.NameToLayer(capaDash);
        rb.linearVelocity = direccionInput.normalized * fuerzaDash;

        // Feedback visual: vaciamos la barra mientras dura el dash
        float tiempo = 0f;
        while (tiempo < duracionDash)
        {
            tiempo += Time.deltaTime;
            if (skillBarFill != null) skillBarFill.fillAmount = 1f - (tiempo / duracionDash);
            yield return null;
        }

        haciendoDash = false;
        if (movPlayer != null) movPlayer.enabled = true;
        gameObject.layer = LayerMask.NameToLayer(capaJugador);

        // --- Inicio del Cooldown ---
        enCooldown = true;
        tiempoCooldownActual = 0f;
        if (skillBarFill != null)
        {
            skillBarFill.color = colorEnCooldown;
            skillBarFill.fillAmount = 0f;
        }

        while (tiempoCooldownActual < cooldown)
        {
            tiempoCooldownActual += Time.deltaTime;
            if (skillBarFill != null) skillBarFill.fillAmount = Mathf.Clamp01(tiempoCooldownActual / cooldown);
            yield return null;
        }

        enCooldown = false;

        // Habilidad lista de nuevo
        if (skillBarFill != null)
        {
            skillBarFill.color = colorListo;
            skillBarFill.fillAmount = 1f;
        }
        
        habilidadLista = true;
    }

    public void ReducirCooldown(float segundos)
    {
        if (!enCooldown || segundos <= 0f) return;

        tiempoCooldownActual = Mathf.Min(cooldown, tiempoCooldownActual + segundos);
        if (skillBarFill != null)
        {
            skillBarFill.fillAmount = Mathf.Clamp01(tiempoCooldownActual / cooldown);
        }
    }
}
