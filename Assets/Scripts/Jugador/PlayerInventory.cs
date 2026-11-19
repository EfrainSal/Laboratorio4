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
    public float curaEstimulante = 100f;

    [Header("Bufo del Estimulante")]
    public float multiplicadorVelocidad = 1.5f; 
    public float multiplicadorDaño = 1.5f; 
    public float duracionEstimulante = 5f; 
    private bool estimulanteActivo = false;
    public bool EstimulanteActivo => estimulanteActivo;

    [Header("UI Textos (Asignar desde el Canvas)")]
    public TextMeshProUGUI textoVendas;
    public TextMeshProUGUI textoBotiquines;
    public TextMeshProUGUI textoEstimulantes;
    public GameObject iconoTarjetaUI;
    [Tooltip("Color del efecto visual en pantalla mientras el estimulante está activo (amarillo translúcido suave).")]
    public Color colorFeedbackEstimulante = new Color(1f, 0.92f, 0.15f, 0.08f);
    [Tooltip("Segundos antes de que termine el efecto en los que la opacidad va disminuyendo suavemente hasta desaparecer.")]
    [Min(0.1f)] public float duracionDesvanecimientoEstimulante = 1.5f;
    public string mensajeTarjetaRecogida = "TARJETA OBTENIDA  /  Acércate al panel de la puerta de salida";
    public float duracionAvisoTarjeta = 3f;

    [Header("Audio SFX")]
    public AudioSource audioSource; // Arrastra aquí el AudioSource de tu Player
    public AudioClip sfxRecogerObjeto; // Asigna el clip de vendas/botiquín
    public AudioClip sfxRecogerEspecial; // Asigna el clip de llave/estimulante

    private PlayerHealth playerHealth;
    private MovimientoPlayer movimiento; 
    private PlayerCombat combate; 
    private PlayerShooting disparo;
    private Image overlayEstimulante;
    private Coroutine rutinaEstimulante;
    private float finEfectoEstimulante; 

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        movimiento = GetComponent<MovimientoPlayer>();
        combate = GetComponent<PlayerCombat>();
        disparo = GetComponent<PlayerShooting>();
        
        // Si no asignas el AudioSource manualmente, lo busca en el mismo GameObject
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

    }

    void Start() { ActualizarUI(); }

    public void OnConsumirVenda(InputValue value)
    {
        if (!enabled || DeathScreenController.GameplayBlocked) return;
        if (value.isPressed) ConsumirVenda();
    }

    public void OnConsumirBotiquin(InputValue value)
    {
        if (!enabled || DeathScreenController.GameplayBlocked) return;
        if (value.isPressed) ConsumirBotiquin();
    }

    public void OnConsumirEstimulante(InputValue value)
    {
        if (!enabled || DeathScreenController.GameplayBlocked) return;
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
                if (!string.IsNullOrEmpty(mensajeTarjetaRecogida))
                {
                    AvisoPuertaUI.MostrarGlobal(mensajeTarjetaRecogida, duracionAvisoTarjeta);
                }
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
        if (playerHealth == null || playerHealth.IsDead) return;
        if (vendasCount > 0 && playerHealth.currentHealth < playerHealth.maxHealth)
        {
            vendasCount--;
            playerHealth.Heal(curaVendas);
            ActualizarUI();
        }
    }

    private void ConsumirBotiquin()
    {
        if (playerHealth == null || playerHealth.IsDead) return;
        if (botiquinesCount > 0 && playerHealth.currentHealth < playerHealth.maxHealth)
        {
            botiquinesCount--;
            playerHealth.Heal(curaBotiquin);
            ActualizarUI();
        }
    }

    private void ConsumirEstimulante()
    {
        if (playerHealth == null || playerHealth.IsDead) return;
        if (estimulantesCount > 0)
        {
            estimulantesCount--;
            
            if (playerHealth.currentHealth < playerHealth.maxHealth)
            {
                playerHealth.Heal(playerHealth.maxHealth);
            }
            
            finEfectoEstimulante = Time.time + duracionEstimulante;
            if (!estimulanteActivo)
            {
                rutinaEstimulante = StartCoroutine(EfectoEstimulanteRoutine());
            }
            else
            {
                EstablecerOpacidadEstimulante(1f);
            }
            
            ActualizarUI();
        }
    }

    private IEnumerator EfectoEstimulanteRoutine()
    {
        estimulanteActivo = true;
        EstablecerOpacidadEstimulante(1f);

        if (movimiento != null) movimiento.velocidad *= multiplicadorVelocidad;
        if (combate != null) combate.attackDamage *= multiplicadorDaño;

        float ventanaFade = Mathf.Clamp(duracionDesvanecimientoEstimulante, 0.1f, Mathf.Max(0.1f, duracionEstimulante));

        while (Time.time < finEfectoEstimulante)
        {
            if (playerHealth != null && playerHealth.IsDead) break;

            float restante = finEfectoEstimulante - Time.time;
            float factorOpacidad = restante < ventanaFade ? Mathf.Clamp01(restante / ventanaFade) : 1f;
            EstablecerOpacidadEstimulante(factorOpacidad);
            yield return null;
        }

        if (movimiento != null) movimiento.velocidad /= multiplicadorVelocidad;
        if (combate != null) combate.attackDamage /= multiplicadorDaño;

        estimulanteActivo = false;
        rutinaEstimulante = null;
        EstablecerOpacidadEstimulante(0f);
    }

    private void AsegurarOverlayEstimulante()
    {
        if (overlayEstimulante != null) return;

        Image refDamageFlash = playerHealth != null ? playerHealth.damageFlash : null;
        if (refDamageFlash == null && LevelManager.Instance != null)
        {
            refDamageFlash = LevelManager.Instance.damageFlashUI;
        }

        if (refDamageFlash != null && refDamageFlash.transform.parent != null)
        {
            Transform parent = refDamageFlash.transform.parent;
            Transform existente = parent.Find("StimulantFlash");
            if (existente != null)
            {
                overlayEstimulante = existente.GetComponent<Image>();
            }
            else
            {
                GameObject go = new GameObject("StimulantFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = refDamageFlash.gameObject.layer;
                go.transform.SetParent(parent, false);
                go.transform.SetSiblingIndex(refDamageFlash.transform.GetSiblingIndex());

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.localScale = Vector3.one;

                overlayEstimulante = go.GetComponent<Image>();
                overlayEstimulante.sprite = refDamageFlash.sprite;
                overlayEstimulante.type = refDamageFlash.type;
                overlayEstimulante.preserveAspect = refDamageFlash.preserveAspect;
                overlayEstimulante.fillCenter = refDamageFlash.fillCenter;
                overlayEstimulante.raycastTarget = false;
                overlayEstimulante.color = new Color(colorFeedbackEstimulante.r, colorFeedbackEstimulante.g, colorFeedbackEstimulante.b, 0f);
            }
        }
    }

    private void EstablecerOpacidadEstimulante(float factor)
    {
        if (factor > 0f) AsegurarOverlayEstimulante();
        if (overlayEstimulante != null)
        {
            float alpha = Mathf.Clamp01(factor) * colorFeedbackEstimulante.a;
            overlayEstimulante.color = new Color(
                colorFeedbackEstimulante.r,
                colorFeedbackEstimulante.g,
                colorFeedbackEstimulante.b,
                alpha
            );
        }
    }

    private void OnDisable()
    {
        EstablecerOpacidadEstimulante(0f);
    }

    public void ActualizarUI()
    {
        if (textoVendas != null) textoVendas.text = "" + vendasCount;
        if (textoBotiquines != null) textoBotiquines.text = "Botiquín [3]: " + botiquinesCount;
        if (textoEstimulantes != null) textoEstimulantes.text = "" + estimulantesCount;
        if (iconoTarjetaUI != null) iconoTarjetaUI.SetActive(llavesCount > 0);
    }
}
