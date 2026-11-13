using UnityEngine;

[DisallowMultipleComponent]
public class BrilloObjeto : MonoBehaviour
{
    [Header("Configuración del Brillo en Bordes")]
    [Tooltip("Color del contorno luminoso alrededor del objeto.")]
    public Color colorBrillo = new Color(1f, 0.92f, 0.45f, 0.85f);

    [Tooltip("Grosor del borde iluminado en píxeles del sprite.")]
    [Range(0.5f, 6f)]
    public float grosorBordePixeles = 2.2f;

    [Tooltip("Velocidad del pulso de brillo.")]
    public float velocidadPulso = 3.2f;

    [Tooltip("Opacidad mínima del contorno durante el pulso.")]
    [Range(0.1f, 1f)]
    public float opacidadMinima = 0.38f;

    [Tooltip("Opacidad máxima del contorno durante el pulso.")]
    [Range(0.1f, 1f)]
    public float opacidadMaxima = 0.92f;

    [Tooltip("Fuerza el color del SpriteRenderer principal a blanco para que no se vea grisáceo.")]
    public bool restaurarBrilloSpriteBase = true;

    private SpriteRenderer renderBase;
    private SpriteRenderer[] bordes;
    private static Material materialUnlitCompartido;
    private float desfaseTiempo;

    private static readonly Vector2[] DireccionesBorde = new Vector2[]
    {
        new Vector2(1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2(0f, 1f),
        new Vector2(0f, -1f),
        new Vector2(0.7071f, 0.7071f),
        new Vector2(-0.7071f, 0.7071f),
        new Vector2(0.7071f, -0.7071f),
        new Vector2(-0.7071f, -0.7071f)
    };

    private void Awake()
    {
        desfaseTiempo = Random.Range(0f, Mathf.PI * 2f);
        ConfigurarColorPorTipo();
        InicializarBordes();
    }

    private void ConfigurarColorPorTipo()
    {
        if (GetComponent<NotaInteractable>() != null)
        {
            colorBrillo = new Color(1f, 0.92f, 0.45f, 0.9f); // Dorado cálido para notas
            grosorBordePixeles = 3.2f;
        }
        else if (GetComponent<AmmoDrop>() != null)
        {
            colorBrillo = new Color(1f, 0.85f, 0.3f, 0.9f); // Ámbar brillante para munición
            grosorBordePixeles = 2.5f;
        }
        else if (TryGetComponent<ItemPickup>(out var pickup))
        {
            switch (pickup.tipoDeObjeto)
            {
                case ItemPickup.PickupType.Llave:
                    colorBrillo = new Color(0.35f, 0.95f, 1f, 0.95f); // Cian brillante para Tarjeta/Llave
                    grosorBordePixeles = 3.5f;
                    break;
                case ItemPickup.PickupType.Estimulante:
                    colorBrillo = new Color(0.4f, 1f, 0.55f, 0.9f); // Verde energético para Estimulante
                    grosorBordePixeles = 2.6f;
                    break;
                default:
                    colorBrillo = new Color(1f, 0.95f, 0.55f, 0.85f);
                    grosorBordePixeles = 2.2f;
                    break;
            }
        }
        else if (GetComponent<Laboratorio4.Recepcion.TarjetaSalidaRecepcion>() != null)
        {
            colorBrillo = new Color(0.35f, 0.95f, 1f, 0.95f);
            grosorBordePixeles = 3.5f;
        }
    }

    private static Material ObtenerMaterialBrillo()
    {
        if (materialUnlitCompartido != null) return materialUnlitCompartido;

        Shader shader = Shader.Find("GUI/Text Shader");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            materialUnlitCompartido = new Material(shader)
            {
                name = "BrilloObjeto_RimMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        return materialUnlitCompartido;
    }

    private void InicializarBordes()
    {
        renderBase = GetComponent<SpriteRenderer>();
        if (renderBase == null || renderBase.sprite == null) return;

        if (restaurarBrilloSpriteBase && renderBase.color.r < 0.95f && renderBase.color.g < 0.95f && renderBase.color.b < 0.95f)
        {
            renderBase.color = new Color(1f, 1f, 1f, renderBase.color.a);
        }

        // Asegurar material unlit en el objeto principal también si se desea que sus propios bordes resalten en la oscuridad,
        // o un contenedor hijo de bordes con siluetas sólidas desplazadas detrás del sprite.
        Transform contenedorExistente = transform.Find("BrilloContorno");
        if (contenedorExistente != null)
        {
            Destroy(contenedorExistente.gameObject);
        }

        GameObject contenedor = new GameObject("BrilloContorno");
        contenedor.transform.SetParent(transform, false);
        contenedor.transform.localPosition = Vector3.zero;
        contenedor.transform.localRotation = Quaternion.identity;
        contenedor.transform.localScale = Vector3.one;

        Material matBrillo = ObtenerMaterialBrillo();
        float ppu = renderBase.sprite.pixelsPerUnit > 0f ? renderBase.sprite.pixelsPerUnit : 100f;
        // Compensar si la escala local del objeto es muy pequeña (ej. TarjetaAcceso 0.05 o Nota 0.07)
        float escalaPromedio = Mathf.Max(0.02f, (Mathf.Abs(transform.localScale.x) + Mathf.Abs(transform.localScale.y)) * 0.5f);
        float desplazamientoLocal = (grosorBordePixeles / ppu) * Mathf.Clamp(0.25f / escalaPromedio, 0.8f, 4.5f);

        bordes = new SpriteRenderer[DireccionesBorde.Length];
        for (int i = 0; i < DireccionesBorde.Length; i++)
        {
            GameObject goBorde = new GameObject("Borde_" + i);
            goBorde.transform.SetParent(contenedor.transform, false);
            goBorde.transform.localPosition = new Vector3(
                DireccionesBorde[i].x * desplazamientoLocal,
                DireccionesBorde[i].y * desplazamientoLocal,
                0f
            );
            goBorde.transform.localRotation = Quaternion.identity;
            goBorde.transform.localScale = Vector3.one * 1.03f;

            SpriteRenderer sr = goBorde.AddComponent<SpriteRenderer>();
            sr.sprite = renderBase.sprite;
            sr.flipX = renderBase.flipX;
            sr.flipY = renderBase.flipY;
            sr.sortingLayerID = renderBase.sortingLayerID;
            sr.sortingOrder = renderBase.sortingOrder - 1;
            if (matBrillo != null) sr.sharedMaterial = matBrillo;
            sr.color = colorBrillo;
            bordes[i] = sr;
        }
    }

    private void LateUpdate()
    {
        if (renderBase == null || bordes == null) return;

        float pulso = (Mathf.Sin(Time.unscaledTime * velocidadPulso + desfaseTiempo) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(opacidadMinima, opacidadMaxima, pulso) * renderBase.color.a;
        Color c = new Color(colorBrillo.r, colorBrillo.g, colorBrillo.b, alpha);

        int ordenBorde = renderBase.sortingOrder - 1;
        int capaSorting = renderBase.sortingLayerID;
        Sprite spriteActual = renderBase.sprite;
        bool visible = renderBase.enabled && spriteActual != null;

        for (int i = 0; i < bordes.Length; i++)
        {
            SpriteRenderer sr = bordes[i];
            if (sr == null) continue;
            sr.enabled = visible;
            if (!visible) continue;
            if (sr.sprite != spriteActual) sr.sprite = spriteActual;
            sr.sortingLayerID = capaSorting;
            sr.sortingOrder = ordenBorde;
            sr.flipX = renderBase.flipX;
            sr.flipY = renderBase.flipY;
            sr.color = c;
        }
    }
}
