using System;
using System.Collections.Generic;
using UnityEngine;
namespace DonkeyForest
{
    /// <summary>Shared flat-shaded meshes; avoids thousands of sphere triangles per tree.</summary>
    public sealed class ForestPropMeshes:IDisposable
    {
        Mesh cone,trunk,stone;
        public Mesh Cone=>cone?cone:(cone=Round(true));
        public Mesh Trunk=>trunk?trunk:(trunk=Round(false));
        public Mesh Stone=>stone?stone:(stone=Rock());
        static Mesh Round(bool pointed)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
                Vector3 p=new Vector3(Mathf.Cos(a)*.5f,pointed?-.5f:-1,Mathf.Sin(a)*.5f);
                Vector3 q=new Vector3(Mathf.Cos(b)*.5f,pointed?-.5f:-1,Mathf.Sin(b)*.5f);
                Add(vertices,triangles,p,Vector3.up*(pointed?.5f:1)+new Vector3(pointed?0:p.x,0,pointed?0:p.z),q);
                if(!pointed){Vector3 topP=new Vector3(p.x,1,p.z),topQ=new Vector3(q.x,1,q.z);Add(vertices,triangles,q,topP,topQ);Add(vertices,triangles,Vector3.up,topQ,topP);}
                Add(vertices,triangles,Vector3.down*(pointed?.5f:1),p,q);
            }
            return Build(pointed?"8-sided pine canopy":"8-sided trunk",vertices,triangles);
        }
        static void Add(List<Vector3> vertices,List<int> triangles,Vector3 a,Vector3 b,Vector3 c)
        {int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);}
        static Mesh Rock()
        {
            float t=(1+Mathf.Sqrt(5))/2;
            var p=new[]{new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)};
            int[] faces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<faces.Length;i+=3)Add(vertices,triangles,p[faces[i]].normalized*.5f,p[faces[i+1]].normalized*.5f,p[faces[i+2]].normalized*.5f);
            return Build("20-face weathered rock",vertices,triangles);
        }
        static Mesh Build(string name,List<Vector3> vertices,List<int> triangles)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void Dispose(){if(cone)UnityEngine.Object.Destroy(cone);if(trunk)UnityEngine.Object.Destroy(trunk);if(stone)UnityEngine.Object.Destroy(stone);}
    }
}
