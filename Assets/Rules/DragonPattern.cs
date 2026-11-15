using System;
namespace DonkeyForest
{
    public enum DragonPhase { Airborne, DiveWarning, Diving, Stunned }
    /// <summary>Engine-independent encounter clock, advanced only during active gameplay.</summary>
    public sealed class DragonPattern
    {
        public const double AirSeconds=10, WarningSeconds=1.5, DiveSeconds=.8;
        public const int MaxStamina=100;
        public const double ExhaustionBonus=2;
        readonly double baseStunDuration;
        readonly int staminaDamagePerHit;
        bool exhaustedStun;
        public int Stamina { get; private set; }=MaxStamina;
        public double StunDuration => baseStunDuration+(exhaustedStun?ExhaustionBonus:0);
        public DragonPattern(int difficulty=0)
        {var profile=DifficultyRules.Get(difficulty);baseStunDuration=profile.DragonStunSeconds;staminaDamagePerHit=profile.StaminaDamagePerHit;}
        // Only a confirmed, damaging player hit calls this. Extend this stun once.
        public bool RegisterPlayerHit()
        {
            if(!Vulnerable||Stamina==0)return false;
            Stamina=Math.Max(0,Stamina-staminaDamagePerHit);
            if(Stamina!=0)return false;
            exhaustedStun=true;return true;
        }
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
            data.gemDepositedMask=offered;
        }
        public static int DropHeldOnDeath(Progress data)
        {
            NormalizeState(data);
            if(DifficultyRules.Normalize(data.difficulty)!=2)return 0;
            int dropped=data.gemMask&~data.gemDepositedMask;
            data.gemMask=data.gemDepositedMask;
            return dropped;
        }
        // Reserve missing IDs for ground drops, so simultaneous kills cannot
        // produce duplicate orbs or more than the ten needed by the altar.
        public static int RollMonsterDrop(Progress data,int pendingMask,float roll)
        {
            if(data.dragonDefeated||float.IsNaN(roll)||roll<0||roll>=DifficultyRules.Get(data.difficulty).OrbDropChance)return -1;
            int unavailable=(data.gemMask|pendingMask)&FullMask;
            for(int id=0;id<Required;id++)if((unavailable&(1<<id))==0)return id;
            return -1;
        }
        public static void DepositAll(Progress data)
        {
            NormalizeState(data);data.gemsPlaced=Count(data.gemMask);data.gemDepositedMask=data.gemMask;
        }
    }
}
