using UnityEngine;
using Laboratorio4.Animacion;

[RequireComponent(typeof(AnimadorSprites8Direcciones), typeof(Rigidbody2D))]
public class AnimacionJefeDinamica : MonoBehaviour
{
    private AnimadorSprites8Direcciones animador;
    private Rigidbody2D cuerpo;
    private int direccion;
    private bool accion, muerto;
    private bool corriendo;

    private void Awake()
    {
        animador = GetComponent<AnimadorSprites8Direcciones>();
        cuerpo = GetComponent<Rigidbody2D>();
    }
    private void LateUpdate()
    {
        if (muerto || accion) return;
        Vector2 velocidad = cuerpo.linearVelocity;
        corriendo = velocidad.sqrMagnitude > (corriendo ? .0025f : .01f);
        if (corriendo)
        {
            int candidata = AnimacionJasonDinamica.Direccion8(velocidad);
            // Histéresis angular: no oscilar entre dos vistas al cruzar una diagonal.
            Vector2 eje = Quaternion.Euler(0, 0, direccion * 45f) * Vector2.down;
            if (Vector2.Angle(eje, velocidad) > 28f) direccion = candidata;
            animador.Reproducir("Correr", direccion);
        }
        else animador.Reproducir("Quieto", direccion);
    }
    public float ComenzarAccion(string nombre, Vector2 objetivo, float duracion = 0f)
    {
        if (muerto) return 0;
        direccion = AnimacionJasonDinamica.Direccion8(objetivo - (Vector2)transform.position);
        accion = true;
        animador.Reproducir(nombre, direccion, true);
        if (duracion > 0) animador.AjustarDuracionActual(duracion);
        return duracion > 0 ? duracion : animador.Duracion(nombre, direccion);
    }
    public void TerminarAccion() { accion = false; }
    public float Morir()
    {
        muerto = true;
        animador.Reproducir("Muerte", direccion, true);
        foreach (var clip in animador.Animaciones)
            if (clip.nombre == "Muerte")
                return (AnimadorSprites8Direcciones.Frames(clip, direccion)?.Length ?? 0) / Mathf.Max(1, clip.fps);
        return 0;
    }
}
