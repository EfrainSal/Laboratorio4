using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class AnimacionEnemigoDinamica : MonoBehaviour
{
    [Header("Configuración de Rutas")]
    [Tooltip("Ejemplo: Art/Personajes/Zorbo")]
    public string rutaBaseSprites = "Art/Personajes/Zorbo";

    [Header("Animación")]
    public float fpsCaminar = 8f;

    // Arreglo maestro para las 8 direcciones. 
    // Índice: 0=Sur, 1=Sureste, 2=Este, 3=Noreste, 4=Norte, 5=Noroeste, 6=Oeste, 7=Suroeste
    private Sprite[][] animacionesPorDireccion = new Sprite[8][];
    
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    
    private int direccionActual = 0;          
    private float tiempo;
    private Vector2 direccionMirada = Vector2.down; // Empieza mirando al Sur

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        CargarSpritesEnemigo();
    }

    private void CargarSpritesEnemigo()
    {
        // Carga todos los sprites de la carpeta base (e.g. "Art/Personajes/Zorbo")
        Sprite[] todosLosSprites = Resources.LoadAll<Sprite>(rutaBaseSprites);

        if (todosLosSprites.Length == 0)
        {
            Debug.LogError($"No se encontraron sprites en Resources/{rutaBaseSprites}. Revisa la ruta y que estén dentro de una carpeta Resources.");
            return;
        }

        // Listas temporales para agrupar los frames de cada dirección
        List<Sprite>[] framesDireccion = new List<Sprite>[8];
        for (int i = 0; i < 8; i++) framesDireccion[i] = new List<Sprite>();

        foreach (Sprite s in todosLosSprites)
        {
            string nombre = s.name.ToLower();
            
            // Mapeo automático basado en los sufijos que tienes (01 al 08)
            // Asumiendo: 01=Sur, 02=Sureste, 03=Este, 04=Noreste, 05=Norte, 06=Noroeste, 07=Oeste, 08=Suroeste
            if (nombre.Contains("01")) framesDireccion[0].Add(s);
            else if (nombre.Contains("02")) framesDireccion[1].Add(s);
            else if (nombre.Contains("03")) framesDireccion[2].Add(s);
            else if (nombre.Contains("04")) framesDireccion[3].Add(s); // Nota: En tu imagen hay dos "04", cuida el renombrado.
            else if (nombre.Contains("05")) framesDireccion[4].Add(s);
            else if (nombre.Contains("06")) framesDireccion[5].Add(s);
            else if (nombre.Contains("07")) framesDireccion[6].Add(s);
            else if (nombre.Contains("08")) framesDireccion[7].Add(s);
        }

        // Convertir listas a arreglos
        for (int i = 0; i < 8; i++)
        {
            animacionesPorDireccion[i] = framesDireccion[i].ToArray();
        }
    }

    private void Update()
    {
        Vector2 velocidad = rb.linearVelocity;

        // Si el enemigo se está moviendo (incluyendo la patrulla)
        if (velocidad.sqrMagnitude > 0.01f)
        {
            direccionMirada = velocidad.normalized;
            direccionActual = CalcularDireccion8(direccionMirada);
            
            tiempo += Time.deltaTime;
            
            Sprite[] animActual = animacionesPorDireccion[direccionActual];
            
            if (animActual != null && animActual.Length > 0)
            {
                // Si es un spritesheet con varios frames, se animará. Si es solo 1 sprite por lado, se mantendrá estático pero orientado correctamente.
                sr.sprite = animActual[Mathf.FloorToInt(tiempo * fpsCaminar) % animActual.Length];
            }
        }
        else
        {
            // Si está quieto (idle), mostrar el primer frame de su última dirección
            tiempo = 0f;
            Sprite[] animActual = animacionesPorDireccion[direccionActual];
            if (animActual != null && animActual.Length > 0)
            {
                sr.sprite = animActual[0];
            }
        }
    }

    // Calcula un número del 0 al 7 basado en el vector de dirección (Misma lógica que usaste para el jugador)
    private static int CalcularDireccion8(Vector2 d)
    {
        float a = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg; 
        int i = Mathf.RoundToInt(a / 45f);
        return ((i % 8) + 8) % 8;
    }
}