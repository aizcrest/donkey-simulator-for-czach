using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
namespace DonkeyForest.Tests
{
    public sealed class CombatHudTests
    {
        static Type RuntimeType(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
        [Test] public void DeadSaveShowsSkinnedReviveControlBeforeAnEncounterExists()
        {
            var host=new GameObject("Inactive death HUD test");host.SetActive(false);
            var ui=new GameObject("Test UI",typeof(RectTransform));
            try
            {
                var game=host.AddComponent(RuntimeType("DonkeyForest.ForestGame"));
                var data=(Progress)game.GetType().GetField("data").GetValue(game);data.health=0;data.difficulty=2;
                game.GetType().GetField("font",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                var controls=host.AddComponent(RuntimeType("DonkeyForest.TouchControlSkin"));
                game.GetType().GetProperty("Controls").GetSetMethod(true).Invoke(game,new object[]{controls});
                var hud=host.AddComponent(RuntimeType("DonkeyForest.CombatHud"));
                int clicks=0;UnityAction revive=()=>{clicks++;data.health=100;};
                Assert.DoesNotThrow(()=>hud.GetType().GetMethod("Setup").Invoke(hud,new object[]{game,ui.transform,revive}));
                var death=(GameObject)hud.GetType().GetField("death",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
                Assert.IsTrue(death.activeSelf);
                var textType=RuntimeType("UnityEngine.UI.Text");
                Assert.IsTrue(death.GetComponentsInChildren(textType,true).Any(t=>(string)textType.GetProperty("text").GetValue(t)=="You died."));
                var button=death.GetComponentInChildren(RuntimeType("UnityEngine.UI.Button"),true);
                var image=button.GetComponent(RuntimeType("UnityEngine.UI.Image"));
                Assert.AreSame(Resources.Load<Sprite>("UI/button"),image.GetType().GetProperty("sprite").GetValue(image));
                ((UnityEvent)button.GetType().GetProperty("onClick").GetValue(button)).Invoke();
                hud.GetType().GetMethod("Refresh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hud,null);
                Assert.AreEqual(1,clicks);Assert.IsFalse(death.activeSelf);
            }
            finally{UnityEngine.Object.DestroyImmediate(ui);UnityEngine.Object.DestroyImmediate(host);}
        }
        [Test] public void UnityJsonRetainsDroppedGemLocationsAndExactAltarIds()
        {
            var data=new Progress{difficulty=2,gemMask=(1<<7)|(1<<9)};GemRules.DepositAll(data);
            GemRules.TryCollect(ref data.gemMask,2);GemRules.DropHeldOnDeath(data);
            data.gemX[2]=-44.5f;data.gemZ[2]=31.25f;data.health=0;
            var loaded=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(data));GemRules.NormalizeState(loaded);
            Assert.AreEqual(data.gemDepositedMask,loaded.gemDepositedMask);Assert.AreEqual(data.gemMask,loaded.gemMask);
            Assert.AreEqual(1<<2,loaded.scatteredGemMask);Assert.AreEqual(2,loaded.gemsPlaced);
            Assert.AreEqual(-44.5f,loaded.gemX[2]);Assert.AreEqual(31.25f,loaded.gemZ[2]);
            Assert.AreEqual(0,GemRules.DropHeldOnDeath(loaded),"Reloading a death must not scatter twice.");
        }
    }
}
