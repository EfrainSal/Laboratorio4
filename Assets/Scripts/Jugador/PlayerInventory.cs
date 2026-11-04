using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [Header("Cantidades en Inventario")]
    public int vendasCount = 0;
    public int botiquinesCount = 0;
    public int estimulantesCount = 0;
    public int llavesCount = 0; 

    [Header("Configuración de Curación")]
    public float curaVendas = 10f;
    public float curaBotiquin = 30f;
    public float curaEstimulante = 30f; 

    [Header("Bufo del Estimulante")]
    public float multiplicadorVelocidad = 1.5f; 
    public float multiplicadorDaño = 1.5f; 
    public float duracionEstimulante = 5f; 
    private bool estimulanteActivo = false;

    [Header("UI Textos (Asignar desde el Canvas)")]
    public TextMeshProUGUI textoVendas;
    public TextMeshProUGUI textoBotiquines;
    public TextMeshProUGUI textoEstimulantes;

    [Header("Audio SFX")]
    public AudioSource audioSource; // Arrastra aquí el AudioSource de tu Player
    public AudioClip sfxRecogerObjeto; // Asigna el clip de vendas/botiquín
    public AudioClip sfxRecogerEspecial; // Asigna el clip de llave/estimulante

    private PlayerHealth playerHealth;
    private MovimientoPlayer movimiento; 
    private PlayerCombat combate; 
    private PlayerShooting disparo; 

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        movimiento = GetComponent<MovimientoPlayer>();
        combate = GetComponent<PlayerCombat>();
        disparo = GetComponent<PlayerShooting>();
        
        // Si no asignas el AudioSource manualmente, lo busca en el mismo GameObject
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        ActualizarUI();
    }

    public void OnConsumirVenda(InputValue value)
    {
        if (value.isPressed) ConsumirVenda();
    }

    public void OnConsumirBotiquin(InputValue value)
    {
        if (value.isPressed) ConsumirBotiquin();
    }

    public void OnConsumirEstimulante(InputValue value)
    {
        if (value.isPressed) ConsumirEstimulante();
    }

    public void AgregarObjeto(ItemPickup.PickupType tipo)
    {
        switch (tipo)
        {
            case ItemPickup.PickupType.Venda: 
                vendasCount++; 
                ReproducirSFX(sfxRecogerObjeto);
                break;
            case ItemPickup.PickupType.Botiquin: 
                botiquinesCount++; 
                ReproducirSFX(sfxRecogerObjeto); // Asumí que el botiquín también usa el sonido normal
                break;
            case ItemPickup.PickupType.Estimulante: 
                estimulantesCount++; 
                ReproducirSFX(sfxRecogerEspecial);
                break;
            case ItemPickup.PickupType.Llave: 
                llavesCount++; 
                Debug.Log("¡Llave obtenida!");
                ReproducirSFX(sfxRecogerEspecial);
                break;
        }
        ActualizarUI();
    }

    // Método auxiliar para reproducir el sonido sin interrumpir otros
    private void ReproducirSFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public bool IntentarUsarLlave()
    {
        if (llavesCount > 0)
        {
            llavesCount--; 
            ActualizarUI();
            return true;
        }
        return false;
    }

    public void LimpiarLlaves()
    {
        llavesCount = 0;
        ActualizarUI();
    }

    private void ConsumirVenda()
    {
        if (vendasCount > 0 && playerHealth.currentHealth < playerHealth.maxHealth)
        {
            vendasCount--;
            playerHealth.Heal(curaVendas);
            ActualizarUI();
        }
    }

    private void ConsumirBotiquin()
    {
        if (botiquinesCount > 0 && playerHealth.currentHealth < playerHealth.maxHealth)
        {
            botiquinesCount--;
            playerHealth.Heal(curaBotiquin);
            ActualizarUI();
        }
    }

    private void ConsumirEstimulante()
    {
        if (estimulantesCount > 0)
        {
            estimulantesCount--;
            
            if (playerHealth.currentHealth < playerHealth.maxHealth)
            {
                playerHealth.Heal(curaEstimulante);
            }
            
            if (!estimulanteActivo)
            {
                StartCoroutine(EfectoEstimulanteRoutine());
            }
            
            ActualizarUI();
        }
    }

    private IEnumerator EfectoEstimulanteRoutine()
    {
        estimulanteActivo = true;

        if (movimiento != null) movimiento.velocidad *= multiplicadorVelocidad;
        if (combate != null) combate.attackDamage *= multiplicadorDaño;

        yield return new WaitForSeconds(duracionEstimulante);

        if (movimiento != null) movimiento.velocidad /= multiplicadorVelocidad;
        if (combate != null) combate.attackDamage /= multiplicadorDaño;

        estimulanteActivo = false;
    }

    public void ActualizarUI()
    {
        if (textoVendas != null) textoVendas.text = "[1]:" + vendasCount;
        if (textoBotiquines != null) textoBotiquines.text = "Botiquín [2]: " + botiquinesCount;
        if (textoEstimulantes != null) textoEstimulantes.text = "[3]: " + estimulantesCount;
    }
}