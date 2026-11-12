using UnityEngine;
namespace DonkeyForest
{
    public static class MobilePerformance
    {
        static int nativeWidth,nativeHeight;
        public static int EnemyCap(int quality)=>quality==0?12:quality==1?18:22;
        public static int InitialEnemies(int quality)=>quality==0?8:quality==1?12:14;
        public static void Apply(ForestGame game)
        {
            int quality=Mathf.Clamp(game.data.quality,0,2);
            Application.targetFrameRate=quality==2?60:30;QualitySettings.vSyncCount=0;
            QualitySettings.shadows=ShadowQuality.Disable;QualitySettings.antiAliasing=quality==0?0:2;
            QualitySettings.pixelLightCount=1;QualitySettings.realtimeReflectionProbes=false;
            QualitySettings.skinWeights=quality==0?SkinWeights.TwoBones:SkinWeights.FourBones;
            QualitySettings.globalTextureMipmapLimit=quality==0?1:0;
            QualitySettings.anisotropicFiltering=quality==0?AnisotropicFiltering.Disable:AnisotropicFiltering.Enable;
            RenderSettings.fogDensity=quality==0?.022f:quality==1?.017f:.014f;
            game.View.allowHDR=quality==2;game.View.allowMSAA=quality>0;
            game.View.farClipPlane=quality==0?85:quality==1?120:160;
            var terrain=game.Landscape.Surface;terrain.drawInstanced=SystemInfo.supportsInstancing;
            terrain.heightmapPixelError=quality==0?16:quality==1?10:8;
            terrain.basemapDistance=quality==0?35:quality==1?50:65;
            terrain.detailObjectDistance=quality==0?16:quality==1?27:38;
            terrain.detailObjectDensity=quality==0?.16f:quality==1?.4f:.65f;
            if(Application.isMobilePlatform)
            {
                int width=Display.main.systemWidth,height=Display.main.systemHeight;
                if(width>0&&height>0)
                {
                    bool landscape=Screen.width>=Screen.height;
                    nativeWidth=landscape?Mathf.Max(width,height):Mathf.Min(width,height);
                    nativeHeight=landscape?Mathf.Min(width,height):Mathf.Max(width,height);
                }
                else if(nativeWidth<=0){nativeWidth=Screen.width;nativeHeight=Screen.height;}
                float scale=quality==0?.75f:quality==1?.9f:1;
                Screen.SetResolution(Mathf.Max(320,Mathf.RoundToInt(nativeWidth*scale)),Mathf.Max(180,Mathf.RoundToInt(nativeHeight*scale)),true);
            }
            // Lowering a preset removes surplus wildlife without granting rewards.
            for(int i=game.enemies.Count-1;i>=EnemyCap(quality);i--)
            {var enemy=game.enemies[i];game.enemies.RemoveAt(i);if(enemy)Object.Destroy(enemy.gameObject);}
        }
    }
}
