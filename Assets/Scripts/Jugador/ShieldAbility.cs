using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class ShieldAbility : MonoBehaviour
{
    [Header("Configuración del Escudo")]
    public float duracionEscudo = 3f;
    public float cooldown = 8f;
    public GameObject escudoVisual;

    [Header("Configuración de UI")]
    public Sprite iconoHabilidad; // Arrastra aquí el sprite del escudo (icono_escudo)
    public Color colorListo = new Color(0, 1, 1, 1); // Cian brillante
    public Color colorEnCooldown = new Color(0, 0.5f, 0.5f, 1); // Cian oscuro
    
    [HideInInspector] public Image skillBarFill;
    [HideInInspector] public Image skillIconImage;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoEscudo;

    private bool habilidadLista = true;
    private PlayerHealth playerHealth;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (escudoVisual != null) escudoVisual.SetActive(false);
    }

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

    public void OnAbility(InputValue value)
    {
        if (!enabled || DeathScreenController.GameplayBlocked) return;
        if (value.isPressed && habilidadLista)
        {
            StartCoroutine(ActivarEscudo());
        }
    }

    private IEnumerator ActivarEscudo()
    {
        habilidadLista = false;
        GetComponent<AnimacionJasonDinamica>()?.ReproducirAccion("Habilidad");

        if (sfxSource != null && sonidoEscudo != null) sfxSource.PlayOneShot(sonidoEscudo);
        
        playerHealth.isInvulnerable = true;
        if (escudoVisual != null) escudoVisual.SetActive(true);

        // Feedback visual: La barra baja lentamente mientras el escudo está activo
        float tiempo = 0f;
        while (tiempo < duracionEscudo)
        {
            tiempo += Time.deltaTime;
            if (skillBarFill != null) skillBarFill.fillAmount = 1f - (tiempo / duracionEscudo);
            yield return null;
        }

        // Se apaga el escudo
        playerHealth.isInvulnerable = false;
        if (escudoVisual != null) escudoVisual.SetActive(false);

        // --- Inicio del Cooldown ---
        if (skillBarFill != null)
        {
            skillBarFill.color = colorEnCooldown;
            skillBarFill.fillAmount = 0f;
        }

        tiempo = 0f;
        while (tiempo < cooldown)
        {
            tiempo += Time.deltaTime;
            if (skillBarFill != null) skillBarFill.fillAmount = tiempo / cooldown;
            yield return null;
        }

        // Habilidad lista de nuevo
        if (skillBarFill != null)
        {
            skillBarFill.color = colorListo;
            skillBarFill.fillAmount = 1f;
        }

        habilidadLista = true;
    }
}
