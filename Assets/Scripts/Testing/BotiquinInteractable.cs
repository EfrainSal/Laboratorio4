using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class BotiquinInteractable : MonoBehaviour
{
    [Header("Recompensas")]
    [Tooltip("El prefab del objeto que va a soltar (ej. una cura o un arma)")]
    public GameObject objetoASoltar;
    [Tooltip("Distancia hacia abajo donde aparecerá el objeto")]
    public float distanciaSoltar = 0.5f;

    private Animator anim;
    private bool yaAbierto = false;
    public bool YaAbierto => yaAbierto;
    public string IdPersistencia { get; private set; }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (string.IsNullOrEmpty(IdPersistencia))
        {
            IdPersistencia = LevelManager.GenerarIdObjetoEscena(gameObject);
        }
    }

    public void Abrir()
    {
        if (yaAbierto) return;

        yaAbierto = true;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RegistrarContenedorAbierto(IdPersistencia);
        }

        // 1. Reproducir la animación usando el Trigger configurado
        if (anim != null)
        {
            anim.SetTrigger("Abrir");
        }

        // 2. Soltar el objeto
        if (objetoASoltar != null)
        {
            InstanciarRecompensa();
        }
        
        // Se eliminó la línea que desactivaba el BoxCollider2D para conservar la colisión.
    }

    public void RestaurarEstadoAbierto(bool soltarRecompensaPendiente)
    {
        yaAbierto = true;
        if (anim == null) anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play("BotiquinAbrir", 0, 1f);
            anim.Update(0f);
        }

        if (soltarRecompensaPendiente && objetoASoltar != null)
        {
            InstanciarRecompensa();
        }
    }

    private void InstanciarRecompensa()
    {
        Vector3 posicionCaida = transform.position + new Vector3(0, -distanciaSoltar, 0);
        GameObject drop = Instantiate(objetoASoltar, posicionCaida, Quaternion.identity);
        string idDrop = IdPersistencia + "_Drop";
        if (drop.TryGetComponent<ItemPickup>(out var pickup))
        {
            pickup.IdPersistencia = idDrop;
        }
        if (drop.TryGetComponent<AmmoDrop>(out var ammo))
        {
            ammo.IdPersistencia = idDrop;
        }
    }
}