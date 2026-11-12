using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DonkeyForest.Editor
{
    [InitializeOnLoad] public static class GameBuild
    {
        const string Scene="Assets/Scenes/Forest.unity";
        static GameBuild(){EditorApplication.delayCall+=EnsureScene;}
        [MenuItem("Forest Rider/Create or open game scene")]
        public static void EnsureScene()
        {
            PlayerSettings.companyName="Aizcrest";PlayerSettings.productName="Forest Rider";
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.aizcrest.forestrider");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,"com.aizcrest.forestrider");
            KeepRuntimeShaders();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if(!System.IO.File.Exists(Scene))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene,Scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        }
        // Runtime-created terrain has no renderer in the saved scene for the build scanner.
        // Retain the terrain and grass shaders explicitly so player builds render them too.
        static void KeepRuntimeShaders()
        {
            var assets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if(assets.Length==0)return;
            var settings=new SerializedObject(assets[0]);
            var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            if(shaders==null)return;
            foreach(string name in new[]{"Standard","Nature/Terrain/Standard","Hidden/TerrainEngine/Details/BillboardWavingDoublePass","UI/Default"})
            {
                var shader=Shader.Find(name);
                if(!shader){Debug.LogWarning("Required runtime shader was not found: "+name);continue;}
                bool found=false;
                for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(found)continue;
                int next=shaders.arraySize;shaders.InsertArrayElementAtIndex(next);
                shaders.GetArrayElementAtIndex(next).objectReferenceValue=shader;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Forest Rider/Build Android APK")]
        public static void Android()
        {
            EnsureScene();System.IO.Directory.CreateDirectory("Builds");
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARMv7|AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
            var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/ForestRider.apk",BuildTarget.Android,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Android build failed: "+report.summary.result);
        }
        [MenuItem("Forest Rider/Build iOS Xcode project")]
        public static void IOS()
        {
            EnsureScene();
            var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/iOS",BuildTarget.iOS,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("iOS export failed: "+report.summary.result);
        }
    }
}
