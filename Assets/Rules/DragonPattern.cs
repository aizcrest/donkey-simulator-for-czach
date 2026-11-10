using System;
namespace DonkeyForest
{
    public enum DragonPhase { Airborne, DiveWarning, Diving, Stunned }
    /// <summary>Engine-independent encounter clock, advanced only during active gameplay.</summary>
    public sealed class DragonPattern
    {
        public const double AirSeconds=10, WarningSeconds=1.5, DiveSeconds=.8, StunSeconds=5;
        public DragonPhase Phase { get; private set; } = DragonPhase.Airborne;
        public double Elapsed { get; private set; }
        public double Duration => Phase==DragonPhase.Airborne?AirSeconds:Phase==DragonPhase.DiveWarning?WarningSeconds:Phase==DragonPhase.Diving?DiveSeconds:StunSeconds;
        public double Remaining => Math.Max(0,Duration-Elapsed);
        public bool Vulnerable => Phase==DragonPhase.Stunned;
        public event Action<DragonPhase> Changed;
        public void Tick(double delta)
        {
            if(double.IsNaN(delta)||double.IsInfinity(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
            Elapsed+=delta;
            while(Elapsed>=Duration)
            {
                Elapsed-=Duration;
                Phase=(DragonPhase)(((int)Phase+1)%4);
                Changed?.Invoke(Phase);
            }
        }
    }
    public static class GemRules
    {
        public const int Required=10;
        public const int FullMask=(1<<Required)-1;
        public static int Count(int mask)
        {
            mask &= FullMask;int total=0;
            while(mask!=0){total+=mask&1;mask>>=1;}return total;
        }
        public static bool TryCollect(ref int mask,int id)
        {
            if(id<0||id>=Required||(mask&(1<<id))!=0)return false;
            mask|=1<<id;return true;
        }
        public static int Deposit(int mask,int placed) => Math.Min(Required,Math.Max(Math.Max(0,placed),Count(mask)));
        public static int Held(int mask,int placed) => Math.Max(0,Count(mask)-placed);
    }
}
