using UnityEngine;
using UnityEngine.Rendering;
namespace DonkeyForest
{
    /// <summary>Reused ribbons: no spawning, particle simulation, lights or bloom required.</summary>
    public sealed class SkillVisualEffects:MonoBehaviour
    {
        ForestGame game;
        Material material;
        Transform root;
        readonly LineRenderer[,] lines=new LineRenderer[4,3];
        readonly float[] remaining=new float[4],duration=new float[4],radius=new float[4];
        readonly Color[] colors={new Color(.25f,.92f,1),new Color(.35f,.65f,1),new Color(1,.78f,.22f),new Color(1,.46f,.12f)};
        public void Setup(ForestGame owner)
        {
            game=owner;root=new GameObject("Mounted skill effects").transform;root.SetParent(game.rider,false);
            material=new Material(Shader.Find("Forest Rider/Skill Glow"));
            for(int skill=0;skill<4;skill++)for(int ribbon=0;ribbon<3;ribbon++)
            {
                var obj=new GameObject(((SkillId)skill)+" ribbon "+ribbon);obj.transform.SetParent(root,false);
                var line=obj.AddComponent<LineRenderer>();lines[skill,ribbon]=line;
                line.useWorldSpace=false;line.sharedMaterial=material;line.enabled=false;
                line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                line.lightProbeUsage=LightProbeUsage.Off;line.reflectionProbeUsage=ReflectionProbeUsage.Off;
                line.numCapVertices=0;line.numCornerVertices=0;line.textureMode=LineTextureMode.Stretch;
            }
        }
        public void Play(SkillId id,float seconds,float reach)
        {int i=(int)id;remaining[i]=duration[i]=seconds;radius[i]=reach;}
        public void Clear()
        {
            for(int i=0;i<4;i++){remaining[i]=0;for(int j=0;j<3;j++)if(lines[i,j])lines[i,j].enabled=false;}
        }
        void LateUpdate()
        {
            if(!game)return;
            if(game.Dead){Clear();return;}
            if(game.Paused)return;
            int segments=game.data.quality==0?16:game.data.quality==1?24:32;
            int ribbons=game.data.quality==0?2:3;
            for(int skill=0;skill<4;skill++)
            {
                if(remaining[skill]<=0)continue;
                remaining[skill]=Mathf.Max(0,remaining[skill]-Time.deltaTime);
                float progress=1-remaining[skill]/duration[skill],elapsed=duration[skill]-remaining[skill];
                // Short skills flash immediately; guard ramps in, then fades before expiry.
                float alpha=Mathf.Min(1,remaining[skill]/.15f)*(skill==1?Mathf.Min(1,elapsed/.12f):1);
                for(int ribbon=0;ribbon<3;ribbon++)
                {
                    var line=lines[skill,ribbon];line.enabled=ribbon<ribbons&&remaining[skill]>0;
                    if(!line.enabled)continue;
                    if(line.positionCount!=segments+1)line.positionCount=segments+1;
                    var color=colors[skill];color.a=alpha*.9f;line.startColor=color;color.a=alpha*.12f;line.endColor=color;
                    line.startWidth=skill==1?.08f:.18f;line.endWidth=.025f;
                    for(int k=0;k<=segments;k++)
                    {
                        float t=k/(float)segments;Vector3 p;
                        if(skill==0) // Two/three spinning sword arcs sweep the damage radius.
                        {
                            float a=progress*Mathf.PI*4+ribbon*Mathf.PI*2/ribbons+t*Mathf.PI*1.15f;
                            float reach=radius[skill]*Mathf.Lerp(.45f,1,Mathf.Sin(progress*Mathf.PI*.5f));
                            p=new Vector3(Mathf.Cos(a)*reach,.85f+ribbon*.23f+Mathf.Sin(t*Mathf.PI)*.3f,Mathf.Sin(a)*reach);
                        }
                        else if(skill==1) // Rotating upright shield hoops enclose the mount and rider.
                        {
                            float a=t*Mathf.PI*2,turn=elapsed*.9f+ribbon*Mathf.PI/ribbons;
                            p=new Vector3(Mathf.Cos(a)*Mathf.Cos(turn)*1.35f,1.5f+Mathf.Sin(a)*1.45f,Mathf.Cos(a)*Mathf.Sin(turn)*1.6f);
                        }
                        else if(skill==2) // Tapered speed ribbons extend behind the moving donkey.
                        {
                            float side=ribbon==0?-1:ribbon==1?1:0;
                            p=new Vector3(side*(.65f+t*.45f),.65f+(ribbon==2?1.1f:0)+Mathf.Sin(t*6-elapsed*24)*.08f,-.5f-t*3.2f);
                        }
                        else // Rear-facing hoof shockwaves expand through the kick cone.
                        {
                            float a=Mathf.PI+(t-.5f)*2.3f;
                            float reach=radius[skill]*Mathf.Lerp(.25f,1,progress)*(1-ribbon*.16f);
                            p=new Vector3(Mathf.Sin(a)*reach,.35f+ribbon*.24f+Mathf.Sin(t*Mathf.PI)*.2f,Mathf.Cos(a)*reach);
                        }
                        line.SetPosition(k,p);
                    }
                }
            }
        }
        void OnDestroy(){if(root)Destroy(root.gameObject);if(material)Destroy(material);}
    }
}
