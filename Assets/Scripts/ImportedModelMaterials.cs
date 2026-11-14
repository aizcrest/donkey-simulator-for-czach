using System.Collections.Generic;
using UnityEngine;
namespace DonkeyForest
{
    /// <summary>Bind imported textures after import; share owned copies without modifying source assets.</summary>
    public static class ImportedModelMaterials
    {
        static readonly Dictionary<Material,Material> cache=new Dictionary<Material,Material>();
        public static void Apply(GameObject model,string kind)
        {
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(!source)continue;
                    string texture=kind=="Bear"?"Bear_Tex_Bear":
                        kind=="Moose"&&!source.name.ToLowerInvariant().Contains("antler")?"Moose_Elk_BaseColor":
                        kind=="Zombie"?"Zombie_Zombie_Atlas":
                        kind=="Rider"?"Rider_sonic-robo-blast-2-v22-32x":null;
                    if(texture==null&&kind!="Deer")continue;
                    if(!cache.TryGetValue(source,out var copy))
                    {
                        // Resources loads are safe here: this runs when spawning, never in an import callback.
                        Texture2D diffuse=null;Shader shader=null;
                        if(texture!=null)
                        {
                            string path="Enemies/"+kind+".fbm/"+texture;
                            diffuse=Resources.Load<Texture2D>(path);
                            if(!diffuse)
                            {
                                if(source.HasProperty("_MainTex"))diffuse=source.mainTexture as Texture2D;
                                Debug.LogWarning("Missing 2D model color texture: "+path+". Using "+(diffuse?"the existing 2D texture.":"a plain coat color. Reimport the model textures to restore the artwork."));
                            }
                        }
                        if(kind=="Deer")
                        {
                            shader=Shader.Find("Forest Rider/Model Vertex Colors");
                            if(!shader)Debug.LogWarning("Missing model vertex-color shader; using the imported shader.");
                        }
                        copy=CreateMaterial(source,kind,diffuse,shader);
                        cache.Add(source,copy);
                    }
                    materials[i]=copy;
                }
                renderer.sharedMaterials=materials;
            }
        }
        static Material CreateMaterial(Material source,string kind,Texture2D diffuse,Shader shader)
        {
            // Do not clone a source material's possibly invalid cubemap binding into
            // Standard's 2D _MainTex slot. Bind only a verified Texture2D.
            var copy=new Material(shader?shader:source.shader){name=source.name+" runtime",enableInstancing=true};
            if(copy.HasProperty("_MainTex"))
            {
                copy.mainTexture=diffuse;
                if(source.HasProperty("_MainTex"))
                {copy.mainTextureScale=source.mainTextureScale;copy.mainTextureOffset=source.mainTextureOffset;}
            }
            copy.color=diffuse||kind=="Deer"?Color.white:
                kind=="Bear"?new Color(.30f,.20f,.12f):kind=="Moose"?new Color(.39f,.30f,.20f):
                kind=="Zombie"?new Color(.4f,.6f,.3f):new Color(.55f,.6f,.65f);
            if(copy.HasProperty("_Metallic"))copy.SetFloat("_Metallic",0);
            if(copy.HasProperty("_Glossiness"))copy.SetFloat("_Glossiness",.1f);
            return copy;
        }
        public static void ReleaseAll()
        {
            foreach(var material in cache.Values)if(material)UnityEngine.Object.Destroy(material);
            cache.Clear();
        }
    }
}
