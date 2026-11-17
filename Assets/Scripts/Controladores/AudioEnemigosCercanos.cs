using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Distancia XY al personaje real: la cámara de un juego 2D está desplazada en Z.
// Una mezcla acotada sustituye los PlayOneShot masivos y los sonidos de muerte sin límite.
public class AudioEnemigosCercanos : MonoBehaviour
{
    public int maxVocesSorbos = 4;
    public int maxVocesJefe = 2;
    [Range(0, 1)] public float volumenDuranteDisparo = .25f;
    public float duracionPrioridadDisparo = .16f;
    public static AudioEnemigosCercanos Instancia { get; private set; }
    private float prioridadHasta;
    private class Voz
    {
        public AudioSource fuente;
        public Transform emisor;
        public Vector3 posicion;
        public float radio, volumen;
        public bool jefe;
    }
    private readonly List<Voz> voces = new List<Voz>();
    public int VocesActivas => voces.FindAll(v => v.fuente.isPlaying).Count;
    public float GananciaEnemigos => Time.unscaledTime < prioridadHasta ? volumenDuranteDisparo : 1f;
    private static AudioEnemigosCercanos Obtener()
    {
        if (Instancia != null) return Instancia;
        var go = new GameObject("AudioEnemigosCercanos");
        return go.AddComponent<AudioEnemigosCercanos>();
    }
    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this; DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += AlCargarEscena;
    }
    private static Transform Jugador()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.JugadorActual != null)
            return LevelManager.Instance.JugadorActual.transform;
        var salud = FindFirstObjectByType<PlayerHealth>();
        return salud != null && !salud.IsDead ? salud.transform : null;
    }
    public static void PriorizarDisparoJugador()
    {
        var mezcla = Obtener(); mezcla.prioridadHasta = Time.unscaledTime + mezcla.duracionPrioridadDisparo;
        mezcla.ActualizarVolumen();
    }
    public static bool Reproducir(AudioClip clip, Transform emisor, AudioSource referencia, float radio = 10f,
        float volumen = .55f, float pitch = 1f, bool jefe = false)
    {
        if (clip == null || emisor == null || DeathScreenController.GameplayBlocked) return false;
        var jugador = Jugador();
        if (jugador == null || Vector2.Distance(jugador.position, emisor.position) >= radio) return false;
        var mezcla = Obtener(); Voz libre = null, lejana = null;
        int activas = 0; float distanciaLejana = -1;
        foreach (var voz in mezcla.voces)
        {
            if (voz.jefe != jefe) continue;
            if (voz.fuente.isPlaying)
            {
                activas++;
                // Un enemigo nunca superpone su alerta, daño y muerte consigo mismo.
                if (voz.emisor == emisor) { libre = voz; break; }
                float d = Vector2.Distance(jugador.position, voz.posicion);
                if (d > distanciaLejana) { lejana = voz; distanciaLejana = d; }
            }
            else if (libre == null) libre = voz;
        }
        int limite = Mathf.Max(1, jefe ? mezcla.maxVocesJefe : mezcla.maxVocesSorbos);
        if (libre == null && activas < limite)
        {
            var go = new GameObject(jefe ? "VozJefe" : "VozSorbo"); go.transform.SetParent(mezcla.transform);
            libre = new Voz { fuente = go.AddComponent<AudioSource>(), jefe = jefe }; mezcla.voces.Add(libre);
        }
        if (libre == null)
        {
            if (lejana == null || Vector2.Distance(jugador.position, emisor.position) >= distanciaLejana) return false;
            libre = lejana;
        }
        libre.fuente.Stop(); libre.emisor = emisor; libre.posicion = emisor.position;
        libre.radio = Mathf.Max(.1f, radio); libre.volumen = Mathf.Clamp01(volumen) * (referencia != null ? referencia.volume : 1f);
        var audio = libre.fuente; audio.playOnAwake = false; audio.loop = false; audio.spatialBlend = 0;
        audio.priority = jefe ? 96 : 192; audio.dopplerLevel = 0;
        audio.outputAudioMixerGroup = referencia != null ? referencia.outputAudioMixerGroup : null;
        audio.pitch = Mathf.Clamp(pitch, .5f, 2f); audio.clip = clip;
        mezcla.AjustarVoz(libre, jugador); audio.Play();
        return true;
    }
    private void AjustarVoz(Voz voz, Transform jugador)
    {
        if (voz.emisor != null) voz.posicion = voz.emisor.position;
        float d = Vector2.Distance(jugador.position, voz.posicion);
        if (d >= voz.radio) { voz.fuente.Stop(); voz.fuente.volume = 0; return; }
        float ganancia = 1f - Mathf.InverseLerp(voz.radio * .25f, voz.radio, d);
        voz.fuente.volume = voz.volumen * ganancia * ganancia * GananciaEnemigos;
    }
    private void ActualizarVolumen()
    {
        var jugador = Jugador();
        if (jugador == null || DeathScreenController.GameplayBlocked) { Silenciar(); return; }
        foreach (var voz in voces) if (voz.fuente.isPlaying) AjustarVoz(voz, jugador);
    }
    private void Update() { ActualizarVolumen(); }
    private void Silenciar() { foreach (var voz in voces) voz.fuente.Stop(); }
    private void AlCargarEscena(Scene escena, LoadSceneMode modo) { Silenciar(); }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (Instancia == this) Instancia = null;
    }
}
