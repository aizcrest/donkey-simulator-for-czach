using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class TutorialArtImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/Tutorial/")||!assetPath.EndsWith(".png"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.isReadable=false;importer.mipmapEnabled=false;importer.sRGBTexture=true;
            importer.alphaSource=TextureImporterAlphaSource.None;importer.alphaIsTransparency=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Compressed;
        }
    }
}
