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
                var background=new GameObject("Letterbox",typeof(RectTransform),typeof(Image));
                background.transform.SetParent(transform,false);
                Stretch(background.GetComponent<RectTransform>());
                background.GetComponent<Image>().color=new Color(.025f,.06f,.065f);
                var picture=new GameObject("Full splash artwork",typeof(RectTransform),typeof(Image));
                picture.transform.SetParent(background.transform,false);
                Stretch(picture.GetComponent<RectTransform>());
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
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
