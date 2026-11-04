using UnityEngine;
using System.Collections; // Importante añadir esto para poder usar IEnumerator (Corrutinas)

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Componentes")]
    public AudioSource musicSource;

    [Header("Pistas de Audio")]
    public AudioClip musicaExploracion; 
    public AudioClip musicaCombate;     

    [Header("Configuración de Transición")]
    [Tooltip("Tiempo en segundos que tardará en bajar y subir el volumen")]
    public float tiempoFade = 1.0f; 

    private int enemigosActivos = 0;
    private bool enCombate = false;
    
    // Variables para controlar el Fade
    private Coroutine transicionActiva;
    private float volumenMaximo;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Guardamos el volumen inicial del inspector para saber hasta dónde subirlo
        volumenMaximo = musicSource.volume;
        
        // Iniciamos con la música de exploración
        CambiarMusica(musicaExploracion);
    }

    public void RegistrarEnemigoSpawneado()
    {
        enemigosActivos++;
        if (!enCombate)
        {
            CambiarMusica(musicaCombate);
            enCombate = true;
        }
    }

    public void RegistrarEnemigoDerrotado()
    {
        enemigosActivos--;
        
        if (enemigosActivos <= 0)
        {
            enemigosActivos = 0;
            if (enCombate)
            {
                CambiarMusica(musicaExploracion);
                enCombate = false;
            }
        }
    }

    private void CambiarMusica(AudioClip nuevoClip)
    {
        if (musicSource.clip == nuevoClip) return; 
        
        // Si ya hay una transición ocurriendo (ej. mataste al último enemigo justo cuando acababa de aparecer), la detenemos
        if (transicionActiva != null)
        {
            StopCoroutine(transicionActiva);
        }

        // Iniciamos el efecto de transición
        transicionActiva = StartCoroutine(TransicionFade(nuevoClip));
    }

    private IEnumerator TransicionFade(AudioClip nuevoClip)
    {
        // 1. Fade Out: Bajar el volumen progresivamente
        while (musicSource.volume > 0)
        {
            musicSource.volume -= volumenMaximo * (Time.deltaTime / tiempoFade);
            yield return null; // Espera al siguiente frame
        }

        // 2. Cambiar la pista
        musicSource.Stop();
        musicSource.clip = nuevoClip;
        musicSource.Play();

        // 3. Fade In: Subir el volumen progresivamente
        while (musicSource.volume < volumenMaximo)
        {
            musicSource.volume += volumenMaximo * (Time.deltaTime / tiempoFade);
            yield return null; // Espera al siguiente frame
        }

        // Aseguramos que el volumen quede exactamente al máximo establecido al terminar
        musicSource.volume = volumenMaximo; 
    }
}