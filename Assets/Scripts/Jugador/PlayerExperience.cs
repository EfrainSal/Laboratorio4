using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class PlayerExperience : MonoBehaviour
{
    [Header("Nivel y Experiencia")]
    public int nivelActual = 1;
    public float experienciaActual = 0f;
    public float expParaSiguienteNivel = 100f;
    [Tooltip("Cuánto aumenta la experiencia requerida por nivel")]
    public float multiplicadorDificultad = 1.5f; 

    [Header("Árbol de Atributos")]
    public int puntosDeMejora = 0; // Puntos para gastar en el futuro

    [Header("UI Textos (Asignar desde el Canvas)")]
    public TextMeshProUGUI textoNivel;
    public TextMeshProUGUI textoXP;

    void Start()
    {
        ActualizarUI();
    }

    public void GanarExperiencia(float cantidadXP)
    {
        experienciaActual += cantidadXP;
        
        // Usamos un while por si gana tanta XP de golpe que sube 2 niveles seguidos
        while (experienciaActual >= expParaSiguienteNivel)
        {
            SubirDeNivel();
        }
        
        ActualizarUI();
    }

    public void RestaurarProgreso(int nivel, float experiencia)
    {
        nivelActual = Mathf.Max(1, nivel);
        experienciaActual = Mathf.Max(0, experiencia);
        expParaSiguienteNivel = 100f * Mathf.Pow(multiplicadorDificultad, nivelActual - 1);
        ActualizarUI();
    }

    private void SubirDeNivel()
    {
        // Restamos la exp requerida (conservando el sobrante)
        experienciaActual -= expParaSiguienteNivel;
        
        nivelActual++;
        puntosDeMejora++; // Otorgamos un punto para el árbol de habilidades
        
        // Aumentamos la meta de XP para el próximo nivel
        expParaSiguienteNivel *= multiplicadorDificultad;
    }

    private void ActualizarUI()
    {
        if (textoNivel != null) 
            textoNivel.text = "Nivel: " + nivelActual;
            
        if (textoXP != null) 
            textoXP.text = "XP: " + Mathf.FloorToInt(experienciaActual) + " / " + Mathf.FloorToInt(expParaSiguienteNivel);
    }
}
