using UnityEngine;
namespace DonkeyForest
{
    public sealed class ForestEnemy:MonoBehaviour
    {
        public string kind;
        ForestGame game;
        float health,maxHealth,cooldown,wanderTime;
        Vector3 wander;
        Transform healthBar;
        bool angry;
        public void Setup(ForestGame owner,string species)
        {
            game=owner;kind=species;
            maxHealth=health=kind=="Bear"?100:kind=="Zombie"?75:45;
            var fur=game.Mat(kind=="Bear"?new Color(.3f,.16f,.08f):kind=="Deer"?new Color(.65f,.4f,.21f):new Color(.27f,.43f,.27f));
            var dark=game.Mat(new Color(.1f,.08f,.06f));
            if(kind=="Zombie")
            {
                game.Shape("Torso",PrimitiveType.Capsule,transform,new Vector3(0,1,0),new Vector3(.65f,.65f,.4f),fur);
                game.Shape("Head",PrimitiveType.Cube,transform,new Vector3(0,1.85f,0),Vector3.one*.45f,fur);
                for(int s=-1;s<=1;s+=2)
                {
                    game.Shape("Arm",PrimitiveType.Cube,transform,new Vector3(s*.45f,1.3f,.35f),new Vector3(.2f,.2f,.9f),fur);
                    game.Shape("Leg",PrimitiveType.Cube,transform,new Vector3(s*.18f,.4f,0),new Vector3(.22f,.8f,.25f),dark);
                }
            }
            else
            {
                float scale=kind=="Bear"?1.3f:1;
                game.Shape("Body",PrimitiveType.Sphere,transform,new Vector3(0,.9f,0),new Vector3(.9f,.9f,1.6f)*scale,fur);
                game.Shape("Head",PrimitiveType.Sphere,transform,new Vector3(0,1.25f,.85f),Vector3.one*.65f*scale,fur);
                for(int s=-1;s<=1;s+=2)
                {
                    for(int e=-1;e<=1;e+=2)game.Shape("Leg",PrimitiveType.Cube,transform,new Vector3(s*.3f,.4f,e*.5f),new Vector3(.18f,.8f,.18f),fur);
                    if(kind=="Deer")game.Shape("Antler",PrimitiveType.Cube,transform,new Vector3(s*.25f,1.9f,.85f),new Vector3(.08f,.8f,.08f),dark).transform.localRotation=Quaternion.Euler(0,0,s*25);
                    game.Shape("Eye",PrimitiveType.Sphere,transform,new Vector3(s*.26f,1.4f,1.04f),Vector3.one*.08f,dark);
                }
            }
            var collider=gameObject.AddComponent<CapsuleCollider>();collider.radius=.65f;collider.height=2;collider.center=Vector3.up;
            healthBar=game.Shape("Health",PrimitiveType.Cube,transform,new Vector3(0,2.6f,0),new Vector3(1,.08f,.08f),game.Mat(new Color(.8f,.22f,.15f))).transform;
        }
        void Update()
        {
            if(!game||game.Paused||game.Dead)return;
            if(kind=="Zombie"&&!game.Night){game.enemies.Remove(this);Destroy(gameObject);return;}
            float dt=Time.deltaTime;cooldown-=dt;wanderTime-=dt;
            Vector3 delta=game.rider.position-transform.position;delta.y=0;float distance=delta.magnitude;
            Vector3 direction;
            bool chase=(kind!="Deer"||angry)&&distance<18;
            if(chase) direction=distance>1.7f?delta.normalized:Vector3.zero;
            else if(kind=="Deer"&&!angry&&distance<7)direction=-delta.normalized;
            else {if(wanderTime<0){wanderTime=Random.Range(2,5);wander=Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward;}direction=wander*.45f;}
            if(direction.sqrMagnitude>.001f)
            {
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),dt*5);
                // Sphere casts keep animals from walking through trees and other creatures.
                if(!Physics.SphereCast(transform.position+Vector3.up,.3f,direction,out _,1f))
                    transform.position+=direction*(kind=="Deer"?3.8f:kind=="Bear"?2.8f:2.3f)*dt;
                else {wander=-direction;wanderTime=1;transform.position+=Vector3.Cross(direction,Vector3.up)*dt;}
            }
            var p=transform.position;p.x=Mathf.Clamp(p.x,-85,85);p.z=Mathf.Clamp(p.z,-85,85);transform.position=p;
            if(chase&&distance<2.2f&&cooldown<=0){cooldown=1.4f;game.Damage(kind=="Bear"?18:kind=="Zombie"?13:8);}
            if(distance>65){game.enemies.Remove(this);Destroy(gameObject);}
        }
        public void Hit(float damage)
        {
            if(health<=0)return;angry=true;health-=damage;healthBar.localScale=new Vector3(Mathf.Max(0,health/maxHealth),.08f,.08f);
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
