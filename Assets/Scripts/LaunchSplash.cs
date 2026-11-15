using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace DonkeyForest
{
    public sealed class LaunchSplash:MonoBehaviour
    {
        Sprite artwork;
        RectTransform canvasRect,safeRect,pictureRect,backdropRect,footerRect;
        Text credit;
        Vector2 lastCanvasSize;
        Rect lastSafeArea;
        int lastWidth,lastHeight;

        IEnumerator Start()
        {
            artwork=Resources.Load<Sprite>("Branding/CzachsDonkeyJourney");
            if(artwork)
            {
                var canvas=gameObject.AddComponent<Canvas>();
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
                canvasRect=(RectTransform)transform;
                var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
                var background=new GameObject("Splash background",typeof(RectTransform),typeof(Image));
                background.transform.SetParent(transform,false);Stretch(background.GetComponent<RectTransform>());
                background.GetComponent<Image>().color=new Color(.025f,.06f,.065f);
                // A dim cover image fills wide-screen margins; the readable foreground always fits in full.
                backdropRect=CreateImage("Dim full-screen backdrop",transform,new Color(.2f,.24f,.23f,1));
                var safe=new GameObject("Splash safe area",typeof(RectTransform));safe.transform.SetParent(transform,false);
                safeRect=safe.GetComponent<RectTransform>();Stretch(safeRect);
                pictureRect=CreateImage("Full splash artwork",safeRect,Color.white);
                AddCredit(safeRect);
                // Fit after the scaler and canvas layout have updated, including the very first rendered frame.
                Canvas.willRenderCanvases+=RefreshLayout;
                Canvas.ForceUpdateCanvases();RefreshLayout();
                yield return new WaitForSecondsRealtime(2f);
            }
            if(!FindAnyObjectByType<ForestGame>())new GameObject("Czach’s Donkey Journey").AddComponent<ForestGame>();
            yield return null;
            var overlay=GetComponent<Canvas>();if(overlay)overlay.enabled=false;
            if(artwork)
            {
                var texture=artwork.texture;
                Resources.UnloadAsset(artwork);Resources.UnloadAsset(texture);
            }
            Destroy(gameObject);
        }
        RectTransform CreateImage(string name,Transform parent,Color tint)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            var image=obj.GetComponent<Image>();image.sprite=artwork;image.type=Image.Type.Simple;
            image.preserveAspect=true;image.useSpriteMesh=false;image.color=tint;image.raycastTarget=false;
            return rect;
        }
        void RefreshLayout()
        {
            if(!canvasRect||!artwork||Screen.width<=0||Screen.height<=0)return;
            Vector2 size=canvasRect.rect.size;Rect safe=Screen.safeArea;
            safe=Rect.MinMaxRect(Mathf.Clamp(safe.xMin,0,Screen.width),Mathf.Clamp(safe.yMin,0,Screen.height),
                Mathf.Clamp(safe.xMax,0,Screen.width),Mathf.Clamp(safe.yMax,0,Screen.height));
            if(size.x<=0||size.y<=0)return;
            if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
            if(size==lastCanvasSize&&safe==lastSafeArea&&lastWidth==Screen.width&&lastHeight==Screen.height)return;
            lastCanvasSize=size;lastSafeArea=safe;lastWidth=Screen.width;lastHeight=Screen.height;
            safeRect.anchorMin=new Vector2(safe.x/Screen.width,safe.y/Screen.height);
            safeRect.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            safeRect.offsetMin=safeRect.offsetMax=Vector2.zero;
            Vector2 available=new Vector2(size.x*safe.width/Screen.width,size.y*safe.height/Screen.height);
            Vector2 native=artwork.rect.size;
            float fit=Mathf.Min(available.x/native.x,available.y/native.y);
            pictureRect.sizeDelta=native*fit;
            // Only the decorative background uses cover/cropping. No reserved footer shrinks the main image.
            backdropRect.sizeDelta=native*Mathf.Max(size.x/native.x,size.y/native.y);
            float height=Mathf.Clamp(available.y*.055f,26,40);
            footerRect.sizeDelta=new Vector2(Mathf.Min(available.x,pictureRect.sizeDelta.x),height);
            footerRect.anchoredPosition=new Vector2(0,Mathf.Clamp(available.y*.012f,4,10));
            credit.resizeTextMaxSize=Mathf.Clamp(Mathf.RoundToInt(height*.6f),16,24);
        }
        void AddCredit(Transform parent)
        {
            var footer=new GameObject("Developer credit",typeof(RectTransform),typeof(Image));footer.transform.SetParent(parent,false);
            footerRect=footer.GetComponent<RectTransform>();footerRect.anchorMin=footerRect.anchorMax=footerRect.pivot=new Vector2(.5f,0);
            var backdrop=footer.GetComponent<Image>();backdrop.color=new Color(.025f,.06f,.065f,.8f);backdrop.raycastTarget=false;
            var label=new GameObject("Credit text",typeof(RectTransform),typeof(Text));label.transform.SetParent(footer.transform,false);
            credit=label.GetComponent<Text>();credit.text="Developed and built by Joven";
            credit.font=Resources.Load<Font>("Fonts/LilitaOne-Regular");if(!credit.font)credit.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            credit.fontSize=24;credit.resizeTextForBestFit=true;credit.resizeTextMinSize=12;credit.resizeTextMaxSize=24;
            credit.alignment=TextAnchor.MiddleCenter;credit.color=new Color(1,.87f,.62f);credit.raycastTarget=false;
            Stretch(credit.rectTransform);credit.rectTransform.offsetMin=new Vector2(12,3);credit.rectTransform.offsetMax=new Vector2(-12,-3);
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        void OnDestroy(){Canvas.willRenderCanvases-=RefreshLayout;}
    }
}
