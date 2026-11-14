using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DonkeyForest.Tests
{
    public sealed class TerrainBuildSupportTests
    {
        const string Support="Assets/Scenes/TerrainBuildSupport.unity";
        static EditorBuildSettingsScene[] Include(EditorBuildSettingsScene[] scenes)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("DonkeyForest.Editor.TerrainBuildSupport")).First(t=>t!=null);
            return (EditorBuildSettingsScene[])type.GetMethod("IncludeSupport").Invoke(null,new object[]{scenes});
        }
        [Test] public void SupportSceneContainsSerializedInactiveTerrain()
        {
            var scene=EditorSceneManager.OpenPreviewScene(Support);
            try
            {
                var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
                Assert.IsFalse(terrain.gameObject.activeSelf,"The support object must never draw or simulate at runtime.");
                Assert.IsNotNull(terrain.GetComponent<TerrainCollider>());
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void IncludeSupportPreservesStartupSceneAndIsIdempotent()
        {
            var scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Forest.unity",true),new EditorBuildSettingsScene("Assets/Optional.unity",false)};
            var result=Include(Include(scenes));
            Assert.AreEqual(3,result.Length);Assert.AreEqual(scenes[0].path,result[0].path);
            Assert.IsTrue(result[0].enabled);Assert.IsFalse(result[1].enabled);
            Assert.AreEqual(Support,result[2].path);Assert.IsTrue(result[2].enabled);
            Assert.AreEqual(2,scenes.Length,"Do not mutate the supplied scene list.");
        }
        [Test] public void IncludeSupportReenablesExistingDisabledEntry()
        {
            var result=Include(new[]{new EditorBuildSettingsScene("Assets/Scenes/Forest.unity",true),new EditorBuildSettingsScene(Support,false)});
            Assert.AreEqual(2,result.Length);Assert.IsTrue(result[1].enabled);
        }
    }
}
