using UnityEngine;
using Laboratorio4.Animacion;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(AnimadorSprites8Direcciones))]
public class AnimacionEnemigoDinamica : MonoBehaviour
{
    [HideInInspector] public string rutaBaseSprites = "Art/Personajes/Zorbo";
    [HideInInspector] public float fpsCaminar = 8f;
    private AnimadorSprites8Direcciones anim;
    private Rigidbody2D cuerpo;
    private int direccion;
    private bool atacando, muerto;

    private void Awake()
    {
        anim = GetComponent<AnimadorSprites8Direcciones>();
        cuerpo = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (muerto || (atacando && !anim.Terminada)) return;
        atacando = false;
        Vector2 v = cuerpo.linearVelocity;
        if (v.sqrMagnitude > 0.01f)
        {
            direccion = AnimacionJasonDinamica.Direccion8(v);
            anim.Reproducir(anim.Tiene("Caminar") ? "Caminar" : "Correr", direccion);
        }
        else anim.Reproducir("Quieto", direccion);
    }

    public float Atacar(Vector2 objetivo)
    {
        if (muerto || !isActiveAndEnabled) return 0f;
        direccion = AnimacionJasonDinamica.Direccion8(objetivo - (Vector2)transform.position);
        atacando = true;
        anim.Reproducir("Ataque", direccion, true);
        foreach (var a in anim.Animaciones)
            if (a.nombre == "Ataque")
                return Mathf.Min(5, Mathf.Max(0, anim.TotalFotogramas - 1)) / Mathf.Max(1f, a.fps);
        return 0f;
    }

    public float Morir()
    {
        muerto = true;
        anim.Reproducir("Muerte", direccion, true);
        foreach (var a in anim.Animaciones)
            if (a.nombre == "Muerte")
            {
                var frames = AnimadorSprites8Direcciones.Frames(a, direccion);
                return frames != null ? frames.Length / Mathf.Max(1f, a.fps) : 0f;
            }
        return 0f;
    }
}
