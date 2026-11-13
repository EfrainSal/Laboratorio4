using UnityEngine;

namespace Laboratorio4.Escenario4
{
    /// <summary>Barra de vida del Doctor Yiba, vida de Jason y Milena, escudo y mensajes.</summary>
    public class E4HUD : MonoBehaviour
    {
        public E4Jefe jefe;
        public E4Jugador jason;
        public E4Vida vidaJason;
        public E4Vida vidaMilena;
        public E4PuertaSalida puerta;
        public float tiempoAyuda = 8f;

        private GUIStyle titulo, texto, centro;
        private Texture2D blanco;

        private void Estilos()
        {
            if (blanco != null) return;
            blanco = Texture2D.whiteTexture;
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titulo.normal.textColor = new Color(1f, 0.85f, 0.8f);
            texto = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            texto.normal.textColor = Color.white;
            centro = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centro.normal.textColor = Color.white;
        }

        private void Barra(Rect r, float f, Color c)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.7f); GUI.DrawTexture(r, blanco);
            GUI.color = c; GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(f), r.height - 4), blanco);
            GUI.color = Color.white;
        }

        private void OnGUI()
        {
            Estilos();
            float W = Screen.width, H = Screen.height;
            if (jefe != null && jefe.Activo && !jefe.Muerto)
            {
                GUI.Label(new Rect(0, 8, W, 26), "DOCTOR YIBA", titulo);
                Barra(new Rect(W * 0.2f, 36, W * 0.6f, 18), jefe.Vida.Fraccion, new Color(0.85f, 0.12f, 0.1f));
            }
            if (vidaJason != null)
            {
                GUI.Label(new Rect(16, H - 92, 200, 22), "Jason", texto);
                Barra(new Rect(16, H - 70, 200, 14), vidaJason.Fraccion, new Color(0.3f, 0.8f, 1f));
            }
            if (vidaMilena != null)
            {
                GUI.Label(new Rect(232, H - 92, 200, 22), "Milena", texto);
                Barra(new Rect(232, H - 70, 200, 14), vidaMilena.Fraccion, new Color(0.95f, 0.45f, 0.5f));
            }
            if (jason != null)
            {
                string e = jason.EscudoRestante > 0f ? "Escudo ACTIVO" :
                           jason.RecargaRestante > 0f ? "Escudo en " + jason.RecargaRestante.ToString("0.0") + " s" : "Escudo listo (Q)";
                GUI.Label(new Rect(16, H - 50, 300, 22), e, texto);
            }
            if (Time.timeSinceLevelLoad < tiempoAyuda)
                GUI.Label(new Rect(16, 64, 700, 22), "WASD mover · Espacio/J/clic disparar · Q escudo · C colisiones · R reiniciar", texto);

            string msg = null;
            if (vidaJason != null && vidaJason.Muerto) msg = "Jason ha caído — pulsa R para reintentar";
            else if (puerta != null && puerta.Escapo) msg = puerta.mensajeFinal;
            else if (jefe != null && jefe.Muerto) msg = "¡El Doctor Yiba ha caído! La salida está abierta";
            if (msg != null) GUI.Label(new Rect(0, H * 0.4f, W, 50), msg, centro);
        }
    }
}
