using UnityEngine;
namespace DonkeyForest
{
    public sealed class DragonBoss:MonoBehaviour
    {
        const float BaseHealth=650;
        float maxHealth;
        DifficultyProfile difficulty;
        ForestGame game;
        DragonEncounter encounter;
        EnemyVisual visual;
        DragonPattern pattern;
        Transform hazards;
        Vector3 diveTarget,diveStart;
        float health,shotClock=1.2f,orbit;
        bool waiting;
        public string Status => waiting?"DRAGON — return within 32m of the altar (fight resets on retreat)":
            $"DRAGON {Mathf.CeilToInt(health)}/{maxHealth:0} • {(pattern.Phase==DragonPhase.Airborne?"FIREBALLS — avoid orange circles":pattern.Phase==DragonPhase.DiveWarning?"DIVE INCOMING — DODGE!":pattern.Phase==DragonPhase.Diving?"DIVE — move out!":"STUNNED — ATTACK NOW!")} • {pattern.Remaining:0.0}s";
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
            difficulty=game.Difficulty;maxHealth=BaseHealth*difficulty.HealthMultiplier;
            pattern=new DragonPattern(game.data.difficulty);pattern.Changed+=ChangePhase;
            health=maxHealth;shotClock=1.2f;ClearHazards();visual.Play("fly");
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
                if(shotClock<=0){shotClock+=1.4f;if(pattern.Remaining>difficulty.FireballTravelSeconds)Fireball();}
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
                game.Audio.Play("roar",.65f);ClearHazards();diveTarget=game.Ground(game.rider.position,.12f);diveStart=visual.transform.position;
                encounter.Ring(hazards,diveTarget,3,new Color(1,.15f,.08f));
                game.Show("DIVE INCOMING! The red circle is locked — ride or DODGE away.");
            }
            else if(phase==DragonPhase.Diving){visual.Play("attack");game.Audio.Play("dive");}
            else if(phase==DragonPhase.Stunned)
            {
                game.Audio.Play("kick",.65f);visual.transform.position=diveTarget;visual.transform.rotation=Quaternion.Euler(0,visual.transform.eulerAngles.y,0);
                if(Inside(game.rider.position,diveTarget,3))game.Damage(36*difficulty.DamageMultiplier);
                ClearHazards();visual.Play("stun");
                encounter.Ring(hazards,diveTarget,3.4f,new Color(.3f,1,.2f));
                game.Show($"Dragon stunned for {difficulty.DragonStunSeconds:0.##} seconds — get close and ATTACK!");
            }
            else
            {
                ClearHazards();shotClock=1.4f;visual.Play("fly");
                game.Show("Dragon takes flight! Dodge the fireballs.");
            }
        }
        void Fireball()
        {
            game.Audio.Play("fireball",.45f);
            var obj=new GameObject("Dragon fireball");obj.transform.SetParent(hazards,false);
            obj.AddComponent<DragonFireball>().Setup(game,encounter,visual.transform.position+Vector3.down*.5f,game.Ground(game.rider.position,.1f),difficulty);
        }
        public Vector3 Position=>visual.transform.position;
        public void Hit(float damage)=>HitArea(damage,game.rider.position,game.rider.forward,4,-.15f);
        public bool HitArea(float damage,Vector3 origin,Vector3 direction,float radius,float minDot)
        {
            if(game.Paused||game.Dead||waiting||!pattern.Vulnerable||health<=0)return false;
            if(!MountedSkills.InArea(Position,origin,direction,radius,minDot))return false;
            health-=damage;game.Audio.Play("hit");if(health<=0)encounter.Win();return true;
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
        DifficultyProfile difficulty;
        public void Setup(ForestGame owner,DragonEncounter encounter,Vector3 origin,Vector3 destination,DifficultyProfile settings)
        {
            game=owner;start=origin;target=destination;difficulty=settings;
            warning=encounter.Ring(transform,target,difficulty.FireballRadius,new Color(1,.46f,.04f));
            ball=game.Shape("Fire",PrimitiveType.Sphere,transform,start,Vector3.one*.7f,game.Glow(new Color(1,.3f,.02f))).transform;
        }
        void Update()
        {
            if(!game||game.Paused||game.Dead)return;
            age+=Time.deltaTime;
            if(!impacted)
            {
                ball.position=Vector3.Lerp(start,target,Mathf.Clamp01(age/difficulty.FireballTravelSeconds));
                warning.widthMultiplier=.13f*(1+Mathf.Sin(age*18)*.2f);
                if(age>=difficulty.FireballTravelSeconds)
                {
                    impacted=true;warning.gameObject.SetActive(false);
                    if(DragonBoss.Inside(game.rider.position,target,difficulty.FireballRadius))game.Damage(22*difficulty.DamageMultiplier);
                }
            }
            else
            {
                ball.localScale=new Vector3(difficulty.FireballRadius*2,.2f,difficulty.FireballRadius*2)*Mathf.Max(0,1-(age-difficulty.FireballTravelSeconds)/.3f);
                if(age>=difficulty.FireballTravelSeconds+.3f)Destroy(gameObject);
            }
        }
    }
}
