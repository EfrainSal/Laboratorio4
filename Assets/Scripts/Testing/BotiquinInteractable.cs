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

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Abrir()
    {
        if (yaAbierto) return;

        yaAbierto = true;

        // 1. Reproducir la animación usando el Trigger configurado
        if (anim != null)
        {
            anim.SetTrigger("Abrir");
        }

        // 2. Soltar el objeto
        if (objetoASoltar != null)
        {
            Vector3 posicionCaida = transform.position + new Vector3(0, -distanciaSoltar, 0);
            Instantiate(objetoASoltar, posicionCaida, Quaternion.identity);
        }
        
        // Se eliminó la línea que desactivaba el BoxCollider2D para conservar la colisión.
    }
}