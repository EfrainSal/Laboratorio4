using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip sonidoDisparo;   

    private float nextFireTime = 0f;
    private bool municionRestaurada;
    private PlayerInput entrada;
    private InputAction accionDisparo;

    void Awake()
    {
        entrada = GetComponent<PlayerInput>();
    }

    void Start()
    {
        if (!municionRestaurada) currentAmmoInClip = maxClipSize;
        UpdateAmmoUI();
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
    }

    void Update()
    {
        if (DeathScreenController.GameplayBlocked) return;
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
        if (!enabled || DeathScreenController.GameplayBlocked) return;

        if (value.isPressed)
        {
            Reload();
        }
    }

    void Shoot()
    {
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
    }

    void Reload()
    {
        if (currentAmmoInClip == maxClipSize || totalAmmo <= 0) return;

        int bulletsNeeded = maxClipSize - currentAmmoInClip;
        int bulletsToReload = Mathf.Min(bulletsNeeded, totalAmmo);

        currentAmmoInClip += bulletsToReload;
        totalAmmo -= bulletsToReload;

        UpdateAmmoUI();
    }

    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        UpdateAmmoUI();
    }

    public void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = currentAmmoInClip + " / " + totalAmmo;
        }
    }
}
