using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Laboratorio4.Escenario3
{
    /// <summary>
    /// Textos en pantalla (vida, escudo, avisos) y teclas de prueba:
    /// C = ver/ocultar colisiones (verde Jason, rojo muros y objetos, amarillo puertas, morado Sorbos), R = reiniciar.
    /// </summary>
    public class E3Hud : MonoBehaviour
    {
        private static string mensaje = "";
        private static float mensajeHasta;

        public static void Mensaje(string texto, float segundos)
        {
            mensaje = texto;
            mensajeHasta = Time.time + segundos;
        }

        private E3Jugador jugador;
        private E3Vida vida;
        private bool verColisiones;
        private Material mat;
        private readonly List<Collider2D> lista = new List<Collider2D>();
        private GUIStyle estilo, estiloCentro;

        private void Start()
        {
            jugador = FindAnyObjectByType<E3Jugador>();
            if (jugador != null) vida = jugador.GetComponent<E3Vida>();
            mensaje = "";
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += FinCamara;
            Camera.onPostRender += PostRender;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= FinCamara;
            Camera.onPostRender -= PostRender;
        }

        private void Update()
        {
            if (E3Entrada.Reiniciar()) { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : SceneManager.GetActiveScene().path); return; }
            if (E3Entrada.VerColisiones())
            {
                verColisiones = !verColisiones;
                lista.Clear();
                if (verColisiones)
                    foreach (Collider2D c in Resources.FindObjectsOfTypeAll<Collider2D>())
                        if (c != null && c.hideFlags == HideFlags.None && c.gameObject.scene.isLoaded) lista.Add(c);
            }
        }

        private void OnGUI()
        {
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                estilo.normal.textColor = Color.white;
                estiloCentro = new GUIStyle(estilo) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            }
            if (vida != null)
            {
                string esc = jugador.EscudoActivo ? "ACTIVO" : (jugador.RecargaRestante > 0f ? jugador.RecargaRestante.ToString("0.0") + " s" : "listo (Q)");
                Sombra(new Rect(12, 10, 500, 24), "Vida: " + vida.actual + "/" + vida.maxima + "    Escudo: " + esc, estilo);
            }
            Sombra(new Rect(12, Screen.height - 30, 900, 24), "WASD mover · Espacio/J/clic disparar · Q escudo · C colisiones · R reiniciar", estilo);
            if (!string.IsNullOrEmpty(mensaje) && Time.time < mensajeHasta)
                Sombra(new Rect(0, Screen.height * 0.18f, Screen.width, 40), mensaje, estiloCentro);
        }

        private static void Sombra(Rect r, string t, GUIStyle s)
        {
            Color c = s.normal.textColor;
            s.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, s);
            s.normal.textColor = c;
            GUI.Label(r, t, s);
        }

        private void FinCamara(ScriptableRenderContext ctx, Camera cam) { Dibujar(cam); }
        private void PostRender(Camera cam) { Dibujar(cam); }

        private void Dibujar(Camera cam)
        {
            if (!verColisiones || cam == null || cam != Camera.main) return;
            if (mat == null)
            {
                Shader sh = Shader.Find("Hidden/Internal-Colored");
                if (sh == null) return;
                mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                mat.SetInt("_ZTest", (int)CompareFunction.Always);
                mat.SetInt("_Cull", (int)CullMode.Off);
            }
            mat.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            foreach (Collider2D c in lista)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                Color col = Color.red;
                if (c.isTrigger) col = new Color(1f, 0.85f, 0.2f);
                else if (c.GetComponentInParent<E3Jugador>() != null) col = Color.green;
                else if (c.GetComponentInParent<E3Sorbo>() != null) col = new Color(0.8f, 0.3f, 1f);
                else if (c.GetComponentInParent<E3Puerta>() != null || c.name.Contains("Bloqueo")) col = Color.yellow;
                GL.Color(col);
                Bounds b = c.bounds;
                Vector3 a = new Vector3(b.min.x, b.min.y, 0f), bb = new Vector3(b.max.x, b.min.y, 0f), cc = new Vector3(b.max.x, b.max.y, 0f), dd = new Vector3(b.min.x, b.max.y, 0f);
                GL.Vertex(a); GL.Vertex(bb); GL.Vertex(bb); GL.Vertex(cc); GL.Vertex(cc); GL.Vertex(dd); GL.Vertex(dd); GL.Vertex(a);
            }
            GL.End();
            GL.PopMatrix();
        }
    }
}
