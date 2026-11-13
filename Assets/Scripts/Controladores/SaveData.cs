[System.Serializable]
public class SaveData
{
    public int version = 1;
    public string escenaActual;
    public int enemigosDerrotadosPiso;
    public bool tarjetaGenerada;
    public int nivelJugador = 1;
    public float experienciaJugador;

    // Datos de la run
    public int personajeSeleccionado;
    public float tiempoRun;
    public int enemigosDerrotadosTotal;
    public int puntuacionTotal;
    public int pisoActual;

    // Nivel de cada mejora comprada en esta run
    public int nivelMejoraVida;
    public int nivelMejoraDefensa;
    public int nivelMejoraDanio;
    public int puntosDeMejora;

    // Datos del jugador (Transform y Health)
    public float posicionX;
    public float posicionY;
    public float vidaActual;

    // Inventario
    public int cantidadVendas;
    public int cantidadBotiquines;
    public int cantidadEstimulantes;
    public int llavesCount;

    // Armas (PlayerShooting)
    public int currentAmmoInClip;
    public int totalAmmo;
}
