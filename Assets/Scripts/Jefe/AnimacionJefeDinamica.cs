using UnityEngine;
using Laboratorio4.Animacion;

[RequireComponent(typeof(AnimadorSprites8Direcciones), typeof(Rigidbody2D))]
public class AnimacionJefeDinamica : MonoBehaviour
{
    private AnimadorSprites8Direcciones animador;
    private Rigidbody2D cuerpo;
    private int direccion;
    private bool accion, muerto;

    private void Awake()
    {
        animador = GetComponent<AnimadorSprites8Direcciones>();
        cuerpo = GetComponent<Rigidbody2D>();
    }
    private void LateUpdate()
    {
        if (muerto || accion) return;
        Vector2 velocidad = cuerpo.linearVelocity;
        if (velocidad.sqrMagnitude > .01f)
        {
            direccion = AnimacionJasonDinamica.Direccion8(velocidad);
            animador.Reproducir("Correr", direccion);
        }
        else animador.Reproducir("Quieto", direccion);
    }
    public void ComenzarAccion(string nombre, Vector2 objetivo)
    {
        if (muerto) return;
        direccion = AnimacionJasonDinamica.Direccion8(objetivo - (Vector2)transform.position);
        accion = true;
        animador.Reproducir(nombre, direccion, true);
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
