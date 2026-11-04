using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Spawneo del Jugador")]
    [Tooltip("El punto donde aparecerá el jugador al iniciar el nivel")]
    public Transform puntoDeInicio;

    [Header("Referencias UI - Inventario")]
    public TextMeshProUGUI textoVendasUI;
    public TextMeshProUGUI textoBotiquinesUI;
    public TextMeshProUGUI textoEstimulantesUI;

    [Header("Referencias UI - Salud")]
    public Image healthBarFillUI;
    public Image damageTrailFillUI;
    public Image damageFlashUI;
    public TextMeshProUGUI healthTextUI;

    [Header("Referencias UI - Armas")]
    public GameObject ammoUIGO;
    public TextMeshProUGUI ammoTextUI;
    public Image weaponIndicatorImageUI;

    [Header("Condiciones del Piso")]
    [SerializeField] private int enemigosParaAvanzar = 10;
    [SerializeField] private int spawnersRequeridos = 3;
    
    [Header("Objetos de Progresión")]
    [SerializeField] private GameObject llavePrefab;
    
    [Header("Estado Actual (Solo lectura)")]
    [SerializeField] private int enemigosDerrotados = 0;
    [SerializeField] private int spawnersActivados = 0;

    private bool condicionesCumplidas = false;
    private bool llaveDropeada = false;

    public bool JugadorTieneLlave { get; set; } = false; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        PrepararJugador();
    }

    private void PrepararJugador()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Falta GameManager. Asegúrate de iniciar el juego desde el Menú Principal.");
            return;
        }

        // 1. Instanciar al jugador
        GameObject prefabAInstanciar = GameManager.Instance.ObtenerPrefabJugador();
        GameObject jugadorInstanciado = Instantiate(prefabAInstanciar, puntoDeInicio.position, Quaternion.identity);

        // --- APLICAR DATOS GUARDADOS (SI EXISTEN) ---
        SaveData datosGuardados = GameManager.Instance.DatosCargados;
        if (datosGuardados != null)
        {
            // Mover al jugador a donde estaba al guardar
            jugadorInstanciado.transform.position = new Vector3(datosGuardados.posicionX, datosGuardados.posicionY, 0f);
        }

        // 2. Inyectar UI y Datos al Inventario
        PlayerInventory inventario = jugadorInstanciado.GetComponent<PlayerInventory>();
        if (inventario != null)
        {
            inventario.textoVendas = textoVendasUI;
            inventario.textoBotiquines = textoBotiquinesUI;
            inventario.textoEstimulantes = textoEstimulantesUI;
            
            if (datosGuardados != null)
            {
                inventario.vendasCount = datosGuardados.cantidadVendas;
                inventario.botiquinesCount = datosGuardados.cantidadBotiquines;
                inventario.estimulantesCount = datosGuardados.cantidadEstimulantes;
                inventario.llavesCount = datosGuardados.llavesCount;
            }
            else
            {
                inventario.LimpiarLlaves();
            }
            inventario.ActualizarUI(); 
        }

        // 3. Inyectar UI y Datos a la Salud
        PlayerHealth salud = jugadorInstanciado.GetComponent<PlayerHealth>();
        if (salud != null)
        {
            salud.healthBarFill = healthBarFillUI;
            salud.damageTrailFill = damageTrailFillUI;
            salud.damageFlash = damageFlashUI;
            salud.healthText = healthTextUI;

            if (datosGuardados != null)
            {
                salud.cargadoDesdeGuardado = true;
                salud.currentHealth = datosGuardados.vidaActual;
                
            }
            salud.UpdateHealthUI(); 
        }

                // 5. Inyectar UI y Datos a la Experiencia / Mejoras
        PlayerExperience exp = jugadorInstanciado.GetComponent<PlayerExperience>();
        if (exp != null && datosGuardados != null)
        {
            exp.puntosDeMejora = datosGuardados.puntosDeMejora;
        }



        // 4. Inyectar UI y Datos a las Armas
        WeaponManager armas = jugadorInstanciado.GetComponent<WeaponManager>();
        if (armas != null)
        {
            armas.weaponIndicatorImage = weaponIndicatorImageUI;
            armas.ammoUI = ammoUIGO;
            armas.EquipWeapon(true); 
        }

        PlayerShooting disparo = jugadorInstanciado.GetComponent<PlayerShooting>();
        if (disparo != null)
        {
            disparo.ammoText = ammoTextUI;
            if (datosGuardados != null)
            {
                disparo.currentAmmoInClip = datosGuardados.currentAmmoInClip;
                disparo.totalAmmo = datosGuardados.totalAmmo;
            }
            disparo.UpdateAmmoUI();
        }

        // Limpiar los datos temporales del GameManager una vez inyectados para evitar que se dupliquen al cambiar de piso
        if (datosGuardados != null)
        {
            GameManager.Instance.LimpiarDatosCargados();
        }
    }

    public void RegistrarEnemigoDerrotado(Vector3 posicionMuerte)
    {
        enemigosDerrotados++;
        VerificarCondiciones();

        if (condicionesCumplidas && !llaveDropeada && llavePrefab != null)
        {
            Instantiate(llavePrefab, posicionMuerte, Quaternion.identity);
            llaveDropeada = true;
            Debug.Log("¡Condiciones cumplidas! El enemigo soltó la llave de salida.");
        }
    }

    public void RegistrarSpawnerActivado()
    {
        spawnersActivados++;
        VerificarCondiciones();
    }

    private void VerificarCondiciones()
    {
        if (!condicionesCumplidas && enemigosDerrotados >= enemigosParaAvanzar && spawnersActivados >= spawnersRequeridos)
        {
            condicionesCumplidas = true;
            Debug.Log("Cuota alcanzada. El próximo enemigo derrotado soltará la llave.");
        }
    }

    public void RecogerLlave()
    {
        JugadorTieneLlave = true;
        Debug.Log("Llave recogida. Ya puedes abrir la puerta.");
    }
}