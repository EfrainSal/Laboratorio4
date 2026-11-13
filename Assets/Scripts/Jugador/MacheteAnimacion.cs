using UnityEngine;

// El círculo comparte centro y radio con OverlapCircleAll; el tajo sólo es decorativo.
[DefaultExecutionOrder(50)]
public class MacheteAnimacion : MonoBehaviour
{
    public Sprite[] fotogramas;
    public float duracion = 0.24f;
    public int segmentos = 96;
    public bool mostrarAlcance = false;
    public bool AlcanceVisible => alcance != null && alcance.enabled;
    public bool Reproduciendo => tajo != null && tajo.enabled;
    public LineRenderer Indicador => alcance;
    private PlayerCombat combate;
    private WeaponManager armas;
    private ArmaIndependiente arma;
    private PlayerHealth salud;
    private LineRenderer alcance;
    private SpriteRenderer tajo;
    private float inicio = float.NegativeInfinity;
    private Material material;

    private void Awake()
    {
        combate = GetComponent<PlayerCombat>();
        armas = GetComponent<WeaponManager>();
        arma = GetComponent<ArmaIndependiente>();
        salud = GetComponent<PlayerHealth>();
        var cuerpo = GetComponent<SpriteRenderer>();
        var borde = new GameObject("Alcance exacto del machete");
        borde.transform.SetParent(transform, false);
        alcance = borde.AddComponent<LineRenderer>();
        alcance.useWorldSpace = true;
        alcance.loop = true;
        alcance.widthMultiplier = 0.025f;
        alcance.positionCount = segmentos;
        material = new Material(Shader.Find("Sprites/Default"));
        alcance.sharedMaterial = material;
        alcance.sortingLayerID = cuerpo.sortingLayerID;
        alcance.sortingOrder = cuerpo.sortingOrder + 2;
        var efecto = new GameObject("Animacion tajo machete");
        efecto.transform.SetParent(transform, false);
        tajo = efecto.AddComponent<SpriteRenderer>();
        tajo.sharedMaterial = material; // VFX visible también fuera de las luces locales del mapa.
        tajo.sortingLayerID = cuerpo.sortingLayerID;
        tajo.sortingOrder = cuerpo.sortingOrder + 3;
        alcance.enabled = tajo.enabled = false;
    }

    public void Reproducir()
    {
        inicio = Time.time;
        tajo.transform.position = combate.attackPoint.position;
        tajo.transform.rotation = Quaternion.Euler(0, 0, (arma.pivote.eulerAngles.z - 45f));
        // El arte permanece dentro del círculo físico del golpe.
        tajo.transform.localScale = Vector3.one * (combate.attackRange * 1.9f);
    }

    private void LateUpdate()
    {
        bool visible = armas.isUsingMelee && combate.attackPoint != null
            && !salud.IsDead && !DeathScreenController.GameplayBlocked;
        alcance.enabled = visible && mostrarAlcance;
        float progreso = (Time.time - inicio) / Mathf.Max(0.01f, duracion);
        bool animando = visible && progreso >= 0 && progreso < 1;
        tajo.enabled = animando && fotogramas != null && fotogramas.Length > 0;
        arma.AnguloTajo = animando ? Mathf.Lerp(0f, -120f, progreso) : 0f;
        if (!visible) return;
        Color color = animando ? new Color(1f, .85f, .35f, .95f) : new Color(1f, .9f, .55f, .32f);
        alcance.startColor = alcance.endColor = color;
        for (int i = 0; i < segmentos; i++)
        {
            float angulo = i * Mathf.PI * 2f / segmentos;
            alcance.SetPosition(i, combate.attackPoint.position + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo)) * combate.attackRange);
        }
        if (tajo.enabled) tajo.sprite = fotogramas[Mathf.Min(fotogramas.Length - 1, (int)(progreso * fotogramas.Length))];
    }

    private void OnDisable()
    {
        if (alcance != null) alcance.enabled = false;
        if (tajo != null) tajo.enabled = false;
        if (arma != null) arma.AnguloTajo = 0;
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
