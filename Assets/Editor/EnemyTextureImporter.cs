using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    /// <summary>Model color maps are ordinary 2D textures, never cubemaps or sprites.</summary>
    public sealed class EnemyTextureImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>1;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/Enemies/")||!assetPath.Contains(".fbm/")||!assetPath.EndsWith(".png"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.convertToNormalmap=false;importer.sRGBTexture=true;
            importer.isReadable=false;importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Repeat;importer.npotScale=TextureImporterNPOTScale.None;
            // This callback only configures its own import. No asset loads/reimports.
        }
    }
}
