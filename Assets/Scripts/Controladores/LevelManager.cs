using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Spawneo del Jugador")]
    [Tooltip("El punto donde aparecerá el jugador al iniciar el nivel")]
    public Transform puntoDeInicio;
    private GameObject jugadorExistente;
    public GameObject JugadorActual { get; private set; }
    public GameObject AyudanteActual { get; private set; }

    public void UsarJugadorExistente(GameObject jugador)
    {
        jugadorExistente = jugador;
    }

    [Header("Referencias UI - Inventario")]
    public TextMeshProUGUI textoVendasUI;
    public TextMeshProUGUI textoBotiquinesUI;
    public TextMeshProUGUI textoEstimulantesUI;
    public GameObject tarjetaIconoUI;

    [Header("Referencias UI - Salud")]
    public Image healthBarFillUI;
    public Image damageTrailFillUI;
    public Image damageFlashUI;
    public TextMeshProUGUI healthTextUI;

    [Header("Referencias UI - Armas")]
    public GameObject ammoUIGO;
    public TextMeshProUGUI ammoTextUI;
    public GameObject macheteIconGO;
    public GameObject subfusilIconGO;

    [Header("Condiciones del Piso")]
    [SerializeField] private int enemigosParaAvanzar = 10;
    [SerializeField] private int spawnersRequeridos = 3;
    [SerializeField] private bool contarSoloEnemigosDeSpawners;
    public int EnemigosDerrotados => enemigosDerrotados;
    public bool TarjetaGenerada => llaveDropeada;
    
    [Header("Objetos de Progresión")]
    [SerializeField] private GameObject llavePrefab;
    
    [Header("Estado Actual (Solo lectura)")]
    [SerializeField] private int enemigosDerrotados = 0;
    [SerializeField] private int spawnersActivados = 0;

    // Añade esto en la parte superior donde tienes tus otros [Header]
    [Header("Referencias UI - Habilidad")]
    public Image skillBarFillUI; // La imagen "Fill" de la barra de habilidad
    public Image skillIconUI; // La imagen donde va el icono de la habilidad

    private bool condicionesCumplidas = false;
    private bool llaveDropeada = false;

    public bool JugadorTieneLlave { get; set; } = false;

    private readonly HashSet<string> objetosRecogidosPiso = new HashSet<string>();
    private readonly HashSet<string> contenedoresAbiertosPiso = new HashSet<string>();

    public List<string> ObtenerObjetosRecogidosPiso() => new List<string>(objetosRecogidosPiso);
    public List<string> ObtenerContenedoresAbiertosPiso() => new List<string>(contenedoresAbiertosPiso);

    public static string GenerarIdObjetoEscena(GameObject obj)
    {
        if (obj == null) return string.Empty;
        string escena = obj.scene.IsValid() ? obj.scene.name : SceneManager.GetActiveScene().name;
        string ruta = obj.name;
        Transform padre = obj.transform.parent;
        while (padre != null)
        {
            ruta = padre.name + "/" + ruta;
            padre = padre.parent;
        }
        Vector3 pos = obj.transform.position;
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2:F2}_{3:F2}",
            escena,
            ruta,
            pos.x,
            pos.y
        );
    }

    public void RegistrarObjetoRecogido(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            objetosRecogidosPiso.Add(id);
        }
    }

    public void RegistrarContenedorAbierto(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            contenedoresAbiertosPiso.Add(id);
        }
    }

    private void RestaurarEstadoObjetosEscena(SaveData datos)
    {
        objetosRecogidosPiso.Clear();
        contenedoresAbiertosPiso.Clear();

        if (datos.objetosRecogidosPiso != null)
        {
            foreach (string id in datos.objetosRecogidosPiso)
            {
                if (!string.IsNullOrEmpty(id)) objetosRecogidosPiso.Add(id);
            }
        }

        if (datos.contenedoresAbiertosPiso != null)
        {
            foreach (string id in datos.contenedoresAbiertosPiso)
            {
                if (!string.IsNullOrEmpty(id)) contenedoresAbiertosPiso.Add(id);
            }
        }

        foreach (var pickup in FindObjectsByType<ItemPickup>())
        {
            if (pickup == null) continue;
            string id = !string.IsNullOrEmpty(pickup.IdPersistencia)
                ? pickup.IdPersistencia
                : GenerarIdObjetoEscena(pickup.gameObject);
            pickup.IdPersistencia = id;

            if (objetosRecogidosPiso.Contains(id))
            {
                pickup.gameObject.SetActive(false);
                Destroy(pickup.gameObject);
            }
        }

        foreach (var ammo in FindObjectsByType<AmmoDrop>())
        {
            if (ammo == null) continue;
            string id = !string.IsNullOrEmpty(ammo.IdPersistencia)
                ? ammo.IdPersistencia
                : GenerarIdObjetoEscena(ammo.gameObject);
            ammo.IdPersistencia = id;

            if (objetosRecogidosPiso.Contains(id))
            {
                ammo.gameObject.SetActive(false);
                Destroy(ammo.gameObject);
            }
        }

        foreach (var contenedor in FindObjectsByType<BotiquinInteractable>())
        {
            if (contenedor == null) continue;
            string id = !string.IsNullOrEmpty(contenedor.IdPersistencia)
                ? contenedor.IdPersistencia
                : GenerarIdObjetoEscena(contenedor.gameObject);

            if (contenedoresAbiertosPiso.Contains(id))
            {
                bool recompensaYaRecogida = objetosRecogidosPiso.Contains(id + "_Drop");
                contenedor.RestaurarEstadoAbierto(!recompensaYaRecogida);
            }
        }
    }

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
        GameObject jugadorInstanciado = jugadorExistente != null
            ? jugadorExistente
            : Instantiate(prefabAInstanciar, puntoDeInicio.position, Quaternion.identity);
        JugadorActual = jugadorInstanciado;
        if (jugadorInstanciado.GetComponent<AvisoInteraccionUI>() == null)
            jugadorInstanciado.AddComponent<AvisoInteraccionUI>();

        // --- NUEVO: Inyectar UI a la Habilidad ---
        int idPersonaje = GameManager.Instance.personajeSeleccionado;

        if (idPersonaje == 0) // Jason (Escudo)
        {
            ShieldAbility shield = jugadorInstanciado.GetComponent<ShieldAbility>();
            if (shield != null)
            {
                shield.skillBarFill = skillBarFillUI;
                shield.skillIconImage = skillIconUI;
                shield.InicializarUI();
            }
        }
        else // Milena (Dash)
        {
            DashAbility dash = jugadorInstanciado.GetComponent<DashAbility>();
            if (dash != null)
            {
                dash.skillBarFill = skillBarFillUI;
                dash.skillIconImage = skillIconUI;
                dash.InicializarUI();
            }
        }
        // --- APLICAR DATOS GUARDADOS (SI EXISTEN) ---
        SaveData datosGuardados = GameManager.Instance.DatosCargados;
        if (datosGuardados != null)
        {
            enemigosDerrotados = datosGuardados.enemigosDerrotadosPiso;
            // Si no se recogió antes de guardar, permitir que el siguiente Sorbo la vuelva a soltar.
            llaveDropeada = datosGuardados.tarjetaGenerada && datosGuardados.llavesCount > 0;
            VerificarCondiciones();
            RestaurarEstadoObjetosEscena(datosGuardados);
            // Mover al jugador a donde estaba al guardar
            jugadorInstanciado.transform.position = new Vector3(datosGuardados.posicionX, datosGuardados.posicionY, 0f);
        }

        // 2. Inyectar UI y Datos al Inventario
        PlayerInventory inventario = jugadorInstanciado.GetComponent<PlayerInventory>();
        if (inventario != null)
        {
            if (tarjetaIconoUI == null)
            {
                Transform healthContainer = healthBarFillUI != null ? healthBarFillUI.transform.parent : null;
                if (healthContainer != null)
                {
                    Transform found = healthContainer.Find("TarjetaIconoUI");
                    if (found == null && healthContainer.parent != null)
                        found = healthContainer.parent.Find("TarjetaIconoUI");
                    if (found != null) tarjetaIconoUI = found.gameObject;
                }
            }
            inventario.textoVendas = textoVendasUI;
            inventario.textoBotiquines = textoBotiquinesUI;
            inventario.textoEstimulantes = textoEstimulantesUI;
            inventario.iconoTarjetaUI = tarjetaIconoUI;
            
            if (datosGuardados != null)
            {
                inventario.vendasCount = datosGuardados.cantidadVendas;
                inventario.botiquinesCount = datosGuardados.cantidadBotiquines;
                inventario.estimulantesCount = datosGuardados.cantidadEstimulantes;
                inventario.llavesCount = datosGuardados.llavesCount;
            }
            // NUEVO: Verificamos si hay datos de transición naturales
            else if (GameManager.Instance.usarDatosDeTransicion)
            {
                inventario.vendasCount = GameManager.Instance.vendasPendientes;
                inventario.botiquinesCount = GameManager.Instance.botiquinesPendientes;
                inventario.estimulantesCount = GameManager.Instance.estimulantesPendientes;
                inventario.llavesCount = 0; // La llave siempre se consume en la puerta
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
                salud.RestaurarVida(datosGuardados.vidaActual, datosGuardados.vidaMaxima);
            }
            else if (GameManager.Instance.usarDatosDeTransicion)
            {
                salud.RestaurarVida(GameManager.Instance.vidaPendiente, GameManager.Instance.vidaMaximaPendiente);
            }
            salud.UpdateHealthUI(); 
        }

        // 4. Inyectar UI y Datos a las Armas
        WeaponManager armas = jugadorInstanciado.GetComponent<WeaponManager>();
        if (armas != null)
        {
            // ASIGNA LOS NUEVOS GAMEOBJECTS DEL CANVAS AL JUGADOR
            armas.macheteIcon = macheteIconGO;
            armas.subfusilIcon = subfusilIconGO;
            armas.ammoUI = ammoUIGO;
            
            bool cuerpoACuerpo = datosGuardados != null ? datosGuardados.armaCuerpoACuerpo :
                GameManager.Instance.usarDatosDeTransicion ? GameManager.Instance.armaCuerpoACuerpoPendiente : armas.isUsingMelee;
            armas.EquipWeapon(cuerpoACuerpo);
        }

        PlayerShooting disparo = jugadorInstanciado.GetComponent<PlayerShooting>();
        if (disparo != null)
        {
            disparo.ammoText = ammoTextUI;
            if (datosGuardados != null)
            {
                disparo.RestaurarMunicion(datosGuardados.currentAmmoInClip, datosGuardados.totalAmmo);
            }
            else if (GameManager.Instance.usarDatosDeTransicion)
            {
                disparo.RestaurarMunicion(GameManager.Instance.municionCargadorPendiente, GameManager.Instance.municionTotalPendiente);
            }
            disparo.UpdateAmmoUI();
        }

        // 5. Inyectar UI y Datos a la Experiencia / Mejoras
        PlayerExperience exp = jugadorInstanciado.GetComponent<PlayerExperience>();
        if (exp != null)
        {
            if (datosGuardados != null)
            {
                exp.puntosDeMejora = datosGuardados.puntosDeMejora;
                exp.RestaurarProgreso(datosGuardados.nivelJugador, datosGuardados.experienciaJugador);
            }
            else if (GameManager.Instance.usarDatosDeTransicion)
            {
                exp.puntosDeMejora = GameManager.Instance.puntosDeMejoraPendientes;
                exp.RestaurarProgreso(GameManager.Instance.nivelActualPendiente, GameManager.Instance.expActualPendiente);
                
            }
        }

        // Limpiar los datos temporales del GameManager una vez inyectados para evitar que se dupliquen o interfieran
        if (datosGuardados != null)
        {
            GameManager.Instance.LimpiarDatosCargados();
        }
        
        GameManager.Instance.usarDatosDeTransicion = false;

        // 6. Si estamos en el Piso 4 (Escenario4_Jefe), instanciar al personaje no elegido como ayudante
        PrepararAyudanteJefe(jugadorInstanciado);
    }

    private void PrepararAyudanteJefe(GameObject jugadorInstanciado)
    {
        if (AyudanteActual != null || GameManager.Instance == null) return;

        string nombreEscena = SceneManager.GetActiveScene().name;
        bool esEscenaJefe = nombreEscena == "Escenario4_Jefe" || FindAnyObjectByType<BossAI>() != null;
        if (!esEscenaJefe) return;

        GameObject prefabAyudante = GameManager.Instance.ObtenerPrefabAyudante();
        if (prefabAyudante == null) return;

        Vector3 basePos = jugadorInstanciado != null
            ? jugadorInstanciado.transform.position
            : (puntoDeInicio != null ? puntoDeInicio.position : Vector3.zero);
        Vector3 posicionAyudante = basePos + new Vector3(1.6f, -0.75f, 0f);

        // Desactivar temporalmente el prefab antes de instanciar para que no corran Awake/OnEnable
        // de PlayerInput, MiraPersonalizada ni PlayerShooting antes de convertirlo en IA ayudante
        bool estadoOriginalPrefab = prefabAyudante.activeSelf;
        prefabAyudante.SetActive(false);
        GameObject instanciaAyudante = Instantiate(prefabAyudante, posicionAyudante, Quaternion.identity);
        prefabAyudante.SetActive(estadoOriginalPrefab);

        AyudanteJefeAI.ConfigurarInstanciaComoAyudante(instanciaAyudante);
        instanciaAyudante.SetActive(true);
        AyudanteActual = instanciaAyudante;
    }

    public void RegistrarEnemigoDerrotado(Vector3 posicionMuerte, EnemySpawner origen = null)
    {
        if (contarSoloEnemigosDeSpawners && origen == null) return;
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
            Debug.Log("Cuota alcanzada. Este enemigo soltará la Tarjeta de Acceso.");
        }
    }

    public void RecogerLlave()
    {
        JugadorTieneLlave = true;
        Debug.Log("Llave recogida. Ya puedes abrir la puerta.");
    }
}
