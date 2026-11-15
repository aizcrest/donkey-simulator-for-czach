using System;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace DonkeyForest.Tests
{
    public sealed class CampUiTests
    {
        // Runtime scripts live in Assembly-CSharp, which an asmdef test assembly cannot
        // reference directly. Reflection lets this exercise the real HUD builder.
        static Type RuntimeType(string name)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {var type=assembly.GetType(name);if(type!=null)return type;}
            throw new InvalidOperationException("Missing runtime type: "+name);
        }
        static void SetProperty(Component target,string name,object value)
            =>target.GetType().GetProperty(name).GetSetMethod(true).Invoke(target,new[]{value});
        static object Field(Component target,string name)
            =>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Click(Component button)
        {
            var action=button.GetType().GetProperty("onClick").GetValue(button);
            action.GetType().GetMethod("Invoke",Type.EmptyTypes).Invoke(action,null);
        }
        [Test] public void PausedHudSetupAndClosedSubmenuRefreshKeepTheirLabels()
        {
            var eventType=RuntimeType("UnityEngine.EventSystems.EventSystem");
            var previousEventSystem=UnityEngine.Object.FindAnyObjectByType(eventType);
            var host=new GameObject("Inactive Camp UI test");host.SetActive(false);
            string folder=Path.Combine(Path.GetTempPath(),"forest-camp-ui-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                // Keep Awake disabled: this test must not load, overwrite or reset a real save.
                var game=host.AddComponent(RuntimeType("DonkeyForest.ForestGame"));
                var skills=host.AddComponent(RuntimeType("DonkeyForest.MountedSkills"));
                SetProperty(skills,"Book",new SkillBook(new SkillSave()));SetProperty(game,"Skills",skills);
                SetProperty(game,"Controls",host.AddComponent(RuntimeType("DonkeyForest.TouchControlSkin")));
                SetProperty(game,"Paused",true);
                var build=game.GetType().GetMethod("BuildUI",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.DoesNotThrow(()=>build.Invoke(game,null),"HUD setup must tolerate inactive submenus while paused.");
                var hud=(GameObject)Field(game,"hudRoot");
                var left=hud.transform.Find("Safe Area/Player stats").GetComponent<RectTransform>();
                var right=hud.transform.Find("Safe Area/Orb objective").GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0,1),left.anchorMin);Assert.AreEqual(Vector2.one,right.anchorMin);
                Assert.Less(left.sizeDelta.x,300);Assert.Less(right.sizeDelta.x,300);
                Assert.AreSame(Resources.Load<Font>("Fonts/LilitaOne-Regular"),Field(game,"font"));
                var additions=(Component)game.GetType().GetProperty("Additions").GetValue(game);
                var refresh=additions.GetType().GetMethod("Refresh");
                var close=additions.GetType().GetMethod("Close");
                foreach(string panelName in new[]{"skills","journal","audioPanel","difficultyPanel","resetPanel","savePanel","confirmPanel"})
                {
                    var panel=(GameObject)Field(additions,panelName);panel.SetActive(true);
                    close.Invoke(additions,null);Assert.IsFalse(panel.activeSelf);
                    Assert.DoesNotThrow(()=>refresh.Invoke(additions,null),panelName+" labels must update while hidden.");
                }
                var selector=(Component)Field(additions,"difficultySelector");
                var text=selector.transform.Find("Label").GetComponent(RuntimeType("UnityEngine.UI.Text"));
                Assert.AreEqual("DIFFICULTY: NORMAL",text.GetType().GetProperty("text").GetValue(text));
                // Selecting a mode opens confirmation; cancellation never mutates progress.
                var progress=(Progress)game.GetType().GetField("data").GetValue(game);progress.coins=321;
                var modes=(Array)Field(additions,"difficultyButtons");Click((Component)modes.GetValue(1));
                var confirmation=(GameObject)Field(additions,"confirmPanel");Assert.IsTrue(confirmation.activeSelf);
                Assert.AreEqual(0,progress.difficulty);Assert.AreEqual(321,progress.coins);
                Click(confirmation.transform.Find("BACK").GetComponent(RuntimeType("UnityEngine.UI.Button")));
                Assert.IsFalse(confirmation.activeSelf);Assert.IsNull(Field(additions,"pendingAction"));
                Assert.AreEqual(0,progress.difficulty);Assert.AreEqual(321,progress.coins);

                var store=new SaveSlotStore(folder);
                game.GetType().GetField("saves",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,store);
                var saved=new Progress{difficulty=2,coins=72,sword=4,armor=3,barding=2,health=80};saved.journey.level=7;
                store.Write(2,JsonUtility.ToJson(saved));
                additions.GetType().GetMethod("OpenSaves").Invoke(additions,null);
                var rows=(Array)Field(additions,"saveDetails");var loads=(Array)Field(additions,"loadButtons");
                Assert.AreEqual(5,rows.Length);Assert.AreEqual(5,loads.Length);
                var row=(Component)rows.GetValue(1);string summary=(string)row.GetType().GetProperty("text").GetValue(row);
                StringAssert.Contains("Extreme",summary);StringAssert.Contains("Lv.7",summary);StringAssert.Contains("Sword 4",summary);
                StringAssert.Contains("Coins 72",summary);StringAssert.Contains("HP 80",summary);
                Assert.AreEqual(false,loads.GetValue(0).GetType().GetProperty("interactable").GetValue(loads.GetValue(0)));
                Assert.AreEqual(true,loads.GetValue(1).GetType().GetProperty("interactable").GetValue(loads.GetValue(1)));
                Click((Component)loads.GetValue(1));Assert.IsTrue(confirmation.activeSelf);
                Assert.AreEqual(321,progress.coins);Assert.AreEqual(0,progress.difficulty);
                Click(confirmation.transform.Find("BACK").GetComponent(RuntimeType("UnityEngine.UI.Button")));
                Assert.IsNull(Field(additions,"pendingAction"));Assert.AreEqual(0,progress.difficulty);
                Assert.AreEqual(JsonUtility.ToJson(saved),File.ReadAllText(store.PathFor(2)),"Browsing/canceling must not overwrite saves.");
                SetProperty(game,"Paused",false);Assert.DoesNotThrow(()=>refresh.Invoke(additions,null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                Directory.Delete(folder,true);
                if(!previousEventSystem)
                {
                    var created=UnityEngine.Object.FindAnyObjectByType(eventType) as Component;
                    if(created)UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }
        }
    }
}
