using System;
namespace DonkeyForest
{
    public enum DragonPhase { Airborne, DiveWarning, Diving, Stunned }
    /// <summary>Engine-independent encounter clock, advanced only during active gameplay.</summary>
    public sealed class DragonPattern
    {
        public const double AirSeconds=10, WarningSeconds=1.5, DiveSeconds=.8;
        public const int MaxStamina=100,FireballCost=10,DiveCost=30;
        public const double ExhaustionBonus=2;
        readonly double baseStunDuration;
        bool exhaustedStun;
        public int Stamina { get; private set; }=MaxStamina;
        public double StunDuration => baseStunDuration+(exhaustedStun?ExhaustionBonus:0);
        public DragonPattern(int difficulty=0){baseStunDuration=DifficultyRules.Get(difficulty).DragonStunSeconds;}
        public void FireballAttack(){if(Phase==DragonPhase.Airborne)Stamina=Math.Max(0,Stamina-FireballCost);}
        public void DiveAttack(){if(Phase==DragonPhase.Diving)Stamina=Math.Max(0,Stamina-DiveCost);}
        public DragonPhase Phase { get; private set; } = DragonPhase.Airborne;
        public double Elapsed { get; private set; }
        public double Duration => Phase==DragonPhase.Airborne?AirSeconds:Phase==DragonPhase.DiveWarning?WarningSeconds:Phase==DragonPhase.Diving?DiveSeconds:StunDuration;
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
                if(Phase==DragonPhase.Stunned)exhaustedStun=Stamina==0;
                if(Phase==DragonPhase.Airborne&&exhaustedStun){Stamina=MaxStamina;exhaustedStun=false;}
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
        public static void NormalizeState(Progress data)
        {
            data.gemMask&=FullMask;data.gemsPlaced=Math.Max(0,Math.Min(Count(data.gemMask),data.gemsPlaced));
            int offered=data.gemDepositedMask&data.gemMask&FullMask;
            // Older saves tracked only the altar count: reserve that many collected IDs.
            for(int i=Required-1;Count(offered)>data.gemsPlaced;i--)offered&=~(1<<i);
            for(int i=0;i<Required&&Count(offered)<data.gemsPlaced;i++)if((data.gemMask&(1<<i))!=0)offered|=1<<i;
            data.gemDepositedMask=offered;data.scatteredGemMask&=FullMask;
            if(data.gemX==null||data.gemX.Length!=Required)data.gemX=new float[Required];
            if(data.gemZ==null||data.gemZ.Length!=Required)data.gemZ=new float[Required];
        }
        public static int DropHeldOnDeath(Progress data)
        {
            NormalizeState(data);
            if(DifficultyRules.Normalize(data.difficulty)!=2)return 0;
            int dropped=data.gemMask&~data.gemDepositedMask;
            data.gemMask=data.gemDepositedMask;data.scatteredGemMask|=dropped;
            return dropped;
        }
        public static void DepositAll(Progress data)
        {
            NormalizeState(data);data.gemsPlaced=Count(data.gemMask);data.gemDepositedMask=data.gemMask;
        }
    }
}
