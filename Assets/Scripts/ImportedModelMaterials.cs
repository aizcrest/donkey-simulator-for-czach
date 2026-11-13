using System;
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
                            if(!diffuse)throw new InvalidOperationException("Missing model color texture: "+path);
                        }
                        if(kind=="Deer")
                        {
                            shader=Shader.Find("Forest Rider/Model Vertex Colors");
                            if(!shader)throw new InvalidOperationException("Missing model vertex-color shader.");
                        }
                        copy=new Material(source){name=source.name+" runtime",enableInstancing=true};
                        if(shader)copy.shader=shader;
                        if(diffuse)copy.mainTexture=diffuse;
                        copy.color=Color.white;
                        cache.Add(source,copy);
                    }
                    materials[i]=copy;
                }
                renderer.sharedMaterials=materials;
            }
        }
        public static void ReleaseAll()
        {
            foreach(var material in cache.Values)if(material)UnityEngine.Object.Destroy(material);
            cache.Clear();
        }
    }
}
