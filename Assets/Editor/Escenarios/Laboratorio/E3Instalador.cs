using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Laboratorio4.Escenario3.Herramientas
{
    /// <summary>Tras importar el paquete, si la escena aún no existe, ofrece construirla y abrirla.</summary>
    [InitializeOnLoad]
    public static class E3Instalador
    {
        static E3Instalador() { EditorApplication.delayCall += Revisar; }

        static void Revisar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(E3Constructor.RutaEscena)) return;
            if (!File.Exists(E3Constructor.RutaDatos)) return;
            if (SessionState.GetBool("Lab4_E3_Preguntado", false)) return;
            SessionState.SetBool("Lab4_E3_Preguntado", true);
            if (!EditorUtility.DisplayDialog("Laboratorio 4 - Escenario 3",
                    "Se importó el Escenario 3. ¿Construir la escena y abrirla ahora?\n\n(También: menú Laboratorio 4 > Escenario 3 > Construir escena)",
                    "Construir y abrir", "Más tarde")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string r = E3Constructor.Construir();
            if (File.Exists(E3Constructor.RutaEscena)) EditorSceneManager.OpenScene(E3Constructor.RutaEscena, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Laboratorio 4 - Escenario 3", r, "OK");
            Debug.Log("[Escenario 3] " + r);
        }
    }
}
