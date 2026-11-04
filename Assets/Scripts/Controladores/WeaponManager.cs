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
    public Image weaponIndicatorImage;
    public GameObject ammoUI; // Arrastra aquí el GameObject que contiene el texto de Ammo
    //public TextMeshProUGUI weaponIndicatorText;  Crea un texto en tu Canvas para mostrar el arma actual y arrástralo aquí
    
    [Header("Sprites de Armas")]
    public Sprite macheteSprite; // Aquí asignarás el sprite del Machete
    public Sprite subfusilSprite; // Aquí asignarás el sprite del Subfusil

    public bool isUsingMelee = true; // True = Machete, False = Subfusil

    void Start()
    {
        // Inicializamos con el machete equipado
        EquipWeapon(true);
    }

    // Se llama automáticamente al presionar la tecla Q (Acción: SwitchWeapon)
    public void OnSwitchWeapon(InputValue value)
    {
        if (value.isPressed)
        {
            isUsingMelee = !isUsingMelee;
            EquipWeapon(isUsingMelee);
        }
    }

    public void EquipWeapon(bool useMelee)
    {
        // Activa/Desactiva los scripts
        meleeWeapon.enabled = useMelee;
        rangedWeapon.enabled = !useMelee;

        // Activa/Desactiva la UI de munición
        if (ammoUI != null) 
        {
            ammoUI.SetActive(!useMelee);
        }

        // Actualiza el texto del arma actual en el Canvas
        if (weaponIndicatorImage != null)
        {
            weaponIndicatorImage.sprite = useMelee ? macheteSprite : subfusilSprite;
            
            // Opcional: Ajustar el tamaño nativo del sprite para que no se deforme
            // weaponIndicatorImage.SetNativeSize(); 
        }
    }
}