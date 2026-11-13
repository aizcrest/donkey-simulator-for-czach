using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DonkeyForest.Editor
{
    [InitializeOnLoad] public static class GameBuild
    {
        const string Scene="Assets/Scenes/Forest.unity";
        static GameBuild(){EditorApplication.delayCall+=EnsureScene;}
        [MenuItem("Forest Rider/Create or open game scene")]
        public static void OpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            EnsureScene();
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
        }
        public static void EnsureScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            PlayerSettings.companyName="Aizcrest";PlayerSettings.productName="Forest Rider";
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.aizcrest.forestrider");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.aizcrest.forestrider");
            EnsureLegacyInput();
            KeepRuntimeShaders();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if(!System.IO.File.Exists(Scene))
            {
                // Startup must not discard another open scene or the user's unsaved edits.
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene,Scene);
                EditorSceneManager.CloseScene(scene,true);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        }
        static void EnsureLegacyInput()
        {
            var assets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if(assets.Length==0)return;
            var settings=new SerializedObject(assets[0]);
            var input=settings.FindProperty("activeInputHandler");
            if(input==null || input.intValue==0)return;
            // The game and StandaloneInputModule use the legacy input backend on all platforms.
            input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();
            Debug.LogWarning("Forest Rider enabled Input Manager (Old). Save your work and restart the Editor before entering Play Mode.");
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
            foreach(string name in new[]{"Standard","Forest Rider/Model Vertex Colors","Nature/Terrain/Standard","Hidden/TerrainEngine/Details/BillboardWavingDoublePass","UI/Default"})
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
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
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
