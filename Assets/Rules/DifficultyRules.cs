using System;
namespace DonkeyForest
{
    /// <summary>Immutable balance profiles shared by combat, warnings and settings.</summary>
    public sealed class DifficultyProfile
    {
        public string Name { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }
        public float FireballRadius { get; }
        public float FireballTravelSeconds { get; }
        public float DragonStunSeconds { get; }
        public float DiveRadius { get; }
        internal DifficultyProfile(string name,float health,float damage,float radius,float travel,float stun,float dive)
        {Name=name;HealthMultiplier=health;DamageMultiplier=damage;FireballRadius=radius;FireballTravelSeconds=travel;DragonStunSeconds=stun;DiveRadius=dive;}
    }
    public static class DifficultyRules
    {
        // Index zero preserves the original balance and is the default for older saves.
        static readonly DifficultyProfile[] profiles={
            new DifficultyProfile("Normal",1,1,2.1f,1.2f,5,4),
            new DifficultyProfile("Hard",1.5f,1.35f,2.7f,.85f,3.75f,4.5f),
            new DifficultyProfile("Extreme",2,1.75f,3.3f,.5f,2.5f,5)
        };
        public static int Normalize(int value)=>value>=0&&value<profiles.Length?value:0;
        public static DifficultyProfile Get(int value)=>profiles[Normalize(value)];
        public static int EnemyCap(int quality,int difficulty)
        {quality=Math.Max(0,Math.Min(2,quality));return (quality==0?12:quality==1?18:22)+Normalize(difficulty)*(quality==0?4:quality==1?6:8);}
        public static int InitialEnemies(int quality,int difficulty)
        {quality=Math.Max(0,Math.Min(2,quality));return (quality==0?8:quality==1?12:14)+Normalize(difficulty)*(quality==2?6:4);}
        // Changing settings preserves health percentage and cannot revive a defeated enemy.
        public static float RescaleHealth(float health,float previousMax,float nextMax)
            =>previousMax<=0?0:Math.Max(0,Math.Min(1,health/previousMax))*nextMax;
    }
}
