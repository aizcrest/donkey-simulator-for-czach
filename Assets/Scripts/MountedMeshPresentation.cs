using System.Collections.Generic;
using UnityEngine;

namespace DonkeyForest
{
    // Only the two player models use this compatibility path. Wildlife retains GPU skinning.
    // Ordinary MeshRenderers avoid imported SkinnedMeshRenderer culling bounds on Unity 6.
    [DefaultExecutionOrder(100)]
    public sealed class MountedMeshPresentation:MonoBehaviour
    {
        readonly List<SkinnedMeshRenderer> sources=new List<SkinnedMeshRenderer>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<MeshRenderer> outputs=new List<MeshRenderer>();
        ForestGame game;
        float nextBake;
        public void Setup(ForestGame owner,EnemyVisual mount,EnemyVisual rider)
        {
            game=owner;
            foreach(var visual in new[]{mount,rider})
            {
                foreach(var animation in visual.GetComponentsInChildren<Animation>())
                    animation.cullingType=AnimationCullingType.AlwaysAnimate;
                foreach(var skin in visual.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if(!skin.sharedMesh)continue;
                    var mesh=new Mesh{name=skin.name+" mounted pose"};mesh.MarkDynamic();
                    var obj=new GameObject(skin.name+" visible mesh",typeof(MeshFilter),typeof(MeshRenderer));
                    obj.transform.SetParent(skin.transform,false);
                    obj.layer=skin.gameObject.layer;
                    obj.GetComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows=false;
                    sources.Add(skin);meshes.Add(mesh);outputs.Add(renderer);
                    skin.enabled=false;
                }
            }
            Bake();
        }
        void LateUpdate()
        {
            if(!game||game.Paused||game.Dead||Time.unscaledTime<nextBake)return;
            Bake();
            // Bound CPU work on budget devices; bone animation and sword attachments still run normally.
            nextBake=Time.unscaledTime+1f/(game.data.quality==0?15:game.data.quality==1?24:30);
        }
        void Bake()
        {
            for(int i=0;i<sources.Count;i++)
            {
                sources[i].BakeMesh(meshes[i],false);meshes[i].RecalculateBounds();
                outputs[i].enabled=meshes[i].vertexCount>0;
            }
        }
        public string Diagnostics(Camera camera)
        {
            var text=new System.Text.StringBuilder("Mounted mesh rendering:");
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            for(int i=0;i<outputs.Count;i++)
                text.Append($"\n{sources[i].name}: vertices={meshes[i].vertexCount}, active={outputs[i].gameObject.activeInHierarchy}, enabled={outputs[i].enabled}, inCamera={GeometryUtility.TestPlanesAABB(planes,outputs[i].bounds)}, worldBounds={outputs[i].bounds}");
            return text.ToString();
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
    }
}
