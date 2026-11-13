using UnityEngine;

public class MiraPersonalizada : MonoBehaviour
{
    public bool EstaActiva { get; private set; }
    public Texture2D Textura => textura;
    private Texture2D textura;

    private void OnEnable()
    {
        if (textura == null) textura = CrearTextura();
        Aplicar(true);
    }

    private void Update()
    {
        bool activa = !DeathScreenController.GameplayBlocked && !VictoryScreenController.IsShowing && Time.timeScale > 0f;
        if (activa != EstaActiva) Aplicar(activa);
    }

    private void Aplicar(bool activa)
    {
        EstaActiva = activa;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(activa ? textura : null, activa ? new Vector2(16f, 16f) : Vector2.zero, CursorMode.ForceSoftware);
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

    private void OnDisable() { Aplicar(false); }
    private void OnDestroy() { if (textura != null) Destroy(textura); }
}
