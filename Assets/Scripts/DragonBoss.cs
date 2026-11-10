using UnityEngine;
namespace DonkeyForest
{
    public sealed class DragonBoss:MonoBehaviour
    {
        const float MaxHealth=650;
        ForestGame game;
        DragonEncounter encounter;
        EnemyVisual visual;
        DragonPattern pattern;
        Transform hazards;
        Vector3 diveTarget,diveStart;
        float health=MaxHealth,shotClock=1.2f,orbit;
        bool waiting;
        public string Status => waiting?"DRAGON — return within 32m of the altar (fight resets on retreat)":
            $"DRAGON {Mathf.CeilToInt(health)}/{MaxHealth:0} • {(pattern.Phase==DragonPhase.Airborne?"FIREBALLS — avoid orange circles":pattern.Phase==DragonPhase.DiveWarning?"DIVE INCOMING — DODGE!":pattern.Phase==DragonPhase.Diving?"DIVE — move out!":"STUNNED — ATTACK NOW!")} • {pattern.Remaining:0.0}s";
        public void Setup(ForestGame owner,DragonEncounter source)
        {
            game=owner;encounter=source;
            visual=EnemyVisual.Create("Dragon",transform,3.5f);
            visual.transform.position=game.Ground(new Vector3(0,0,8),8);
            ResetPattern();
        }
        void ResetPattern()
        {
            if(pattern!=null)pattern.Changed-=ChangePhase;
            pattern=new DragonPattern();pattern.Changed+=ChangePhase;
            health=MaxHealth;shotClock=1.2f;ClearHazards();visual.Play("fly");
        }
        void ClearHazards()
        {
            if(hazards){hazards.gameObject.SetActive(false);Destroy(hazards.gameObject);}
            hazards=new GameObject("Dragon attack warnings").transform;hazards.SetParent(transform,false);
        }
        void Update()
        {
            if(!game||game.Paused||game.Dead)return;
            if(new Vector2(game.rider.position.x,game.rider.position.z).magnitude>32)
            {
                if(!waiting){waiting=true;ResetPattern();game.Show("You left the altar arena. The dragon recovers; return to try again.");}
                return;
            }
            if(waiting){waiting=false;ResetPattern();}
            float dt=Time.deltaTime;
            // Projectiles resolve only in the aerial phase. Transitions cancel lingering shots.
            pattern.Tick(dt);
            if(pattern.Phase==DragonPhase.Airborne)
            {
                orbit+=dt*.5f;
                Vector3 desired=game.Ground(new Vector3(Mathf.Cos(orbit)*9,0,Mathf.Sin(orbit)*9),8);
                visual.transform.position=Vector3.Lerp(visual.transform.position,desired,1-Mathf.Exp(-3*dt));
                Face(game.rider.position);shotClock-=dt;
                if(shotClock<=0){shotClock+=1.4f;if(pattern.Remaining>1.2)Fireball();}
            }
            else if(pattern.Phase==DragonPhase.DiveWarning)Face(diveTarget);
            else if(pattern.Phase==DragonPhase.Diving)
            {
                visual.transform.position=Vector3.Lerp(diveStart,diveTarget,(float)(pattern.Elapsed/DragonPattern.DiveSeconds));
                Face(diveTarget+Vector3.down*2);
            }
        }
        void Face(Vector3 point)
        {
            Vector3 direction=point-visual.transform.position;
            if(direction.sqrMagnitude>.01f)visual.transform.rotation=Quaternion.LookRotation(direction);
        }
        void ChangePhase(DragonPhase phase)
        {
            if(phase==DragonPhase.DiveWarning)
            {
                ClearHazards();diveTarget=game.Ground(game.rider.position,.12f);diveStart=visual.transform.position;
                encounter.Ring(hazards,diveTarget,3,new Color(1,.15f,.08f));
                game.Show("DIVE INCOMING! The red circle is locked — ride or DODGE away.");
            }
            else if(phase==DragonPhase.Diving)visual.Play("attack");
            else if(phase==DragonPhase.Stunned)
            {
                visual.transform.position=diveTarget;visual.transform.rotation=Quaternion.Euler(0,visual.transform.eulerAngles.y,0);
                if(Inside(game.rider.position,diveTarget,3))game.Damage(36);
                ClearHazards();visual.Play("stun");
                encounter.Ring(hazards,diveTarget,3.4f,new Color(.3f,1,.2f));
                game.Show("Dragon stunned for 5 seconds — get close and ATTACK!");
            }
            else
            {
                ClearHazards();shotClock=1.4f;visual.Play("fly");
                game.Show("Dragon takes flight! Dodge the fireballs.");
            }
        }
        void Fireball()
        {
            var obj=new GameObject("Dragon fireball");obj.transform.SetParent(hazards,false);
            obj.AddComponent<DragonFireball>().Setup(game,encounter,visual.transform.position+Vector3.down*.5f,game.Ground(game.rider.position,.1f));
        }
        public void Hit(float damage)
        {
            if(game.Paused||game.Dead||waiting||!pattern.Vulnerable||health<=0)return;
            Vector3 delta=visual.transform.position-game.rider.position;
            if(Mathf.Abs(delta.y)>3)return;delta.y=0;
            if(delta.magnitude>4 || Vector3.Dot(game.rider.forward,delta.normalized)<-.15f)return;
            health-=damage;if(health<=0)encounter.Win();
        }
        public static bool Inside(Vector3 point,Vector3 center,float radius)
        {
            Vector3 delta=point-center;return Mathf.Abs(delta.y)<3 && new Vector2(delta.x,delta.z).sqrMagnitude<=radius*radius;
        }
    }
    public sealed class DragonFireball:MonoBehaviour
    {
        ForestGame game;Vector3 start,target;Transform ball;LineRenderer warning;
        float age;bool impacted;
        public void Setup(ForestGame owner,DragonEncounter encounter,Vector3 origin,Vector3 destination)
        {
            game=owner;start=origin;target=destination;
            warning=encounter.Ring(transform,target,2.1f,new Color(1,.46f,.04f));
            ball=game.Shape("Fire",PrimitiveType.Sphere,transform,start,Vector3.one*.7f,game.Glow(new Color(1,.3f,.02f))).transform;
        }
        void Update()
        {
            if(!game||game.Paused||game.Dead)return;
            age+=Time.deltaTime;
            if(!impacted)
            {
                ball.position=Vector3.Lerp(start,target,Mathf.Clamp01(age/1.2f));
                warning.widthMultiplier=.13f*(1+Mathf.Sin(age*18)*.2f);
                if(age>=1.2f)
                {
                    impacted=true;warning.gameObject.SetActive(false);
                    if(DragonBoss.Inside(game.rider.position,target,2.1f))game.Damage(22);
                }
            }
            else
            {
                ball.localScale=new Vector3(4.2f,.2f,4.2f)*Mathf.Max(0,1-(age-1.2f)/.3f);
                if(age>=1.5f)Destroy(gameObject);
            }
        }
    }
}
