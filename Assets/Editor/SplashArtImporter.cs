using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class SplashArtImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(assetPath!="Assets/Art/Splash/CzachsDonkeyJourney.png")return;
            var importer=(TextureImporter)assetImporter;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.mipmapEnabled=false;importer.isReadable=false;
            importer.sRGBTexture=true;importer.alphaIsTransparency=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Compressed;
        }
    }
}
