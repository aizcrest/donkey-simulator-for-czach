using System.Collections.Generic;
using UnityEngine;
namespace DonkeyForest
{
    public sealed class MountedSkills:MonoBehaviour
    {
        ForestGame game;
        public SkillBook Book { get; private set; }
        public bool Charging=>Book.Active(SkillId.Charge)>0;
        public float GuardMultiplier=>Book.Active(SkillId.Guard)>0?SkillRules.GuardMultiplier(Book.Level(SkillId.Guard)):1;
        public Vector3 ChargeDirection { get; private set; }
        readonly HashSet<ForestEnemy> charged=new HashSet<ForestEnemy>();
        bool hitDragon;
        SkillVisualEffects effects;
        public void Setup(ForestGame owner)
        {
            game=owner;Book=new SkillBook(game.data.skills);
            effects=gameObject.AddComponent<SkillVisualEffects>();effects.Setup(game);
        }
        public void Use(SkillId id)
        {
            if(game.Paused||game.Dead)return;
            if(id==SkillId.Charge&&game.IsDodging){game.Show("Finish dodging before charging.");return;}
            if(!Book.TryUse(id,true))return;
            int level=Book.Level(id);
            effects.Play(id,SkillRules.Duration(id,level),SkillRules.Radius(id,level));
            if(id==SkillId.Guard){game.Audio.Play("guard");}
            else if(id==SkillId.Charge)
            {
                ChargeDirection=game.rider.forward;charged.Clear();hitDragon=false;
                game.Audio.Play("charge");
            }
            else
            {
                Vector3 facing=id==SkillId.Kick?-game.rider.forward:game.rider.forward;
                float radius=SkillRules.Radius(id,level),dot=id==SkillId.Kick?.25f:-1;
                foreach(var enemy in game.enemies.ToArray())
                {
                    if(!enemy||!InArea(enemy.transform.position,game.rider.position,facing,radius,dot))continue;
                    Vector3 push=(enemy.transform.position-game.rider.position);push.y=0;
                    enemy.SkillHit(SkillRules.Damage(id,level,game.WeaponDamage),id==SkillId.Kick?1.5f+.25f*(level-1):.35f,id==SkillId.Kick?push.normalized*(2+.25f*(level-1)):Vector3.zero);
                }
                if(game.Encounter.Boss)game.Encounter.Boss.HitArea(SkillRules.Damage(id,level,game.WeaponDamage),game.rider.position,facing,radius,dot);
                game.Audio.Play(id==SkillId.Kick?"kick":"whirlwind");
            }
            game.Show(SkillRules.Name(id)+"!");game.Save(false);
        }
        public void Tick(float dt)
        {
            if(game.Dead){Book.CancelEffects();effects.Clear();return;}
            Book.Tick(dt,!game.Paused);
        }
        public void CancelEffects(){Book.CancelEffects();effects.Clear();charged.Clear();}
        public void CheckCharge(Vector3 from,Vector3 to)
        {
            int level=Book.Level(SkillId.Charge);
            for(int i=game.enemies.Count-1;i>=0;i--)
            {
                var enemy=game.enemies[i];
                if(!enemy||charged.Contains(enemy))continue;
                Vector3 origin=Closest(from,to,enemy.transform.position);
                if(!InArea(enemy.transform.position,origin,ChargeDirection,1.9f,-1))continue;
                charged.Add(enemy);enemy.SkillHit(SkillRules.Damage(SkillId.Charge,level,game.WeaponDamage),1+.2f*(level-1),ChargeDirection*1.1f);
            }
            if(!hitDragon&&game.Encounter.Boss)
            {
                Vector3 origin=Closest(from,to,game.Encounter.Boss.Position);
                hitDragon=game.Encounter.Boss.HitArea(SkillRules.Damage(SkillId.Charge,level,game.WeaponDamage),origin,ChargeDirection,1.9f,-1);
            }
        }
        static Vector3 Closest(Vector3 from,Vector3 to,Vector3 point)
        {
            Vector3 delta=to-from;delta.y=0;Vector3 offset=point-from;offset.y=0;
            float t=delta.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(offset,delta)/delta.sqrMagnitude);
            return Vector3.Lerp(from,to,t);
        }
        public static bool InArea(Vector3 point,Vector3 origin,Vector3 facing,float radius,float minDot)
        {
            Vector3 delta=point-origin;if(Mathf.Abs(delta.y)>2.5f)return false;delta.y=0;
            return delta.sqrMagnitude<=radius*radius && (delta.sqrMagnitude<.01f||Vector3.Dot(facing,delta.normalized)>=minDot);
        }
    }
}
