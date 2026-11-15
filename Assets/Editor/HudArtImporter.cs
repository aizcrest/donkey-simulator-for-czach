using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class HudArtImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(assetPath!="Assets/Resources/Hud/Stats.png")return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
