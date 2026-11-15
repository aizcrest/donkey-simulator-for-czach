using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace DonkeyForest
{
    public sealed class CombatHud:MonoBehaviour
    {
        ForestGame game;
        GameObject death;
        Text deathText;
        public void Setup(ForestGame owner,Transform parent,UnityAction revive)
        {
            game=owner;
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
            if(game.Dead)deathText.text=game.data.difficulty==2?"Carried orbs are lost. Hunt enemies for more.\nYour gear and altar offerings are safe.":"Your equipment and progress are safe.\nRevive at camp to continue your journey.";

        }
    }
}
