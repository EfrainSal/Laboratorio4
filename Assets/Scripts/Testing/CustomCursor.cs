using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class CustomCursor : MonoBehaviour
{
    public static CustomCursor Instance { get; private set; }

    [Header("Sprites del Cursor")]
    public Sprite cursorMachete;
    public Sprite cursorSubfusil;

    [Header("Tamaño del Cursor (Unidades del Mundo)")]
    [Tooltip("Tamaño visual máximo en unidades del mundo para el cursor del machete")]
    public float tamanoMachete = 0.45f;
    [Tooltip("Tamaño visual máximo en unidades del mundo para el cursor de la mira (subfusil)")]
    public float tamanoSubfusil = 0.40f;

    [Header("Referencias")]
    public Transform player;
    public PlayerCombat playerCombat;

    private SpriteRenderer spriteRenderer;
    private Camera cam;
    private bool isMachete = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Asegurar que el cursor siempre se renderice por encima del escenario y personajes
        spriteRenderer.sortingOrder = 30000;
    }

    void Start()
    {
        cam = Camera.main;
        BuscarJugador();
        ActualizarCursor(isMachete);
    }

    void OnDisable()
    {
        Cursor.visible = true;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Cursor.visible = true;
            Instance = null;
        }
    }

    void LateUpdate()
    {
        // Si hay menús abiertos (Pausa, Mejoras, Muerte, Victoria, Notas), mostramos el cursor del sistema y ocultamos el dinámico
        bool menuAbierto = DeathScreenController.GameplayBlocked
                           || VictoryScreenController.IsShowing
                           || (UIManagerNotas.Instance != null && UIManagerNotas.Instance.NotaActiva)
                           || Time.timeScale == 0f;

        if (menuAbierto)
        {
            if (!Cursor.visible) Cursor.visible = true;
            if (spriteRenderer != null && spriteRenderer.enabled) spriteRenderer.enabled = false;
            return;
        }

        // Durante la partida ocultamos el puntero del sistema y mostramos el cursor dinámico
        if (Cursor.visible) Cursor.visible = false;
        if (spriteRenderer != null && !spriteRenderer.enabled) spriteRenderer.enabled = true;

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        if (player == null || playerCombat == null)
        {
            BuscarJugador();
        }

        if (Mouse.current == null) return;

        // 1. Obtener la posición del ratón en la pantalla y pasarla al mundo 2D
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(cam.transform.position.z)));
        Vector2 targetPos = new Vector2(worldPoint.x, worldPoint.y);

        // 2. Si tenemos el machete equipado, limitamos el movimiento dentro del área de ataque del machete
        if (isMachete && player != null && playerCombat != null)
        {
            // Calculamos el centro y radio efectivos del área de ataque del machete alrededor del jugador
            Vector2 playerPos = player.position;
            float maxRadius = playerCombat.attackRange;

            if (playerCombat.attackPoint != null)
            {
                // El attackPoint rota con el PivoteArma alrededor del jugador; sumamos su distancia al radio de ataque
                // para que el cursor pueda moverse en 360° dentro de todo el alcance real del machete
                float offsetDist = Vector2.Distance(playerPos, (Vector2)playerCombat.attackPoint.position);
                maxRadius += offsetDist;
            }

            Vector2 direccion = targetPos - playerPos;
            if (direccion.magnitude > maxRadius)
            {
                targetPos = playerPos + (direccion.normalized * maxRadius);
            }
        }

        // 3. Movemos nuestro sprite a la posición final calculada
        transform.position = new Vector3(targetPos.x, targetPos.y, 0f);
    }

    private void BuscarJugador()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerCombat = p.GetComponent<PlayerCombat>();
        }
    }

    public void AsignarJugador(Transform nuevoPlayer, PlayerCombat nuevoCombat)
    {
        player = nuevoPlayer;
        playerCombat = nuevoCombat;
    }

    // El WeaponManager llamará a esta función al cambiar de arma
    public void ActualizarCursor(bool usarMachete)
    {
        isMachete = usarMachete;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            Sprite nuevoSprite = usarMachete ? cursorMachete : cursorSubfusil;
            spriteRenderer.sprite = nuevoSprite;
            AjustarEscala(nuevoSprite, usarMachete ? tamanoMachete : tamanoSubfusil);
        }
    }

    private void AjustarEscala(Sprite sprite, float tamanoDeseado)
    {
        if (sprite == null) return;

        // Obtenemos el tamaño original del sprite en unidades de Unity (sin escalar)
        Vector2 spriteSize = sprite.bounds.size;
        float maxDimension = Mathf.Max(spriteSize.x, spriteSize.y);

        if (maxDimension > 0.0001f)
        {
            float factorEscala = tamanoDeseado / maxDimension;
            transform.localScale = new Vector3(factorEscala, factorEscala, 1f);
        }
    }
}
