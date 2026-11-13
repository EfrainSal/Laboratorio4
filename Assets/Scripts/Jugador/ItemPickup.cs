using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // Agregamos "Llave" al enumerador
    public enum PickupType { Venda, Botiquin, Estimulante, Llave }
    public PickupType tipoDeObjeto;
    private bool recogido;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!recogido)
        {
            PlayerInventory inventario = collision.GetComponentInParent<PlayerInventory>();
            
            if (inventario != null)
            {
                recogido = true;
                inventario.AgregarObjeto(tipoDeObjeto);
                if (tipoDeObjeto == PickupType.Llave && LevelManager.Instance != null)
                    LevelManager.Instance.RecogerLlave();
                Destroy(gameObject);
            }
        }
    }
}
