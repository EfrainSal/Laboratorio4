using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class MenuMejoras : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject panelMenu; 
    public TextMeshProUGUI textoPuntos; 

    [Header("Valores de Mejora por Punto")]
    public float aumentoDaño = 5f;
    public float aumentoVida = 20f;
    public float aumentoDefensa = 2f; 

    private PlayerExperience exp;
    private PlayerCombat combate;
    private PlayerHealth salud;

    void Start()
    {
        if (panelMenu != null) panelMenu.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            exp = player.GetComponent<PlayerExperience>();
            combate = player.GetComponent<PlayerCombat>();
            salud = player.GetComponent<PlayerHealth>();
        }
    }

    // Vinculado a la acción "ToggleMenu"
    public void OnToggleMenu(InputValue value)
    {
        if (value.isPressed)
        {
            bool estadoActual = panelMenu.activeSelf;
            panelMenu.SetActive(!estadoActual);
            
            if (!estadoActual) ActualizarTextoPuntos();
        }
    }

    private void ActualizarTextoPuntos()
    {
        if (textoPuntos != null && exp != null)
        {
            textoPuntos.text = "Puntos Disponibles: " + exp.puntosDeMejora;
        }
    }

    public void MejorarDaño()
    {
        if (exp.puntosDeMejora > 0)
        {
            exp.puntosDeMejora--; 
            combate.attackDamage += aumentoDaño; 
            ActualizarTextoPuntos();
        }
    }

    public void MejorarVida()
    {
        if (exp.puntosDeMejora > 0)
        {
            exp.puntosDeMejora--;
            salud.maxHealth += aumentoVida;
            salud.Heal(aumentoVida); 
            ActualizarTextoPuntos();
        }
    }

    public void MejorarDefensa()
    {
        if (exp.puntosDeMejora > 0 && salud.defensaPorcentual < salud.defensaMaxima)
        {
            exp.puntosDeMejora--;
            salud.defensaPorcentual += aumentoDefensa;
            
            if (salud.defensaPorcentual > salud.defensaMaxima)
            {
                salud.defensaPorcentual = salud.defensaMaxima;
            }
            
            ActualizarTextoPuntos();
        }
    }
}