using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace DonkeyForest.Tests
{
    public sealed class ImportedMaterialTests
    {
        static Material Create(Material source,string kind,Texture2D diffuse,Shader shader=null)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type=assembly.GetType("DonkeyForest.ImportedModelMaterials");
                if(type!=null)return (Material)type.GetMethod("CreateMaterial",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{source,kind,diffuse,shader});
            }
            throw new InvalidOperationException("Runtime material builder not found.");
        }
        [TestCase("Bear")] [TestCase("Moose")] [TestCase("Zombie")] [TestCase("Rider")]
        public void MissingTextureUsesCoatColorWithoutCopyingSourceTexture(string kind)
        {
            var source=new Material(Shader.Find("Standard"));Material copy=null;
            try
            {
                source.mainTexture=Texture2D.blackTexture;source.color=Color.white;
                Assert.DoesNotThrow(()=>copy=Create(source,kind,null));
                Assert.IsNull(copy.mainTexture);Assert.AreNotEqual(Color.white,copy.color);
                Assert.IsTrue(copy.enableInstancing);
                Assert.AreSame(Texture2D.blackTexture,source.mainTexture);Assert.AreEqual(Color.white,source.color);
            }
            finally{if(copy)UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(source);}
        }
        [Test] public void Verified2DTextureAndUvTransformAreAppliedToOwnedMaterial()
        {
            var source=new Material(Shader.Find("Standard"));Material copy=null;
            try
            {
                source.mainTextureScale=new Vector2(2,3);source.mainTextureOffset=new Vector2(.2f,.4f);
                copy=Create(source,"Moose",Texture2D.whiteTexture);
                Assert.AreSame(Texture2D.whiteTexture,copy.mainTexture);Assert.AreEqual(Color.white,copy.color);
                Assert.AreEqual(source.mainTextureScale,copy.mainTextureScale);Assert.AreEqual(source.mainTextureOffset,copy.mainTextureOffset);
                Assert.AreNotSame(source,copy);
            }
            finally{if(copy)UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(source);}
        }
        [Test] public void VertexColorShaderDoesNotReceiveNonexistentTextureProperties()
        {
            var source=new Material(Shader.Find("Standard"));Material copy=null;
            try
            {
                var shader=Shader.Find("Forest Rider/Model Vertex Colors");Assert.IsNotNull(shader);
                Assert.DoesNotThrow(()=>copy=Create(source,"Deer",null,shader));
                Assert.AreSame(shader,copy.shader);Assert.IsFalse(copy.HasProperty("_MainTex"));Assert.AreEqual(Color.white,copy.color);
            }
            finally{if(copy)UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(source);}
        }
    }
}
