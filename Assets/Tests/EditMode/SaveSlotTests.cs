using System;
using System.IO;
using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class SaveSlotTests
    {
        string folder;
        SaveSlotStore store;
        [SetUp] public void Setup()
        {
            folder=Path.Combine(Path.GetTempPath(),"forest-slots-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            store=new SaveSlotStore(folder);
        }
        [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
        static bool Valid(string text)=>text.StartsWith("valid:");
        string Read(int slot){Assert.IsTrue(store.TryRead(slot,Valid,out string json));return json;}
        [Test] public void FiveIndependentSlotsAndLastSelectionSurviveReopening()
        {
            for(int i=1;i<=5;i++){Assert.IsFalse(store.Exists(i));store.Write(i,"valid:adventure-"+i);}
            store.Select(4);store=new SaveSlotStore(folder);
            Assert.AreEqual(4,store.ActiveSlot);
            for(int i=1;i<=5;i++)Assert.AreEqual("valid:adventure-"+i,Read(i));
            store.Write(4,"valid:autosaved");
            Assert.AreEqual("valid:autosaved",Read(4));Assert.AreEqual("valid:adventure-3",Read(3));
        }
        [TestCase(0)] [TestCase(6)] [TestCase(-1)]
        public void OutOfRangeSlotIsRejected(int slot)
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>store.Write(slot,"valid:data"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>store.Select(slot));
            Assert.IsEmpty(Directory.GetFiles(folder));
        }
        [TestCase(".bak")] [TestCase(".tmp")]
        public void CorruptPrimaryFallsBackToRecoveryWithoutModifyingFiles(string suffix)
        {
            var path=store.PathFor(2);File.WriteAllText(path,"broken");File.WriteAllText(path+suffix,"valid:recovered");
            Assert.AreEqual("valid:recovered",Read(2));
            Assert.AreEqual("broken",File.ReadAllText(path));Assert.AreEqual("valid:recovered",File.ReadAllText(path+suffix));
        }
        [Test] public void EmptyAndCorruptSlotsAreDistinctAndCannotBeLoaded()
        {
            Assert.IsFalse(store.Exists(3));Assert.IsFalse(store.TryRead(3,Valid,out _));
            File.WriteAllText(store.PathFor(3),"broken");
            Assert.IsTrue(store.Exists(3));Assert.IsFalse(store.TryRead(3,Valid,out _));
        }
        [Test] public void ResetReplacesOnlySelectedSlotAndPurgesItsOldRecoveryFiles()
        {
            for(int i=1;i<=5;i++){store.Write(i,"valid:first-"+i);store.Write(i,"valid:second-"+i);}
            File.WriteAllText(store.PathFor(3)+".tmp","valid:pending-old");
            store.Select(3);store.Write(3,"valid:fresh-extreme",true);
            Assert.AreEqual("valid:fresh-extreme",Read(3));Assert.AreEqual(3,store.ActiveSlot);
            Assert.IsFalse(File.Exists(store.PathFor(3)+".bak"));Assert.IsFalse(File.Exists(store.PathFor(3)+".tmp"));
            foreach(int i in new[]{1,2,4,5})
            {
                Assert.AreEqual("valid:second-"+i,Read(i));Assert.AreEqual("valid:first-"+i,File.ReadAllText(store.PathFor(i)+".bak"));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedWriteLeavesCurrentAdventureAndOtherSlotsIntact(bool resetting)
        {
            store.Write(1,"valid:current");store.Write(2,"valid:other");store.Select(1);
            Directory.CreateDirectory(store.PathFor(1)+(resetting?".reset":".tmp"));
            Assert.Catch<Exception>(()=>store.Write(1,"valid:new",resetting));
            Assert.AreEqual("valid:current",Read(1));Assert.AreEqual("valid:other",Read(2));Assert.AreEqual(1,store.ActiveSlot);
        }
        [TestCase("")] [TestCase(".bak")] [TestCase(".tmp")]
        public void LegacySaveMigratesOnceToSlotOneAndOriginalRemains(string suffix)
        {
            string legacy=Path.Combine(folder,"forest-rider.json");File.WriteAllText(legacy+suffix,"valid:legacy");
            store.MigrateLegacy(Valid);Assert.AreEqual("valid:legacy",Read(1));Assert.AreEqual(1,store.ActiveSlot);
            Assert.AreEqual("valid:legacy",File.ReadAllText(legacy+suffix));
            for(int i=2;i<=5;i++)Assert.IsFalse(store.Exists(i));
            store.Write(1,"valid:new-difficulty",true);store.MigrateLegacy(Valid);
            Assert.AreEqual("valid:new-difficulty",Read(1));
        }
        [Test] public void ExistingOtherSlotPreventsLegacyResurrection()
        {
            File.WriteAllText(Path.Combine(folder,"forest-rider.json"),"valid:legacy");
            store.Write(5,"valid:current");store.Select(5);store.MigrateLegacy(Valid);
            Assert.IsFalse(store.Exists(1));Assert.AreEqual("valid:current",Read(5));
        }
        [Test] public void CorruptActiveMarkerDefaultsToSlotOneWithoutDeletingSaves()
        {
            store.Write(1,"valid:one");File.WriteAllText(Path.Combine(folder,"forest-active-slot.txt"),"99");
            Assert.AreEqual(1,store.ActiveSlot);Assert.AreEqual("valid:one",Read(1));
        }
        [TestCase(0,1)] [TestCase(0,2)] [TestCase(2,0)] [TestCase(1,2)]
        public void DifficultyResetClearsAdventureButKeepsDevicePreferences(int previous,int next)
        {
            var old=new Progress{difficulty=previous,coins=900,kills=20,sword=8,armor=7,barding=6,health=0,x=90,z=-70,
                gemMask=1023,gemDepositedMask=1023,gemsPlaced=10,dragonDefeated=true,quality=2,masterVolume=.4f,musicVolume=.1f,effectsVolume=.6f};
            old.journey.level=15;old.journey.xp=80;old.skills.levels[0]=5;
            var fresh=Progress.NewAdventure(old,next);
            Assert.AreEqual(next,fresh.difficulty);Assert.AreEqual(previous,old.difficulty);Assert.AreEqual(900,old.coins);
            var defaults=new Progress();
            foreach(var field in typeof(Progress).GetFields())
            {
                if(!field.FieldType.IsValueType||field.Name=="difficulty")continue;
                bool preference=field.Name=="quality"||field.Name.EndsWith("Volume");
                Assert.AreEqual(field.GetValue(preference?old:defaults),field.GetValue(fresh),field.Name);
            }
            Assert.AreEqual(1,fresh.journey.level);Assert.AreEqual(0,fresh.journey.xp);Assert.AreEqual(1,fresh.skills.levels[0]);
            Assert.AreNotSame(old.skills,fresh.skills);Assert.AreNotSame(old.journey,fresh.journey);
        }
    }
}
