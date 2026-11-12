using UnityEngine;
using UnityEngine.InputSystem; // Aseguramos el uso del nuevo Input System
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(MovimientoPlayer))]
public class AnimacionJasonDinamica : MonoBehaviour
{
    [Header("Configuración de Rutas (Relativas a la carpeta Resources)")]
    [Tooltip("Ejemplo: Art/Personajes/Jason")]
    public string rutaBaseSprites = "Art/Personajes/Jason";

    [Header("Velocidad (fotogramas por segundo)")]
    public float fpsCaminar = 10f;
    public float fpsDisparo = 14f;

    // Arreglos de sprites cargados dinámicamente
    private Sprite[] quieto;
    private Sprite[] caminarDerecha;
    private Sprite[] caminarIzquierda;
    private Sprite[] caminarArriba;
    private Sprite[] caminarAbajo;
    private Sprite[] disparoFrente;
    private Sprite[] disparoDerecha;
    private Sprite[] disparoIzquierda;

    private SpriteRenderer sr;
    private MovimientoPlayer movimiento;
    
    private int direccion8 = 0;          
    private bool ultimoLadoDerecho = true;
    private Sprite[] animDisparo;
    private float tiempo;
    private float tiempoDisparo;
    private bool intentandoDisparar = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        movimiento = GetComponent<MovimientoPlayer>();
        CargarSpritesDesdeSubcarpetas();
    }

    // --- NUEVO MÉTODO PARA EL INPUT SYSTEM ---
    // Este método se llamará automáticamente si tienes una acción llamada "Fire" o "Attack"
    // Asegúrate de nombrar este método igual que tu acción en el Input Action Asset (ej: OnFire)
    public void OnFire(InputValue value)
    {
        // Si el botón fue presionado (isPressed)
        if (value.isPressed)
        {
            intentandoDisparar = true;
        }
    }

    private void CargarSpritesDesdeSubcarpetas()
    {
        // Lista de subcarpetas basadas en tu jerarquía
        string[] subcarpetas = {
            "Quieto",
            "CaminarDerecha",
            "CaminarIzquierda",
            "CaminarArriba",
            // "CaminarAbajo", // Si no existe, no la incluimos para evitar errores
            "DisparoFrente",
            "DisparoDerecha",
            "DisparoIzquierda"
        };

        List<Sprite> todosLosSprites = new List<Sprite>();

        // Cargar desde cada subcarpeta individualmente
        foreach (string subcarpeta in subcarpetas)
        {
            string rutaCompleta = $"{rutaBaseSprites}/{subcarpeta}";
            Sprite[] spritesEnCarpeta = Resources.LoadAll<Sprite>(rutaCompleta);
            
            if (spritesEnCarpeta.Length > 0)
            {
                todosLosSprites.AddRange(spritesEnCarpeta);
            }
            else
            {
                Debug.LogWarning($"No se encontraron sprites en la subcarpeta: Resources/{rutaCompleta}");
            }
        }

        if (todosLosSprites.Count == 0)
        {
            Debug.LogError($"No se encontraron sprites en Resources/{rutaBaseSprites} ni en sus subcarpetas.");
            return;
        }

        // Listas temporales para agrupar
        List<Sprite> qList = new List<Sprite>(new Sprite[8]);
        List<Sprite> cDer = new List<Sprite>();
        List<Sprite> cIzq = new List<Sprite>();
        List<Sprite> cArr = new List<Sprite>();
        List<Sprite> dFre = new List<Sprite>();
        List<Sprite> dDer = new List<Sprite>();
        List<Sprite> dIzq = new List<Sprite>();

        foreach (Sprite s in todosLosSprites)
        {
            string nombre = s.name;
            
            if (nombre.Contains("Quieto_0_Sur")) qList[0] = s;
            else if (nombre.Contains("Quieto_1_Sureste")) qList[1] = s;
            else if (nombre.Contains("Quieto_2_Este")) qList[2] = s;
            else if (nombre.Contains("Quieto_3_Noreste")) qList[3] = s;
            else if (nombre.Contains("Quieto_4_Norte")) qList[4] = s;
            else if (nombre.Contains("Quieto_5_Noroeste")) qList[5] = s;
            else if (nombre.Contains("Quieto_6_Oeste")) qList[6] = s;
            else if (nombre.Contains("Quieto_7_Suroeste")) qList[7] = s;
            
            else if (nombre.Contains("CaminarDerecha")) cDer.Add(s);
            else if (nombre.Contains("CaminarIzquierda")) cIzq.Add(s);
            else if (nombre.Contains("CaminarArriba")) cArr.Add(s);
            
            else if (nombre.Contains("DisparoFrente")) dFre.Add(s);
            else if (nombre.Contains("DisparoDerecha")) dDer.Add(s);
            else if (nombre.Contains("DisparoIzquierda")) dIzq.Add(s);
        }

        // Convertir a arreglos
        quieto = qList.ToArray();
        caminarDerecha = cDer.ToArray();
        caminarIzquierda = cIzq.ToArray();
        caminarArriba = cArr.ToArray();
        caminarAbajo = new Sprite[0]; 

        disparoFrente = dFre.ToArray();
        disparoDerecha = dDer.ToArray();
        disparoIzquierda = dIzq.ToArray();
    }

    private void Update()
    {
        Vector2 dirMovimiento = movimiento.DireccionEntrada;
        Vector2 dirMirada = movimiento.DireccionMirada; 

        // Usamos la nueva variable booleana que se activa con el Input System
        if (animDisparo == null && intentandoDisparar)
        {
            animDisparo = ElegirDisparo(dirMirada);
            tiempoDisparo = 0f;
            intentandoDisparar = false; // Reiniciamos la variable
            if (animDisparo == null || animDisparo.Length == 0) animDisparo = null;
        }

        if (animDisparo != null)
        {
            tiempoDisparo += Time.deltaTime;
            int f = Mathf.FloorToInt(tiempoDisparo * fpsDisparo);
            if (f >= animDisparo.Length)
            {
                animDisparo = null;
            }
            else
            {
                sr.sprite = animDisparo[f];
                return;
            }
        }

        if (dirMovimiento.sqrMagnitude > 0.01f)
        {
            direccion8 = Direccion8(dirMirada);
            if (Mathf.Abs(dirMirada.x) > 0.01f) ultimoLadoDerecho = dirMirada.x > 0f;

            Sprite[] anim;
            if (Mathf.Abs(dirMirada.x) >= Mathf.Abs(dirMirada.y))
                anim = dirMirada.x > 0f ? caminarDerecha : caminarIzquierda;
            else if (dirMirada.y > 0f)
                anim = caminarArriba;
            else
                anim = caminarAbajo; 

            tiempo += Time.deltaTime;
            
            if (anim != null && anim.Length > 0)
                sr.sprite = anim[Mathf.FloorToInt(tiempo * fpsCaminar) % anim.Length];
            else
                PonerQuieto();
        }
        else
        {
            tiempo = 0f;
            PonerQuieto();
        }
    }

    private void PonerQuieto()
    {
        if (quieto != null && quieto.Length == 8 && quieto[direccion8] != null)
            sr.sprite = quieto[direccion8];
    }

    private Sprite[] ElegirDisparo(Vector2 dirMirada)
    {
        int dir = Direccion8(dirMirada);
        switch (dir)
        {
            case 1: case 2: case 3: return disparoDerecha;
            case 5: case 6: case 7: return disparoIzquierda;
            case 0: return disparoFrente;
            default: return ultimoLadoDerecho ? disparoDerecha : disparoIzquierda; 
        }
    }

    private static int Direccion8(Vector2 d)
    {
        float a = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg; 
        int i = Mathf.RoundToInt(a / 45f);
        return ((i % 8) + 8) % 8;
    }
}