using System;
using System.Collections.Generic;
using UnityEngine;
namespace DonkeyForest
{
    public sealed class EnemyVisual : MonoBehaviour
    {
        Animation[] animations;
        string state="";
        bool frozen;
        BakedEnemyAnimation bakedAnimation;
        public GameObject Model { get; private set; }
        Dictionary<string,string>[] choices;
        static readonly string[] States={"idle","walk","move","attack","stun","fly","kick","guard"};
        public static EnemyVisual Create(string kind,Transform parent,float height)
        {
            if(BakedEnemyAnimation.Supports(kind))
            {
                var root=new GameObject(kind+" visual");root.transform.SetParent(parent,false);
                var bakedModel=new GameObject(kind+" baked model");bakedModel.transform.SetParent(root.transform,false);bakedModel.transform.localScale=Vector3.one*height;
                var bakedVisual=root.AddComponent<EnemyVisual>();bakedVisual.Model=bakedModel;bakedVisual.animations=Array.Empty<Animation>();
                bakedVisual.bakedAnimation=bakedModel.AddComponent<BakedEnemyAnimation>();bakedVisual.bakedAnimation.Setup(kind);
                return bakedVisual;
            }
            var source=Resources.Load<GameObject>("Enemies/"+kind);
            if(!source)throw new InvalidOperationException("Missing enemy model: "+kind+". Let Unity finish importing Assets/Resources/Enemies.");
            var pivot=new GameObject(kind+" visual").transform;pivot.SetParent(parent,false);
            // Imported root transforms can be animated. Keep normalization outside the FBX hierarchy.
            var fit=new GameObject("Model normalization").transform;fit.SetParent(pivot,false);
            var model=Instantiate(source,fit,false);model.name=kind+" licensed model";
            foreach(var collider in model.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
            var visual=pivot.gameObject.AddComponent<EnemyVisual>();
            visual.Model=model;
            visual.animations=model.GetComponentsInChildren<Animation>();
            visual.choices=new Dictionary<string,string>[visual.animations.Length];
            for(int i=0;i<visual.animations.Length;i++)
            {
                var animation=visual.animations[i];animation.playAutomatically=false;animation.cullingType=AnimationCullingType.BasedOnRenderers;
                var choices=new Dictionary<string,string>();visual.choices[i]=choices;
                foreach(AnimationState clip in animation)
                {
                    clip.wrapMode=WrapMode.Loop;string name=clip.name.ToLowerInvariant();
                    foreach(string desired in States)if(!choices.ContainsKey(desired)&&Matches(desired,name))choices.Add(desired,clip.name);
                }
            }
            visual.Play("idle");
            foreach(var animation in visual.animations)animation.Sample();
            // Renderer.bounds may include every imported animation and may be stale before rendering.
            // Measure the actual posed mesh in local coordinates, once per spawn.
            Bounds bounds=GeometryBounds(model,pivot);
            if(bounds.size.y<.0001f)throw new InvalidOperationException("Model has no usable geometry: "+kind);
            float scale=height/bounds.size.y;
            fit.localScale=Vector3.one*scale;
            fit.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            return visual;
        }
        public static Bounds GeometryBounds(GameObject model,Transform space)
        {
            Bounds result=default;bool found=false;Mesh baked=null;
            try
            {
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh;
                    if(renderer is SkinnedMeshRenderer skin)
                    {
                        if(!skin.sharedMesh)continue;
                        if(!baked)baked=new Mesh();
                        baked.Clear();skin.BakeMesh(baked,false);baked.RecalculateBounds();mesh=baked;
                    }
                    else
                    {
                        var filter=renderer.GetComponent<MeshFilter>();
                        if(!filter||!filter.sharedMesh)continue;
                        mesh=filter.sharedMesh;
                    }
                    Bounds local=mesh.bounds;
                    Matrix4x4 matrix=space.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                    for(int corner=0;corner<8;corner++)
                    {
                        Vector3 point=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,
                            new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                        if(found)result.Encapsulate(point);else{result=new Bounds(point,Vector3.zero);found=true;}
                    }
                }
                if(!found)throw new InvalidOperationException("Model has no mesh renderers: "+model.name);
                return result;
            }
            finally{if(baked){if(Application.isPlaying)Destroy(baked);else DestroyImmediate(baked);}}
        }
        static bool Matches(string desired,string name)
        {
            return desired=="walk"?name.Contains("walk"):desired=="kick"?name.Contains("kick"):desired=="guard"?name.Contains("guard"):desired=="fly"?name.Contains("flying"):desired=="move"?(name.Contains("gallop")||name.Contains("run")||name.Contains("walk")||name.Contains("ride")):desired=="attack"?(name.Contains("attack")||name.Contains("punch")):desired=="stun"?name.Contains("hit"):name.Contains("idle");
        }
        public void Play(string desired)
        {
            if(bakedAnimation){bakedAnimation.Play(desired);return;}
            if(state==desired)return;bool first=state=="";state=desired;
            for(int i=0;i<animations.Length;i++)
            {
                if(choices[i].TryGetValue(desired,out var best))
                {
                    if(first){animations[i].Play(best);animations[i][best].time=0;}
                    else animations[i].CrossFade(best,.15f);
                }
                else animations[i].Stop();
            }
        }
        void Update()
        {
            var game=ForestGame.Instance;
            bool paused=game && (game.Paused||game.Dead);if(paused==frozen)return;frozen=paused;
            foreach(var animation in animations)foreach(AnimationState clip in animation)clip.speed=frozen?0:1;
        }
    }
}
