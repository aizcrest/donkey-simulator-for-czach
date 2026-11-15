using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Tests
{
    // Requires Unity's importer; this is not part of the standalone rules runner.
    public sealed class TouchArtTests
    {
        [Test] public void HudAtlasSlicesWithoutReadabilityAndCartoonFontLoads()
        {
            var atlas=Resources.Load<Texture2D>("Hud/Stats");Assert.IsNotNull(atlas);
            Assert.IsFalse(atlas.isReadable);Assert.LessOrEqual(atlas.width,1024);
            float width=atlas.width/4f,height=atlas.height/2f;
            for(int i=0;i<8;i++)
            {
                var sprite=Sprite.Create(atlas,new Rect(i%4*width,(1-i/4)*height,width,height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                try{Assert.AreSame(atlas,sprite.texture);Assert.AreEqual(4,sprite.vertices.Length);}
                finally{Object.DestroyImmediate(sprite);}
            }
            var font=Resources.Load<Font>("Fonts/LilitaOne-Regular");Assert.IsNotNull(font);
            Assert.IsTrue(font.dynamic,"The cartoon font must support every UI point size.");
            Assert.IsTrue(font.HasCharacter('7'));Assert.IsTrue(font.HasCharacter('A'));
            Assert.IsNotNull(Resources.Load<TextAsset>("Fonts/LilitaOne-OFL"));
        }
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
