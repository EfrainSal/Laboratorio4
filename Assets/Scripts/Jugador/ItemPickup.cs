using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // Agregamos "Llave" al enumerador
    public enum PickupType { Venda, Botiquin, Estimulante, Llave }
    public PickupType tipoDeObjeto;
    private bool recogido;
    public string IdPersistencia { get; set; }

    private void Awake()
    {
        if (string.IsNullOrEmpty(IdPersistencia))
        {
            IdPersistencia = LevelManager.GenerarIdObjetoEscena(gameObject);
        }

        if (GetComponent<BrilloObjeto>() == null)
        {
            gameObject.AddComponent<BrilloObjeto>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!recogido)
        {
            PlayerInventory inventario = collision.GetComponentInParent<PlayerInventory>();
            
            if (inventario != null)
            {
                recogido = true;
                inventario.AgregarObjeto(tipoDeObjeto);
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.RegistrarObjetoRecogido(IdPersistencia);
                    if (tipoDeObjeto == PickupType.Llave)
                        LevelManager.Instance.RecogerLlave();
                }
                Destroy(gameObject);
            }
        }
    }
}
