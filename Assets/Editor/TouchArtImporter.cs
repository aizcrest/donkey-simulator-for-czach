using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class TouchArtImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>1;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/UI/"))return;
            bool sky=assetPath.EndsWith("sky-lookup.png");
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.isReadable=false;
            importer.sRGBTexture=!sky;importer.alphaIsTransparency=!sky;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=sky?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=sky?512:256;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
