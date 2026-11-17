using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class WeaponManager : MonoBehaviour
{
    [Header("Scripts de Armas")]
    public PlayerCombat meleeWeapon;
    public PlayerShooting rangedWeapon;

    [Header("UI (Canvas)")]
    public GameObject ammoUI; // El GameObject que contiene el texto de Ammo

    [Header("Iconos de Armas (GameObjects en Canvas)")]
    public GameObject macheteIcon;  // Asigna el GameObject de la imagen del Machete
    public GameObject subfusilIcon; // Asigna el GameObject de la imagen del Subfusil

    [Header("Sprites estáticos del HUD (orientados a la derecha)")]
    public Sprite spriteMacheteHUD;
    public Sprite spriteSubfusilHUD;

    public bool isUsingMelee = true; // True = Machete, False = Subfusil
    public void SincronizarIcono()
    {
        var objeto = isUsingMelee ? macheteIcon : subfusilIcon;
        var sprite = isUsingMelee ? spriteMacheteHUD : spriteSubfusilHUD;
        if (objeto == null || sprite == null) return;
        // El HUD sólo cambia de imagen al equipar. No depende del apuntado ni del disparo.
        var imagen = objeto.GetComponent<Image>();
        if (imagen != null) imagen.sprite = sprite;
    }

    void Start()
    {
        // Inicializamos con el machete equipado
        EquipWeapon(isUsingMelee);
    }

    // Se llama automáticamente al presionar la tecla Q (Acción: SwitchWeapon)
    public void OnSwitchWeapon(InputValue value)
    {
        if (DeathScreenController.GameplayBlocked) return;
        ShieldAbility shield = GetComponent<ShieldAbility>();
        if (shield != null && shield.IsShieldActive) return;

        if (value.isPressed)
        {
            isUsingMelee = !isUsingMelee;
            EquipWeapon(isUsingMelee);
        }
    }

    public void EquipWeapon(bool useMelee)
    {
        ShieldAbility shield = GetComponent<ShieldAbility>();
        if (!useMelee && shield != null && shield.IsShieldActive)
        {
            useMelee = true;
        }
        isUsingMelee = useMelee;
        if (useMelee) GetComponent<ArmaIndependiente>()?.CancelarDisparoVisual();
        // Activa/Desactiva los scripts de combate
        if (meleeWeapon != null) meleeWeapon.enabled = useMelee;
        if (rangedWeapon != null) rangedWeapon.enabled = !useMelee;

        // Activa/Desactiva la UI de munición
        if (ammoUI != null) 
        {
            ammoUI.SetActive(!useMelee);
        }

        // Activa el icono del arma seleccionada y apaga el otro
        if (macheteIcon != null) 
        {
            macheteIcon.SetActive(useMelee);
        }
        
        if (subfusilIcon != null) 
        {
            subfusilIcon.SetActive(!useMelee);
        }
        SincronizarIcono();
    }
}
