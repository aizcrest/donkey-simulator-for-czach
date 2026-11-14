using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DonkeyForest.Editor
{
    [InitializeOnLoad] public static class GameBuild
    {
        const string Scene="Assets/Scenes/Forest.unity";
        static GameBuild(){EditorApplication.delayCall+=EnsureScene;}
        [MenuItem("Forest Rider/Create or open game scene")]
        public static void OpenScene()
        {
            if(!PrepareSceneForCommand())return;
            EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
        }
        public static void EnsureScene(){TryEnsureScene();}
        static bool HasUntitledScene()
        {
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))return true;
            return false;
        }
        static bool PrepareSceneForCommand()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
            // Only explicit menu commands may prompt. Unity cannot create the missing
            // game scene additively until every Untitled scene has a path on disk.
            if(!System.IO.File.Exists(Scene))
                for(int i=0;i<SceneManager.sceneCount;i++)
                {
                    var current=SceneManager.GetSceneAt(i);
                    if(string.IsNullOrEmpty(current.path)&&!EditorSceneManager.SaveScene(current))return false;
                }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return false;
            return TryEnsureScene();
        }
        static bool TryEnsureScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
            PlayerSettings.companyName="Aizcrest";PlayerSettings.productName="Czach’s Donkey Journey";
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.aizcrest.forestrider");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.aizcrest.forestrider");
            EnsureLegacyInput();
            KeepRuntimeShaders();
            ConfigureSplash();
            // Delay-call startup is noninteractive and must preserve Untitled scenes,
            // including a fresh template scene whose isDirty flag may still be false.
            if(!System.IO.File.Exists(Scene)&&HasUntitledScene())return false;
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if(!System.IO.File.Exists(Scene))
            {
                var previous=SceneManager.GetActiveScene();
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                try
                {
                    if(!EditorSceneManager.SaveScene(scene,Scene))
                    {Debug.LogError("Could not save the Forest scene. Check the project folder permissions.");return false;}
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene,true);
                    if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                }
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            return true;
        }
        static void ConfigureSplash()
        {
            var artwork=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Splash/CzachsDonkeyJourney.png");
            if(!artwork)return;
            PlayerSettings.SplashScreen.show=true;
            PlayerSettings.SplashScreen.showUnityLogo=false;
            PlayerSettings.SplashScreen.background=artwork;
            PlayerSettings.SplashScreen.blurBackgroundImage=false;
            PlayerSettings.SplashScreen.overlayOpacity=0;
            // A blank logo reserves display time for the full background artwork.
            PlayerSettings.SplashScreen.logos=new[]{PlayerSettings.SplashScreenLogo.Create(2f)};
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
            foreach(string name in new[]{"Standard","Forest Rider/Model Vertex Colors","Forest Rider/Day Night Sky","Forest Rider/Skill Glow","Nature/Terrain/Standard","Hidden/TerrainEngine/Details/BillboardWavingDoublePass","UI/Default"})
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
            if(!PrepareSceneForCommand())return;
            System.IO.Directory.CreateDirectory("Builds");
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARMv7|AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
            var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/CzachsDonkeyJourney.apk",BuildTarget.Android,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Android build failed: "+report.summary.result);
        }
        [MenuItem("Forest Rider/Build iOS Xcode project")]
        public static void IOS()
        {
            if(!PrepareSceneForCommand())return;
            var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/iOS",BuildTarget.iOS,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("iOS export failed: "+report.summary.result);
        }
    }
}
