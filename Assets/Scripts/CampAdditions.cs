using UnityEngine;
using UnityEngine.UI;
namespace DonkeyForest
{
    public sealed class CampAdditions:MonoBehaviour
    {
        ForestGame game;
        GameObject skills,journal,audioPanel;
        Text skillSummary,journalSummary;
        readonly Button[] hotbar=new Button[4],upgrades=new Button[4],contracts=new Button[4],qualityButtons=new Button[3];
        readonly Text[] skillDetails=new Text[4],questDetails=new Text[4];
        readonly string[] descriptions={"Rider • spin attack around you", "Rider • reduce damage for 4–5 seconds", "Donkey • rush, hit once per enemy and stun", "Donkey • kick behind you, knock back and stun"};
        public void Setup(ForestGame owner,Transform camp,Transform hud)
        {
            game=owner;
            for(int i=0;i<4;i++)
            {
                var id=(SkillId)i;
                hotbar[i]=game.Button(hud,"",new Vector2(.5f,0),new Vector2(-220+i*140,62),new Vector2(130,80),()=>game.Skills.Use(id));
                hotbar[i].GetComponentInChildren<Text>().fontSize=18;
            }
            game.Button(camp,"SKILLS",new Vector2(0,1),new Vector2(90,-30),new Vector2(150,42),()=>Open(skills));
            game.Button(camp,"JOURNAL",new Vector2(.5f,1),new Vector2(-130,-30),new Vector2(160,42),()=>Open(journal));
            game.Button(camp,"SETTINGS",new Vector2(.5f,1),new Vector2(65,-30),new Vector2(150,42),()=>Open(audioPanel));
            skills=Overlay(camp,"RIDER & DONKEY SKILLS");journal=Overlay(camp,"HUNT CONTRACTS");audioPanel=Overlay(camp,"AUDIO & GRAPHICS");
            skillSummary=game.Label(skills.transform,"",new Vector2(.5f,1),new Vector2(0,-83),new Vector2(850,45),22);
            journalSummary=game.Label(journal.transform,"",new Vector2(.5f,1),new Vector2(0,-83),new Vector2(850,45),22);
            for(int i=0;i<4;i++)
            {
                float y=95-i*78;var id=(SkillId)i;var quest=(ContractId)i;
                skillDetails[i]=game.Label(skills.transform,"",new Vector2(.5f,.5f),new Vector2(-110,y),new Vector2(580,76),20);
                upgrades[i]=game.Button(skills.transform,"",new Vector2(.5f,.5f),new Vector2(315,y),new Vector2(210,65),()=>Upgrade(id));
                upgrades[i].GetComponentInChildren<Text>().fontSize=18;
                questDetails[i]=game.Label(journal.transform,"",new Vector2(.5f,.5f),new Vector2(-110,y),new Vector2(580,76),20);
                contracts[i]=game.Button(journal.transform,"",new Vector2(.5f,.5f),new Vector2(315,y),new Vector2(210,65),()=>Claim(quest));
                contracts[i].GetComponentInChildren<Text>().fontSize=18;
            }
            AudioSlider("Master",105,game.data.masterVolume,value=>game.data.masterVolume=value);
            AudioSlider("Music",40,game.data.musicVolume,value=>game.data.musicVolume=value);
            AudioSlider("Effects",-25,game.data.effectsVolume,value=>game.data.effectsVolume=value);
            for(int i=0;i<3;i++)
            {
                int quality=i;string name=i==0?"LOW • 30 FPS":i==1?"BALANCED • 30 FPS":"HIGH • 60 FPS";
                qualityButtons[i]=game.Button(audioPanel.transform,name,new Vector2(.5f,.5f),new Vector2(-270+i*270,-115),new Vector2(250,55),()=>{game.data.quality=quality;MobilePerformance.Apply(game);game.Save(false);game.Show(name+" applied.");Refresh();});
            }
            game.Label(audioPanel.transform,"Low defaults to fewer enemies, less grass and 75% resolution.\nSettings save when you leave camp.",new Vector2(.5f,.5f),new Vector2(0,-170),new Vector2(830,60),19);
            Close();Refresh();
        }
        GameObject Overlay(Transform camp,string title)
        {
            var panel=game.Panel(title,camp,new Vector2(.5f,.5f),Vector2.zero,new Vector2(880,530),new Color(.06f,.1f,.09f,1));
            game.Label(panel,title,new Vector2(.5f,1),new Vector2(0,-35),new Vector2(850,50),26);
            game.Button(panel,"BACK",new Vector2(.5f,0),new Vector2(0,40),new Vector2(220,55),Close);
            return panel.gameObject;
        }
        void AudioSlider(string name,float y,float initial,UnityEngine.Events.UnityAction<float> changed)
        {
            var label=game.Label(audioPanel.transform,name,new Vector2(.5f,.5f),new Vector2(-285,y),new Vector2(180,45),24);
            var track=game.Panel(name+" volume",audioPanel.transform,new Vector2(.5f,.5f),new Vector2(100,y),new Vector2(480,30),new Color(.2f,.28f,.24f));
            var fill=game.Panel("Fill",track,new Vector2(.5f,.5f),Vector2.zero,new Vector2(460,22),new Color(.42f,.65f,.45f));
            var handle=game.Panel("Handle",track,new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,45),Color.white);
            fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=new Vector2(8,5);fill.offsetMax=new Vector2(-8,-5);
            var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();
            slider.minValue=0;slider.maxValue=1;slider.value=initial;
            slider.onValueChanged.AddListener(value=>{changed(value);label.text=name+" "+Mathf.RoundToInt(value*100)+"%";game.Audio.ApplyVolumes();});
            label.text=name+" "+Mathf.RoundToInt(initial*100)+"%";
        }
        void Open(GameObject panel){Close();panel.SetActive(true);Refresh();game.Audio.Play("ui");}
        public void Close(){if(skills)skills.SetActive(false);if(journal)journal.SetActive(false);if(audioPanel)audioPanel.SetActive(false);}
        void Upgrade(SkillId id)
        {
            if(!game.Skills.Book.TryUpgrade(id,ref game.data.coins,ref game.data.journey.skillPoints)){game.Show("Need a skill point and enough coins; skills cap at level 5.");return;}
            game.Show(SkillRules.Name(id)+" upgraded!");game.Audio.Play("level");game.Save(false);Refresh();
        }
        void Claim(ContractId id)
        {
            int before=game.data.journey.level;
            if(!JourneyRules.Claim(game.data.journey,id,GemRules.Count(game.data.gemMask),game.data.dragonDefeated,ref game.data.coins,ref game.data.potions))return;
            game.data.health=Mathf.Min(game.MaxHealth,game.data.health+(game.data.journey.level-before)*3);
            game.Show("Contract complete: coins, a potion, XP and skill points awarded.");game.Audio.Play("level");game.Save(false);Refresh();
        }
        public void Refresh()
        {
            if(!game||game.Skills==null)return;
            var book=game.Skills.Book;var journey=game.data.journey;
            for(int i=0;i<4;i++)
            {
                var id=(SkillId)i;int level=book.Level(id);float remaining=book.Remaining(id);bool active=book.Active(id)>0;
                hotbar[i].GetComponentInChildren<Text>().text=$"{i+1}: {SkillRules.Name(id)}\nLv.{level} • {(active?"ACTIVE":remaining>0?remaining.ToString("0.0")+"s":"READY")}";
                hotbar[i].interactable=!game.Paused&&!game.Dead&&remaining<=0 && !(id==SkillId.Charge&&game.IsDodging);
            }
            if(!game.Paused)return;
            string xp=journey.level>=JourneyRules.MaxLevel?"MAX LEVEL":$"XP {journey.xp}/{JourneyRules.Needed(journey.level)}";
            skillSummary.text=$"Level {journey.level} • {xp} • Skill points {journey.skillPoints} • Coins {game.data.coins}";
            journalSummary.text=$"Level {journey.level} • {xp} • Claim each completed contract once";
            for(int i=0;i<3;i++)
            {
                qualityButtons[i].GetComponentInChildren<Text>().text=(i==0?"LOW • 30 FPS":i==1?"BALANCED • 30 FPS":"HIGH • 60 FPS")+(i==game.data.quality?" ✓":"");
                bool selected=i==game.data.quality;
                qualityButtons[i].interactable=!selected;
                qualityButtons[i].GetComponent<Image>().color=selected?new Color(.25f,.5f,.32f):new Color(.18f,.25f,.23f);
                var colors=qualityButtons[i].colors;colors.disabledColor=Color.white;qualityButtons[i].colors=colors;
            }
            for(int i=0;i<4;i++)
            {
                var id=(SkillId)i;int level=book.Level(id);
                skillDetails[i].text=$"{SkillRules.Name(id)} • Lv.{level}/{SkillRules.MaxLevel}\n{descriptions[i]} • CD {SkillRules.Cooldown(id,level):0.#}s";
                upgrades[i].GetComponentInChildren<Text>().text=level==SkillRules.MaxLevel?"MAX LEVEL":$"UPGRADE\n{SkillRules.UpgradeCost(level)} coins + 1 point";
                upgrades[i].interactable=level<SkillRules.MaxLevel&&journey.skillPoints>0&&game.data.coins>=SkillRules.UpgradeCost(level);
                var quest=(ContractId)i;int progress=JourneyRules.Progress(journey,quest,GemRules.Count(game.data.gemMask),game.data.dragonDefeated),goal=JourneyRules.Goal(quest);
                bool claimed=JourneyRules.Claimed(journey,quest);
                questDetails[i].text=JourneyRules.ContractName(quest)+$"\n{Mathf.Min(progress,goal)}/{goal} • {(quest==ContractId.DragonSlayer?"200 coins, 2 points":"80 coins, 1 point")} + potion & XP";
                contracts[i].GetComponentInChildren<Text>().text=claimed?"CLAIMED":progress>=goal?"CLAIM REWARD":"IN PROGRESS";
                contracts[i].interactable=!claimed&&progress>=goal;
            }
        }
    }
}
