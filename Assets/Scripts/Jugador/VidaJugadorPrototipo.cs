using UnityEngine;
using Laboratorio4.Escenario3;
using Laboratorio4.Escenario4;

// Puente de salud para los jugadores de mapas antiguos; el valor real sigue siendo float.
[DefaultExecutionOrder(-500)]
[RequireComponent(typeof(PlayerHealth))]
public class VidaJugadorPrototipo : MonoBehaviour
{
    private PlayerHealth salud;
    private E3Vida vidaE3;
    private E4Vida vidaE4;
    private void Awake()
    {
        salud = GetComponent<PlayerHealth>(); vidaE3 = GetComponent<E3Vida>(); vidaE4 = GetComponent<E4Vida>();
        var gm = GameManager.Instance;
        if (gm != null && gm.usarDatosDeTransicion)
        {
            salud.RestaurarVida(gm.vidaPendiente, gm.vidaMaximaPendiente);
            gm.usarDatosDeTransicion = false;
        }
        Sincronizar();
    }
    public void Sincronizar()
    {
        if (vidaE3 != null) { vidaE3.maxima = Mathf.CeilToInt(salud.maxHealth); vidaE3.actual = Mathf.CeilToInt(salud.currentHealth); }
        if (vidaE4 != null) { vidaE4.maximo = Mathf.CeilToInt(salud.maxHealth); vidaE4.actual = Mathf.CeilToInt(salud.currentHealth); }
    }
    private void LateUpdate() { Sincronizar(); }
}
