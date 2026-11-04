using UnityEngine;

public class SeguimientoCamara : MonoBehaviour
{
    public Transform jugador; 
    public float suavizado = 5f;
    public Vector3 compensacion = new Vector3(0, 0, -10f); 

    void LateUpdate()
    {
        // Si no hay jugador asignado, lo busca
        if (jugador == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                jugador = playerObj.transform;
            }
            else
            {
                return; // Si aún no aparece, sale y vuelve a intentar el próximo frame
            }
        }

        // Movimiento de la cámara
        Vector3 posicionDeseada = jugador.position + compensacion;
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);
    }
}