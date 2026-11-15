using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonkeyForest
{
    /// <summary>Four short, illustrated pages. Images load only when a page is opened.</summary>
    public sealed class TutorialGuide:MonoBehaviour
    {
        static readonly string[] Titles={"Ride & look","Fight & grow","Hunt for orbs","Defeat the dragon"};
        static readonly string[] Snapshots={"ride","combat","orbs","dragon"};
        readonly Texture2D[] textures=new Texture2D[4];
        ForestGame game;
        UnityAction close;
        Text heading,body,counter,nextLabel;
        RawImage snapshot;
        Button previous;
        int page;

        public static TutorialGuide Create(ForestGame owner,Transform camp,UnityAction onClose)
        {
            var panel=owner.Panel("Tutorial",camp,new Vector2(.5f,.5f),Vector2.zero,new Vector2(880,530),new Color(.06f,.1f,.09f,1));
            var guide=panel.gameObject.AddComponent<TutorialGuide>();guide.Setup(owner,onClose);
            panel.gameObject.SetActive(false);return guide;
        }
        void Setup(ForestGame owner,UnityAction onClose)
        {
            game=owner;close=onClose;
            heading=game.Label(transform,"",new Vector2(.5f,.5f),new Vector2(-30,232),new Vector2(700,42),28);
            heading.color=new Color(1,.84f,.47f);
            counter=game.Label(transform,"",new Vector2(1,1),new Vector2(-60,-33),new Vector2(90,38),21);
            var frame=game.Panel("Snapshot frame",transform,new Vector2(.5f,.5f),new Vector2(0,70),new Vector2(720,270),new Color(.03f,.06f,.05f));
            var image=new GameObject("Snapshot",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.transform.SetParent(frame,false);
            snapshot=image.GetComponent<RawImage>();snapshot.raycastTarget=false;
            var aspect=image.GetComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=8f/3;
            body=game.Label(transform,"",new Vector2(.5f,.5f),new Vector2(0,-132),new Vector2(830,120),24);
            previous=game.Button(transform,"PREVIOUS",new Vector2(.5f,.5f),new Vector2(-270,-225),new Vector2(210,50),()=>Turn(-1));
            game.Button(transform,"BACK TO CAMP",new Vector2(.5f,.5f),new Vector2(0,-225),new Vector2(260,50),onClose);
            var next=game.Button(transform,"NEXT",new Vector2(.5f,.5f),new Vector2(270,-225),new Vector2(210,50),()=>{
                if(page==Titles.Length-1)close();else Turn(1);
            });
            nextLabel=next.GetComponentInChildren<Text>(true);
        }
        public void Open()
        {
            gameObject.SetActive(true);transform.SetAsLastSibling();ShowPage(0);
        }
        void Turn(int direction)
        {
            ShowPage(Mathf.Clamp(page+direction,0,Titles.Length-1));
            if(game.Audio)game.Audio.Play("ui");
        }
        void ShowPage(int index)
        {
            page=index;heading.text="TUTORIAL • "+Titles[page];counter.text=$"{page+1} / {Titles.Length}";
            if(!textures[page])textures[page]=Resources.Load<Texture2D>("Tutorial/"+Snapshots[page]);
            snapshot.texture=textures[page];snapshot.enabled=textures[page]!=null;
            if(textures[page])snapshot.GetComponent<AspectRatioFitter>().aspectRatio=(float)textures[page].width/textures[page].height;
            previous.interactable=page>0;nextLabel.text=page==Titles.Length-1?"DONE":"NEXT";
            switch(page)
            {
                case 0:
                    body.text="Drag the LEFT STICK to ride.\nSwipe empty space on the RIGHT to rotate the camera.\nPC: WASD to ride; hold right mouse to look.\nWatch for zombies at night.";
                    break;
                case 1:
                    body.text="ATTACK up close; HEAL uses a potion.\nDODGE gives brief invincibility. Use your four skills.\nRide over loot. Upgrade gear and skills in Camp.\nCamp also has SAVE, REVIVE and settings.";
                    break;
                case 2:
                    body.text=$"Enemies drop orbs: Normal {DifficultyRules.Get(0).OrbDropChance*100:0}% • Hard {DifficultyRules.Get(1).OrbDropChance*100:0}% • Extreme {DifficultyRules.Get(2).OrbDropChance*100:0}%.\n"+
                        "Ride over drops to collect them.\nTap OFFER ORBS at the center altar. Offer 10 to summon the dragon!\nExtreme: unplaced orbs are lost when you die.";
                    break;
                default:
                    body.text="Dodge fireballs, then dodge the RED dive circle.\nWhen the dragon lands stunned, get close and ATTACK.\nYour hits drain its stamina; empty it for +2 seconds of stun.";
                    break;
            }
        }
    }
}
