using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public TextMeshProUGUI ammoText;

    [Header("Configuración de Disparo")]
    public float bulletForce = 20f;
    public float fireRate = 0.5f;

    [Header("Munición y Recarga")]
    public int maxClipSize = 30;      
    public int currentAmmoInClip;     
    public int totalAmmo = 60;     
    public float reloadDuration = 1.5f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoDisparo;   

    public bool IsReloading => isReloading;

    private float nextFireTime = 0f;
    private bool municionRestaurada;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private PlayerInput entrada;
    private InputAction accionDisparo;
    private ShieldAbility shieldAbility;

    private GameObject reloadIndicatorRoot;
    private Image reloadBarFill;

    void Awake()
    {
        entrada = GetComponent<PlayerInput>();
        shieldAbility = GetComponent<ShieldAbility>();
        CrearIndicadorRecarga();
    }

    void Start()
    {
        if (!municionRestaurada) currentAmmoInClip = maxClipSize;
        UpdateAmmoUI();
    }

    private void CrearIndicadorRecarga()
    {
        if (reloadIndicatorRoot != null) return;

        reloadIndicatorRoot = new GameObject("IndicadorRecarga");
        reloadIndicatorRoot.transform.SetParent(transform, false);
        reloadIndicatorRoot.transform.localPosition = new Vector3(0f, 1.15f, 0f);

        Canvas canvas = reloadIndicatorRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5000;

        RectTransform canvasRect = reloadIndicatorRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(0.85f, 0.11f);
        canvasRect.localScale = Vector3.one;

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(reloadIndicatorRoot.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        bgImg.raycastTarget = false;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(reloadIndicatorRoot.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(0.02f, 0.02f);
        fillRect.offsetMax = new Vector2(-0.02f, -0.02f);

        reloadBarFill = fillObj.GetComponent<Image>();
        Sprite whiteSprite = CrearSpriteBlanco();
        bgImg.sprite = whiteSprite;
        reloadBarFill.sprite = whiteSprite;
        reloadBarFill.type = Image.Type.Filled;
        reloadBarFill.fillMethod = Image.FillMethod.Horizontal;
        reloadBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        reloadBarFill.fillAmount = 0f;
        reloadBarFill.color = new Color(1f, 0.8f, 0.15f, 1f);
        reloadBarFill.raycastTarget = false;

        reloadIndicatorRoot.SetActive(false);
    }

    private static Sprite spriteBlancoCompartido;
    private static Sprite CrearSpriteBlanco()
    {
        if (spriteBlancoCompartido != null) return spriteBlancoCompartido;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        spriteBlancoCompartido = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        return spriteBlancoCompartido;
    }

    public void RestaurarMunicion(int cargador, int reserva)
    {
        municionRestaurada = true;
        currentAmmoInClip = Mathf.Clamp(cargador, 0, maxClipSize);
        totalAmmo = Mathf.Max(0, reserva);
        UpdateAmmoUI();
    }

    void OnDisable()
    {
        accionDisparo = null;
        CancelarRecarga();
    }

    private void CancelarRecarga()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        isReloading = false;
        if (reloadBarFill != null) reloadBarFill.fillAmount = 0f;
        if (reloadIndicatorRoot != null) reloadIndicatorRoot.SetActive(false);
        UpdateAmmoUI();
    }

    void Update()
    {
        if (DeathScreenController.GameplayBlocked) return;
        if (shieldAbility != null && shieldAbility.IsShieldActive) return;
        if (isReloading) return;

        if (currentAmmoInClip <= 0 && totalAmmo > 0)
        {
            Reload();
            return;
        }

        if (DisparoMantenido() && Time.time >= nextFireTime && currentAmmoInClip > 0)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private bool DisparoMantenido()
    {
        if (entrada == null || !entrada.isActiveAndEnabled || !entrada.inputIsActive) return false;
        var mapa = entrada.currentActionMap;
        if (mapa == null) return false;
        if (accionDisparo == null || accionDisparo.actionMap != mapa)
            accionDisparo = mapa.FindAction("Attack", false);

        // Send Messages no comunica canceled para acciones Button. Leer la acción
        // del jugador cada frame evita conservar el clic después de soltarlo.
        return accionDisparo != null && accionDisparo.enabled && accionDisparo.IsPressed();
    }

    public void OnReload(InputValue value)
    {
        if (!enabled || DeathScreenController.GameplayBlocked || isReloading) return;
        if (shieldAbility != null && shieldAbility.IsShieldActive) return;

        if (value.isPressed)
        {
            Reload();
        }
    }

    void Shoot()
    {
        if (isReloading || (shieldAbility != null && shieldAbility.IsShieldActive)) return;

        GetComponent<ArmaIndependiente>()?.ActualizarApuntado();
        currentAmmoInClip--;
        UpdateAmmoUI();

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        GetComponent<ArmaIndependiente>()?.MostrarDisparo();
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(firePoint.right * bulletForce, ForceMode2D.Impulse);
        }

        // ---> REPRODUCIR SONIDO DE DISPARO <---
        if (sfxSource != null && sonidoDisparo != null)
        {
            sfxSource.PlayOneShot(sonidoDisparo);
        }

        if (currentAmmoInClip <= 0 && totalAmmo > 0)
        {
            Reload();
        }
    }

    void Reload()
    {
        if (isReloading || currentAmmoInClip == maxClipSize || totalAmmo <= 0) return;
        if (!isActiveAndEnabled) return;

        reloadCoroutine = StartCoroutine(RutinaRecarga());
    }

    private IEnumerator RutinaRecarga()
    {
        isReloading = true;
        CrearIndicadorRecarga();
        if (reloadIndicatorRoot != null) reloadIndicatorRoot.SetActive(true);
        if (reloadBarFill != null) reloadBarFill.fillAmount = 0f;
        UpdateAmmoUI();

        float tiempo = 0f;
        float duracion = Mathf.Max(0.1f, reloadDuration);
        while (tiempo < duracion)
        {
            if (!DeathScreenController.GameplayBlocked)
            {
                tiempo += Time.deltaTime;
                if (reloadBarFill != null)
                {
                    reloadBarFill.fillAmount = Mathf.Clamp01(tiempo / duracion);
                }
            }
            yield return null;
        }

        int bulletsNeeded = maxClipSize - currentAmmoInClip;
        int bulletsToReload = Mathf.Min(bulletsNeeded, totalAmmo);

        currentAmmoInClip += bulletsToReload;
        totalAmmo -= bulletsToReload;

        isReloading = false;
        reloadCoroutine = null;
        if (reloadBarFill != null) reloadBarFill.fillAmount = 0f;
        if (reloadIndicatorRoot != null) reloadIndicatorRoot.SetActive(false);
        UpdateAmmoUI();
    }

    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        UpdateAmmoUI();
        if (enabled && !isReloading && currentAmmoInClip <= 0 && totalAmmo > 0)
        {
            Reload();
        }
    }

    public void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = isReloading
                ? "RECARGANDO..."
                : currentAmmoInClip + " / " + totalAmmo;
        }
    }
}
