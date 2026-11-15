using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DonkeyForest
{
    /// <summary>Compact, explicit skin bindings and sampled bone poses. No Avatar or FBX import dependency.</summary>
    public sealed class MountedRig : MonoBehaviour
    {
        struct Pose
        {
            public Vector3 position, scale;
            public Quaternion rotation;
            public static Pose Blend(Pose a, Pose b, float t) => new Pose {
                position=Vector3.LerpUnclamped(a.position,b.position,t),
                rotation=Quaternion.SlerpUnclamped(a.rotation,b.rotation,t),
                scale=Vector3.LerpUnclamped(a.scale,b.scale,t)};
        }
        sealed class Clip { public float duration; public bool loop; public int frames; public Pose[] poses; }
        readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        Transform[] bones;
        Pose[] current, previous;
        Mesh mesh;
        readonly MaterialPropertyBlock tint=new MaterialPropertyBlock();
        bool dodgeTint;
        Clip clip;
        string state;
        float clock, fade=1, playback=1;
        public SkinnedMeshRenderer Renderer { get; private set; }
        public int BoneCount => bones.Length;
        public int VertexCount => mesh.vertexCount;
        public int TriangleCount => (int)mesh.GetIndexCount(0)/3;
        public string State => state;

        public Transform Anchor(string boneName)
        {
            foreach(var bone in bones)if(bone.name==boneName)return bone;
            throw new InvalidOperationException("Missing mounted anchor: "+boneName);
        }
        static int Count(BinaryReader r,int min,int max)
        {
            int n=r.ReadInt32();if(n<min||n>max)throw new InvalidDataException("Mounted rig count is out of range.");return n;
        }
        static float Number(BinaryReader r)
        {
            float n=r.ReadSingle();if(float.IsNaN(n)||float.IsInfinity(n))throw new InvalidDataException("Non-finite mounted pose.");return n;
        }
        static Vector3 V(BinaryReader r)=>new Vector3(Number(r),Number(r),Number(r));
        static string Text(BinaryReader r)=>Encoding.UTF8.GetString(r.ReadBytes(Count(r,1,80)));
        public void Setup(string kind)
        {
            var asset=Resources.Load<TextAsset>("MountedV2/"+kind);
            if(!asset)throw new InvalidOperationException("Missing mounted rig: "+kind);
            try
            {
                using(var r=new BinaryReader(new MemoryStream(asset.bytes)))
                {
                    if(Encoding.ASCII.GetString(r.ReadBytes(4))!="MNT2")throw new InvalidDataException("Unsupported mounted rig.");
                    int nv=Count(r,3,65000),ni=Count(r,3,150000),nb=Count(r,1,128),nc=Count(r,1,16);
                    Vector3 low=V(r),high=V(r);var bounds=new Bounds((low+high)*.5f,high-low+Vector3.one*.3f);
                    bones=new Transform[nb];current=new Pose[nb];previous=new Pose[nb];var bind=new Matrix4x4[nb];
                    for(int b=0;b<nb;b++)
                    {
                        string name=Text(r);int parent=Count(r,-1,b-1);
                        bones[b]=new GameObject(name).transform;bones[b].SetParent(parent<0?transform:bones[parent],false);
                        var m=new Matrix4x4();for(int row=0;row<4;row++)for(int col=0;col<4;col++)m[row,col]=Number(r);bind[b]=m;
                    }
                    var positions=new Vector3[nv];var normals=new Vector3[nv];var colors=new Color32[nv];var weights=new BoneWeight[nv];
                    for(int v=0;v<nv;v++)
                    {
                        positions[v]=V(r);normals[v]=V(r);colors[v]=new Color32(r.ReadByte(),r.ReadByte(),r.ReadByte(),r.ReadByte());
                        var w=new BoneWeight{boneIndex0=r.ReadUInt16(),boneIndex1=r.ReadUInt16(),boneIndex2=r.ReadUInt16(),boneIndex3=r.ReadUInt16(),
                            weight0=Number(r),weight1=Number(r),weight2=Number(r),weight3=Number(r)};
                        if(w.boneIndex0>=nb||w.boneIndex1>=nb||w.boneIndex2>=nb||w.boneIndex3>=nb||
                            Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f)
                            throw new InvalidDataException("Invalid mounted skin weights.");
                        weights[v]=w;
                    }
                    var indices=new int[ni];for(int i=0;i<ni;i++)indices[i]=Count(r,0,nv-1);
                    for(int c=0;c<nc;c++)
                    {
                        string name=Text(r);var entry=new Clip{duration=Number(r),loop=Count(r,0,1)==1,frames=Count(r,2,64)};
                        if(entry.duration<=0)throw new InvalidDataException("Invalid mounted clip duration.");
                        entry.poses=new Pose[entry.frames*nb];
                        for(int i=0;i<entry.poses.Length;i++)entry.poses[i]=new Pose{position=V(r),
                            rotation=new Quaternion(Number(r),Number(r),Number(r),Number(r)),scale=V(r)};
                        clips.Add(name,entry);
                    }
                    if(r.BaseStream.Position!=r.BaseStream.Length)throw new InvalidDataException("Trailing mounted rig data.");
                    mesh=new Mesh{name=kind+" v2 bound mesh"};mesh.vertices=positions;mesh.normals=normals;mesh.colors32=colors;
                    mesh.boneWeights=weights;mesh.bindposes=bind;mesh.triangles=indices;mesh.bounds=bounds;
                    Renderer=gameObject.AddComponent<SkinnedMeshRenderer>();Renderer.sharedMesh=mesh;
                    Renderer.sharedMaterial=Resources.Load<Material>("MountedV2/Model");
                    if(!Renderer.sharedMaterial)throw new InvalidOperationException("Missing mounted model material.");
                    Renderer.bones=bones;Renderer.rootBone=transform;Renderer.localBounds=bounds;
                    // Explicit four-weight skinning and complete clip bounds avoid the old imported-rig culling issue.
                    Renderer.quality=SkinQuality.Bone4;Renderer.updateWhenOffscreen=true;
                    Renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Renderer.receiveShadows=false;
                    Play("idle",0);Tick(0);
                }
            }
            finally{Resources.UnloadAsset(asset);}
        }
        public void Play(string desired,float transition=.16f,float duration=0)
        {
            if(desired==state)return;
            if(!clips.TryGetValue(desired,out var next))throw new InvalidOperationException("Unknown mounted clip: "+desired);
            Array.Copy(current,previous,current.Length);clip=next;state=desired;clock=0;
            fade=transition<=0?1:0;fadeDuration=Mathf.Max(.001f,transition);playback=duration>0?clip.duration/duration:1;
        }
        float fadeDuration=.16f;
        public void Tick(float dt)
        {
            clock+=dt*playback;fade=Mathf.Min(1,fade+dt/fadeDuration);
            float time=clip.loop?Mathf.Repeat(clock,clip.duration):Mathf.Min(clock,clip.duration);
            float frame=time/clip.duration*(clip.loop?clip.frames:clip.frames-1);
            int a=Mathf.Min((int)frame,clip.frames-1),b=clip.loop?(a+1)%clip.frames:Mathf.Min(a+1,clip.frames-1);
            float fraction=frame-a,blend=fade*fade*(3-2*fade);
            for(int i=0;i<bones.Length;i++)
            {
                var pose=Pose.Blend(clip.poses[a*bones.Length+i],clip.poses[b*bones.Length+i],fraction);
                if(fade<1)pose=Pose.Blend(previous[i],pose,blend);
                current[i]=pose;bones[i].localPosition=pose.position;bones[i].localRotation=pose.rotation;bones[i].localScale=pose.scale;
            }
        }
        public void SetDodgeTint(bool active)
        {
            if(active==dodgeTint)return;dodgeTint=active;
            tint.SetColor("_Color",active?new Color(.45f,1,1):Color.white);Renderer.SetPropertyBlock(tint);
        }
        public void ValidateAllClips()
        {
            var savedCurrent=(Pose[])current.Clone();var savedPrevious=(Pose[])previous.Clone();
            var savedClip=clip;string savedState=state;
            float savedClock=clock,savedFade=fade,savedPlayback=playback,savedDuration=fadeDuration;
            try
            {
                foreach(var entry in clips)
                {
                    // Include half-key samples to catch interpolation/culling problems, not just stored poses.
                    state=null;Play(entry.Key,0);Tick(0);ValidatePose();
                    for(int i=0;i<entry.Value.frames*2;i++)
                    {Tick(entry.Value.duration/(entry.Value.frames*2));ValidatePose();}
                }
            }
            finally
            {
                current=savedCurrent;previous=savedPrevious;clip=savedClip;state=savedState;
                clock=savedClock;fade=savedFade;playback=savedPlayback;fadeDuration=savedDuration;
                for(int i=0;i<bones.Length;i++)
                {bones[i].localPosition=current[i].position;bones[i].localRotation=current[i].rotation;bones[i].localScale=current[i].scale;}
            }
        }
        public void ValidatePose()
        {
            if(!Renderer||!Renderer.sharedMaterial||!Renderer.sharedMaterial.shader.isSupported)
                throw new InvalidOperationException(name+" has no supported mounted material.");
            var baked=new Mesh();
            try
            {
                Renderer.BakeMesh(baked);baked.RecalculateBounds();
                if(baked.vertexCount<3||baked.bounds.size.y<.5f||baked.bounds.size.magnitude>6)
                    throw new InvalidOperationException(name+" has invalid skinned geometry: "+baked.bounds);
                foreach(var vertex in baked.vertices)
                    if(!mesh.bounds.Contains(vertex))throw new InvalidOperationException(name+" pose exceeds its culling bounds.");
            }
            finally{Destroy(baked);}
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
