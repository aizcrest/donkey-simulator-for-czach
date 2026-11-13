using UnityEngine;
namespace DonkeyForest
{
    public sealed class DayNightSky:MonoBehaviour
    {
        Material material,previousSky;
        ForestGame game;Light sun;
        public void Setup(ForestGame owner,Light light)
        {
            game=owner;sun=light;previousSky=RenderSettings.skybox;
            material=new Material(Shader.Find("Forest Rider/Day Night Sky"));
            material.SetTexture("_Lookup",Resources.Load<Texture2D>("UI/sky-lookup"));
            RenderSettings.skybox=material;game.View.clearFlags=CameraClearFlags.Skybox;Tick();
        }
        public void Tick()
        {
            float angle=(game.data.time-.24f)*Mathf.PI*2;
            var direction=new Vector3(Mathf.Cos(angle)*.82f,Mathf.Sin(angle),Mathf.Cos(angle)*.57f).normalized;
            float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.18f,direction.y));
            float twilight=(1-Mathf.Clamp01(Mathf.Abs(direction.y)/.28f))*day;
            Color horizon=Color.Lerp(new Color(.065f,.09f,.16f),new Color(.60f,.76f,.83f),day);
            horizon=Color.Lerp(horizon,new Color(.87f,.42f,.24f),twilight*.7f);
            material.SetColor("_Top",Color.Lerp(new Color(.008f,.018f,.055f),new Color(.12f,.39f,.72f),day));
            material.SetColor("_Horizon",horizon);material.SetVector("_SunDir",direction);material.SetFloat("_Day",day);material.SetFloat("_Clock",game.data.time);
            sun.transform.rotation=Quaternion.LookRotation(direction.y>=0?-direction:direction);
            sun.intensity=Mathf.Lerp(.16f,1.1f,day);sun.color=Color.Lerp(new Color(.57f,.69f,1),Color.Lerp(Color.white,new Color(1,.67f,.43f),twilight),day);
            RenderSettings.ambientLight=Color.Lerp(new Color(.18f,.22f,.34f),new Color(.65f,.68f,.57f),day);
            RenderSettings.fogColor=horizon;game.View.backgroundColor=horizon;
        }
        void OnDestroy(){if(RenderSettings.skybox==material)RenderSettings.skybox=previousSky;if(material)Destroy(material);}
    }
}
