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
        Dictionary<string,string>[] choices;
        static readonly string[] States={"idle","walk","move","attack","stun","fly","kick","guard"};
        public static EnemyVisual Create(string kind,Transform parent,float height)
        {
            var source=Resources.Load<GameObject>("Enemies/"+kind);
            if(!source)throw new InvalidOperationException("Missing enemy model: "+kind+". Let Unity finish importing Assets/Resources/Enemies.");
            var pivot=new GameObject(kind+" visual").transform;pivot.SetParent(parent,false);
            var model=Instantiate(source,pivot);model.name=kind+" licensed model";
            foreach(var collider in model.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
            var renderers=model.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)throw new InvalidOperationException("Model has no renderers: "+kind);
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);
            model.transform.localScale*=scale;
            model.transform.localPosition-=new Vector3(bounds.center.x-parent.position.x,bounds.min.y-parent.position.y,bounds.center.z-parent.position.z)*scale;
            var visual=pivot.gameObject.AddComponent<EnemyVisual>();
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
            visual.Play("idle");return visual;
        }
        static bool Matches(string desired,string name)
        {
            return desired=="walk"?name.Contains("walk"):desired=="kick"?name.Contains("kick"):desired=="guard"?name.Contains("guard"):desired=="fly"?name.Contains("flying"):desired=="move"?(name.Contains("gallop")||name.Contains("run")||name.Contains("walk")||name.Contains("ride")):desired=="attack"?(name.Contains("attack")||name.Contains("punch")):desired=="stun"?name.Contains("hit"):name.Contains("idle");
        }
        public void Play(string desired)
        {
            if(state==desired)return;state=desired;
            for(int i=0;i<animations.Length;i++)
            {
                if(choices[i].TryGetValue(desired,out var best))animations[i].CrossFade(best,.15f);
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
