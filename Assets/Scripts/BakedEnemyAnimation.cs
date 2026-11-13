using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DonkeyForest
{
    // Offline Blender poses render as ordinary meshes. Frames are shared by all enemies of a species.
    public sealed class BakedEnemyAnimation:MonoBehaviour
    {
        sealed class Clip { public float duration;public Mesh[] frames; }
        sealed class Library { public Dictionary<string,Clip> clips=new Dictionary<string,Clip>();public Material material; }
        static readonly Dictionary<string,Library> cache=new Dictionary<string,Library>();
        Library library;Clip clip;MeshFilter filter;MeshRenderer meshRenderer;
        string state;float clock;int frame=-1;
        public static bool Supports(string kind)=>kind=="Deer"||kind=="Wolf"||kind=="Fox"||kind=="Zombie"||kind=="Dragon";
        public void Setup(string kind)
        {
            if(!cache.TryGetValue(kind,out library)){library=Load(kind);cache.Add(kind,library);}
            filter=gameObject.AddComponent<MeshFilter>();meshRenderer=gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial=library.material;
            meshRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;meshRenderer.receiveShadows=false;
            Play("idle");
        }
        static Library Load(string kind)
        {
            var asset=Resources.Load<TextAsset>("EnemyFrames/"+kind);
            if(!asset)throw new InvalidOperationException("Missing baked enemy frames: "+kind);
            var result=new Library();
            try
            {
                using(var reader=new BinaryReader(new MemoryStream(asset.bytes)))
                {
                    if(Encoding.ASCII.GetString(reader.ReadBytes(4))!="FRM1")throw new InvalidDataException("Invalid enemy frames: "+kind);
                    int vertices=reader.ReadInt32(),indexCount=reader.ReadInt32(),clips=reader.ReadInt32();
                    if(vertices<3||vertices>65535||indexCount<3||indexCount>300000||clips<1||clips>8)throw new InvalidDataException("Invalid enemy frame counts: "+kind);
                    var colors=new Color32[vertices];for(int v=0;v<vertices;v++)colors[v]=new Color32(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte());
                    var indices=new int[indexCount];for(int i=0;i<indexCount;i++)indices[i]=reader.ReadInt32();
                    for(int c=0;c<clips;c++)
                    {
                        int length=reader.ReadInt32();if(length<1||length>32)throw new InvalidDataException("Invalid clip name.");
                        string name=Encoding.ASCII.GetString(reader.ReadBytes(length));float duration=reader.ReadSingle();int count=reader.ReadInt32();
                        if(duration<=0||count<1||count>32)throw new InvalidDataException("Invalid clip duration/frame count.");
                        var entry=new Clip{duration=duration,frames=new Mesh[count]};result.clips.Add(name,entry);
                        var positions=new Vector3[vertices];var normals=new Vector3[vertices];
                        for(int f=0;f<count;f++)
                        {
                            for(int v=0;v<vertices;v++)
                            {
                                positions[v]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                                normals[v]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                            }
                            var mesh=new Mesh{name=kind+" "+name+" "+f};entry.frames[f]=mesh;
                            mesh.vertices=positions;mesh.normals=normals;mesh.colors32=colors;mesh.triangles=indices;
                            mesh.RecalculateBounds();mesh.UploadMeshData(true);
                        }
                    }
                }
                var shader=Shader.Find("Forest Rider/Model Vertex Colors");
                if(!shader)throw new InvalidOperationException("Missing enemy vertex-color shader.");
                result.material=new Material(shader){name=kind+" baked colors",enableInstancing=true};
                return result;
            }
            catch{Dispose(result);throw;}
            finally{Resources.UnloadAsset(asset);}
        }
        public void Play(string desired)
        {
            if(desired==state)return;state=desired;
            if(!library.clips.TryGetValue(desired,out clip))clip=library.clips["idle"];
            clock=0;frame=0;filter.sharedMesh=clip.frames[0];
        }
        public void ValidateFrames()
        {
            foreach(var entry in library.clips.Values)foreach(var mesh in entry.frames)
                if(!mesh||mesh.vertexCount<3||mesh.GetIndexCount(0)<3||mesh.bounds.size.y<.05f)
                    throw new InvalidDataException("Enemy has an empty mesh frame: "+name);
        }
        void Update()
        {
            var game=ForestGame.Instance;if(game&&(game.Paused||game.Dead))return;
            clock=Mathf.Repeat(clock+Time.deltaTime,clip.duration);
            int next=Mathf.Min(clip.frames.Length-1,(int)(clock/clip.duration*clip.frames.Length));
            if(frame!=next&&meshRenderer.isVisible){frame=next;filter.sharedMesh=clip.frames[frame];}
        }
        static void Dispose(Library data)
        {
            foreach(var entry in data.clips.Values)foreach(var mesh in entry.frames)if(mesh)Destroy(mesh);
            if(data.material)Destroy(data.material);
        }
        public static void ReleaseAll(){foreach(var data in cache.Values)Dispose(data);cache.Clear();}
    }
}
