using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>La escucha la pantalla de muerte.</summary>
    public static event System.Action<RunSummary> OnRunTerminada;

    public bool RunActiva => runActiva;

    /// <summary>Datos de "Continuar": el LevelManager los recoge y llama a LimpiarDatosCargados().</summary>
    public SaveData DatosCargados { get; private set; }

    [Header("Personajes Disponibles")]
    public GameObject prefabJason;
    public GameObject prefabMilena;

    [Header("Datos de la Run")]
    public int personajeSeleccionado = 0; // 0 = Jason, 1 = Milena
    public float tiempoRun = 0f;
    public int enemigosDerrotadosTotal = 0;
    public int puntuacionTotal = 0;
    public int pisoActual = 1;

    [Header("Mejoras (nivel de cada habilidad en esta run)")]
    public int nivelMejoraVida = 0;
    public int nivelMejoraDefensa = 0;
    public int nivelMejoraDanio = 0;

    [Header("Datos de Transición entre Niveles")]
    public bool usarDatosDeTransicion = false;
    public int puntosDeMejoraPendientes = 0;
    public float vidaPendiente = 0f;
    public int vendasPendientes = 0;
    public int botiquinesPendientes = 0;
    public int estimulantesPendientes = 0;
    public int municionCargadorPendiente = 0;
    public int municionTotalPendiente = 0;
    // (Opcional) Si quieres guardar la experiencia actual
    public float expActualPendiente = 0f; 
    public int nivelActualPendiente = 1;

    private bool runActiva = false;

    private void Awake()
    {
        // Configuración DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Solo sumamos tiempo si hay una run activa (con Time.timeScale = 0 se detiene solo)
        if (runActiva)
        {
            tiempoRun += Time.deltaTime;
        }
    }

    // Método que llamamos desde el LevelManager para saber a quién instanciar
    public GameObject ObtenerPrefabJugador()
    {
        return personajeSeleccionado == 0 ? prefabJason : prefabMilena;
    }

    // ---------------------------------------------------------
    // GESTIÓN DE LA RUN (Roguelike)
    // ---------------------------------------------------------

    // Se llama desde el MainMenuController al darle a "Confirmar" y desde "Reintentar"
    public void IniciarNuevaRun(int idPersonaje)
    {
        personajeSeleccionado = idPersonaje;

        tiempoRun = 0f;
        enemigosDerrotadosTotal = 0;
        puntuacionTotal = 0;
        pisoActual = 1;

        nivelMejoraVida = 0;
        nivelMejoraDefensa = 0;
        nivelMejoraDanio = 0;
        
        // LIMPIEZA CRÍTICA: Borramos los datos en tránsito para que no aparezcan en la nueva partida
        usarDatosDeTransicion = false;
        puntosDeMejoraPendientes = 0;
        vidaPendiente = 0f;
        vendasPendientes = 0;
        botiquinesPendientes = 0;
        estimulantesPendientes = 0;
        municionCargadorPendiente = 0;
        municionTotalPendiente = 0;

        DatosCargados = null;   

        runActiva = true;
    }

    // Se llama cuando el jugador avanza de piso
    public void AvanzarPiso()
    {
        pisoActual++;
    }

    // Se llama desde los scripts de los enemigos al morir
    public void RegistrarBajaEnemiga(int experienciaEnemigo)
    {
        if (!runActiva) return;

        enemigosDerrotadosTotal++;

        // Puntuación: (XP base del enemigo) * (multiplicador por piso)
        int puntosGanados = experienciaEnemigo * pisoActual;
        puntuacionTotal += puntosGanados;
    }

    // Se llama desde PlayerHealth.cs cuando el jugador muere
    public void TerminarRunPorMuerte(DeathCause causa = DeathCause.Desconocida)
    {
        runActiva = false;

        int mejorPrevia = PlayerPrefs.GetInt("MejorPuntuacion", 0);
        bool nuevoRecord = puntuacionTotal > mejorPrevia;
        if (nuevoRecord)
        {
            PlayerPrefs.SetInt("MejorPuntuacion", puntuacionTotal);
        }
        PlayerPrefs.Save();

        BorrarGuardadoLocal(); // Limpieza por muerte permanente

        Debug.Log($"--- RUN TERMINADA ({causa}) --- Piso {pisoActual} | Puntos {puntuacionTotal}");

        OnRunTerminada?.Invoke(new RunSummary
        {
            tiempo = tiempoRun,
            piso = pisoActual,
            enemigos = enemigosDerrotadosTotal,
            puntuacion = puntuacionTotal,
            mejorPuntuacion = Mathf.Max(mejorPrevia, puntuacionTotal),
            nuevoRecord = nuevoRecord,
            causa = causa
        });
    }

    public void CompletarRunVictoria()
    {
        runActiva = false;
        
        // Otorgamos bonos extra por ganar si los tienes, calculamos score final...
        Debug.Log($"--- RUN COMPLETADA CON ÉXITO --- Piso {pisoActual} | Puntos {puntuacionTotal}");

        string nombreJugador = PlayerPrefs.GetString("PlayerRankingName", "Desconocido");

        // Enviamos al nuevo sistema de Ranking
        RankingManager.RegistrarPartidaCompletada(nombreJugador, puntuacionTotal, tiempoRun, personajeSeleccionado);

        // Borrar el guardado temporal para evitar clonar el final
        BorrarGuardadoLocal();

        // Puedes invocar un evento de "Pantalla de Victoria"
        OnRunTerminada?.Invoke(new RunSummary
        {
            tiempo = tiempoRun,
            piso = pisoActual,
            enemigos = enemigosDerrotadosTotal,
            puntuacion = puntuacionTotal,
            nuevoRecord = true,
            causa = DeathCause.Desconocida // O crear un enum de Victoria
        });
    }

    // "Guardar y salir": suspende la run en un archivo cifrado
    /// <returns>true si se guardó; si falla, la run sigue activa para no perder el progreso.</returns>
    public bool GuardarRun()
    {
        SaveData data = new SaveData
        {
            personajeSeleccionado = personajeSeleccionado,
            tiempoRun = tiempoRun,
            enemigosDerrotadosTotal = enemigosDerrotadosTotal,
            puntuacionTotal = puntuacionTotal,
            pisoActual = pisoActual,
            nivelMejoraVida = nivelMejoraVida,
            nivelMejoraDefensa = nivelMejoraDefensa,
            nivelMejoraDanio = nivelMejoraDanio
        };

        // Buscamos al jugador en la escena para sacarle sus datos exactos
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.posicionX = player.transform.position.x;
            data.posicionY = player.transform.position.y;

            var health = player.GetComponent<PlayerHealth>();
            if (health != null) data.vidaActual = health.currentHealth;

            var exp = player.GetComponent<PlayerExperience>();
            if (exp != null) data.puntosDeMejora = exp.puntosDeMejora;

            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                data.cantidadVendas = inv.vendasCount;
                data.cantidadBotiquines = inv.botiquinesCount;
                data.cantidadEstimulantes = inv.estimulantesCount;
                data.llavesCount = inv.llavesCount;
            }

            var shoot = player.GetComponent<PlayerShooting>();
            if (shoot != null)
            {
                data.currentAmmoInClip = shoot.currentAmmoInClip;
                data.totalAmmo = shoot.totalAmmo;
            }
        }

        bool guardado = SaveSystem.Guardar(data);
        if (guardado) runActiva = false;
        return guardado;
    }

    public void BorrarGuardadoLocal()
    {
        SaveSystem.Borrar();
    }

    // Se llama desde "Continuar". La partida guardada se consume al cargarla.
    public bool CargarRunGuardada()
    {
        SaveData data = SaveSystem.Cargar();
        if (data == null) return false;

        personajeSeleccionado = data.personajeSeleccionado;
        tiempoRun = data.tiempoRun;
        enemigosDerrotadosTotal = data.enemigosDerrotadosTotal;
        puntuacionTotal = data.puntuacionTotal;
        pisoActual = data.pisoActual;

        nivelMejoraVida = data.nivelMejoraVida;
        nivelMejoraDefensa = data.nivelMejoraDefensa;
        nivelMejoraDanio = data.nivelMejoraDanio;

        // Guardamos los datos temporalmente para que el LevelManager los recoja
        DatosCargados = data;

        BorrarGuardadoLocal(); // Eliminamos el archivo para evitar trampas
        runActiva = true;
        return true;
    }

    public void LimpiarDatosCargados()
    {
        DatosCargados = null;
    }

    // "Abandonar incursión": termina la run sin pantalla de muerte
    public void AbandonarRun()
    {
        runActiva = false;

        int mejorPuntuacion = PlayerPrefs.GetInt("MejorPuntuacion", 0);
        if (puntuacionTotal > mejorPuntuacion)
            PlayerPrefs.SetInt("MejorPuntuacion", puntuacionTotal);
        PlayerPrefs.Save();

        BorrarGuardadoLocal(); // Limpieza por abandono
    }

    // Utilidad para mostrar el tiempo en formato MM:SS
    public string FormatearTiempo(float tiempoEnSegundos)
    {
        int minutos = Mathf.FloorToInt(tiempoEnSegundos / 60);
        int segundos = Mathf.FloorToInt(tiempoEnSegundos % 60);
        return string.Format("{0:00}:{1:00}", minutos, segundos);
    }
}