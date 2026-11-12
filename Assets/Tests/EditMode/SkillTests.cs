using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class SkillTests
    {
        [Test] public void CooldownsRejectSpamAndPausePreservesBothTimers()
        {
            var book=new SkillBook(new SkillSave());
            Assert.IsFalse(book.TryUse(SkillId.Guard,false));Assert.AreEqual(0,book.Remaining(SkillId.Guard));
            Assert.IsTrue(book.TryUse(SkillId.Guard,true));Assert.IsFalse(book.TryUse(SkillId.Guard,true));
            book.Tick(10,false);Assert.AreEqual(4,book.Active(SkillId.Guard));Assert.AreEqual(16,book.Remaining(SkillId.Guard));
            book.Tick(4,true);Assert.AreEqual(0,book.Active(SkillId.Guard));Assert.AreEqual(12,book.Remaining(SkillId.Guard));
            book.Tick(12,true);Assert.IsTrue(book.TryUse(SkillId.Guard,true));
        }
        [Test] public void ReloadKeepsCooldownsButCancelsActiveBuffs()
        {
            var save=new SkillSave();var book=new SkillBook(save);book.TryUse(SkillId.Charge,true);book.Tick(.3f,true);
            var loaded=new SkillBook(save);Assert.Greater(loaded.Remaining(SkillId.Charge),11);Assert.AreEqual(0,loaded.Active(SkillId.Charge));
            Assert.IsFalse(loaded.TryUse(SkillId.Charge,true));loaded.CancelEffects();Assert.Greater(loaded.Remaining(SkillId.Charge),11);
        }
        [Test] public void UpgradeNeedsBothCurrencyAndPointAndHonorsCap()
        {
            var book=new SkillBook(new SkillSave());int coins=10000,points=0;
            Assert.IsFalse(book.TryUpgrade(SkillId.Kick,ref coins,ref points));Assert.AreEqual(10000,coins);
            points=4;for(int i=0;i<4;i++)Assert.IsTrue(book.TryUpgrade(SkillId.Kick,ref coins,ref points));
            Assert.AreEqual(5,book.Level(SkillId.Kick));Assert.AreEqual(0,points);Assert.AreEqual(9400,coins);
            points=1;Assert.IsFalse(book.TryUpgrade(SkillId.Kick,ref coins,ref points));Assert.AreEqual(1,points);
            Assert.Greater(SkillRules.Damage(SkillId.Kick,5,25),SkillRules.Damage(SkillId.Kick,1,25));
        }
        [Test] public void OldOrDamagedSkillSavesHaveSafeDefaults()
        {
            var save=new SkillSave{levels=new[]{0,999},cooldowns=new[]{float.NaN,-3,float.PositiveInfinity,999}};
            var book=new SkillBook(save);Assert.AreEqual(1,book.Level(SkillId.Whirlwind));Assert.AreEqual(5,book.Level(SkillId.Guard));Assert.AreEqual(1,book.Level(SkillId.Charge));
            Assert.AreEqual(0,book.Remaining(SkillId.Whirlwind));Assert.AreEqual(0,book.Remaining(SkillId.Guard));Assert.AreEqual(7,book.Remaining(SkillId.Kick));
        }
        [Test] public void ExperienceLevelsMultipleTimesAndStopsAtCap()
        {
            var save=new JourneySave();Assert.AreEqual(2,JourneyRules.GainXP(save,210));
            Assert.AreEqual(3,save.level);Assert.AreEqual(10,save.xp);Assert.AreEqual(3,save.skillPoints);
            JourneyRules.GainXP(save,int.MaxValue);Assert.AreEqual(20,save.level);Assert.AreEqual(0,save.xp);
            Assert.AreEqual(0,JourneyRules.GainXP(save,1000));
        }
        [Test] public void ContractsCannotBeClaimedEarlyOrTwice()
        {
            var save=new JourneySave();int coins=0,potions=0;
            Assert.IsFalse(JourneyRules.Claim(save,ContractId.ForestHunt,0,false,ref coins,ref potions));
            save.hunts=5;Assert.IsTrue(JourneyRules.Claim(save,ContractId.ForestHunt,0,false,ref coins,ref potions));
            Assert.IsFalse(JourneyRules.Claim(save,ContractId.ForestHunt,0,false,ref coins,ref potions));
            Assert.AreEqual(80,coins);Assert.AreEqual(1,potions);Assert.AreEqual(2,save.skillPoints);
            Assert.IsTrue(JourneyRules.Claim(save,ContractId.GemSeeker,4,false,ref coins,ref potions));
            Assert.IsFalse(JourneyRules.Claim(save,ContractId.DragonSlayer,10,false,ref coins,ref potions));
        }
    }
}
