using UnityEngine;
using UnityEngine.UI;
namespace DonkeyForest
{
    /// <summary>Right-thumb combat arc, fitted below the objective card inside the screen safe area.</summary>
    public sealed class CombatControlLayout:MonoBehaviour
    {
        public static readonly Vector2 AttackPosition=new Vector2(-120,155);
        public const float AttackSize=180,AbilitySize=92,OrbitRadius=175;
        RectTransform safe;
        Vector2 lastSize;
        Button dodge;
        Image dodgeFill;
        public static Vector2 AbilityPosition(int slot)
        {
            float angle=(75+35*slot)*Mathf.Deg2Rad;
            return AttackPosition+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*OrbitRadius;
        }
        public static CombatControlLayout Create(Transform parent)
        {
            var obj=new GameObject("Combat controls",typeof(RectTransform),typeof(CombatControlLayout));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-12,12);rect.sizeDelta=new Vector2(355,380);
            var layout=obj.GetComponent<CombatControlLayout>();layout.safe=(RectTransform)parent;layout.Fit();return layout;
        }
        public void SetDodge(Button button,Image fill){dodge=button;dodgeFill=fill;}
        public void RefreshDodge(float remaining,bool allowed)
        {
            dodgeFill.fillAmount=Mathf.Clamp01(remaining/DodgeWindow.CooldownSeconds);
            dodge.interactable=allowed&&remaining<=0;
        }
        void LateUpdate(){if(safe&&safe.rect.size!=lastSize)Fit();}
        void Fit()
        {
            if(!safe||safe.rect.width<=0||safe.rect.height<=0)return;
            lastSize=safe.rect.size;
            // Leave a 16-unit gap below OFFER ORBS, plus the safe-area bottom inset.
            float scale=Mathf.Min(1,Mathf.Max(1,lastSize.y-337)/372,lastSize.x*.4f/355);
            transform.localScale=Vector3.one*scale;
        }
    }
    /// <summary>Transparent corners between circular controls remain available for camera dragging.</summary>
    public sealed class CircularControlHitArea:MonoBehaviour,ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
        {
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screenPoint,eventCamera,out var point))return false;
            if(rect.rect.width<=0||rect.rect.height<=0)return false;
            point-=rect.rect.center;
            float x=point.x/(rect.rect.width*.5f),y=point.y/(rect.rect.height*.5f);
            return x*x+y*y<=1;
        }
    }
}
