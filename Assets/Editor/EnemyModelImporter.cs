using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace DonkeyForest.Editor
{
    /// <summary>Import converted free models with their original animation clips.</summary>
    public sealed class EnemyModelImporter : AssetPostprocessor
    {
        public override uint GetVersion()=>2;
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
        void OnPostprocessMaterial(Material material)
        {
            if(!assetPath.StartsWith("Assets/Resources/Enemies/"))return;
            string kind=Path.GetFileNameWithoutExtension(assetPath);
            if(kind=="Deer")
            {
                // This source stores its coat/eye colors on vertices, which Standard ignores.
                const string shaderPath="Assets/Shaders/ModelVertexColors.shader";
                context.DependsOnArtifact(shaderPath);
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if(!shader)throw new System.InvalidOperationException("Missing model vertex-color shader: "+shaderPath);
                material.shader=shader;material.color=Color.white;
            }
            string texture=kind=="Bear"?"Bear_Tex_Bear.png":
                kind=="Moose"&&!material.name.ToLowerInvariant().Contains("antler")?"Moose_Elk_BaseColor.png":
                kind=="Zombie"?"Zombie_Zombie_Atlas.png":
                kind=="Rider"?"Rider_sonic-robo-blast-2-v22-32x.png":null;
            if(texture!=null)
            {
                string path="Assets/Resources/Enemies/"+kind+".fbm/"+texture;
                context.DependsOnArtifact(path);
                var diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(!diffuse)throw new System.InvalidOperationException("Missing model color texture: "+path);
                material.mainTexture=diffuse;material.color=Color.white;
            }
            if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.1f);
            material.enableInstancing=true;
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
