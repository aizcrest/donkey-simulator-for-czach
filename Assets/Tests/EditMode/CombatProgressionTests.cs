using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class CombatProgressionTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ExhaustionAddsExactlyTwoSecondsAndRefillsOnlyAfterStun(int mode)
        {
            var pattern=new DragonPattern(mode);
            for(int i=0;i<7;i++)pattern.FireballAttack();
            Assert.AreEqual(30,pattern.Stamina);
            pattern.Tick(10);pattern.Tick(1.5);pattern.DiveAttack();
            Assert.AreEqual(0,pattern.Stamina);
            pattern.Tick(.8);Assert.AreEqual(DragonPhase.Stunned,pattern.Phase);
            Assert.AreEqual(DifficultyRules.Get(mode).DragonStunSeconds+2,pattern.Remaining,1e-6);
            pattern.FireballAttack();pattern.DiveAttack();pattern.Tick(0);
            Assert.AreEqual(0,pattern.Stamina);
            pattern.Tick(pattern.Remaining-.1);Assert.IsTrue(pattern.Vulnerable);
            pattern.Tick(.1+1e-8);Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);
            Assert.AreEqual(100,pattern.Stamina);
            Assert.AreEqual(DifficultyRules.Get(mode).DragonStunSeconds,pattern.StunDuration);
        }
        [Test] public void PartialStaminaCarriesAcrossCyclesAndNeverGoesNegative()
        {
            var pattern=new DragonPattern();
            for(int i=0;i<6;i++)pattern.FireballAttack();
            pattern.Tick(11.5);pattern.DiveAttack();pattern.Tick(.8);
            Assert.AreEqual(10,pattern.Stamina);Assert.AreEqual(5,pattern.Remaining);
            pattern.Tick(5);Assert.AreEqual(10,pattern.Stamina);
            for(int i=0;i<30;i++)pattern.FireballAttack();
            Assert.AreEqual(0,pattern.Stamina);
            pattern.Tick(12.3);Assert.AreEqual(7,pattern.Remaining,1e-6);
            pattern.Tick(7.25);Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);
            Assert.AreEqual(.25,pattern.Elapsed,1e-6);Assert.AreEqual(100,pattern.Stamina);
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
            Assert.AreEqual(2,data.gemsPlaced);Assert.AreEqual(0,data.scatteredGemMask);
        }
        [Test] public void EveryLegacyGemSubsetProtectsOfferingsAndAllowsRecoveryWithoutDuplication()
        {
            for(int mask=0;mask<=GemRules.FullMask;mask++)for(int placed=0;placed<=GemRules.Count(mask);placed++)
            {
                var data=new Progress{difficulty=2,gemMask=mask,gemsPlaced=placed,gemX=null,gemZ=null};
                int dropped=GemRules.DropHeldOnDeath(data);
                Assert.AreEqual(placed,GemRules.Count(data.gemMask));Assert.AreEqual(placed,data.gemsPlaced);
                Assert.AreEqual(GemRules.Count(mask)-placed,GemRules.Count(dropped));
                Assert.AreEqual(0,data.gemDepositedMask&dropped);Assert.AreEqual(0,GemRules.Held(data.gemMask,placed));
                Assert.AreEqual(0,GemRules.DropHeldOnDeath(data));Assert.AreEqual(10,data.gemX.Length);
                for(int id=0;id<10;id++)if((dropped&(1<<id))!=0)
                {Assert.IsTrue(GemRules.TryCollect(ref data.gemMask,id));Assert.IsFalse(GemRules.TryCollect(ref data.gemMask,id));}
                Assert.AreEqual(mask,data.gemMask);GemRules.DepositAll(data);
                Assert.AreEqual(GemRules.Count(mask),data.gemsPlaced);Assert.AreEqual(0,GemRules.DropHeldOnDeath(data));
            }
        }
        [Test] public void NewSavesRetainExactDepositedIdsAndScatteredCoordinates()
        {
            var data=new Progress{difficulty=2,gemMask=(1<<9)|(1<<7)};GemRules.DepositAll(data);
            GemRules.TryCollect(ref data.gemMask,0);GemRules.TryCollect(ref data.gemMask,4);
            Assert.AreEqual((1<<0)|(1<<4),GemRules.DropHeldOnDeath(data));
            data.gemX[4]=45;data.gemZ[4]=-23;
            GemRules.NormalizeState(data);
            Assert.AreEqual((1<<9)|(1<<7),data.gemMask);Assert.AreEqual(2,data.gemsPlaced);
            Assert.AreEqual(45,data.gemX[4]);Assert.AreEqual(-23,data.gemZ[4]);
            var reset=Progress.NewAdventure(data);
            Assert.AreEqual(0,reset.scatteredGemMask);Assert.AreEqual(0,reset.gemDepositedMask);
            Assert.AreEqual(0,reset.gemX[4]);Assert.AreNotSame(data.gemX,reset.gemX);
        }
    }
}
