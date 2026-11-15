using UnityEngine;
namespace DonkeyForest
{
    public sealed class ForestEnemy:MonoBehaviour
    {
        public string kind;
        ForestGame game;
        float baseHealth,baseDamage;
        float health,maxHealth,wanderTime,speed,damage,attackReach;
        EnemyAttackCycle attack;
        bool attackSound;
        Vector3 attackDirection;
        Vector3 wander;
        Transform healthBar;
        CharacterController controller;
        EnemyVisual visual;
        bool angry,timid,rushHit;
        public bool Elite { get; private set; }
        float stunTime,chargeWarning,rushTime,chargeCooldown=4;
        Vector3 rushDirection;
        Renderer healthRenderer;
        public void Setup(ForestGame owner,string species,bool elite=false)
        {
            game=owner;kind=species;Elite=elite;
            if(Elite)name="Elite "+species;
            timid=kind=="Deer"||kind=="Fox";
            maxHealth=health=kind=="Moose"?150:kind=="Bear"?100:kind=="Zombie"?75:kind=="Wolf"?65:kind=="Fox"?35:45;
            speed=kind=="Wolf"?4.8f:kind=="Fox"?4.4f:kind=="Deer"?3.8f:kind=="Moose"?3.2f:kind=="Bear"?2.8f:2.3f;
            damage=kind=="Moose"?24:kind=="Bear"?18:kind=="Wolf"?12:kind=="Zombie"?13:7;
            float height=kind=="Moose"?3:kind=="Zombie"?1.9f:kind=="Deer"?2:kind=="Bear"?1.65f:kind=="Wolf"?1.1f:.85f;
            maxHealth*= (1+(game.data.journey.level-1)*.025f)*(Elite?1.6f:1);health=maxHealth;
            damage*=(1+(game.data.journey.level-1)*.02f)*(Elite?1.3f:1);
            baseHealth=maxHealth;baseDamage=damage;ApplyDifficulty();
            attackReach=kind=="Moose"?2.6f:2.2f;
            attack=new EnemyAttackCycle(EnemyAttackProfile.For(kind));
            visual=EnemyVisual.Create(kind,transform,height);
            controller=gameObject.AddComponent<CharacterController>();
            controller.radius=kind=="Moose"?.7f:.5f;controller.height=Mathf.Max(1.2f,height*.8f);controller.center=Vector3.up*controller.height*.5f;
            controller.stepOffset=.4f;controller.slopeLimit=45;
            healthBar=game.Shape(kind+" health",PrimitiveType.Cube,transform,new Vector3(0,height+.3f,0),new Vector3(1,.08f,.08f),game.Mat(new Color(.8f,.22f,.15f))).transform;
            healthRenderer=healthBar.GetComponent<Renderer>();
            if(Elite)game.Shape("Elite marker",PrimitiveType.Cube,transform,new Vector3(0,height+.6f,0),Vector3.one*.18f,game.Glow(new Color(1,.75f,.1f))).transform.localRotation=Quaternion.Euler(0,45,45);
        }
        public void ApplyDifficulty()
        {
            float nextMax=baseHealth*game.Difficulty.HealthMultiplier;
            health=DifficultyRules.RescaleHealth(health,maxHealth,nextMax);
            maxHealth=nextMax;damage=baseDamage*game.Difficulty.DamageMultiplier;
            if(healthBar)healthBar.localScale=new Vector3(Mathf.Max(0,health/maxHealth),.08f,.08f);
        }
        void Update()
        {
            if(!game||game.Paused||game.Dead||health<=0)return;
            if(kind=="Zombie"&&!game.Night){game.enemies.Remove(this);Destroy(gameObject);return;}
            float dt=Time.deltaTime;wanderTime-=dt;chargeCooldown-=dt;
            if(stunTime>0)
            {
                stunTime=Mathf.Max(0,stunTime-dt);controller.Move(Vector3.down*15*dt);ClampPosition();visual.Play("stun");
                if(stunTime<=0)healthRenderer.sharedMaterial=game.Mat(new Color(.8f,.22f,.15f));
                return;
            }
            if(chargeWarning>0)
            {
                chargeWarning-=dt;visual.AttackPose(Mathf.Clamp01(1-chargeWarning)*.4f);controller.Move(Vector3.down*15*dt);ClampPosition();
                if(chargeWarning<=0){rushTime=.7f;rushHit=false;healthRenderer.sharedMaterial=game.Mat(new Color(.8f,.22f,.15f));}
                return;
            }
            if(rushTime>0)
            {
                Vector3 before=transform.position;float moveTime=Mathf.Min(dt,rushTime);rushTime=Mathf.Max(0,rushTime-dt);
                var flags=controller.Move((rushDirection*8+Vector3.down*15)*moveTime);ClampPosition();visual.Play("move");
                Vector3 tip=rushDirection*1.35f+Vector3.up*1.55f;
                if(!rushHit&&Connects(before+tip,transform.position+tip,.6f)){rushHit=true;game.Damage(damage*1.4f);}
                if((flags&CollisionFlags.Sides)!=0&&(transform.position-before).sqrMagnitude<.01f)rushTime=0;
                return;
            }
            if(attack.Running){TickAttack(dt);return;}
            Vector3 delta=game.rider.position-transform.position;delta.y=0;float distance=delta.magnitude;
            if(kind=="Moose"&&distance>3&&distance<11&&chargeCooldown<=0)
            {
                chargeCooldown=7;chargeWarning=1; rushDirection=delta.normalized;
                transform.rotation=Quaternion.LookRotation(rushDirection);healthRenderer.sharedMaterial=game.Glow(new Color(1,.4f,.04f));
                game.Show("Moose charge incoming — move sideways or DODGE!");game.Audio.Play("roar",.18f);return;
            }
            Vector3 direction;
            // Moose defend their space. Wolves and bears pursue from farther away.
            bool chase=(!timid||angry)&&distance<(kind=="Moose"&&!angry?8:18);
            if(chase) direction=distance>attackReach-.5f?delta.normalized:Vector3.zero;
            else if(timid&&!angry&&distance<7)direction=-delta.normalized;
            else {if(wanderTime<0){wanderTime=Random.Range(2,5);wander=Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward;}direction=wander*.45f;}
            Vector3 velocity=Vector3.zero;
            if(direction.sqrMagnitude>.001f)
            {
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),dt*5);
                if(Physics.SphereCast(transform.position+Vector3.up,.35f,direction.normalized,out _,1.2f,1<<ForestTerrain.SceneryLayer))
                {direction=Vector3.Cross(direction,Vector3.up);wander=direction;wanderTime=1;}
                velocity=direction*speed;
            }
            if(kind=="Wolf"&&chase&&distance>3&&distance<10)velocity+=Vector3.Cross(delta.normalized,Vector3.up)*((GetInstanceID()&1)==0?1.2f:-1.2f);
            controller.Move((velocity+Vector3.down*15)*dt);ClampPosition();
            delta=game.rider.position-transform.position;float heightDelta=Mathf.Abs(delta.y);delta.y=0;distance=delta.magnitude;
            if(chase&&distance<attackReach&&distance>.001f)
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),1-Mathf.Exp(-10*dt));
            if(chase&&distance<attackReach&&heightDelta<2.2f&&Vector3.Dot(transform.forward,delta.normalized)>.7f)
            {
                attackDirection=transform.forward;attack.Begin();attackSound=false;
                healthRenderer.sharedMaterial=game.Mat(new Color(1,.55f,.1f));visual.AttackPose(0);
            }
            else visual.Play(velocity.sqrMagnitude>.1f?"move":"idle");
            if(distance>65){game.enemies.Remove(this);Destroy(gameObject);}
        }
        void TickAttack(float dt)
        {
            Vector3 before=transform.position;attack.Tick(dt);var profile=attack.Profile;
            float activeStep=(float)(attack.ActiveTo-attack.ActiveFrom);
            controller.Move(attackDirection*(profile.Lunge*activeStep/(float)profile.Active)+Vector3.down*15*dt);ClampPosition();
            if(attack.ActiveThisStep)
            {
                if(!attackSound){attackSound=true;game.Audio.Play("swing",.2f);}
                // Sweep the active part only, split at 40 ms to retain curved swipes on slow phones.
                int steps=Mathf.Clamp(Mathf.CeilToInt(activeStep/.04f),1,6);
                for(int i=0;i<steps&&!attack.ContactSpent;i++)
                {
                    float a=(float)i/steps,b=(float)(i+1)/steps;
                    float start=(float)(attack.ActiveFrom+activeStep*a)/(float)profile.Active;
                    float end=(float)(attack.ActiveFrom+activeStep*b)/(float)profile.Active;
                    Vector3 from=StrikePoint(Vector3.Lerp(before,transform.position,a),start);
                    Vector3 to=StrikePoint(Vector3.Lerp(before,transform.position,b),end);
                    if(attack.TryContact(Connects(from,to,profile.Radius)))game.Damage(damage);
                }
            }
            visual.AttackPose((float)attack.PoseTime);
            healthRenderer.sharedMaterial=game.Mat(attack.Phase==EnemyAttackPhase.Windup?new Color(1,.55f,.1f):new Color(.8f,.22f,.15f));
            if(!attack.Running)visual.Play("idle");
        }
        Vector3 StrikePoint(Vector3 origin,float progress)
        {
            var p=attack.Profile;Vector3 right=Vector3.Cross(Vector3.up,attackDirection);
            return origin+Vector3.up*p.Height+attackDirection*(p.Offset+Mathf.Sin(progress*Mathf.PI)*.2f)+right*(p.Sweep*(progress*2-1));
        }
        static CombatPoint Point(Vector3 value)=>new CombatPoint(value.x,value.y,value.z);
        bool Connects(Vector3 from,Vector3 to,float radius)
        {
            Bounds target=game.PlayerHitBounds;float bodyRadius=Mathf.Min(target.extents.x,target.extents.z);
            float half=Mathf.Max(0,target.extents.y-bodyRadius);
            if(!AttackContact.SweptSphereHitsCapsule(Point(from),Point(to),radius,
                Point(target.center-Vector3.up*half),Point(target.center+Vector3.up*half),bodyRadius))return false;
            int obstacles=(1<<ForestTerrain.SceneryLayer)|(1<<ForestTerrain.GroundLayer);
            return !Physics.Linecast(transform.position+Vector3.up*attack.Profile.Height,target.center,obstacles,QueryTriggerInteraction.Ignore);
        }
        void ClampPosition()
        {
            var p=transform.position;var correction=new Vector3(Mathf.Clamp(p.x,-85,85)-p.x,0,Mathf.Clamp(p.z,-85,85)-p.z);
            if(correction.sqrMagnitude>0)controller.Move(correction);
            game.Landscape.KeepAboveGround(controller);
        }
        public void SkillHit(float amount,float stun,Vector3 push)
        {
            if(health<=0)return;Hit(amount);if(health<=0)return;
            if(stun>0)
            {
                stunTime=Mathf.Max(stunTime,stun);attack.Cancel();chargeWarning=0;rushTime=0;visual.Play("stun");
                healthRenderer.sharedMaterial=game.Glow(new Color(1,.8f,.1f));
            }
            if(push.sqrMagnitude>0){controller.Move(push);ClampPosition();}
        }
        public void Hit(float damage)
        {
            if(health<=0)return;game.Audio.Play("hit",.55f);angry=true;health-=damage;healthBar.localScale=new Vector3(Mathf.Max(0,health/maxHealth),.08f,.08f);
            if(health<=0){attack.Cancel();game.Killed(this);gameObject.SetActive(false);Destroy(gameObject);}
        }
    }
    public sealed class ForestLoot:MonoBehaviour
    {
        public int reward;float age;
        void Update()
        {
            var game=ForestGame.Instance;if(!game||game.Paused||game.Dead)return;
            age+=Time.deltaTime;transform.Rotate(0,70*Time.deltaTime,0);
            if(Vector3.Distance(transform.position,game.rider.position+Vector3.up*.6f)<2){game.Collect(reward);Destroy(gameObject);}
            else if(age>90)Destroy(gameObject);
        }
    }
}
