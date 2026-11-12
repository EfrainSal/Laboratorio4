using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4
{
    /// <summary>R = reiniciar la escena. C = mostrar/ocultar colisiones (líneas verdes, rojas y amarillas).</summary>
    public class Depuracion : MonoBehaviour
    {
        readonly List<GameObject> lineas = new List<GameObject>();
        Material mat;

        void Update()
        {
            if (Entrada.Tecla('R')) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if (Entrada.Tecla('C')) { if (lineas.Count > 0) Limpiar(); else Dibujar(); }
        }

        void Limpiar() { foreach (var g in lineas) if (g != null) Destroy(g); lineas.Clear(); }

        void Dibujar()
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                Color col = c.isTrigger ? Color.yellow : (c.attachedRigidbody != null && c.attachedRigidbody.bodyType != RigidbodyType2D.Static ? Color.green : Color.red);
                var b = c as BoxCollider2D;
                var k = c as CapsuleCollider2D;
                Vector2 tam = b != null ? b.size : (k != null ? k.size : Vector2.zero);
                if (tam == Vector2.zero) continue;
                Vector2 off = c.offset;
                Transform t = c.transform;
                Vector3[] p = {
                    t.TransformPoint(off + new Vector2(-tam.x, -tam.y) * 0.5f), t.TransformPoint(off + new Vector2(tam.x, -tam.y) * 0.5f),
                    t.TransformPoint(off + new Vector2(tam.x, tam.y) * 0.5f), t.TransformPoint(off + new Vector2(-tam.x, tam.y) * 0.5f) };
                var go = new GameObject("_Colision");
                go.transform.SetParent(c.transform, true);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = mat; lr.startColor = lr.endColor = col;
                lr.widthMultiplier = 0.03f; lr.loop = true; lr.positionCount = 4; lr.SetPositions(p);
                lr.useWorldSpace = false;
                for (int i = 0; i < 4; i++) lr.SetPosition(i, go.transform.InverseTransformPoint(p[i]));
                lr.sortingOrder = 32000;
                lineas.Add(go);
            }
        }
    }
}
