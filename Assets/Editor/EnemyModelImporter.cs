using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    /// <summary>Import converted free models with their original animation clips.</summary>
    public sealed class EnemyModelImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Resources/Enemies/"))return;
            var importer=(ModelImporter)assetImporter;
            importer.animationType=ModelImporterAnimationType.Legacy;
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
            importer.addCollider=false;importer.isReadable=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.materialSearch=ModelImporterMaterialSearch.RecursiveUp;
        }
        void OnPostprocessModel(GameObject model)
        {
            if(!assetPath.StartsWith("Assets/Resources/Enemies/"))return;
            foreach(var animation in model.GetComponentsInChildren<Animation>(true))
            {
                animation.playAutomatically=false;animation.cullingType=AnimationCullingType.AlwaysAnimate;
                foreach(AnimationState state in animation)state.wrapMode=WrapMode.Loop;
            }
        }
    }
}
