using System;
using System.Reflection;
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
        [Test] public void PausedHudSetupAndClosedSubmenuRefreshKeepTheirLabels()
        {
            var eventType=RuntimeType("UnityEngine.EventSystems.EventSystem");
            var previousEventSystem=UnityEngine.Object.FindAnyObjectByType(eventType);
            var host=new GameObject("Inactive Camp UI test");host.SetActive(false);
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
                var additions=(Component)game.GetType().GetProperty("Additions").GetValue(game);
                var refresh=additions.GetType().GetMethod("Refresh");
                var close=additions.GetType().GetMethod("Close");
                foreach(string panelName in new[]{"skills","journal","audioPanel","difficultyPanel","resetPanel"})
                {
                    var panel=(GameObject)Field(additions,panelName);panel.SetActive(true);
                    close.Invoke(additions,null);Assert.IsFalse(panel.activeSelf);
                    Assert.DoesNotThrow(()=>refresh.Invoke(additions,null),panelName+" labels must update while hidden.");
                }
                var selector=(Component)Field(additions,"difficultySelector");
                var text=selector.transform.Find("Label").GetComponent(RuntimeType("UnityEngine.UI.Text"));
                Assert.AreEqual("DIFFICULTY: NORMAL",text.GetType().GetProperty("text").GetValue(text));
                SetProperty(game,"Paused",false);Assert.DoesNotThrow(()=>refresh.Invoke(additions,null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                if(!previousEventSystem)
                {
                    var created=UnityEngine.Object.FindAnyObjectByType(eventType) as Component;
                    if(created)UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }
        }
    }
}
