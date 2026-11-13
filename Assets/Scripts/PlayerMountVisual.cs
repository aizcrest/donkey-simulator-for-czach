using System;
using System.Collections.Generic;
using UnityEngine;
namespace DonkeyForest
{
    /// <summary>Original segmented models; ordinary meshes and transform animation, no FBX skinning.</summary>
    public sealed class PlayerMountVisual:MonoBehaviour
    {
        [Serializable] public class ModelData { public Node[] nodes; }
        [Serializable] public class Node
        {
            public string name,parent,shape;
            public float[] position,rotation,scale,color;
        }
        readonly Dictionary<string,Transform> joints=new Dictionary<string,Transform>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<MeshRenderer> renderers=new List<MeshRenderer>();
        ForestGame game;
        bool moving,fast;
        float swing,phase;
        static readonly string[] Legs={"FL","FR","RL","RR"};
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        public void Setup(ForestGame owner)
        {
            game=owner;
            var asset=Resources.Load<TextAsset>("MountedModels");
            if(!asset)throw new InvalidOperationException("Missing original mounted model data.");
            var data=JsonUtility.FromJson<ModelData>(asset.text);
            Mesh round=MakeShape(false),box=MakeShape(true);
            meshes.Add(round);meshes.Add(box);
            var groups=new Dictionary<string,List<CombineInstance>>();
            var parents=new Dictionary<string,Transform>();var colors=new Dictionary<string,Color>();
            foreach(var node in data.nodes)
            {
                Transform parent=string.IsNullOrEmpty(node.parent)?transform:joints[node.parent];
                if(string.IsNullOrEmpty(node.shape))
                {
                    var joint=new GameObject(node.name).transform;joint.SetParent(parent,false);
                    joint.localPosition=V(node.position);joint.localRotation=Quaternion.Euler(V(node.rotation));
                    joints.Add(node.name,joint);continue;
                }
                Color color=new Color(node.color[0],node.color[1],node.color[2]);
                string key=node.parent+"|"+string.Join(",",node.color);
                if(!groups.ContainsKey(key)){groups.Add(key,new List<CombineInstance>());parents.Add(key,parent);colors.Add(key,color);}
                groups[key].Add(new CombineInstance{mesh=node.shape=="box"?box:round,
                    transform=Matrix4x4.TRS(V(node.position),Quaternion.Euler(V(node.rotation)),V(node.scale))});
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh{name="Mounted "+parents[group.Key].name};mesh.CombineMeshes(group.Value.ToArray(),true,true);
                mesh.RecalculateBounds();meshes.Add(mesh);
                var obj=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parents[group.Key],false);
                obj.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=game.Mat(colors[group.Key]);
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderers.Add(renderer);
            }
        }
        static Mesh MakeShape(bool box)
        {
            var verts=new List<Vector3>();var indices=new List<int>();
            Action<Vector3,Vector3,Vector3> triangle=(a,b,c)=>
            {
                var normal=Vector3.Cross(b-a,c-a);if(normal.sqrMagnitude<.00000001f)return;
                if(Vector3.Dot(normal,(a+b+c)/3)<0){var swap=b;b=c;c=swap;}
                int start=verts.Count;verts.Add(a);verts.Add(b);verts.Add(c);indices.Add(start);indices.Add(start+1);indices.Add(start+2);
            };
            if(box)
            {
                var p=new Vector3[8];for(int i=0;i<8;i++)p[i]=new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f);
                int[] faces={0,1,3,0,3,2,4,6,7,4,7,5,0,4,5,0,5,1,2,3,7,2,7,6,0,2,6,0,6,4,1,5,7,1,7,3};
                for(int i=0;i<faces.Length;i+=3)triangle(p[faces[i]],p[faces[i+1]],p[faces[i+2]]);
            }
            else
            {
                Func<int,int,Vector3> point=(ring,side)=>
                {
                    float phi=ring*Mathf.PI/6,theta=side*Mathf.PI/4;
                    return new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta))*.5f;
                };
                for(int r=0;r<6;r++)for(int s=0;s<8;s++)
                {triangle(point(r,s),point(r+1,s),point(r+1,s+1));triangle(point(r,s),point(r+1,s+1),point(r,s+1));}
            }
            var mesh=new Mesh{name=box?"Mounted unit box":"Mounted low-poly round"};
            mesh.SetVertices(verts);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void Animate(bool riding,bool charging,float attack){moving=riding;fast=charging;swing=attack;}
        void LateUpdate()
        {
            if(!game||game.Paused||game.Dead)return;
            phase+=Time.deltaTime*(moving?(fast?14:8):2);
            joints["Body"].localPosition=Vector3.up*(moving?Mathf.Abs(Mathf.Sin(phase))*.035f:Mathf.Sin(phase)*.008f);
            for(int i=0;i<Legs.Length;i++)
            {
                float stride=moving?Mathf.Sin(phase+(i==0||i==3?0:Mathf.PI))*(fast?32:22):0;
                bool kick=i>=2&&game.Skills.Book.Active(SkillId.Kick)>0;
                joints[Legs[i]].localRotation=Quaternion.Euler(kick?70:stride,0,0);
                joints[Legs[i]+"Knee"].localRotation=Quaternion.Euler(kick?-12:Mathf.Max(0,-stride)*.6f,0,0);
            }
            joints["Neck"].localRotation=Quaternion.Euler(Mathf.Sin(phase)*(moving?3:1),0,0);
            joints["Tail"].localRotation=Quaternion.Euler(0,Mathf.Sin(phase*.7f)*15,0);
            joints["Rider"].localRotation=Quaternion.Euler(moving?Mathf.Sin(phase)*2:0,0,0);
            bool guard=game.Skills.Book.Active(SkillId.Guard)>0;
            joints["LeftArm"].localRotation=Quaternion.Euler(guard?-70:0,0,guard?-20:0);
            float slash=swing>0?Mathf.Sin(swing/.3f*Mathf.PI):0;
            float spin=game.Skills.Book.Active(SkillId.Whirlwind)>0?Time.time*800:0;
            joints["RightArm"].localRotation=Quaternion.Euler(-slash*45,-slash*100+spin,0);
        }
        public string Diagnostics(Camera camera)
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);int vertices=0,visible=0;
            foreach(var renderer in renderers)
            {vertices+=renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount;if(GeometryUtility.TestPlanesAABB(planes,renderer.bounds))visible++;}
            return $"Original mounted models: {renderers.Count} mesh groups, {vertices} vertices, {visible} groups in camera; no skinned renderers or FBX animation dependencies.";
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
    }
}
