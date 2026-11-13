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

    public bool isUsingMelee = true; // True = Machete, False = Subfusil

    void Start()
    {
        // Inicializamos con el machete equipado
        EquipWeapon(isUsingMelee);
    }

    // Se llama automáticamente al presionar la tecla Q (Acción: SwitchWeapon)
    public void OnSwitchWeapon(InputValue value)
    {
        if (DeathScreenController.GameplayBlocked) return;
        if (value.isPressed)
        {
            isUsingMelee = !isUsingMelee;
            EquipWeapon(isUsingMelee);
        }
    }

    public void EquipWeapon(bool useMelee)
    {
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
    }
}
