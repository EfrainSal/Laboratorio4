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
    [Tooltip("Límite máximo de enemigos vivos al mismo tiempo en este piso (<= 0 significa sin límite)")]
    public int limiteEnemigosSpawneados = 10;
    public int EnemigosDerrotados => enemigosDerrotados;
    public int EnemigosVivosCount => ContarEnemigosVivos();
    public int TotalNotasPiso => totalNotasPiso;
    public int NotasLeidasCount => notasLeidasCount;
    public int EnemigosRestantes => Mathf.Max(0, enemigosParaAvanzar - enemigosDerrotados);
    public int NotasRestantes => Mathf.Max(0, totalNotasPiso - notasLeidasCount);
    public bool TarjetaGenerada => llaveDropeada;
    
    [Header("Objetos de Progresión")]
    [SerializeField] private GameObject llavePrefab;
    
    [Header("Estado Actual (Solo lectura)")]
    [SerializeField] private int enemigosDerrotados = 0;
    [SerializeField] private int enemigosVivosActuales = 0;
    [SerializeField] private int spawnersActivados = 0;
    [SerializeField] private int totalNotasPiso = 0;
    [SerializeField] private int notasLeidasCount = 0;

    // Añade esto en la parte superior donde tienes tus otros [Header]
    [Header("Referencias UI - Habilidad")]
    public Image skillBarFillUI; // La imagen "Fill" de la barra de habilidad
    public Image skillIconUI; // La imagen donde va el icono de la habilidad

    [Header("Referencias UI - Progreso del Piso")]
    public GameObject panelProgresoPisoUI;
    public TextMeshProUGUI textoEnemigosRestantesUI;
    public TextMeshProUGUI textoNotasRestantesUI;

    private bool condicionesCumplidas = false;
    private bool llaveDropeada = false;
    private bool tieneultimaPosicionEnemigo = false;
    private Vector3 ultimaPosicionEnemigo;

    public bool JugadorTieneLlave { get; set; } = false;

    private readonly HashSet<string> objetosRecogidosPiso = new HashSet<string>();
    private readonly HashSet<string> contenedoresAbiertosPiso = new HashSet<string>();
    private readonly HashSet<string> notasLeidasPiso = new HashSet<string>();

    public List<string> ObtenerObjetosRecogidosPiso() => new List<string>(objetosRecogidosPiso);
    public List<string> ObtenerContenedoresAbiertosPiso() => new List<string>(contenedoresAbiertosPiso);
    public List<string> ObtenerNotasLeidasPiso() => new List<string>(notasLeidasPiso);

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
        notasLeidasPiso.Clear();

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

        if (datos.notasLeidasPiso != null)
        {
            foreach (string id in datos.notasLeidasPiso)
            {
                if (!string.IsNullOrEmpty(id)) notasLeidasPiso.Add(id);
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

    private void InicializarNotasYProgresoUI()
    {
        if (panelProgresoPisoUI == null || textoEnemigosRestantesUI == null || textoNotasRestantesUI == null)
        {
            Canvas canvas = healthBarFillUI != null ? healthBarFillUI.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                if (panelProgresoPisoUI == null)
                {
                    Transform foundPanel = canvas.transform.Find("FloorProgressContainer");
                    if (foundPanel != null) panelProgresoPisoUI = foundPanel.gameObject;
                }
                if (textoEnemigosRestantesUI == null)
                {
                    Transform t = panelProgresoPisoUI != null ? panelProgresoPisoUI.transform.Find("EnemigosRestantesText") : canvas.transform.Find("EnemigosRestantesText");
                    if (t != null) textoEnemigosRestantesUI = t.GetComponent<TextMeshProUGUI>();
                }
                if (textoNotasRestantesUI == null)
                {
                    Transform t = panelProgresoPisoUI != null ? panelProgresoPisoUI.transform.Find("NotasRestantesText") : canvas.transform.Find("NotasRestantesText");
                    if (t != null) textoNotasRestantesUI = t.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        RecalcularNotasEscena();
        ActualizarUIProgresoPiso();
    }

    public void RecalcularNotasEscena()
    {
        var notas = FindObjectsByType<NotaInteractable>(FindObjectsSortMode.None);
        int total = 0;
        int leidas = 0;
        foreach (var nota in notas)
        {
            if (nota == null || !nota.isActiveAndEnabled) continue;
            total++;
            string id = !string.IsNullOrEmpty(nota.IdPersistencia) ? nota.IdPersistencia : GenerarIdObjetoEscena(nota.gameObject);
            if (nota.YaLeida || notasLeidasPiso.Contains(id))
            {
                nota.MarcarComoLeida();
                notasLeidasPiso.Add(id);
                leidas++;
            }
        }
        totalNotasPiso = total;
        notasLeidasCount = leidas;
    }

    public void ActualizarUIProgresoPiso()
    {
        string nombreEscena = SceneManager.GetActiveScene().name;
        bool ocultarEnEscena = nombreEscena == "Recepcion" || nombreEscena == "Escenario4_Jefe" || nombreEscena == "MainMenu" || FindAnyObjectByType<BossAI>() != null || enemigosParaAvanzar > 100000;

        if (panelProgresoPisoUI != null)
        {
            panelProgresoPisoUI.SetActive(!ocultarEnEscena);
        }

        if (ocultarEnEscena) return;

        int enemigosFaltantes = Mathf.Max(0, enemigosParaAvanzar - enemigosDerrotados);
        int notasFaltantes = Mathf.Max(0, totalNotasPiso - notasLeidasCount);

        if (textoEnemigosRestantesUI != null)
        {
            textoEnemigosRestantesUI.text = $": {enemigosFaltantes}";
        }

        if (textoNotasRestantesUI != null)
        {
            textoNotasRestantesUI.text = $": {notasFaltantes}";
        }
    }

    public int ContarEnemigosVivos()
    {
        int vivos = 0;
        foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (e != null && e.isActiveAndEnabled && !e.IsDead && (!contarSoloEnemigosDeSpawners || e.OrigenSpawner != null))
            {
                vivos++;
            }
        }
        enemigosVivosActuales = vivos;
        return vivos;
    }

    public int ObtenerCupoSpawneoPiso(int cantidadSolicitada)
    {
        if (cantidadSolicitada <= 0) return 0;
        int vivos = ContarEnemigosVivos();
        if (limiteEnemigosSpawneados <= 0) return cantidadSolicitada;
        int disponibles = Mathf.Max(0, limiteEnemigosSpawneados - vivos);
        return Mathf.Min(cantidadSolicitada, disponibles);
    }

    public void RegistrarEnemigoSpawneado(int cantidad = 1)
    {
        if (cantidad <= 0) return;
        enemigosVivosActuales += cantidad;
    }

    public void RegistrarNotaLeida(NotaInteractable nota)
    {
        if (nota == null) return;
        string id = !string.IsNullOrEmpty(nota.IdPersistencia)
            ? nota.IdPersistencia
            : GenerarIdObjetoEscena(nota.gameObject);
        nota.MarcarComoLeida();
        notasLeidasPiso.Add(id);

        RecalcularNotasEscena();
        VerificarCondiciones();
        ActualizarUIProgresoPiso();

        if (condicionesCumplidas && !llaveDropeada && llavePrefab != null)
        {
            Vector3 posDrop = JugadorActual != null
                ? JugadorActual.transform.position
                : (tieneultimaPosicionEnemigo ? ultimaPosicionEnemigo : nota.transform.position);
            Instantiate(llavePrefab, posDrop, Quaternion.identity);
            llaveDropeada = true;
            Debug.Log("¡Condiciones cumplidas tras leer todas las notas! Se soltó la llave de salida.");
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
            RestaurarEstadoObjetosEscena(datosGuardados);
            // Mover al jugador a donde estaba al guardar
            jugadorInstanciado.transform.position = new Vector3(datosGuardados.posicionX, datosGuardados.posicionY, 0f);
        }

        ContarEnemigosVivos();
        InicializarNotasYProgresoUI();
        VerificarCondiciones();

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
        enemigosVivosActuales = Mathf.Max(0, enemigosVivosActuales - 1);
        if (contarSoloEnemigosDeSpawners && origen == null) return;
        enemigosDerrotados++;
        ultimaPosicionEnemigo = posicionMuerte;
        tieneultimaPosicionEnemigo = true;
        VerificarCondiciones();
        ActualizarUIProgresoPiso();

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
        RecalcularNotasEscena();
        bool todasLasNotasLeidas = notasLeidasCount >= totalNotasPiso;
        if (!condicionesCumplidas && enemigosDerrotados >= enemigosParaAvanzar && spawnersActivados >= spawnersRequeridos && todasLasNotasLeidas)
        {
            condicionesCumplidas = true;
            Debug.Log("Cuota de enemigos y notas alcanzada. Se puede soltar la Tarjeta de Acceso.");
        }
    }

    public void RecogerLlave()
    {
        JugadorTieneLlave = true;
        Debug.Log("Llave recogida. Ya puedes abrir la puerta.");
    }
}
