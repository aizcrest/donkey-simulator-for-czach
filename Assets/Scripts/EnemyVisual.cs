using System;
using UnityEngine;
namespace DonkeyForest
{
    public sealed class EnemyVisual : MonoBehaviour
    {
        Animation[] animations;
        string state="";
        public static EnemyVisual Create(string kind,Transform parent,float height)
        {
            var source=Resources.Load<GameObject>("Enemies/"+kind);
            if(!source)throw new InvalidOperationException("Missing enemy model: "+kind+". Let Unity finish importing Assets/Resources/Enemies.");
            var pivot=new GameObject(kind+" visual").transform;pivot.SetParent(parent,false);
            var model=Instantiate(source,pivot);model.name=kind+" licensed model";
            foreach(var collider in model.GetComponentsInChildren<Collider>())Destroy(collider);
            var renderers=model.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)throw new InvalidOperationException("Model has no renderers: "+kind);
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);
            model.transform.localScale*=scale;
            model.transform.localPosition-=new Vector3(bounds.center.x-parent.position.x,bounds.min.y-parent.position.y,bounds.center.z-parent.position.z)*scale;
            var visual=pivot.gameObject.AddComponent<EnemyVisual>();
            visual.animations=model.GetComponentsInChildren<Animation>();
            foreach(var animation in visual.animations)
            {
                animation.playAutomatically=false;
                foreach(AnimationState clip in animation)clip.wrapMode=WrapMode.Loop;
            }
            visual.Play("idle");return visual;
        }
        public void Play(string desired)
        {
            if(state==desired)return;state=desired;
            foreach(var animation in animations)
            {
                string best=null;
                foreach(AnimationState clip in animation)
                {
                    string name=clip.name.ToLowerInvariant();
                    bool match=desired=="fly"?name.Contains("flying"):desired=="move"?(name.Contains("gallop")||name.Contains("run")||name.Contains("walk")):desired=="attack"?(name.Contains("attack")||name.Contains("punch")):desired=="stun"?name.Contains("hit"):name.Contains("idle");
                    if(match){best=clip.name;break;}
                }
                if(best!=null)animation.CrossFade(best,.15f);
                else animation.Stop();
            }
        }
        void Update()
        {
            var game=ForestGame.Instance;
            foreach(var animation in animations)
                foreach(AnimationState clip in animation)clip.speed=game && (game.Paused||game.Dead)?0:1;
        }
    }
}
