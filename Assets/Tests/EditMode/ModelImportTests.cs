using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Tests
{
    // Unity integration test: excluded from the standalone, engine-independent rule runner.
    public sealed class ModelImportTests
    {
        [Test] public void RepeatedTexturedModelImportsKeepDependenciesAndTextureSourcesStable()
        {
            string[] kinds={"Bear","Moose","Zombie","Rider"};
            string[] textures={"Bear_Tex_Bear","Moose_Elk_BaseColor","Zombie_Zombie_Atlas","Rider_sonic-robo-blast-2-v22-32x"};
            var hashes=new Hash128[kinds.Length];var sourceHashes=new string[kinds.Length];
            const ImportAssetOptions options=ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport;
            for(int i=0;i<kinds.Length;i++)
            {
                string texture=$"Assets/Resources/Enemies/{kinds[i]}.fbm/{textures[i]}.png";
                sourceHashes[i]=HashFile(texture);
                AssetDatabase.ImportAsset(texture,options);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texture);
                Assert.AreEqual(TextureImporterShape.Texture2D,importer.textureShape);
                Assert.IsInstanceOf<Texture2D>(AssetDatabase.LoadMainAssetAtPath(texture));
                Assert.IsNotNull(Resources.Load<Texture2D>($"Enemies/{kinds[i]}.fbm/{textures[i]}"),"Runtime 2D texture lookup failed: "+texture);
            }
            // Warm every model before measuring the second import, so import order is irrelevant.
            for(int i=0;i<kinds.Length;i++)AssetDatabase.ImportAsset(ModelPath(kinds[i]),options);
            for(int i=0;i<kinds.Length;i++)hashes[i]=AssetDatabase.GetAssetDependencyHash(ModelPath(kinds[i]));
            for(int i=0;i<kinds.Length;i++)
            {
                string model=ModelPath(kinds[i]);AssetDatabase.ImportAsset(model,options);
                Assert.AreEqual(hashes[i],AssetDatabase.GetAssetDependencyHash(model),kinds[i]+" import dependencies changed on repeat import.");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(model));
                string texture=$"Assets/Resources/Enemies/{kinds[i]}.fbm/{textures[i]}.png";
                Assert.AreEqual(sourceHashes[i],HashFile(texture),kinds[i]+" import rewrote its texture source.");
            }
        }
        static string ModelPath(string kind)=>"Assets/Resources/Enemies/"+kind+".fbx";
        static string HashFile(string path)
        {using(var sha=SHA256.Create())return System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
    }
}
