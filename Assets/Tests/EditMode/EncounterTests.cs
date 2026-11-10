using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class EncounterTests
    {
        [Test] public void DragonRepeatsExactAttackOrderAndVulnerability()
        {
            var pattern=new DragonPattern();
            Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);Assert.IsFalse(pattern.Vulnerable);
            pattern.Tick(9);Assert.AreEqual(1,pattern.Remaining);
            pattern.Tick(1);Assert.AreEqual(DragonPhase.DiveWarning,pattern.Phase);
            pattern.Tick(1.5);Assert.AreEqual(DragonPhase.Diving,pattern.Phase);Assert.IsFalse(pattern.Vulnerable);
            pattern.Tick(.8);Assert.AreEqual(DragonPhase.Stunned,pattern.Phase);Assert.IsTrue(pattern.Vulnerable);
            pattern.Tick(4);Assert.IsTrue(pattern.Vulnerable);Assert.AreEqual(1,pattern.Remaining);
            pattern.Tick(1);Assert.AreEqual(DragonPhase.Airborne,pattern.Phase);Assert.IsFalse(pattern.Vulnerable);
        }
        [Test] public void LargeFramePreservesOvershootAndTransitionEvents()
        {
            var pattern=new DragonPattern();int transitions=0;pattern.Changed+=_=>transitions++;
            pattern.Tick(12);Assert.AreEqual(DragonPhase.Diving,pattern.Phase);
            Assert.AreEqual(.5,pattern.Elapsed,1e-6);Assert.AreEqual(2,transitions);
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>pattern.Tick(-1));
        }
        [Test] public void TenUniqueGemsAndPartialDepositsCannotDuplicateProgress()
        {
            int mask=0,placed=0;
            for(int i=0;i<4;i++)Assert.IsTrue(GemRules.TryCollect(ref mask,i));
            placed=GemRules.Deposit(mask,placed);Assert.AreEqual(4,placed);Assert.AreEqual(0,GemRules.Held(mask,placed));
            Assert.IsFalse(GemRules.TryCollect(ref mask,0));Assert.IsFalse(GemRules.TryCollect(ref mask,10));
            for(int i=4;i<10;i++)Assert.IsTrue(GemRules.TryCollect(ref mask,i));
            Assert.AreEqual(6,GemRules.Held(mask,placed));Assert.AreEqual(10,GemRules.Deposit(mask,placed));
            Assert.AreEqual(GemRules.FullMask,mask);Assert.AreEqual(10,GemRules.Count(mask));
        }
    }
}
