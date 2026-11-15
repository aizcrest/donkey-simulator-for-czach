using UnityEngine;
using UnityEngine.UI;
namespace DonkeyForest
{
    /// <summary>Compact safe-area side panels; refreshed at the game's existing 5Hz HUD cadence.</summary>
    public sealed class SideHud:MonoBehaviour
    {
        ForestGame game;
        readonly Sprite[] icons=new Sprite[8];
        Text healthText,coins,kills,sword,armor,barding,potions,level,mode,quality;
        Text orbCount,altarCount,huntHint,bossPhase,bossHealth,bossStamina,dodge;
        RectTransform healthFill,altarFill,bossHealthFill,bossStaminaFill;
        GameObject playerPanel,objectivePanel,bossPanel;
        void LoadIcons()
        {
            if(icons[0])return;
            var atlas=Resources.Load<Texture2D>("Hud/Stats");if(!atlas)return;
            float w=atlas.width/4f,h=atlas.height/2f;
            for(int i=0;i<8;i++)icons[i]=Sprite.Create(atlas,new Rect(i%4*w,(1-i/4)*h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        }
        public void Setup(ForestGame owner,Transform parent,Text dodgeLabel)
        {
            game=owner;dodge=dodgeLabel;LoadIcons();
            var left=Card("Player stats",parent,new Vector2(0,1),new Vector2(125,-110),new Vector2(230,200));playerPanel=left.gameObject;
            level=game.Label(left,"",new Vector2(0,1),new Vector2(42,-24),new Vector2(70,26),21);
            mode=game.Label(left,"",new Vector2(1,1),new Vector2(-94,-24),new Vector2(140,24),15);
            DrawIcon(left,6,new Vector2(-88,42),36);
            healthFill=Bar(left,new Vector2(17,42),new Vector2(160,20),new Color(.77f,.17f,.22f));
            healthText=game.Label(left,"",new Vector2(.5f,.5f),new Vector2(17,42),new Vector2(160,26),18);
            coins=Stat(left,4,new Vector2(-62,8),"Coins");kills=Stat(left,5,new Vector2(47,8),"Kills");
            sword=Gear(left,0,-81,"Sword level");armor=Gear(left,1,-27,"Armor level");
            barding=Gear(left,2,27,"Donkey gear level");potions=Gear(left,3,81,"Potions");
            quality=game.Label(left,"",new Vector2(.5f,0),new Vector2(0,15),new Vector2(205,20),13);

            var right=Card("Orb objective",parent,new Vector2(1,1),new Vector2(-125,-160),new Vector2(230,190));objectivePanel=right.gameObject;
            DrawIcon(right,7,new Vector2(-69,51),52);
            orbCount=game.Label(right,"",new Vector2(.5f,.5f),new Vector2(24,51),new Vector2(125,40),25);
            game.Label(right,"CARRIED",new Vector2(.5f,.5f),new Vector2(24,24),new Vector2(125,20),14);
            altarFill=Bar(right,new Vector2(0,-8),new Vector2(185,22),new Color(.16f,.64f,.55f));
            altarCount=game.Label(right,"",new Vector2(.5f,.5f),new Vector2(0,-8),new Vector2(185,26),17);
            huntHint=game.Label(right,"",new Vector2(.5f,.5f),new Vector2(0,-52),new Vector2(195,55),16);

            var boss=Card("Dragon combat",parent,new Vector2(1,1),new Vector2(-125,-160),new Vector2(230,190));bossPanel=boss.gameObject;
            bossPhase=game.Label(boss,"",new Vector2(.5f,.5f),new Vector2(0,54),new Vector2(205,52),22);
            bossHealthFill=Bar(boss,new Vector2(0,7),new Vector2(185,22),new Color(.68f,.18f,.24f));
            bossHealth=game.Label(boss,"",new Vector2(.5f,.5f),new Vector2(0,7),new Vector2(185,26),17);
            bossStaminaFill=Bar(boss,new Vector2(0,-31),new Vector2(185,22),new Color(.16f,.64f,.7f));
            bossStamina=game.Label(boss,"",new Vector2(.5f,.5f),new Vector2(0,-31),new Vector2(185,26),17);
            game.Label(boss,"DRAGON STAMINA",new Vector2(.5f,.5f),new Vector2(0,-62),new Vector2(200,22),13);
            bossPanel.SetActive(false);
        }
        RectTransform Card(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var card=game.Panel(name,parent,anchor,position,size,Color.white);
            var image=card.GetComponent<Image>();image.sprite=game.Controls.Sprite("button");
            image.type=image.sprite?Image.Type.Sliced:Image.Type.Simple;image.raycastTarget=false;
            if(!image.sprite)image.color=new Color(.04f,.12f,.1f,.9f);return card;
        }
        void DrawIcon(Transform parent,int index,Vector2 position,float size)
        {
            var obj=new GameObject("Stat icon "+index,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*size;
            var image=obj.GetComponent<Image>();image.sprite=icons[index];image.preserveAspect=true;image.raycastTarget=false;image.enabled=image.sprite;
        }
        Text Stat(Transform parent,int icon,Vector2 position,string name)
        {
            DrawIcon(parent,icon,position+Vector2.left*24,36);
            var label=game.Label(parent,"",new Vector2(.5f,.5f),position+Vector2.right*21,new Vector2(58,30),20);label.name=name;label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=20;return label;
        }
        Text Gear(Transform parent,int icon,float x,string name)
        {
            DrawIcon(parent,icon,new Vector2(x,-32),43);
            var label=game.Label(parent,"",new Vector2(.5f,.5f),new Vector2(x,-61),new Vector2(50,25),20);label.name=name;label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=20;return label;
        }
        RectTransform Bar(Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var track=game.Panel("Bar track",parent,new Vector2(.5f,.5f),position,size,new Color(.015f,.045f,.04f));track.GetComponent<Image>().raycastTarget=false;
            var fill=game.Panel("Bar fill",track,Vector2.zero,Vector2.zero,Vector2.zero,color);fill.GetComponent<Image>().raycastTarget=false;
            fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;return fill;
        }
        public void Refresh(float fps,float dodgeSeconds)
        {
            if(!game||!healthText)return;
            var data=game.data;playerPanel.SetActive(!game.Paused);
            healthFill.anchorMax=new Vector2(Mathf.Clamp01(data.health/game.MaxHealth),1);healthText.text=$"{Mathf.CeilToInt(data.health)} / {game.MaxHealth:0}";
            level.text=$"Lv {data.journey.level}";mode.text=(game.Night?"NIGHT":"DAY")+" · "+game.Difficulty.Name.ToUpperInvariant();
            coins.text=data.coins.ToString();kills.text=data.kills.ToString();sword.text=data.sword.ToString();armor.text=data.armor.ToString();barding.text=data.barding.ToString();potions.text=data.potions.ToString();
            quality.text=$"{(data.quality==0?"LOW":data.quality==1?"BALANCED":"HIGH")} · {Mathf.RoundToInt(fps)} FPS";
            dodge.text=dodgeSeconds<=0?"DODGE":$"{dodgeSeconds:0.0}s";
            var boss=game.Encounter?game.Encounter.Boss:null;bool showBoss=boss&&!game.Dead;
            objectivePanel.SetActive(!showBoss&&!game.Paused);bossPanel.SetActive(showBoss&&!game.Paused);
            if(showBoss)
            {
                bossPhase.text=$"{boss.PhaseLabel}\n{boss.PhaseRemaining:0.0}s";
                bossHealthFill.anchorMax=new Vector2(Mathf.Clamp01(boss.Health/boss.MaxHealth),1);bossHealth.text=$"{boss.Health:0} / {boss.MaxHealth:0}";
                bossStaminaFill.anchorMax=new Vector2((float)boss.Stamina/DragonPattern.MaxStamina,1);bossStamina.text=boss.Exhausted?"EXHAUSTED +2s":$"{boss.Stamina} / {DragonPattern.MaxStamina}";
            }
            else
            {
                orbCount.text=GemRules.Held(data.gemMask,data.gemsPlaced).ToString();altarCount.text=$"ALTAR {data.gemsPlaced} / 10";
                altarFill.anchorMax=new Vector2(data.gemsPlaced/10f,1);
                huntHint.text=data.dragonDefeated?"DRAGON DEFEATED":GemRules.Count(data.gemMask)>=10?"RETURN TO ALTAR":$"HUNT ORBS\n{game.Difficulty.OrbDropChance*100:0}% PER KILL";
            }
        }
        void OnDestroy(){foreach(var icon in icons)if(icon){if(Application.isPlaying)Destroy(icon);else DestroyImmediate(icon);}}
    }
}
