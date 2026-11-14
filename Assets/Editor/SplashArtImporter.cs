using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class SplashArtImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>2;
        void OnPreprocessTexture()
        {
            bool splash=assetPath=="Assets/Resources/Branding/CzachsDonkeyJourney.png";
            bool icon=assetPath=="Assets/Art/AppIcon/DonkeyKnight.png";
            if(!splash&&!icon)return;
            var importer=(TextureImporter)assetImporter;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.textureType=splash?TextureImporterType.Sprite:TextureImporterType.Default;
            importer.spriteImportMode=SpriteImportMode.Single;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.mipmapEnabled=false;importer.isReadable=false;
            importer.sRGBTexture=true;importer.alphaIsTransparency=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.maxTextureSize=splash?2048:1024;
            importer.textureCompression=splash?TextureImporterCompression.Compressed:TextureImporterCompression.Uncompressed;
        }
    }
}
