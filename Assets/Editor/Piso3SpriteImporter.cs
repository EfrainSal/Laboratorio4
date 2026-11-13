#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public class Piso3SpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.Contains("/Resources/Piso3/"))return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Sprite;
        t.spriteImportMode=SpriteImportMode.Single;
        t.spritePixelsPerUnit=32;
        t.filterMode=FilterMode.Point;
        t.textureCompression=TextureImporterCompression.Uncompressed;
        t.mipmapEnabled=false;
        t.alphaIsTransparency=true;
    }
}
#endif
