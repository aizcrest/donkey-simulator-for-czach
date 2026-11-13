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
        internal DifficultyProfile(string name,float health,float damage,float radius,float travel,float stun)
        {Name=name;HealthMultiplier=health;DamageMultiplier=damage;FireballRadius=radius;FireballTravelSeconds=travel;DragonStunSeconds=stun;}
    }
    public static class DifficultyRules
    {
        // Index zero preserves the original balance and is the default for older saves.
        static readonly DifficultyProfile[] profiles={
            new DifficultyProfile("Normal",1,1,2.1f,1.2f,5),
            new DifficultyProfile("Hard",1.5f,1.35f,2.7f,.85f,3.75f),
            new DifficultyProfile("Extreme",2,1.75f,3.3f,.5f,2.5f)
        };
        public static int Normalize(int value)=>value>=0&&value<profiles.Length?value:0;
        public static DifficultyProfile Get(int value)=>profiles[Normalize(value)];
        // Changing settings preserves health percentage and cannot revive a defeated enemy.
        public static float RescaleHealth(float health,float previousMax,float nextMax)
            =>previousMax<=0?0:Math.Max(0,Math.Min(1,health/previousMax))*nextMax;
    }
}
