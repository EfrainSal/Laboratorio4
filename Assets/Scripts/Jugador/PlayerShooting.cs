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
    private bool isShooting = false;  

    void Start()
    {
        currentAmmoInClip = maxClipSize; 
        UpdateAmmoUI();
    }

    // Método crucial de seguridad: detiene el disparo si el script se desactiva
    void OnDisable()
    {
        isShooting = false;
    }

    void Update()
    {
        if (isShooting && Time.time >= nextFireTime && currentAmmoInClip > 0)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    // Cambiado de OnShoot a OnAttack
    public void OnAttack(InputValue value)
    {
        if (!enabled) return;
        isShooting = value.isPressed;
    }

    public void OnReload(InputValue value)
    {
        if (!enabled) return; // No recargar si tienes el machete en mano

        if (value.isPressed)
        {
            Reload();
        }
    }

    void Shoot()
    {
        currentAmmoInClip--;
        UpdateAmmoUI();

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
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