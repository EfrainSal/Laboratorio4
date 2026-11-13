using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class ArmaIndependiente : MonoBehaviour
{
    public Transform pivote;
    public SpriteRenderer visual;
    public Transform salidaDisparo;
    public Sprite subfusil;
    public Sprite machete;
    public Vector2 cadera = new Vector2(0f, 0.85f);
    public float anchoArma = 0.9f;

    [Header("Subfusil: sprites en 8 direcciones (Sur a Suroeste)")]
    public Sprite[] spritesApuntar = new Sprite[8];
    public Sprite[] spritesDisparo = new Sprite[8];
    [Min(0.01f)] public float duracionDisparoVisual = 0.12f;
    [Min(0f)] public float retrocesoVisual = 0.035f;
    private float ultimoDisparo = float.NegativeInfinity;
    public bool DisparoVisualActivo => armas != null && !armas.isUsingMelee
        && Time.time - ultimoDisparo < duracionDisparoVisual;

    [Header("Machete: postura y agarre")]
    [Tooltip("Refleja el filo sin intercambiar la punta y el mango.")]
    public bool invertirFiloMachete = true;
    [Min(0.01f), Tooltip("Multiplicador exclusivo del machete. 0.5 equivale a la mitad del tamaño anterior.")]
    public float escalaMachete = 0.5f;
    [Tooltip("Rotación respecto al apuntado hacia la derecha. 225° deja la hoja levantada. Se refleja automáticamente hacia la izquierda.")]
    public float rotacionReposoMachete = 225f;
    [Tooltip("Posición del mango respecto a la cadera: X hacia donde apuntas, Y perpendicular al apuntado. Sólo afecta al machete.")]
    public Vector2 posicionManoMachete = new Vector2(0.25f, 0f);
    [Tooltip("Punto del mango dentro del rectángulo del sprite, de 0 a 1 desde la esquina inferior izquierda. El machete gira alrededor de este agarre.")]
    public Vector2 agarreMachete = new Vector2(0.78f, 0.80f);
    private WeaponManager armas;
    private PlayerHealth salud;
    private SpriteRenderer cuerpo;
    private Camera camara;
    private PlayerInput entrada;
    public float AnguloTajo { get; set; }

    public void MostrarDisparo()
    {
        ultimoDisparo = Time.time;
        ActualizarApuntado();
    }

    public void CancelarDisparoVisual() { ultimoDisparo = float.NegativeInfinity; }

    private void Awake()
    {
        armas = GetComponent<WeaponManager>();
        salud = GetComponent<PlayerHealth>();
        cuerpo = GetComponent<SpriteRenderer>();
        entrada = GetComponent<PlayerInput>();
    }

    private void LateUpdate()
    {
        ActualizarApuntado();
    }

    public void ActualizarApuntado()
    {
        if (salud != null && salud.IsDead) return;
        if (DeathScreenController.GameplayBlocked) return;
        if (camara == null) camara = Camera.main;
        if (camara == null || pivote == null) return;
        Mouse mouse = Mouse.current;
        if (entrada != null)
        {
            foreach (var dispositivo in entrada.devices)
            {
                if (dispositivo is Gamepad mando && entrada.currentControlScheme == "Gamepad")
                {
                    Vector2 direccion = mando.rightStick.ReadValue();
                    if (direccion.sqrMagnitude > .1f) Apuntar((Vector2)pivote.position + direccion);
                    return;
                }
                if (dispositivo is Mouse emparejado) mouse = emparejado;
            }
        }
        if (mouse == null) return;
        Vector2 cursor = mouse.position.ReadValue();
        Vector3 destino = camara.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, -camara.transform.position.z));
        Apuntar(destino);
    }

    public void Apuntar(Vector2 destino)
    {
        if (pivote == null || visual == null) return;
        pivote.localPosition = new Vector3(cadera.x, cadera.y, 0f);
        pivote.localScale = Vector3.one;
        Vector2 direccion = destino - (Vector2)pivote.position;
        if (direccion.sqrMagnitude < 0.0001f) return;
        pivote.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg);
        bool izquierda = direccion.x < 0f;
        bool cuerpoACuerpo = armas != null && armas.isUsingMelee;
        int dir = AnimacionJasonDinamica.Direccion8(direccion);
        Sprite reposo = spritesApuntar != null && dir < spritesApuntar.Length ? spritesApuntar[dir] : null;
        Sprite disparando = spritesDisparo != null && dir < spritesDisparo.Length ? spritesDisparo[dir] : null;
        bool direccional = reposo != null;
        bool fogonazo = DisparoVisualActivo && disparando != null;
        visual.sprite = cuerpoACuerpo ? machete : (fogonazo ? disparando : reposo != null ? reposo : subfusil);
        visual.flipX = false;
        visual.flipY = cuerpoACuerpo ? izquierda != invertirFiloMachete : izquierda && !direccional;
        visual.enabled = isActiveAndEnabled && (salud == null || !salud.IsDead);
        // Los fogonazos tienen un lienzo de 64px y el reposo de 48px: conservar escala del arma.
        Sprite referenciaEscala = cuerpoACuerpo ? machete : subfusil;
        float escala = referenciaEscala != null ? anchoArma / Mathf.Max(0.01f, referenciaEscala.bounds.size.x) : 1f;
        if (cuerpoACuerpo) escala *= escalaMachete;
        visual.transform.localScale = Vector3.one * escala;
        if (cuerpoACuerpo && visual.sprite != null)
        {
            // El sprite original es diagonal: compensar el reflejo para conservar la dirección de la punta.
            float giro = rotacionReposoMachete + (invertirFiloMachete ? 90f : 0f) + AnguloTajo;
            var rotacion = Quaternion.Euler(0f, 0f, izquierda ? -giro : giro);
            visual.transform.localRotation = rotacion;
            // Compensar el pivote central del asset: el punto fijo es el mango, no el centro de la hoja.
            Vector2 puntoSprite = (Vector2.Scale(agarreMachete, visual.sprite.rect.size) - visual.sprite.pivot)
                / visual.sprite.pixelsPerUnit;
            if (visual.flipY) puntoSprite.y = -puntoSprite.y;
            Vector3 mano = new Vector3(posicionManoMachete.x, izquierda ? -posicionManoMachete.y : posicionManoMachete.y, 0f);
            visual.transform.localPosition = mano - rotacion * (Vector3)(puntoSprite * escala);
        }
        else
        {
            var rotacion = Quaternion.Euler(0f, 0f, direccional ? 90f - dir * 45f : 0f);
            visual.transform.localRotation = rotacion;
            Vector2 compensacion = Vector2.zero;
            if (fogonazo && reposo != null)
            {
                // El disparo conserva el dibujo de 48px en la esquina superior izquierda
                // del lienzo de 64px: el margen extra está abajo y a la derecha.
                Vector2 margen = new Vector2(0, disparando.rect.height - reposo.rect.height);
                compensacion = (margen + reposo.pivot - disparando.pivot)
                    / disparando.pixelsPerUnit;
            }
            float retroceso = fogonazo ? retrocesoVisual * Mathf.Clamp01(1f - (Time.time - ultimoDisparo) / duracionDisparoVisual) : 0f;
            visual.transform.localPosition = new Vector3(0.25f - retroceso, 0f, 0f) - rotacion * (Vector3)(compensacion * escala);
        }
        if (cuerpo != null)
        {
            visual.sortingLayerID = cuerpo.sortingLayerID;
            visual.sortingOrder = cuerpo.sortingOrder + (direccion.y > Mathf.Abs(direccion.x) ? -1 : 1);
        }
        if (salidaDisparo != null) salidaDisparo.localPosition = new Vector3(0.7f, izquierda ? -0.04f : 0.04f, 0f);
    }

    private void OnDisable()
    {
        CancelarDisparoVisual();
        if (visual != null) visual.enabled = false;
    }
}
