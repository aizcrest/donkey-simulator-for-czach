using System;
using NUnit.Framework;
namespace DonkeyForest.Tests
{
    public sealed class EnemyCombatTests
    {
        [TestCase("Bear")][TestCase("Moose")][TestCase("Wolf")][TestCase("Fox")][TestCase("Deer")][TestCase("Zombie")]
        public void EverySpeciesTelegraphsStrikesOnceThenRecovers(string kind)
        {
            var profile=EnemyAttackProfile.For(kind);var attack=new EnemyAttackCycle(profile);attack.Begin();
            attack.Tick(profile.Windup*.5);Assert.AreEqual(EnemyAttackPhase.Windup,attack.Phase);Assert.IsFalse(attack.TryContact(true));
            attack.Tick(profile.Windup*.5+.01);Assert.AreEqual(EnemyAttackPhase.Strike,attack.Phase);
            Assert.IsFalse(attack.TryContact(false),"Being in attack range is not a hit.");
            Assert.IsTrue(attack.TryContact(true));Assert.IsFalse(attack.TryContact(true),"One strike cannot hit repeatedly.");
            attack.Tick(profile.Active);Assert.AreEqual(EnemyAttackPhase.Recovery,attack.Phase);Assert.IsFalse(attack.TryContact(true));
            attack.Tick(profile.Recovery);Assert.IsFalse(attack.Running);Assert.AreEqual(1,attack.PoseTime,1e-6);
        }
        [Test] public void EscapingTheActiveWindowDoesNotCauseDelayedDamage()
        {
            var attack=new EnemyAttackCycle(EnemyAttackProfile.For("Wolf"));attack.Begin();
            attack.Tick(.4);Assert.IsFalse(attack.TryContact(false));
            attack.Tick(.3);attack.Tick(.01);Assert.AreEqual(EnemyAttackPhase.Recovery,attack.Phase);
            Assert.IsFalse(attack.TryContact(true),"Returning during recovery must not trigger an old strike.");
        }
        [Test] public void SlowFrameStillSweepsTheStrikeAndCannotRepeatIt()
        {
            var attack=new EnemyAttackCycle(EnemyAttackProfile.For("Bear"));attack.Begin();attack.Tick(2);
            Assert.IsFalse(attack.Running);Assert.AreEqual(attack.Profile.Active,attack.ActiveTo,1e-6);
            Assert.IsTrue(attack.TryContact(true));attack.Tick(.016);Assert.IsFalse(attack.TryContact(true));
        }
        [Test] public void StunOrDeathCancelsPendingContact()
        {
            var attack=new EnemyAttackCycle(EnemyAttackProfile.For("Moose"));attack.Begin();attack.Tick(.4);attack.Cancel();attack.Tick(2);
            Assert.IsFalse(attack.TryContact(true));Assert.IsFalse(attack.Running);
            attack.Begin();attack.Tick(.7);Assert.IsTrue(attack.TryContact(true),"A later attack may hit after recovery from stun.");
        }
        static CombatPoint P(double x,double y,double z)=>new CombatPoint(x,y,z);
        static bool Hit(CombatPoint a,CombatPoint b,CombatPoint player)=>AttackContact.SweptSphereHitsCapsule(a,b,.4,
            player+P(0,.55,0),player+P(0,1.45,0),.55);
        [Test] public void ContactUsesThePlayerCapsuleInsteadOfAnOmnidirectionalRange()
        {
            var tip=P(0,.9,1.1);
            Assert.IsTrue(Hit(tip,tip,P(0,0,1.8)));
            Assert.IsFalse(Hit(tip,tip,P(0,0,-1)),"Player behind attacker is safe.");
            Assert.IsFalse(Hit(tip,tip,P(1.5,0,1.1)),"Sidestepping outside the swipe must miss.");
            Assert.IsFalse(Hit(tip,tip,P(0,3,1.1)),"Height separation must miss.");
            Assert.IsFalse(Hit(tip,tip,P(0,0,3)),"Leaving reach must miss.");
        }
        [Test] public void FastChargeCannotTunnelThroughPlayerBetweenFrames()
        {
            Assert.IsTrue(Hit(P(0,1,-4),P(0,1,4),P(0,0,0)));
            Assert.IsFalse(Hit(P(2,1,-4),P(2,1,4),P(0,0,0)));
            Assert.IsTrue(AttackContact.SweptSphereHitsCapsule(P(0,0,0),P(0,0,0),.5,P(1,0,0),P(1,0,0),.5));
        }
        [TestCase(30)][TestCase(60)][TestCase(120)]
        public void DodgeProtectsTheLastMovementFrameAndExpiresOnTheNext(int fps)
        {
            var dodge=new DodgeWindow();Assert.IsTrue(dodge.TryStart(true));float dt=1f/fps;int steps=0;
            while(dodge.Remaining>0){dodge.BeginFrame(dt,false);Assert.IsTrue(dodge.Invulnerable);Assert.Less(++steps,100);}
            Assert.IsTrue(dodge.Invulnerable);Assert.IsFalse(dodge.TryStart(true),"Cooldown cannot be bypassed.");
            dodge.BeginFrame(dt,false);Assert.IsFalse(dodge.Invulnerable);
            dodge.BeginFrame(2,false);Assert.IsTrue(dodge.TryStart(true));
        }
        [Test] public void LateTouchDodgeRejectsAllAlreadyQueuedDamageInThatFrame()
        {
            var dodge=new DodgeWindow();var inbox=new DamageInbox();dodge.BeginFrame(.016f,false);
            inbox.Add(18);inbox.Add(24);Assert.IsTrue(dodge.TryStart(true));
            Assert.AreEqual(0,inbox.Resolve(dodge.Invulnerable));
            dodge.BeginFrame(.5f,false);Assert.IsTrue(dodge.Invulnerable);inbox.Add(20);Assert.AreEqual(0,inbox.Resolve(dodge.Invulnerable));
            dodge.BeginFrame(.016f,false);inbox.Add(13);Assert.AreEqual(13,inbox.Resolve(dodge.Invulnerable));Assert.AreEqual(0,inbox.Resolve(false));
        }
        [Test] public void PausingDoesNotSpendDodgeAndResetClearsProtectionAndPendingDamage()
        {
            var dodge=new DodgeWindow();Assert.IsFalse(dodge.TryStart(false));dodge.TryStart(true);
            dodge.BeginFrame(20,true);Assert.AreEqual(DodgeWindow.Duration,dodge.Remaining);Assert.AreEqual(DodgeWindow.CooldownSeconds,dodge.Cooldown);
            dodge.Reset();Assert.IsFalse(dodge.Invulnerable);Assert.AreEqual(0,dodge.Cooldown);
            var inbox=new DamageInbox();inbox.Add(50);inbox.Clear();Assert.AreEqual(0,inbox.Resolve(false));
            inbox.Add(float.NaN);inbox.Add(-1);inbox.Add(float.PositiveInfinity);Assert.AreEqual(0,inbox.Resolve(false));
        }
    }
}
