using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para detectar cambios de escena
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Componentes")]
    public AudioSource musicSource;

    [Header("Pistas de Audio")]
    public AudioClip musicaMenu;        // Nueva pista para el menú principal
    public AudioClip musicaExploracion; 
    public AudioClip musicaCombate;     

    [Header("Configuración de Transición")]
    [Tooltip("Tiempo en segundos que tardará en bajar y subir el volumen")]
    public float tiempoFade = 1.0f; 

    private int enemigosActivos = 0;
    private bool enCombate = false;
    
    private Coroutine transicionActiva;
    private float volumenMaximo;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Suscribimos el método OnSceneLoaded al evento de Unity
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        volumenMaximo = musicSource.volume;
        // Quitamos CambiarMusica de aquí, ya que OnSceneLoaded lo manejará
    }

    // Este método se dispara automáticamente al cargar CUALQUIER escena (incluso al reintentar)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Reseteamos los estados de combate para solucionar el Bug 2
        enemigosActivos = 0;
        enCombate = false;

        // 2. Gestionamos qué música debe sonar según la escena para solucionar el Bug 1
        if (scene.name == "MainMenu") 
        {
            CambiarMusica(musicaMenu);
        }
        else 
        {
            CambiarMusica(musicaExploracion);
        }
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
        
        if (transicionActiva != null)
        {
            StopCoroutine(transicionActiva);
        }

        transicionActiva = StartCoroutine(TransicionFade(nuevoClip));
    }

    private IEnumerator TransicionFade(AudioClip nuevoClip)
    {
        while (musicSource.volume > 0)
        {
            musicSource.volume -= volumenMaximo * (Time.deltaTime / tiempoFade);
            yield return null; 
        }

        musicSource.Stop();
        musicSource.clip = nuevoClip;
        musicSource.Play();

        while (musicSource.volume < volumenMaximo)
        {
            musicSource.volume += volumenMaximo * (Time.deltaTime / tiempoFade);
            yield return null; 
        }

        musicSource.volume = volumenMaximo; 
    }

    // Es buena práctica desuscribirse del evento si el Singleton llega a destruirse
    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}