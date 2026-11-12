using System;
namespace DonkeyForest
{
    public enum SkillId { Whirlwind, Guard, Charge, Kick }
    [Serializable] public sealed class SkillSave
    {
        public int[] levels={1,1,1,1};
        public float[] cooldowns=new float[4];
    }
    public static class SkillRules
    {
        public const int Count=4,MaxLevel=5;
        public static string Name(SkillId id) => id==SkillId.Whirlwind?"Whirlwind":id==SkillId.Guard?"Iron Guard":id==SkillId.Charge?"Donkey Charge":"Rear Kick";
        public static float Cooldown(SkillId id,int level) => id==SkillId.Whirlwind?8-.5f*(level-1):id==SkillId.Guard?16-(level-1):id==SkillId.Charge?12-.75f*(level-1):7-.5f*(level-1);
        public static float Duration(SkillId id,int level) => id==SkillId.Whirlwind?.45f:id==SkillId.Guard?4+.25f*(level-1):id==SkillId.Charge?.8f+.05f*(level-1):.4f;
        public static float Radius(SkillId id,int level) => id==SkillId.Whirlwind?4.25f+.25f*(level-1):id==SkillId.Kick?3.2f+.15f*(level-1):1.9f;
        public static float Damage(SkillId id,int level,float weapon) => weapon*(id==SkillId.Whirlwind?1.35f+.25f*(level-1):id==SkillId.Charge?1.5f+.2f*(level-1):id==SkillId.Kick?1.2f+.15f*(level-1):0);
        public static float GuardMultiplier(int level) => .5f-.05f*(level-1);
        public static int UpgradeCost(int level) => 60*level;
        public static void Normalize(SkillSave save)
        {
            var levels=new int[Count];var timers=new float[Count];
            for(int i=0;i<Count;i++)
            {
                levels[i]=save.levels!=null&&i<save.levels.Length?Math.Max(1,Math.Min(MaxLevel,save.levels[i])):1;
                float time=save.cooldowns!=null&&i<save.cooldowns.Length?save.cooldowns[i]:0;
                timers[i]=float.IsNaN(time)||float.IsInfinity(time)?0:Math.Max(0,Math.Min(Cooldown((SkillId)i,levels[i]),time));
            }
            save.levels=levels;save.cooldowns=timers;
        }
    }
    public sealed class SkillBook
    {
        public SkillSave Save { get; }
        readonly float[] active=new float[SkillRules.Count];
        public SkillBook(SkillSave save){Save=save??new SkillSave();SkillRules.Normalize(Save);}
        public int Level(SkillId id)=>Save.levels[(int)id];
        public float Remaining(SkillId id)=>Save.cooldowns[(int)id];
        public float Active(SkillId id)=>active[(int)id];
        public bool TryUse(SkillId id,bool playable)
        {
            int index=(int)id;
            if(!playable||Remaining(id)>0)return false;
            Save.cooldowns[index]=SkillRules.Cooldown(id,Level(id));active[index]=SkillRules.Duration(id,Level(id));return true;
        }
        public void Tick(float dt,bool playable)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)throw new ArgumentOutOfRangeException(nameof(dt));
            if(!playable)return;
            for(int i=0;i<active.Length;i++){active[i]=Math.Max(0,active[i]-dt);Save.cooldowns[i]=Math.Max(0,Save.cooldowns[i]-dt);}
        }
        public void CancelEffects(){Array.Clear(active,0,active.Length);}
        public bool TryUpgrade(SkillId id,ref int coins,ref int points)
        {
            int level=Level(id),cost=SkillRules.UpgradeCost(level);
            if(level>=SkillRules.MaxLevel||coins<cost||points<1)return false;
            coins-=cost;points--;Save.levels[(int)id]++;return true;
        }
    }
    [Serializable] public sealed class JourneySave
    {
        public int level=1,xp,skillPoints=1,hunts,zombies,claimed;
    }
    public enum ContractId { ForestHunt, NightWatch, GemSeeker, DragonSlayer }
    public static class JourneyRules
    {
        public const int MaxLevel=20;
        public static int Needed(int level)=>80+40*(level-1);
        public static void Normalize(JourneySave save)
        {
            save.level=Math.Max(1,Math.Min(MaxLevel,save.level));save.xp=Math.Max(0,Math.Min(Needed(save.level)-1,save.xp));
            save.skillPoints=Math.Max(0,Math.Min(100,save.skillPoints));save.hunts=Math.Max(0,save.hunts);save.zombies=Math.Max(0,save.zombies);save.claimed&=15;
            if(save.level==MaxLevel)save.xp=0;
        }
        public static int GainXP(JourneySave save,int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            if(save.level==MaxLevel)return 0;
            save.xp=(int)Math.Min(int.MaxValue,(long)save.xp+amount);int gained=0;
            while(save.level<MaxLevel&&save.xp>=Needed(save.level))
            {save.xp-=Needed(save.level);save.level++;save.skillPoints++;gained++;}
            if(save.level==MaxLevel)save.xp=0;return gained;
        }
        public static int XP(string kind,bool elite)
        {int value=kind=="Moose"?36:kind=="Bear"?24:kind=="Wolf"?22:kind=="Zombie"?25:kind=="Fox"?12:14;return value*(elite?2:1);}
        public static string ContractName(ContractId id)=>id==ContractId.ForestHunt?"Forest Hunt: defeat 5 animals":id==ContractId.NightWatch?"Night Watch: defeat 3 zombies":id==ContractId.GemSeeker?"Gem Seeker: find 4 gems":"Dragon Slayer: defeat the dragon";
        public static int Goal(ContractId id)=>id==ContractId.ForestHunt?5:id==ContractId.NightWatch?3:id==ContractId.GemSeeker?4:1;
        public static int Progress(JourneySave save,ContractId id,int gemCount,bool dragonDefeated)=>id==ContractId.ForestHunt?save.hunts:id==ContractId.NightWatch?save.zombies:id==ContractId.GemSeeker?gemCount:dragonDefeated?1:0;
        public static bool Claimed(JourneySave save,ContractId id)=>(save.claimed&(1<<(int)id))!=0;
        public static bool Claim(JourneySave save,ContractId id,int gemCount,bool dragonDefeated,ref int coins,ref int potions)
        {
            if(Claimed(save,id)||Progress(save,id,gemCount,dragonDefeated)<Goal(id))return false;
            save.claimed|=1<<(int)id;save.skillPoints+=id==ContractId.DragonSlayer?2:1;
            coins+=id==ContractId.DragonSlayer?200:80;potions++;GainXP(save,id==ContractId.DragonSlayer?200:60);return true;
        }
    }
}
