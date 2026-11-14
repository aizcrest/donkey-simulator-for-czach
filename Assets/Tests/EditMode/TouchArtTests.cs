using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Tests
{
    // Requires Unity's importer; this is not part of the standalone rules runner.
    public sealed class TouchArtTests
    {
        [Test] public void ControlsLoadImportedSpritesAndButtonKeepsItsSliceBorder()
        {
            foreach(string name in new[]{"button","stick","thumb","attack","dodge","heal","whirlwind","guard","charge","kick"})
            {
                string path="Assets/Resources/UI/"+name+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                var sprite=Resources.Load<Sprite>("UI/"+name);
                Assert.IsNotNull(sprite,"Runtime sprite missing: "+name);
                Assert.IsNotNull(sprite.texture);
                Assert.AreEqual(name=="button"?Vector4.one*32:Vector4.zero,sprite.border);
                Assert.Greater(sprite.rect.width,0);Assert.Greater(sprite.rect.height,0);
            }
            Assert.IsNotNull(Resources.Load<Texture2D>("UI/sky-lookup"),"The packed sky lookup must remain a texture.");
        }
    }
}
