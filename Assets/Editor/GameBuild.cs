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
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if(!System.IO.File.Exists(Scene))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene,Scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        }
        [MenuItem("Forest Rider/Build Android APK")]
        public static void Android()
        {
            EnsureScene();System.IO.Directory.CreateDirectory("Builds");
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
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
