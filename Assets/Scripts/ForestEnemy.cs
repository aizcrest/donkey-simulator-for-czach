using UnityEngine;
namespace DonkeyForest
{
    public sealed class ForestEnemy:MonoBehaviour
    {
        public string kind;
        ForestGame game;
        float baseHealth,baseDamage;
        float health,maxHealth,cooldown,wanderTime,speed,damage,attackReach;
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
            if(!game||game.Paused||game.Dead)return;
            if(kind=="Zombie"&&!game.Night){game.enemies.Remove(this);Destroy(gameObject);return;}
            float dt=Time.deltaTime;cooldown-=dt;wanderTime-=dt;chargeCooldown-=dt;
            if(stunTime>0)
            {
                stunTime=Mathf.Max(0,stunTime-dt);controller.Move(Vector3.down*15*dt);visual.Play("stun");
                if(stunTime<=0)healthRenderer.sharedMaterial=game.Mat(new Color(.8f,.22f,.15f));
                return;
            }
            if(chargeWarning>0)
            {
                chargeWarning-=dt;visual.Play("attack");controller.Move(Vector3.down*15*dt);
                if(chargeWarning<=0){rushTime=.7f;rushHit=false;healthRenderer.sharedMaterial=game.Mat(new Color(.8f,.22f,.15f));}
                return;
            }
            if(rushTime>0)
            {
                rushTime-=dt;controller.Move((rushDirection*8+Vector3.down*15)*dt);ClampPosition();visual.Play("move");
                if(!rushHit&&MountedSkills.InArea(game.rider.position,transform.position,rushDirection,2.6f,-1)){rushHit=true;game.Damage(damage*1.4f);}
                return;
            }
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
            bool attacking=chase&&distance<attackReach&&Mathf.Abs(transform.position.y-game.rider.position.y)<2.5f;
            visual.Play(attacking?"attack":velocity.sqrMagnitude>.1f?"move":"idle");
            if(attacking&&cooldown<=0){cooldown=kind=="Wolf"?1.1f:1.4f;game.Damage(damage);}
            if(distance>65){game.enemies.Remove(this);Destroy(gameObject);}
        }
        void ClampPosition()
        {
            var p=transform.position;var correction=new Vector3(Mathf.Clamp(p.x,-85,85)-p.x,0,Mathf.Clamp(p.z,-85,85)-p.z);
            if(correction.sqrMagnitude>0)controller.Move(correction);
        }
        public void SkillHit(float amount,float stun,Vector3 push)
        {
            if(health<=0)return;Hit(amount);if(health<=0)return;
            if(stun>0)
            {
                stunTime=Mathf.Max(stunTime,stun);cooldown=Mathf.Max(cooldown,stun+.2f);chargeWarning=0;rushTime=0;
                healthRenderer.sharedMaterial=game.Glow(new Color(1,.8f,.1f));
            }
            if(push.sqrMagnitude>0){controller.Move(push);ClampPosition();}
        }
        public void Hit(float damage)
        {
            if(health<=0)return;game.Audio.Play("hit",.55f);angry=true;health-=damage;healthBar.localScale=new Vector3(Mathf.Max(0,health/maxHealth),.08f,.08f);
            if(health<=0){game.Killed(this);Destroy(gameObject);}
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
