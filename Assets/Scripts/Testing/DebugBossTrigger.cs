using UnityEngine;

public class DebugBossTrigger : MonoBehaviour
{
    private bool yaActivado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!yaActivado && collision.CompareTag("Player"))
        {
            yaActivado = true; // Bloquea futuros eventos en los siguientes milisegundos
            Debug.Log("Jefe de Debug derrotado. Finalizando la run con victoria...");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompletarRunVictoria();
            }
            
            Destroy(gameObject);
        }
    }
}