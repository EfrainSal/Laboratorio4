using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Laboratorio4.Recepcion;
using Object = UnityEngine.Object;

public static class IntegrarPulidoInteracciones
{
    public const string NombreNota = "NotaTutorialProgreso";
    [MenuItem("Laboratorio 4/Pulido/Integrar nota tutorial y hurtbox del jefe")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Finaliza Play antes de integrar");
        var anterior = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var escena = EditorSceneManager.OpenScene("Assets/Scenes/Recepcion.unity");
            var inicio = Object.FindFirstObjectByType<InicioGameplayRecepcion>();
            if (inicio == null || inicio.puntoDeInicio == null) throw new Exception("Falta el punto de aparición de recepción");
            var nota = Object.FindObjectsByType<NotaInteractable>(FindObjectsSortMode.None).FirstOrDefault(n => n.name == NombreNota);
            if (nota == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Nota.prefab");
                if (prefab == null) throw new Exception("Falta el prefab de nota existente");
                nota = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, escena)).GetComponent<NotaInteractable>();
                nota.name = NombreNota;
                Physics2D.SyncTransforms();
                bool libre = false;
                foreach (var desplazamiento in new[] { new Vector2(1.4f, .3f), new Vector2(-1.4f, .3f), new Vector2(0, 1.4f), new Vector2(0, -1.4f) })
                {
                    var posicion = (Vector2)inicio.puntoDeInicio.position + desplazamiento;
                    if (Physics2D.OverlapBoxAll(posicion, new Vector2(.85f, 1.05f), 0).Any(c => !c.isTrigger && !c.transform.IsChildOf(nota.transform))) continue;
                    nota.transform.position = posicion; libre = true; break;
                }
                if (!libre) throw new Exception("No hay espacio libre para la nota cerca del inicio de recepción");
            }
            nota.tituloNota = "PROTOCOLO DE AVANCE";
            nota.textoNota = "Para avanzar por los pisos del Laboratorio 4 debes completar los dos objetivos del portapapeles de tu HUD:\n\n" +
                "<b>LEE TODAS LAS NOTAS</b>\nAcércate a cada documento y oprime {Interact} para leerlo. Cada nota se cuenta una sola vez.\n\n" +
                "<b>ELIMINA A TODOS LOS SORBOS</b>\nDerrota a los enemigos indicados en el contador del portapapeles. Revisa ambos contadores hasta que no quede ninguno pendiente.\n\n" +
                "Al completar ambos objetivos del piso podrás obtener la Tarjeta de Acceso. Recógela y oprime {Interact} junto a la puerta para continuar.\n\n" +
                "En recepción, la tarjeta de inicio está en el suelo junto a la puerta de salida.";
            foreach (var c in nota.GetComponentsInChildren<Collider2D>(true)) c.isTrigger = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(nota);
            PrefabUtility.RecordPrefabInstancePropertyModifications(nota.transform);
            EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena);
            string posicionNota = nota.transform.position.ToString("F2");

            escena = EditorSceneManager.OpenScene("Assets/Scenes/Escenario4_Jefe.unity");
            var jefe = Object.FindFirstObjectByType<BossHealth>();
            if (jefe == null) throw new Exception("Falta Yiba en su sala de combate");
            var pies = jefe.GetComponent<BoxCollider2D>(); var tamano = pies.size; var offset = pies.offset;
            var hurtbox = HurtboxJefe.Asegurar(jefe);
            if (!hurtbox.GetComponent<Collider2D>().isTrigger || pies.isTrigger || pies.size != tamano || pies.offset != offset)
                throw new Exception("La hurtbox no puede alterar el collider físico de pies");
            EditorSceneManager.MarkSceneDirty(escena); EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Reports/PulidoInteracciones");
            File.WriteAllText("Reports/PulidoInteracciones/integracion.txt", "PASS NotaTutorialProgreso en Recepcion " + posicionNota +
                "\nPASS HurtboxCuerpoCompleto en Escenario4_Jefe; collider físico original conservado.\n");
            Debug.Log("[Pulido] Nota tutorial y hurtbox integradas sin reconstruir escenas ni modificar Resources.");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(anterior); }
    }
    public static void IntegrarYProbarEnLote()
    {
        try { Integrar(); ProbarPulidoInteracciones.ProbarEnLote(); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
