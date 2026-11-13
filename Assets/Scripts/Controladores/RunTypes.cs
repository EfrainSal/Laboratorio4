public enum DeathCause
{
    Desconocida,
    Mutante,    // cuerpo a cuerpo
    Disparo,    // enemigos a distancia
    Jefe,       // jefe del piso 4
    Entorno     // trampas, peligros
}

public struct RunSummary
{
    public float tiempo;
    public int piso;
    public int enemigos;
    public int puntuacion;
    public int mejorPuntuacion;
    public bool nuevoRecord;
    public DeathCause causa;
}