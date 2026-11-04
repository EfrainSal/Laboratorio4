using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Captura la pantalla y la desenfoca (bajando y subiendo resolución con filtrado bilineal).
public static class BackdropBlur
{
    /// <param name="steps">Más pasos = más desenfoque (2 suave, 4 medio, 6 muy fuerte)</param>
    public static IEnumerator Capture(Action<RenderTexture> onReady, int steps = 4)
    {
        yield return new WaitForEndOfFrame();

        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        if (shot == null)
        {
            onReady?.Invoke(null);
            yield break;
        }

        steps = Mathf.Clamp(steps, 2, 6);
        var chain = new List<RenderTexture>(steps);
        Texture source = shot;
        int w = shot.width, h = shot.height;

        // Bajada: cada paso reduce a la mitad
        for (int i = 0; i < steps; i++)
        {
            w = Mathf.Max(4, w / 2);
            h = Mathf.Max(4, h / 2);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            Graphics.Blit(source, rt);
            chain.Add(rt);
            source = rt;
        }

        // Subida: reescala paso a paso suavizando
        for (int i = chain.Count - 2; i >= 0; i--)
            Graphics.Blit(chain[i + 1], chain[i]);

        var result = new RenderTexture(chain[0].width, chain[0].height, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        result.Create();
        Graphics.Blit(chain[0], result);

        foreach (var rt in chain) RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.Destroy(shot);

        onReady?.Invoke(result);
    }

    public static void Release(RenderTexture rt)
    {
        if (rt == null) return;
        rt.Release();
        UnityEngine.Object.Destroy(rt);
    }
}