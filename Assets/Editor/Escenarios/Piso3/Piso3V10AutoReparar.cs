using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LaboratorioYiba.Piso3V10;

namespace LaboratorioYiba.Piso3V10.EditorTools
{
    /// <summary>
    /// V14/V15: al pulsar Play (o con "0 - Revisar y arreglar escena ahora") revisa la escena del Piso 3 y
    /// arregla, sin mover nada de lugar:
    ///  - Muros y cornisa: se anclan al fondo REAL de la escena (si el fondo está movido/escalado/importado
    ///    distinto, los muros invisibles ya no quedan desfasados del dibujo).
    ///  - Jugador con otro script de movimiento (PlayerMovement con transform.Translate): se desactiva.
    ///  - Jason / Zorbos sin animaciones o con colisión antigua: se completan.
    ///  - Sprites de personajes sin configurar y orden de dibujo antiguo.
    /// Siempre escribe un INFORME en la Consola ("[Piso3 V15] Informe ...").
    /// </summary>
    [InitializeOnLoad]
    public static class Piso3V10AutoReparar
    {
        private const string Clave = "LaboratorioYiba.Piso3.AutoReparar";
        private const string RutaMenu = "Laboratorio Yiba/Piso 3 V10/Auto-reparar al dar Play";

        static Piso3V10AutoReparar()
        {
            EditorApplication.playModeStateChanged += AlCambiar;
        }

        [MenuItem(RutaMenu)]
        private static void Alternar() { EditorPrefs.SetBool(Clave, !Activo); }

        [MenuItem(RutaMenu, true)]
        private static bool AlternarValidar() { Menu.SetChecked(RutaMenu, Activo); return true; }

        private static bool Activo { get { return EditorPrefs.GetBool(Clave, true); } }

        private static void AlCambiar(PlayModeStateChange estado)
        {
            if (estado != PlayModeStateChange.ExitingEditMode || !Activo) return;
            try { Revisar(); }
            catch (System.Exception e) { Debug.LogWarning("[Piso3 V15] Auto-reparación omitida: " + e); }
        }

        private static bool EsScriptMovimientoAjeno(MonoBehaviour mb)
        {
            return Piso3V10Movimiento.EsMovimientoAjeno(mb);
        }

        [MenuItem("Laboratorio Yiba/Piso 3 V10/0 - Revisar y arreglar escena ahora")]
        public static void Revisar()
        {
            Scene escena = SceneManager.GetActiveScene();

            // V16.1: SOLO escenas del Piso 3 (por nombre). En cualquier otra escena no se toca nada, salvo
            // quitar el fondo/muros del Piso 3 que versiones anteriores le metieron por error.
            if (!Piso3V10Generador.EsEscenaPiso3())
            {
                List<string> q = Piso3V10Generador.QuitarRestosPiso3();
                if (q.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    Debug.LogWarning("[Piso3] La escena '" + escena.name + "' no es del Piso 3 y tenía restos suyos (muros invisibles). " +
                                     "Quitado: " + string.Join(", ", q) + ". Guarda la escena (Ctrl+S).");
                }
                return;
            }

            // V15: escena MODULAR (Grid + tilemaps). Nunca se le añade el fondo dibujado V10: se arregla
            // con sus propias paredes de tiles (antes se creaba el fondo V10 encima y quedaban dos mapas).
            if (Piso3ModularReparar.EsModular())
            {
                if (!Piso3ModularReparar.EsPiso3Modular()) return;
                Piso3V10Generador.ConfigurarSpritesSiHaceFalta();
                Debug.Log("[Piso3 V15] Informe (escena modular '" + escena.name + "'): " + Piso3ModularReparar.Reparar());
                return;
            }

            SpriteRenderer fondo = Piso3V10Generador.BuscarFondo();
            bool esPiso3 = fondo != null
                           || GameObject.Find(Piso3V10Generador.NombreMuros) != null
                           || Object.FindFirstObjectByType<Piso3V10Movimiento>(FindObjectsInactive.Include) != null
                           || Object.FindFirstObjectByType<Piso3V10Zorbo>(FindObjectsInactive.Include) != null;
            if (!esPiso3) return;

            Piso3V10Generador.Datos d = Piso3V10Generador.CargarDatos();
            List<string> hecho = new List<string>();
            StringBuilder inf = new StringBuilder();

            // --- sprites de personajes sin configurar
            if (Piso3V10Generador.ConfigurarSpritesSiHaceFalta()) hecho.Add("sprites configurados");

            // --- fondo, muros y cornisa anclados al fondo real
            if (fondo == null && d != null) { Piso3V10Generador.ConstruirMuros(d); fondo = Piso3V10Generador.BuscarFondo(); hecho.Add("fondo creado"); }
            if (fondo != null)
            {
                Transform tf = fondo.transform;
                inf.Append("Fondo '").Append(fondo.name).Append("' pos ").Append(tf.position.ToString("F2"))
                   .Append(" escala ").Append(tf.lossyScale.ToString("F2")).Append(" sprite ").Append(Piso3V10Generador.Firma(fondo)).Append(". ");
                if (d != null)
                {
                    GameObject muros = GameObject.Find(Piso3V10Generador.NombreMuros);
                    if (!Piso3V10Generador.MurosAlDia(muros, fondo))
                    {
                        Piso3V10Generador.ConstruirMuros(d);
                        hecho.Add("muros sólidos re-anclados al fondo");
                    }
                    GameObject corn = GameObject.Find(Piso3V10Generador.NombreCornisa);
                    bool cornOk = corn != null && corn.transform.parent == tf && GameObject.Find("90_MurosFrente (cornisa delante)") == null;
                    if (!cornOk)
                    {
                        Piso3V10Generador.ConstruirCornisa(d);
                        hecho.Add("cornisa re-anclada al fondo");
                    }
                    if (Piso3V10Generador.AjustarAccesoPiso4(fondo)) hecho.Add("puerta al Piso 4: se puede entrar al área de franjas (choque solo en la base de la puerta)");
                    int o = Piso3V10Generador.ActualizarOrdenes(fondo);
                    if (o > 0) hecho.Add(o + " órdenes de dibujo (fondo/decals/sombras)");
                }
            }

            // --- jugadores
            HashSet<GameObject> jugadores = new HashSet<GameObject>();
            foreach (Piso3V10JasonAnimacion j in Object.FindObjectsByType<Piso3V10JasonAnimacion>(FindObjectsInactive.Include, FindObjectsSortMode.None)) jugadores.Add(j.gameObject);
            foreach (Piso3V10Movimiento m in Object.FindObjectsByType<Piso3V10Movimiento>(FindObjectsInactive.Include, FindObjectsSortMode.None)) jugadores.Add(m.gameObject);
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (EsScriptMovimientoAjeno(mb) && mb.GetComponent<SpriteRenderer>() != null) jugadores.Add(mb.gameObject);
            foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (sr.gameObject.CompareTag("Player")) jugadores.Add(sr.gameObject);

            foreach (GameObject go in jugadores)
            {
                List<string> ajenos = new List<string>();
                foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                {
                    if (!EsScriptMovimientoAjeno(mb)) continue;
                    ajenos.Add(mb.GetType().Name);
                    if (mb.enabled)
                    {
                        Undo.RecordObject(mb, "Desactivar movimiento ajeno");
                        mb.enabled = false;
                        hecho.Add("desactivado '" + mb.GetType().Name + "' en " + go.name + " (movía sin colisiones)");
                    }
                }
                Piso3V10Personaje8Dir pa = go.GetComponent<Piso3V10Personaje8Dir>();
                Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                CapsuleCollider2D cap = go.GetComponent<CapsuleCollider2D>();
                bool falta = pa == null || !pa.Tiene("Quieto") || !pa.Tiene("Caminar") || go.GetComponent<Piso3V10Movimiento>() == null
                             || go.GetComponent<Piso3V10JasonAnimacion>() == null || rb == null || rb.bodyType != RigidbodyType2D.Kinematic
                             || cap == null || Mathf.Abs(cap.size.x - 0.44f) > 0.001f;
                if (falta)
                {
                    Piso3V10Generador.PrepararJason(go);
                    hecho.Add("Jason (" + go.name + ") completado");
                }
                Collider2D[] otros = go.GetComponents<Collider2D>();
                inf.Append("Jugador '").Append(go.name).Append("' pos ").Append(go.transform.position.ToString("F2"))
                   .Append(" capa ").Append(LayerMask.LayerToName(go.layer)).Append(" colliders ").Append(otros.Length);
                if (ajenos.Count > 0) inf.Append(" scripts ajenos desactivados: ").Append(string.Join(",", ajenos));
                inf.Append(". ");
            }
            if (jugadores.Count > 1) Debug.LogWarning("[Piso3 V15] Hay " + jugadores.Count + " jugadores en la escena. Deja solo uno (la cámara sigue a uno).");

            // --- Zorbos
            int z = 0, zt = 0;
            foreach (Piso3V10Zorbo zo in Object.FindObjectsByType<Piso3V10Zorbo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                zt++;
                Piso3V10Personaje8Dir pa = zo.GetComponent<Piso3V10Personaje8Dir>();
                CapsuleCollider2D cap = zo.GetComponent<CapsuleCollider2D>();
                if (pa == null || !pa.Tiene("Quieto") || !(pa.Tiene("Caminar") || pa.Tiene("Correr")) || cap == null || Mathf.Abs(cap.size.x - 0.48f) > 0.001f)
                {
                    string tipo = zo.gameObject.name.Contains("Zorbo2") ? "Zorbo2" : "Zorbo";
                    Piso3V10Generador.PrepararZorbo(zo.gameObject, tipo, zo.orientacion);
                    z++;
                }
            }
            if (z > 0) hecho.Add(z + " Zorbos actualizados");
            inf.Append("Zorbos: ").Append(zt).Append(". ");

            if (hecho.Count > 0) EditorSceneManager.MarkSceneDirty(escena);
            Debug.Log("[Piso3 V15] Informe: " + inf + (hecho.Count > 0 ? " Arreglado: " + string.Join(" · ", hecho) + ". Guarda la escena (Ctrl+S)." : " Todo al día."));
        }
    }
}
