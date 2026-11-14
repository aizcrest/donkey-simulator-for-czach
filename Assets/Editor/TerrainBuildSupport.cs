using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
namespace DonkeyForest.Editor
{
    // Runtime-only terrains need a serialized Terrain in the build for Unity's
    // native terrain resources. The inactive support scene is never loaded.
    public sealed class TerrainBuildSupport:IPreprocessBuildWithReport
    {
        public int callbackOrder=>-1000;
        public static EditorBuildSettingsScene[] IncludeSupport(EditorBuildSettingsScene[] scenes)
        {
            var result=new List<EditorBuildSettingsScene>(scenes);
            for(int i=0;i<result.Count;i++)
                if(result[i].path==GameBuild.TerrainSupportScene)
                {result[i]=new EditorBuildSettingsScene(GameBuild.TerrainSupportScene,true);return result.ToArray();}
            result.Add(new EditorBuildSettingsScene(GameBuild.TerrainSupportScene,true));
            return result.ToArray();
        }
        public static void UpdateProfiles()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:BuildProfile"))
            {
                var profile=AssetDatabase.LoadAssetAtPath<BuildProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if(!profile||!profile.overrideGlobalScenes)continue;
                var scenes=profile.scenes;
                bool present=false;
                foreach(var scene in scenes)if(scene.path==GameBuild.TerrainSupportScene&&scene.enabled)present=true;
                if(present)continue;
                profile.scenes=IncludeSupport(scenes);EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
        }
        public void OnPreprocessBuild(BuildReport report)
        {
            GameBuild.ConfigurePlayerAssets();
            EditorBuildSettings.scenes=IncludeSupport(EditorBuildSettings.scenes);
            UpdateProfiles();
        }
    }
}
