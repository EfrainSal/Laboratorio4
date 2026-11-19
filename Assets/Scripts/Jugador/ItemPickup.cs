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
    { IntentarRecoger(collision, true); }
    private void OnTriggerStay2D(Collider2D collision)
    { IntentarRecoger(collision, false); }
    private void IntentarRecoger(Collider2D collision, bool avisarSiLleno)
    {
        if (!recogido && !DeathScreenController.GameplayBlocked)
        {
            PlayerInventory inventario = collision.GetComponentInParent<PlayerInventory>();
            
            if (inventario != null && inventario.isActiveAndEnabled && (LevelManager.Instance == null || LevelManager.Instance.JugadorActual == inventario.gameObject))
            {
                if (!avisarSiLleno && !inventario.PuedeRecoger(tipoDeObjeto)) return;
                if (!inventario.IntentarAgregarObjeto(tipoDeObjeto)) return;
                recogido = true;
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
