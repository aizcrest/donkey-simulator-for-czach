using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace DonkeyForest
{
    public sealed class CombatHud:MonoBehaviour
    {
        ForestGame game;
        GameObject death,stamina;
        RectTransform staminaFill;
        Text staminaText,deathText;
        int displayedStamina=-1;
        public void Setup(ForestGame owner,Transform parent,UnityAction revive)
        {
            game=owner;displayedStamina=-1;
            var frame=game.Panel("Dragon stamina",parent,new Vector2(.5f,1),new Vector2(0,-207),new Vector2(420,42),Color.white);
            Skin(frame);stamina=frame.gameObject;
            var track=game.Panel("Stamina track",frame,new Vector2(.5f,.5f),Vector2.zero,new Vector2(375,18),new Color(.03f,.09f,.09f));
            staminaFill=game.Panel("Stamina fill",track,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.22f,.72f,.65f));
            staminaFill.anchorMin=Vector2.zero;staminaFill.anchorMax=Vector2.one;staminaFill.offsetMin=staminaFill.offsetMax=Vector2.zero;
            staminaText=game.Label(frame,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(375,30),16);
            var veil=game.Panel("Death overlay",parent,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.015f,.03f,.03f,.78f));
            veil.anchorMin=Vector2.zero;veil.anchorMax=Vector2.one;veil.offsetMin=veil.offsetMax=Vector2.zero;death=veil.gameObject;
            var card=game.Panel("Death card",veil,new Vector2(.5f,.5f),Vector2.zero,new Vector2(600,300),Color.white);Skin(card);
            game.Label(card,"You died.",new Vector2(.5f,.5f),new Vector2(0,85),new Vector2(520,65),42).color=new Color(1,.84f,.47f);
            deathText=game.Label(card,"",new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(500,85),21);
            game.Controls.Icon(game.Button(card,"REVIVE",new Vector2(.5f,.5f),new Vector2(0,-92),new Vector2(240,65),revive),"heal");
            Refresh();
        }
        void Skin(RectTransform rect)
        {
            var image=rect.GetComponent<Image>();image.sprite=game.Controls.Sprite("button");
            image.type=image.sprite?Image.Type.Sliced:Image.Type.Simple;
            image.color=image.sprite?Color.white:new Color(.08f,.15f,.13f);
        }
        void Update(){Refresh();}
        void Refresh()
        {
            if(!game||!death)return;
            death.SetActive(game.Dead);
            if(game.Dead)deathText.text=game.data.difficulty==2?"Unoffered gems scatter across the forest.\nYour gear and altar offerings are safe.":"Your equipment and progress are safe.\nRevive at camp to continue your journey.";
            var boss=game.Encounter?game.Encounter.Boss:null;
            stamina.SetActive(boss&&!game.Dead&&!game.Paused);
            if(!boss){displayedStamina=-1;return;}
            if(displayedStamina==boss.Stamina)return;
            displayedStamina=boss.Stamina;
            staminaFill.anchorMax=new Vector2((float)boss.Stamina/DragonPattern.MaxStamina,1);
            staminaText.text=boss.Exhausted?"EXHAUSTED • STUN +2s":$"DRAGON STAMINA {boss.Stamina} / {DragonPattern.MaxStamina}";
        }
    }
}
