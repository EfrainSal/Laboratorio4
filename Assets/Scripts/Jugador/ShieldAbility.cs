using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ShieldAbility : MonoBehaviour
{
    [Header("Configuración del Escudo")]
    public float duracionEscudo = 3f;
    public float cooldown = 8f;
    public GameObject escudoVisual; // Un sprite hijo que servirá de burbuja/escudo

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

    // Se llama automáticamente por el Player Input cuando presionas el botón "Ability"
    public void OnAbility(InputValue value)
    {
        if (value.isPressed && habilidadLista)
        {
            StartCoroutine(ActivarEscudo());
        }
    }

    private IEnumerator ActivarEscudo()
    {
        habilidadLista = false;

        // Reproducir sonido de escudo
        if (sfxSource != null && sonidoEscudo != null)
        {
            sfxSource.PlayOneShot(sonidoEscudo);
        }
        
        // Activar invulnerabilidad y visual
        playerHealth.isInvulnerable = true;
        if (escudoVisual != null) escudoVisual.SetActive(true);

        // Esperar la duración
        yield return new WaitForSeconds(duracionEscudo);

        // Desactivar
        playerHealth.isInvulnerable = false;
        if (escudoVisual != null) escudoVisual.SetActive(false);

        // Esperar el cooldown
        yield return new WaitForSeconds(cooldown);
        habilidadLista = true;
    }
}