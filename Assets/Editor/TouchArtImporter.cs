using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class TouchArtImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>3;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/UI/"))return;
            bool sky=assetPath.EndsWith("sky-lookup.png");
            var importer=(TextureImporter)assetImporter;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.textureType=sky?TextureImporterType.Default:TextureImporterType.Sprite;importer.isReadable=false;
            if(!sky)
            {
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;
                importer.spriteBorder=assetPath.EndsWith("/button.png")?Vector4.one*32:Vector4.zero;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            }
            importer.sRGBTexture=!sky;importer.alphaIsTransparency=!sky;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=sky?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=sky?512:256;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
