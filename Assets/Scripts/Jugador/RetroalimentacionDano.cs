using UnityEngine;

[DefaultExecutionOrder(300)]
public class RetroalimentacionDano : MonoBehaviour
{
    public float duracionFlash = .18f;
    public int GolpesMostrados { get; private set; }
    public bool FlashActivo => Time.unscaledTime < fin;
    private SpriteRenderer cuerpo;
    private Color original;
    private Color tono;
    private float fin = float.NegativeInfinity;

    private void Awake()
    {
        cuerpo = GetComponent<SpriteRenderer>();
        if (cuerpo != null) original = cuerpo.color;
    }

    public void Mostrar(float cantidad, bool esJugador)
    {
        if (cantidad <= 0 || cuerpo == null) return;
        GolpesMostrados++;
        tono = esJugador ? new Color(1f, .18f, .18f) : new Color(1f, .75f, .15f);
        fin = Time.unscaledTime + duracionFlash;
        cuerpo.color = tono;
        var objeto = new GameObject("Daño " + Mathf.CeilToInt(cantidad));
        objeto.transform.position = cuerpo.bounds.center + Vector3.up * (cuerpo.bounds.extents.y + .1f);
        var texto = objeto.AddComponent<TextMesh>();
        texto.text = "−" + Mathf.CeilToInt(cantidad);
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 44;
        texto.characterSize = .09f;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.color = tono;
        var render = objeto.GetComponent<MeshRenderer>();
        render.sharedMaterial = texto.font.material;
        render.sortingLayerID = cuerpo.sortingLayerID;
        render.sortingOrder = cuerpo.sortingOrder + 20;
        objeto.AddComponent<NumeroDanoFlotante>();
    }

    private void LateUpdate()
    {
        if (cuerpo == null) return;
        if (Time.unscaledTime < fin)
            cuerpo.color = Color.Lerp(original, tono, (fin - Time.unscaledTime) / duracionFlash);
        else cuerpo.color = original;
    }
    private void OnDisable() { if (cuerpo != null) cuerpo.color = original; }
}

public class NumeroDanoFlotante : MonoBehaviour
{
    private float edad;
    private TextMesh texto;
    private Color color;
    private void Start() { texto = GetComponent<TextMesh>(); color = texto.color; }
    private void Update()
    {
        edad += Time.unscaledDeltaTime;
        transform.position += Vector3.up * (.7f * Time.unscaledDeltaTime);
        texto.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(1f - edad / .65f));
        if (edad >= .65f) Destroy(gameObject);
    }
}
