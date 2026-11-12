using UnityEditor;
using System.Collections.Generic;
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
            importer.animationCompression=ModelImporterAnimationCompression.KeyframeReduction;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.materialSearch=ModelImporterMaterialSearch.RecursiveUp;
        }
        void OnPreprocessAnimation()
        {
            if(!assetPath.StartsWith("Assets/Resources/Enemies/"))return;
            var importer=(ModelImporter)assetImporter;var available=importer.defaultClipAnimations;
            var selected=new List<ModelImporterClipAnimation>();
            // Only import one clip for each used action; source downloads retain the full library.
            foreach(string action in new[]{"idle","walk","gallop","run","ride","attack","kick","hit","flying","guard"})
            {
                foreach(var clip in available)
                {
                    string name=clip.name.ToLowerInvariant();
                    if(!name.Contains(action))continue;
                    if(action=="idle"&&(name.Contains("attack")||name.Contains("hit")||name.Contains("jump")))continue;
                    if(action=="run"&&(name.Contains("arms")||name.Contains("attack")))continue;
                    if(!selected.Contains(clip)){clip.loopTime=true;clip.wrapMode=WrapMode.Loop;selected.Add(clip);}break;
                }
            }
            if(selected.Count>0)importer.clipAnimations=selected.ToArray();
        }
        void OnPostprocessModel(GameObject model)
        {
            if(!assetPath.StartsWith("Assets/Resources/Enemies/"))return;
            foreach(var animation in model.GetComponentsInChildren<Animation>(true))
            {
                animation.playAutomatically=false;animation.cullingType=AnimationCullingType.BasedOnRenderers;
                foreach(AnimationState state in animation)state.wrapMode=WrapMode.Loop;
            }
        }
    }
}
