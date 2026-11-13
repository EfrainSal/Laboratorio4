using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario4
{
    /// <summary>C = ver/ocultar colisiones · R = reiniciar la escena.</summary>
    public class E4Depuracion : MonoBehaviour
    {
        private bool visible;
        private readonly List<KeyValuePair<Collider2D, LineRenderer>> lineas = new List<KeyValuePair<Collider2D, LineRenderer>>();
        private Material mat;

        private void Update()
        {
            if (E4Entrada.Reiniciar()) Reiniciar();
            if (E4Entrada.VerColisiones()) { visible = !visible; if (visible) Crear(); else Borrar(); }
        }

        private void LateUpdate()
        {
            if (!visible) return;
            foreach (KeyValuePair<Collider2D, LineRenderer> kv in lineas)
            {
                if (kv.Key == null || kv.Value == null) continue;
                kv.Value.enabled = kv.Key.enabled && kv.Key.gameObject.activeInHierarchy;
                Dibujar(kv.Key, kv.Value);
            }
        }

        private void Reiniciar()
        {
            Scene s = SceneManager.GetActiveScene();
            if (s.buildIndex >= 0) { SceneManager.LoadScene(s.buildIndex); return; }
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(s.path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        }

        private void Crear()
        {
            Borrar();
            if (mat == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                mat = new Material(sh);
            }
            List<Collider2D> todos = new List<Collider2D>();
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
                todos.AddRange(raiz.GetComponentsInChildren<Collider2D>());
            foreach (Collider2D c in todos)
            {
                GameObject go = new GameObject("E4_Debug_Colision");
                go.transform.SetParent(transform, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = mat;
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.positionCount = 4;
                lr.widthMultiplier = 0.035f;
                lr.sortingOrder = 32000;
                Color col = c.isTrigger ? new Color(1f, 0.9f, 0.1f) :
                            c.GetComponentInParent<E4Vida>() != null ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f);
                lr.startColor = col; lr.endColor = col;
                lineas.Add(new KeyValuePair<Collider2D, LineRenderer>(c, lr));
                Dibujar(c, lr);
            }
        }

        private static void Dibujar(Collider2D c, LineRenderer lr)
        {
            Bounds b = c.bounds;
            lr.SetPosition(0, new Vector3(b.min.x, b.min.y, 0f));
            lr.SetPosition(1, new Vector3(b.max.x, b.min.y, 0f));
            lr.SetPosition(2, new Vector3(b.max.x, b.max.y, 0f));
            lr.SetPosition(3, new Vector3(b.min.x, b.max.y, 0f));
        }

        private void Borrar()
        {
            foreach (KeyValuePair<Collider2D, LineRenderer> kv in lineas) if (kv.Value != null) Destroy(kv.Value.gameObject);
            lineas.Clear();
        }
    }
}
