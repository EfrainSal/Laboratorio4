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

    [Header("Cursor Dinámico")]
    public CustomCursor customCursor;
    public Sprite spriteCursorMachete;
    public Sprite spriteCursorSubfusil;

    public bool isUsingMelee = true; // True = Machete, False = Subfusil

    void Start()
    {
        InicializarCursor();
        // Inicializamos con el machete equipado
        EquipWeapon(isUsingMelee);
    }

    private void InicializarCursor()
    {
        if (customCursor == null)
        {
            customCursor = Object.FindAnyObjectByType<CustomCursor>(FindObjectsInactive.Include);
        }

        if (customCursor != null)
        {
            if (!customCursor.gameObject.activeSelf)
            {
                customCursor.gameObject.SetActive(true);
            }
        }
        else
        {
            GameObject cursorGO = new GameObject("CursorDinamico");
            cursorGO.AddComponent<SpriteRenderer>();
            customCursor = cursorGO.AddComponent<CustomCursor>();
        }

        if (spriteCursorMachete != null && customCursor.cursorMachete == null)
        {
            customCursor.cursorMachete = spriteCursorMachete;
        }

        if (spriteCursorSubfusil != null && customCursor.cursorSubfusil == null)
        {
            customCursor.cursorSubfusil = spriteCursorSubfusil;
        }

        customCursor.AsignarJugador(transform, meleeWeapon != null ? meleeWeapon : GetComponent<PlayerCombat>());
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
        isUsingMelee = useMelee;

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

        if (customCursor == null)
        {
            InicializarCursor();
        }

        if (customCursor != null)
        {
            customCursor.ActualizarCursor(useMelee);
        }
    }
}
