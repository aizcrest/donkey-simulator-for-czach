using System;
using System.IO;
using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class ProgressResetTests
    {
        [Test] public void NewAdventureResetsProgressAndPreservesOnlyPreferences()
        {
            var old=new Progress {kills=50,coins=900,sword=7,armor=8,barding=9,potions=12,health=0,time=.8f,x=30,z=-40,
                gemMask=1023,gemsPlaced=10,dragonDefeated=true,quality=2,difficulty=2,masterVolume=.4f,musicVolume=.1f,effectsVolume=.6f};
            old.skills.levels=new[]{5,4,3,2};old.skills.cooldowns=new[]{8f,9f,10f,6f};
            old.journey=new JourneySave{level=15,xp=123,skillPoints=8,hunts=30,zombies=20,claimed=15};
            var fresh=Progress.NewAdventure(old);var defaults=new Progress();
            foreach(var field in typeof(Progress).GetFields())
            {
                bool preference=field.Name=="quality"||field.Name=="difficulty"||field.Name.EndsWith("Volume");
                if(field.FieldType.IsValueType)Assert.AreEqual(field.GetValue(preference?old:defaults),field.GetValue(fresh),field.Name);
            }
            CollectionAssert.AreEqual(defaults.skills.levels,fresh.skills.levels);
            CollectionAssert.AreEqual(defaults.skills.cooldowns,fresh.skills.cooldowns);
            foreach(var field in typeof(JourneySave).GetFields())Assert.AreEqual(field.GetValue(defaults.journey),field.GetValue(fresh.journey),field.Name);
            Assert.AreNotSame(old.skills,fresh.skills);Assert.AreNotSame(old.journey,fresh.journey);
            Assert.AreEqual(900,old.coins);Assert.IsTrue(old.dragonDefeated);
            var book=new SkillBook(fresh.skills);
            foreach(SkillId id in Enum.GetValues(typeof(SkillId))){Assert.AreEqual(0,book.Active(id));Assert.IsTrue(book.TryUse(id,true));}
        }
        [TestCase(false)] [TestCase(true)]
        public void ResetInstallsFreshSaveAndRemovesOldRecoveryCandidates(bool hasPrimary)
        {
            string folder=Path.Combine(Path.GetTempPath(),"forest-reset-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                string path=Path.Combine(folder,"forest-rider.json");
                if(hasPrimary)File.WriteAllText(path,"old primary");
                File.WriteAllText(path+".bak","old backup");File.WriteAllText(path+".tmp","old temporary");
                ProgressResetStore.Replace(path,"fresh adventure");
                Assert.AreEqual("fresh adventure",File.ReadAllText(path));
                Assert.IsFalse(File.Exists(path+".bak"));Assert.IsFalse(File.Exists(path+".tmp"));Assert.IsFalse(File.Exists(path+".reset"));
                ProgressResetStore.Replace(path,"second adventure");Assert.AreEqual("second adventure",File.ReadAllText(path));
            }
            finally{Directory.Delete(folder,true);}
        }
        [Test] public void StagingFailureLeavesCurrentSaveAndRecoveryFilesUntouched()
        {
            string folder=Path.Combine(Path.GetTempPath(),"forest-reset-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                string path=Path.Combine(folder,"forest-rider.json");
                File.WriteAllText(path,"old primary");File.WriteAllText(path+".bak","old backup");File.WriteAllText(path+".tmp","old temporary");
                Directory.CreateDirectory(path+".reset"); // Cross-platform deterministic write failure.
                Assert.Catch<Exception>(()=>ProgressResetStore.Replace(path,"fresh"));
                Assert.AreEqual("old primary",File.ReadAllText(path));Assert.AreEqual("old backup",File.ReadAllText(path+".bak"));Assert.AreEqual("old temporary",File.ReadAllText(path+".tmp"));
            }
            finally{Directory.Delete(folder,true);}
        }
    }
}
