using System;
namespace DonkeyForest
{
    public enum EnemyAttackPhase { Idle,Windup,Strike,Recovery }
    public readonly struct EnemyAttackProfile
    {
        public readonly double Windup,Active,Recovery;
        public readonly float Offset,Radius,Height,Lunge,Sweep;
        public double Duration=>Windup+Active+Recovery;
        public EnemyAttackProfile(double windup,double active,double recovery,float offset,float radius,float height,float lunge,float sweep=0)
        {Windup=windup;Active=active;Recovery=recovery;Offset=offset;Radius=radius;Height=height;Lunge=lunge;Sweep=sweep;}
        public static EnemyAttackProfile For(string kind)
        {
            switch(kind)
            {
                case "Bear":return new EnemyAttackProfile(.55,.22,.63,1.05f,.55f,1.05f,.3f,.5f);
                case "Moose":return new EnemyAttackProfile(.65,.24,.71,1.35f,.6f,1.55f,.35f,.2f);
                case "Zombie":return new EnemyAttackProfile(.5,.22,.68,.95f,.5f,1.15f,.25f,.35f);
                case "Wolf":return new EnemyAttackProfile(.35,.18,.57,.95f,.4f,.65f,.5f);
                case "Fox":return new EnemyAttackProfile(.4,.18,.62,.8f,.35f,.5f,.35f);
                default:return new EnemyAttackProfile(.5,.22,.68,1.1f,.45f,1.15f,.3f);
            }
        }
    }
    /// <summary>One committed attack, including active overlap when a slow frame crosses phase boundaries.</summary>
    public sealed class EnemyAttackCycle
    {
        public EnemyAttackProfile Profile { get; }
        public bool Running { get; private set; }
        public bool ContactSpent { get; private set; }
        public double Elapsed { get; private set; }
        public double ActiveFrom { get; private set; }
        public double ActiveTo { get; private set; }
        public bool ActiveThisStep=>ActiveTo>ActiveFrom;
        public EnemyAttackPhase Phase=>!Running?EnemyAttackPhase.Idle:Elapsed<Profile.Windup?EnemyAttackPhase.Windup:
            Elapsed<Profile.Windup+Profile.Active?EnemyAttackPhase.Strike:EnemyAttackPhase.Recovery;
        public double PoseTime=>Elapsed<Profile.Windup?.45*Elapsed/Profile.Windup:
            Elapsed<Profile.Windup+Profile.Active?.45+.2*(Elapsed-Profile.Windup)/Profile.Active:
            Math.Min(1,.65+.35*(Elapsed-Profile.Windup-Profile.Active)/Profile.Recovery);
        public EnemyAttackCycle(EnemyAttackProfile profile){Profile=profile;}
        public void Begin(){Running=true;ContactSpent=false;Elapsed=ActiveFrom=ActiveTo=0;}
        public void Cancel(){Running=false;ContactSpent=true;Elapsed=ActiveFrom=ActiveTo=0;}
        public void Tick(double dt)
        {
            if(dt<0||double.IsNaN(dt)||double.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            ActiveFrom=ActiveTo=0;if(!Running)return;
            ActiveFrom=Math.Max(0,Math.Min(Profile.Active,Elapsed-Profile.Windup));
            Elapsed=Math.Min(Profile.Duration,Elapsed+dt);
            ActiveTo=Math.Max(0,Math.Min(Profile.Active,Elapsed-Profile.Windup));
            if(Elapsed>=Profile.Duration)Running=false;
        }
        public bool TryContact(bool touching)
        {
            if(!touching||!ActiveThisStep||ContactSpent)return false;
            ContactSpent=true;return true;
        }
    }
    /// <summary>Protection lasts through the whole simulation frame that ends the dodge.</summary>
    public sealed class DodgeWindow
    {
        public const float Duration=.32f,CooldownSeconds=1.6f;
        public float Remaining { get; private set; }
        public float Cooldown { get; private set; }
        public bool ProtectedThisFrame { get; private set; }
        public bool Invulnerable=>Remaining>0||ProtectedThisFrame;
        public bool TryStart(bool allowed)
        {
            if(!allowed||Cooldown>0)return false;
            Remaining=Duration;Cooldown=CooldownSeconds;ProtectedThisFrame=true;return true;
        }
        public void BeginFrame(float dt,bool paused)
        {
            if(dt<0||float.IsNaN(dt)||float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            ProtectedThisFrame=Remaining>0;
            if(paused)return;
            Remaining=Math.Max(0,Remaining-dt);Cooldown=Math.Max(0,Cooldown-dt);
        }
        public void Reset(){Remaining=Cooldown=0;ProtectedThisFrame=false;}
    }
    public sealed class DamageInbox
    {
        float pending;
        public void Add(float amount){if(amount>0&&!float.IsNaN(amount)&&!float.IsInfinity(amount))pending+=amount;}
        public float Resolve(bool blocked){float result=blocked?0:pending;pending=0;return result;}
        public void Clear(){pending=0;}
    }
    public readonly struct CombatPoint
    {
        public readonly double X,Y,Z;
        public CombatPoint(double x,double y,double z){X=x;Y=y;Z=z;}
        public static CombatPoint operator +(CombatPoint a,CombatPoint b)=>new CombatPoint(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static CombatPoint operator -(CombatPoint a,CombatPoint b)=>new CombatPoint(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static CombatPoint operator *(CombatPoint a,double t)=>new CombatPoint(a.X*t,a.Y*t,a.Z*t);
        public static double Dot(CombatPoint a,CombatPoint b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    }
    public static class AttackContact
    {
        static double Clamp(double value)=>Math.Max(0,Math.Min(1,value));
        // Closest points of two 3D segments, including stationary spheres/degenerate capsules.
        public static bool SweptSphereHitsCapsule(CombatPoint from,CombatPoint to,double radius,CombatPoint bottom,CombatPoint top,double bodyRadius)
        {
            CombatPoint d1=to-from,d2=top-bottom,r=from-bottom;
            double a=CombatPoint.Dot(d1,d1),e=CombatPoint.Dot(d2,d2),f=CombatPoint.Dot(d2,r),s,t;
            if(a<1e-10&&e<1e-10){s=t=0;}
            else if(a<1e-10){s=0;t=Clamp(f/e);}
            else
            {
                double c=CombatPoint.Dot(d1,r);
                if(e<1e-10){t=0;s=Clamp(-c/a);}
                else
                {
                    double b=CombatPoint.Dot(d1,d2),denominator=a*e-b*b;
                    s=denominator>1e-10?Clamp((b*f-c*e)/denominator):0;t=(b*s+f)/e;
                    if(t<0){t=0;s=Clamp(-c/a);}else if(t>1){t=1;s=Clamp((b-c)/a);}
                }
            }
            CombatPoint gap=(from+d1*s)-(bottom+d2*t);double reach=radius+bodyRadius;
            return radius>=0&&bodyRadius>=0&&CombatPoint.Dot(gap,gap)<=reach*reach;
        }
    }
}
