using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // Agregamos "Llave" al enumerador
    public enum PickupType { Venda, Botiquin, Estimulante, Llave }
    public PickupType tipoDeObjeto;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerInventory inventario = collision.GetComponent<PlayerInventory>();
            
            if (inventario != null)
            {
                inventario.AgregarObjeto(tipoDeObjeto);
                Destroy(gameObject);
            }
        }
    }
}