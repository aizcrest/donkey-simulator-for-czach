using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class DifficultyTests
    {
        [Test] public void NormalPreservesOriginalBalanceAndUnknownSavesFallBackToIt()
        {
            var normal=DifficultyRules.Get(0);
            Assert.AreEqual("Normal",normal.Name);
            Assert.AreEqual(1,normal.HealthMultiplier);Assert.AreEqual(1,normal.DamageMultiplier);
            Assert.AreEqual(2.1f,normal.FireballRadius);Assert.AreEqual(1.2f,normal.FireballTravelSeconds);
            Assert.AreEqual(5,new DragonPattern().StunDuration);
            foreach(int invalid in new[]{-1,3,int.MinValue,int.MaxValue})
            {Assert.AreEqual(0,DifficultyRules.Normalize(invalid));Assert.AreSame(normal,DifficultyRules.Get(invalid));}
        }
        [Test] public void HardIsBetweenNormalAndExtremeForEveryCombatModifier()
        {
            var normal=DifficultyRules.Get(0);var hard=DifficultyRules.Get(1);var extreme=DifficultyRules.Get(2);
            Assert.Greater(hard.HealthMultiplier,normal.HealthMultiplier);Assert.Greater(extreme.HealthMultiplier,hard.HealthMultiplier);
            Assert.Greater(hard.DamageMultiplier,normal.DamageMultiplier);Assert.Greater(extreme.DamageMultiplier,hard.DamageMultiplier);
            Assert.Greater(hard.FireballRadius,normal.FireballRadius);Assert.Greater(extreme.FireballRadius,hard.FireballRadius);
            Assert.Less(hard.FireballTravelSeconds,normal.FireballTravelSeconds);Assert.Less(extreme.FireballTravelSeconds,hard.FireballTravelSeconds);
            Assert.Less(hard.DragonStunSeconds,normal.DragonStunSeconds);Assert.Less(extreme.DragonStunSeconds,hard.DragonStunSeconds);
            Assert.That(extreme.DragonStunSeconds,Is.InRange(2f,3f));
            Assert.GreaterOrEqual(normal.FireballTravelSeconds/extreme.FireballTravelSeconds,2);
        }
        [TestCase(0,5d)] [TestCase(1,3.75d)] [TestCase(2,2.5d)]
        public void EachDifficultyRepeatsTenSecondAerialDiveAndItsOwnStun(int difficulty,double stun)
        {
            var pattern=new DragonPattern(difficulty);
            for(int cycle=0;cycle<3;cycle++)
            {
                Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);Assert.IsFalse(pattern.Vulnerable);
                pattern.Tick(10);Assert.AreEqual(DragonPhase.DiveWarning,pattern.Phase);
                pattern.Tick(1.5);Assert.AreEqual(DragonPhase.Diving,pattern.Phase);
                pattern.Tick(.8);Assert.IsTrue(pattern.Vulnerable);Assert.AreEqual(stun,pattern.Remaining,1e-6);
                pattern.Tick(stun-.125);Assert.IsTrue(pattern.Vulnerable);
                pattern.Tick(.125);Assert.IsFalse(pattern.Vulnerable);Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);
            }
        }
        [Test] public void LargeFrameCarriesIntoNextCycleWithShortExtremeStun()
        {
            var pattern=new DragonPattern(2);int transitions=0;pattern.Changed+=_=>transitions++;
            pattern.Tick(15.3); // 10 + 1.5 + .8 + 2.5, then .5 seconds airborne.
            Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);Assert.AreEqual(.5,pattern.Elapsed,1e-6);
            Assert.AreEqual(4,transitions);Assert.IsFalse(pattern.Vulnerable);
        }
        [Test] public void SwitchingDifficultyPreservesEnemyInjuryAndCannotReviveDeadEnemies()
        {
            float health=30,max=100;
            foreach(int mode in new[]{1,2,0,2,1,0})
            {
                float nextMax=100*DifficultyRules.Get(mode).HealthMultiplier;
                health=DifficultyRules.RescaleHealth(health,max,nextMax);max=nextMax;
                Assert.AreEqual(.3f,health/max,1e-6);
            }
            Assert.AreEqual(30,health,1e-4);
            Assert.AreEqual(0,DifficultyRules.RescaleHealth(0,100,200));
            Assert.AreEqual(0,DifficultyRules.RescaleHealth(-10,100,200));
        }
    }
}
