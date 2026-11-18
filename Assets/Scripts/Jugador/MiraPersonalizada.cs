using UnityEngine;
using UnityEngine.InputSystem;

public class MiraPersonalizada : MonoBehaviour
{
    [Header("Configuración de Mira con Mando")]
    [Tooltip("Distancia máxima (en unidades del mundo) a la que se posiciona el puntero respecto al jugador al apuntar con mando.")]
    [Min(0.5f)] public float radioMaximoMando = 3.5f;
    [Tooltip("Velocidad de interpolación del puntero al mover el joystick derecho.")]
    [Min(1f)] public float suavizadoMando = 22f;
    [Tooltip("Escala visual del puntero en el mundo cuando se usa mando.")]
    [Min(0.1f)] public float escalaVisualMando = 1f;

    public bool EstaActiva { get; private set; }
    public Texture2D Textura => textura;
    public Vector2 PosicionMundoMando { get; private set; }

    private Texture2D textura;
    private Sprite spriteMira;
    private GameObject objetoMiraMando;
    private SpriteRenderer renderizadorMiraMando;
    private PlayerInput entrada;
    private ArmaIndependiente armaIndependiente;
    private MovimientoPlayer movimientoPlayer;
    private Vector2 direccionActualMando = Vector2.right;
    private float distanciaActualMando;
    private bool ultimoEstadoMando;

    private void Awake()
    {
        entrada = GetComponent<PlayerInput>();
        armaIndependiente = GetComponent<ArmaIndependiente>();
        movimientoPlayer = GetComponent<MovimientoPlayer>();
        distanciaActualMando = radioMaximoMando;
    }

    private void OnEnable()
    {
        if (textura == null) textura = CrearTextura();
        CrearVisualMandoSiFalta();
        ultimoEstadoMando = UsandoMandoActual();
        Aplicar(true);
    }

    private void Update()
    {
        if (entrada != null)
        {
            GestorModoEntrada.AplicarAPlayerInput(entrada);
        }

        bool activa = !DeathScreenController.GameplayBlocked && !VictoryScreenController.IsShowing && Time.timeScale > 0f;
        bool usandoMando = UsandoMandoActual();

        if (activa != EstaActiva || usandoMando != ultimoEstadoMando)
        {
            ultimoEstadoMando = usandoMando;
            Aplicar(activa);
        }

        if (activa && usandoMando)
        {
            ActualizarPosicionMiraMando();
        }
        else if (renderizadorMiraMando != null && renderizadorMiraMando.enabled)
        {
            renderizadorMiraMando.enabled = false;
        }
    }

    private bool UsandoMandoActual()
    {
        if (GestorModoEntrada.ModoActual == ModoEntrada.TecladoYRaton) return false;
        if (GestorModoEntrada.ModoActual == ModoEntrada.Mando) return true;
        if (entrada != null && !string.IsNullOrEmpty(entrada.currentControlScheme))
            return entrada.currentControlScheme == "Gamepad" || GestorModoEntrada.UsandoMando;
        return GestorModoEntrada.UsandoMando;
    }

    private void CrearVisualMandoSiFalta()
    {
        if (objetoMiraMando != null) return;

        if (spriteMira == null && textura != null)
        {
            spriteMira = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f), 48f);
            spriteMira.name = "SpriteMiraProcedural";
        }

        objetoMiraMando = new GameObject("MiraMandoVisual");
        objetoMiraMando.transform.SetParent(transform, false);
        renderizadorMiraMando = objetoMiraMando.AddComponent<SpriteRenderer>();
        renderizadorMiraMando.sprite = spriteMira;
        renderizadorMiraMando.sortingOrder = 30000;
        renderizadorMiraMando.enabled = false;
    }

    private void ActualizarPosicionMiraMando()
    {
        CrearVisualMandoSiFalta();
        if (renderizadorMiraMando == null) return;

        Vector2 centro = armaIndependiente != null && armaIndependiente.pivote != null
            ? (Vector2)armaIndependiente.pivote.position
            : (Vector2)transform.position + new Vector2(0f, 0.85f);

        Vector2 stickDerecho = Vector2.zero;
        if (Gamepad.current != null && GestorModoEntrada.PermitirMando)
        {
            stickDerecho = Gamepad.current.rightStick.ReadValue();
        }

        float magnitud = Mathf.Clamp01(stickDerecho.magnitude);
        if (magnitud > 0.12f)
        {
            direccionActualMando = stickDerecho.normalized;
            float objetivoDistancia = Mathf.Lerp(radioMaximoMando * 0.35f, radioMaximoMando, magnitud);
            distanciaActualMando = Mathf.Lerp(distanciaActualMando, objetivoDistancia, Time.deltaTime * suavizadoMando);
        }
        else
        {
            if (movimientoPlayer != null && movimientoPlayer.DireccionMirada.sqrMagnitude > 0.01f)
            {
                direccionActualMando = movimientoPlayer.DireccionMirada.normalized;
            }
            float objetivoReposo = radioMaximoMando * 0.75f;
            distanciaActualMando = Mathf.Lerp(distanciaActualMando, objetivoReposo, Time.deltaTime * (suavizadoMando * 0.6f));
        }

        distanciaActualMando = Mathf.Clamp(distanciaActualMando, 0.35f, radioMaximoMando);
        PosicionMundoMando = centro + direccionActualMando * distanciaActualMando;

        objetoMiraMando.transform.position = new Vector3(PosicionMundoMando.x, PosicionMundoMando.y, 0f);
        objetoMiraMando.transform.rotation = Quaternion.identity;
        objetoMiraMando.transform.localScale = Vector3.one * escalaVisualMando;
        renderizadorMiraMando.enabled = true;
    }

    private void Aplicar(bool activa)
    {
        EstaActiva = activa;
        bool usandoMando = UsandoMandoActual();

        Cursor.lockState = CursorLockMode.None;

        if (activa && usandoMando)
        {
            Cursor.visible = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
            if (renderizadorMiraMando != null) renderizadorMiraMando.enabled = true;
        }
        else
        {
            Cursor.visible = !usandoMando || !activa;
            Cursor.SetCursor(activa ? textura : null, activa ? new Vector2(16f, 16f) : Vector2.zero, CursorMode.ForceSoftware);
            if (renderizadorMiraMando != null) renderizadorMiraMando.enabled = false;
        }
    }

    private static Texture2D CrearTextura()
    {
        var resultado = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "MiraBlancaProcedural", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixeles = new Color32[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int dx = Mathf.Abs(x - 16), dy = Mathf.Abs(y - 16);
                bool trazo = (dx <= 1 && dy >= 5 && dy <= 12) || (dy <= 1 && dx >= 5 && dx <= 12) || (dx == 0 && dy == 0);
                pixeles[y * 32 + x] = trazo ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        resultado.SetPixels32(pixeles);
        resultado.Apply(false, false);
        return resultado;
    }

    private void OnDisable()
    {
        Aplicar(false);
    }

    private void OnDestroy()
    {
        if (objetoMiraMando != null) Destroy(objetoMiraMando);
        if (spriteMira != null) Destroy(spriteMira);
        if (textura != null) Destroy(textura);
    }
}
