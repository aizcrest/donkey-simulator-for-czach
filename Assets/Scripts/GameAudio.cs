using System.Collections.Generic;
using UnityEngine;
namespace DonkeyForest
{
    public sealed class GameAudio:MonoBehaviour
    {
        ForestGame game;
        AudioSource effects,ui,ambience,music;
        bool lastNight;
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        float footClock,lastImpact=-1;
        public void Setup(ForestGame owner)
        {
            game=owner;effects=Source();ui=Source();ui.ignoreListenerPause=true;
            lastNight=game.Night;ambience=Loop(lastNight?"ambience-night":"ambience-day");music=Loop("music");ApplyVolumes();
        }
        AudioSource Source(){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;return source;}
        AudioClip Clip(string name)
        {
            if(!clips.TryGetValue(name,out var clip)){clip=Resources.Load<AudioClip>("Audio/"+name);clips[name]=clip;}
            return clip;
        }
        AudioSource Loop(string name){var source=Source();source.loop=true;source.clip=Clip(name);if(source.clip)source.Play();return source;}
        public void Play(string name,float gain=1)
        {
            if(name=="hit"){if(Time.unscaledTime-lastImpact<.045f)return;lastImpact=Time.unscaledTime;}
            var clip=Clip(name);if(!clip)return;
            if(name=="ui"||game.Paused)ui.PlayOneShot(clip,gain);else effects.PlayOneShot(clip,gain);
        }
        public void Hooves(float dt,bool moving,bool fast)
        {
            if(!moving){footClock=0;return;}
            footClock-=dt;if(footClock<=0){footClock=fast?.18f:.29f;Play("hoof",.25f);}
        }
        public void ApplyVolumes()
        {
            if(!game)return;float master=game.data.masterVolume;
            effects.volume=ui.volume=master*game.data.effectsVolume;
            ambience.volume=master*.45f;
            music.volume=master*game.data.musicVolume;
        }
        void Update()
        {
            if(!game)return;if(AudioListener.pause!=game.Paused)AudioListener.pause=game.Paused;
            if(lastNight!=game.Night){lastNight=game.Night;ambience.clip=Clip(lastNight?"ambience-night":"ambience-day");ambience.Play();}
        }
        void OnDestroy(){AudioListener.pause=false;}
    }
}
