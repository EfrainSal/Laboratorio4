using UnityEngine;
using UnityEngine.UI;

public class UIManagerNotas : MonoBehaviour
{
    public static UIManagerNotas Instance { get; private set; }

    [Header("Referencias UI")]
    public GameObject panelNota; // Asigna aquí el PanelNotaUI
    public Image imagenNotaMostrada; // Asigna aquí la ImagenNotaMostrada

    public bool NotaActiva => panelNota.activeSelf;

    private void Awake()
    {
        // Singleton para poder llamarlo fácilmente desde cualquier lado
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MostrarNota(Sprite spriteDeLaNota)
    {
        if (spriteDeLaNota != null)
        {
            imagenNotaMostrada.sprite = spriteDeLaNota;
            panelNota.SetActive(true);
        }
    }

    public void OcultarNota()
    {
        panelNota.SetActive(false);
    }
}