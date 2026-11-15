using UnityEngine;
namespace DonkeyForest
{
    /// <summary>Licensed v2 mounted models with continuous skeletal interpolation and pose crossfades.</summary>
    public sealed class PlayerMountVisual:MonoBehaviour
    {
        ForestGame game;
        MountedRig donkey,knight;
        bool moving,fast,attackPending;
        float previousSwing,attackTime,travelSpeed,cadence=1;
        int attackVariant;
        public void Setup(ForestGame owner)
        {
            game=owner;
            donkey=Create("Donkey");knight=Create("Knight");
            // Preserve the fitted seat while following the same spine as the saddle.
            knight.transform.SetParent(donkey.Anchor("Spine"),true);
        }
        MountedRig Create(string kind)
        {
            var obj=new GameObject(kind+" v2");obj.transform.SetParent(transform,false);
            var rig=obj.AddComponent<MountedRig>();rig.Setup(kind);return rig;
        }
        public void Animate(bool riding,bool charging,float attack,float speed=6.5f)
        {
            moving=riding;fast=charging;travelSpeed=speed;
            if(attack>previousSwing+.01f)attackPending=true;
            previousSwing=attack;
        }
        void LateUpdate()
        {
            if(!game||!donkey||!knight)return;
            donkey.SetDodgeTint(game.IsInvulnerable&&!game.Dead);knight.SetDodgeTint(game.IsInvulnerable&&!game.Dead);
            if(game.Paused||game.Dead)return;
            float dt=Time.deltaTime;
            if(attackPending){attackPending=false;attackTime=.5f;attackVariant^=1;}
            else attackTime=Mathf.Max(0,attackTime-dt);
            bool kick=game.Skills.Book.Active(SkillId.Kick)>0;
            bool guard=game.Skills.Book.Active(SkillId.Guard)>0;
            bool whirlwind=game.Skills.Book.Active(SkillId.Whirlwind)>0;
            string travel=moving?(fast||travelSpeed>4.2f?"charge":"move"):"idle";
            float target=travel=="charge"?Mathf.Clamp(travelSpeed/6.5f,.65f,1.9f):Mathf.Clamp(travelSpeed/2f,.45f,1.8f);
            cadence=Mathf.Lerp(cadence,target,1-Mathf.Exp(-10*dt));
            donkey.Play(kick?"kick":travel);
            string rider=whirlwind?"whirlwind":attackTime>0?(attackVariant==0?"attack":"chop"):guard?"guard":kick?"kick":travel;
            knight.Play(rider,.14f,attackTime>0&&!whirlwind?.5f:0);
            donkey.Tick(dt*(moving&&!kick?cadence:1));knight.Tick(dt*(rider==travel&&moving?cadence:1));
        }
        public void ValidateModels(){donkey.ValidateAllClips();knight.ValidateAllClips();}
        public string Diagnostics(Camera camera)
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            return Report(donkey,planes)+"\n"+Report(knight,planes);
        }
        static string Report(MountedRig rig,Plane[] planes)=>
            $"{rig.name}: {rig.TriangleCount} triangles, {rig.VertexCount} vertices, {rig.BoneCount} bones; clip={rig.State}; "+
            $"enabled={rig.Renderer.enabled}, inCamera={GeometryUtility.TestPlanesAABB(planes,rig.Renderer.bounds)}, bounds={rig.Renderer.bounds}.";
    }
}
