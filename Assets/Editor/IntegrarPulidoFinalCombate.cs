using UnityEditor;
using UnityEngine;

public static class IntegrarPulidoFinalCombate
{
    [MenuItem("Laboratorio 4/Pulido/Ajustar agarre fijo y sonido de munición")]
    public static void Integrar()
    {
        foreach (var ruta in new[] { "Assets/Prefabs/Player_Jason.prefab", "Assets/Prefabs/Player_Milena.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                root.GetComponent<ArmaIndependiente>().posicionManoMachete = Vector2.zero;
                PrefabUtility.SaveAsPrefabAsset(root, ruta);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        const string ammo = "Assets/Prefabs/Ammo.prefab";
        var drop = PrefabUtility.LoadPrefabContents(ammo);
        try
        {
            drop.GetComponent<AmmoDrop>().sonidoRecogida = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Recoger_Objeto.ogg");
            PrefabUtility.SaveAsPrefabAsset(drop, ammo);
        }
        finally { PrefabUtility.UnloadPrefabContents(drop); }
        Debug.Log("[PulidoCombate] Jason y Milena: agarre fijo en cadera; Ammo: sonido existente asignado. Resources intactos.");
    }
}
