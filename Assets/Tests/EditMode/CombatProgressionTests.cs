using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class CombatProgressionTests
    {
        [TestCase(0,5)] [TestCase(1,3)] [TestCase(2,2)]
        public void PlayerHitsDrainByDifficultyAndExtendCurrentStunExactlyOnce(int mode,int cost)
        {
            var pattern=new DragonPattern(mode);
            Assert.IsFalse(pattern.RegisterPlayerHit());Assert.AreEqual(100,pattern.Stamina);
            pattern.Tick(10);Assert.IsFalse(pattern.RegisterPlayerHit());
            pattern.Tick(1.5);Assert.IsFalse(pattern.RegisterPlayerHit());
            pattern.Tick(.8);Assert.AreEqual(100,pattern.Stamina,"Own attack cycle must not drain stamina.");
            pattern.Tick(1);double before=pattern.Remaining;
            int hits=(100+cost-1)/cost;
            for(int i=0;i<hits-1;i++)Assert.IsFalse(pattern.RegisterPlayerHit());
            Assert.AreEqual(100-(hits-1)*cost,pattern.Stamina);
            Assert.IsTrue(pattern.RegisterPlayerHit());Assert.AreEqual(0,pattern.Stamina);
            Assert.AreEqual(before+2,pattern.Remaining,1e-6);
            for(int i=0;i<20;i++)Assert.IsFalse(pattern.RegisterPlayerHit());
            Assert.AreEqual(before+2,pattern.Remaining,1e-6);
            pattern.Tick(pattern.Remaining+.25);
            Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);Assert.AreEqual(100,pattern.Stamina);
            Assert.AreEqual(.25,pattern.Elapsed,1e-6);Assert.AreEqual(DifficultyRules.Get(mode).DragonStunSeconds,pattern.StunDuration);
        }
        [Test] public void PartialStaminaPersistsAndOwnAttacksNeverDrainIt()
        {
            var pattern=new DragonPattern();pattern.Tick(12.3);
            pattern.RegisterPlayerHit();Assert.AreEqual(95,pattern.Stamina);
            pattern.Tick(5);Assert.AreEqual(95,pattern.Stamina);
            pattern.Tick(10);Assert.AreEqual(95,pattern.Stamina);
            pattern.Tick(1.5);Assert.AreEqual(95,pattern.Stamina);
            pattern.Tick(.8);Assert.AreEqual(95,pattern.Stamina);
            pattern.Tick(0);Assert.AreEqual(5,pattern.Remaining,1e-6);
            pattern.RegisterPlayerHit();Assert.AreEqual(90,pattern.Stamina);
        }
        [TestCase(0,10)] [TestCase(1,8)] [TestCase(2,4)]
        public void MonsterDropRatesAndThresholdsAreExact(int mode,int percent)
        {
            var data=new Progress{difficulty=mode};float chance=DifficultyRules.Get(mode).OrbDropChance;
            Assert.AreEqual(percent/100f,chance);
            Assert.AreEqual(0,GemRules.RollMonsterDrop(data,0,0));
            Assert.AreEqual(0,GemRules.RollMonsterDrop(data,0,chance-.00001f));
            Assert.AreEqual(-1,GemRules.RollMonsterDrop(data,0,chance));
            foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f,1f})Assert.AreEqual(-1,GemRules.RollMonsterDrop(data,0,invalid));
            int drops=0;
            for(int i=0;i<10000;i++)if(GemRules.RollMonsterDrop(data,0,(i+.5f)/10000)>=0)drops++;
            Assert.AreEqual(percent*100,drops);
        }
        [Test] public void PendingDropsReserveIdsAndCannotExceedTenOrbs()
        {
            var data=new Progress{gemMask=(1<<7)|(1<<9)};GemRules.DepositAll(data);
            int pending=0;
            for(int i=0;i<8;i++)
            {
                int id=GemRules.RollMonsterDrop(data,pending,0);Assert.GreaterOrEqual(id,0);
                Assert.AreEqual(0,(data.gemMask|pending)&(1<<id));pending|=1<<id;
            }
            Assert.AreEqual(-1,GemRules.RollMonsterDrop(data,pending,0));
            for(int i=0;i<10;i++)if((pending&(1<<i))!=0)Assert.IsTrue(GemRules.TryCollect(ref data.gemMask,i));
            Assert.AreEqual(-1,GemRules.RollMonsterDrop(data,0,0));
            data.dragonDefeated=true;data.gemMask=0;Assert.AreEqual(-1,GemRules.RollMonsterDrop(data,0,0));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void HardAndExtremeIncreaseCrowdsWithinQualityBudget(int quality)
        {
            int previousStart=0,previousCap=0;
            for(int mode=0;mode<3;mode++)
            {
                int start=DifficultyRules.InitialEnemies(quality,mode),cap=DifficultyRules.EnemyCap(quality,mode);
                Assert.Greater(start,previousStart);Assert.Greater(cap,previousCap);Assert.LessOrEqual(start,cap);
                previousStart=start;previousCap=cap;
            }
            Assert.LessOrEqual(DifficultyRules.EnemyCap(0,2),20);
            Assert.AreEqual(quality==0?12:quality==1?18:22,DifficultyRules.EnemyCap(quality,0));
        }
        [Test] public void DiveRadiusIncreasesWithDifficultyAndExceedsOldRadius()
        {
            float last=3;
            for(int mode=0;mode<3;mode++){Assert.Greater(DifficultyRules.Get(mode).DiveRadius,last);last=DifficultyRules.Get(mode).DiveRadius;}
        }
        [TestCase(0)] [TestCase(1)]
        public void NormalAndHardDeathsKeepCarriedGems(int mode)
        {
            var data=new Progress{difficulty=mode,gemMask=0b101011,gemsPlaced=2};
            Assert.AreEqual(0,GemRules.DropHeldOnDeath(data));Assert.AreEqual(0b101011,data.gemMask);
            Assert.AreEqual(2,data.gemsPlaced);
        }
        [Test] public void EveryLegacyGemSubsetProtectsOfferingsAndAllowsRecoveryWithoutDuplication()
        {
            for(int mask=0;mask<=GemRules.FullMask;mask++)for(int placed=0;placed<=GemRules.Count(mask);placed++)
            {
                var data=new Progress{difficulty=2,gemMask=mask,gemsPlaced=placed};
                int dropped=GemRules.DropHeldOnDeath(data);
                Assert.AreEqual(placed,GemRules.Count(data.gemMask));Assert.AreEqual(placed,data.gemsPlaced);
                Assert.AreEqual(GemRules.Count(mask)-placed,GemRules.Count(dropped));
                Assert.AreEqual(0,data.gemDepositedMask&dropped);Assert.AreEqual(0,GemRules.Held(data.gemMask,placed));
                Assert.AreEqual(0,GemRules.DropHeldOnDeath(data));
                for(int id=0;id<10;id++)if((dropped&(1<<id))!=0)
                {Assert.IsTrue(GemRules.TryCollect(ref data.gemMask,id));Assert.IsFalse(GemRules.TryCollect(ref data.gemMask,id));}
                Assert.AreEqual(mask,data.gemMask);GemRules.DepositAll(data);
                Assert.AreEqual(GemRules.Count(mask),data.gemsPlaced);Assert.AreEqual(0,GemRules.DropHeldOnDeath(data));
            }
        }
        [Test] public void ExtremeLossRetainsExactAltarIdsAndRequiresNewDrops()
        {
            var data=new Progress{difficulty=2,gemMask=(1<<9)|(1<<7)};GemRules.DepositAll(data);
            GemRules.TryCollect(ref data.gemMask,0);GemRules.TryCollect(ref data.gemMask,4);
            Assert.AreEqual((1<<0)|(1<<4),GemRules.DropHeldOnDeath(data));
            GemRules.NormalizeState(data);
            Assert.AreEqual((1<<9)|(1<<7),data.gemMask);Assert.AreEqual(2,data.gemsPlaced);
            Assert.AreEqual(0,GemRules.RollMonsterDrop(data,0,0));
            Assert.AreEqual(0,GemRules.Held(data.gemMask,data.gemsPlaced));
            var reset=Progress.NewAdventure(data);
            Assert.AreEqual(0,reset.gemMask);Assert.AreEqual(0,reset.gemDepositedMask);
        }
    }
}
