using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace DonkeyForest
{
    public sealed class LaunchSplash:MonoBehaviour
    {
        IEnumerator Start()
        {
            var artwork=Resources.Load<Sprite>("Branding/CzachsDonkeyJourney");
            if(artwork)
            {
                var canvas=gameObject.AddComponent<Canvas>();
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
                var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
                var background=new GameObject("Letterbox",typeof(RectTransform),typeof(Image));
                background.transform.SetParent(transform,false);
                Stretch(background.GetComponent<RectTransform>());
                background.GetComponent<Image>().color=new Color(.025f,.06f,.065f);
                var safe=new GameObject("Splash safe area",typeof(RectTransform),typeof(SafeArea));safe.transform.SetParent(transform,false);
                Stretch(safe.GetComponent<RectTransform>());
                var picture=new GameObject("Full splash artwork",typeof(RectTransform),typeof(Image));
                picture.transform.SetParent(safe.transform,false);
                Stretch(picture.GetComponent<RectTransform>());
                picture.GetComponent<RectTransform>().offsetMin=new Vector2(0,60);
                AddCredit(safe.transform);
                var image=picture.GetComponent<Image>();image.sprite=artwork;image.preserveAspect=true;image.raycastTarget=false;
                // Wait in real time, including if a previous Editor session changed timeScale.
                // No gameplay exists behind the splash, so enemies cannot attack during it.
                yield return new WaitForSecondsRealtime(2f);
            }
            if(!FindAnyObjectByType<ForestGame>())new GameObject("Czach’s Donkey Journey").AddComponent<ForestGame>();
            // Let the fully initialized world render before removing the overlay.
            yield return null;
            var overlay=GetComponent<Canvas>();if(overlay)overlay.enabled=false;
            if(artwork)
            {
                var texture=artwork.texture;
                Resources.UnloadAsset(artwork);Resources.UnloadAsset(texture);
            }
            Destroy(gameObject);
        }
        static void AddCredit(Transform parent)
        {
            var footer=new GameObject("Developer credit",typeof(RectTransform),typeof(Image));footer.transform.SetParent(parent,false);
            var rect=footer.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(0,60);
            var backdrop=footer.GetComponent<Image>();backdrop.color=new Color(.025f,.06f,.065f,.95f);backdrop.raycastTarget=false;
            var label=new GameObject("Credit text",typeof(RectTransform),typeof(Text));label.transform.SetParent(footer.transform,false);
            var text=label.GetComponent<Text>();text.text="Developed and built by Joven";
            text.font=Resources.Load<Font>("Fonts/LilitaOne-Regular");if(!text.font)text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=26;text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=26;
            text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.87f,.62f);text.raycastTarget=false;
            Stretch(text.rectTransform);text.rectTransform.offsetMin=new Vector2(20,8);text.rectTransform.offsetMax=new Vector2(-20,-8);
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
