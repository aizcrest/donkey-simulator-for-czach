using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DonkeyForest
{
    public sealed class TouchControlSkin:MonoBehaviour
    {
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        public Sprite Sprite(string name)
        {
            if(sprites.TryGetValue(name,out var sprite))return sprite;
            sprite=Resources.Load<Sprite>("UI/"+name);
            if(!sprite)Debug.LogWarning("Touch artwork UI/"+name+" is missing or not imported as a Sprite. Using the plain control fallback.");
            sprites.Add(name,sprite);return sprite;
        }
        public void Button(Button button)
        {
            var image=button.GetComponent<Image>();image.sprite=Sprite("button");image.type=image.sprite?Image.Type.Sliced:Image.Type.Simple;image.color=image.sprite?Color.white:new Color(.18f,.25f,.23f,.95f);
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.96f,.79f);colors.pressedColor=new Color(.7f,.78f,.66f);colors.disabledColor=new Color(.45f,.48f,.45f,.75f);colors.fadeDuration=.08f;button.colors=colors;
            button.gameObject.AddComponent<TouchPressFeedback>();
        }
        public Image Icon(Button button,string name,bool skill=false)
        {
            var rect=button.GetComponent<RectTransform>();var obj=new GameObject(name+" icon",typeof(RectTransform),typeof(Image));obj.transform.SetParent(rect,false);
            var icon=obj.GetComponent<Image>();icon.sprite=Sprite(name);icon.enabled=icon.sprite;icon.preserveAspect=true;icon.raycastTarget=false;
            var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=skill?new Vector2(.5f,1):new Vector2(0,.5f);r.anchoredPosition=skill?new Vector2(0,-20):new Vector2(28,0);r.sizeDelta=Vector2.one*(skill?30:34);
            var label=button.GetComponentInChildren<Text>(true);label.rectTransform.anchoredPosition=skill?new Vector2(0,-17):new Vector2(20,0);label.rectTransform.sizeDelta=new Vector2(rect.sizeDelta.x-(skill?8:48),skill?42:rect.sizeDelta.y-8);label.fontSize=skill?14:19;
            if(!skill)return icon;
            var overlay=new GameObject("Cooldown",typeof(RectTransform),typeof(Image));overlay.transform.SetParent(r,false);
            var fill=overlay.GetComponent<Image>();fill.sprite=Sprite("stick");fill.enabled=fill.sprite;fill.color=new Color(.04f,.07f,.09f,.8f);fill.raycastTarget=false;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Radial360;fill.fillOrigin=2;fill.fillAmount=0;
            var fr=overlay.GetComponent<RectTransform>();fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;return fill;
        }
        public void Stick(Image image,bool thumb)
        {image.sprite=Sprite(thumb?"thumb":"stick");image.color=image.sprite?Color.white:thumb?new Color(.7f,.78f,.6f,.9f):new Color(.12f,.18f,.16f,.7f);image.preserveAspect=true;if(thumb)image.raycastTarget=false;}
        // Imported sprites belong to Resources; never destroy their shared assets.
        void OnDestroy(){sprites.Clear();}
    }
    public sealed class TouchPressFeedback:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData e){if(GetComponent<Button>().IsInteractable())transform.localScale=Vector3.one*.95f;}
        public void OnPointerUp(PointerEventData e){transform.localScale=Vector3.one;}
        public void OnPointerExit(PointerEventData e){transform.localScale=Vector3.one;}
        void OnDisable(){transform.localScale=Vector3.one;}
    }
}
