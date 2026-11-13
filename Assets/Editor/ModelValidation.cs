using System;
using UnityEditor;
using UnityEngine;

namespace DonkeyForest.Editor
{
    public static class ModelValidation
    {
        [MenuItem("Forest Rider/Validate model presentation (Play Mode)")]
        public static void Validate()
        {
            Require(EditorApplication.isPlaying, "Enter Play Mode before validating imported models.");
            string[] kinds={"Bear","Deer","Moose","Wolf","Fox","Zombie","Dragon","Donkey","Rider"};
            float[] heights={1.65f,2,3,1.1f,.85f,1.9f,3.5f,1.9f,1.75f};
            for(int i=0;i<kinds.Length;i++)
            {
                var root=new GameObject("Temporary model validation");
                try
                {
                    // A translated, rotated spawn catches accidental use of world-space bounds.
                    root.transform.SetPositionAndRotation(new Vector3(23,40,-31),Quaternion.Euler(0,137,0));
                    var visual=EnemyVisual.Create(kinds[i],root.transform,heights[i]);
                    Bounds initial=EnemyVisual.GeometryBounds(visual.Model,visual.transform);
                    Require(Mathf.Abs(initial.size.y-heights[i])<.03f,kinds[i]+" has incorrect fitted height.");
                    Require(Mathf.Abs(initial.min.y)<.03f,kinds[i]+" feet are not grounded.");
                    Require(new Vector2(initial.center.x,initial.center.z).magnitude<.03f,kinds[i]+" is off center.");
                    foreach(var animation in visual.GetComponentsInChildren<Animation>())
                    {
                        foreach(AnimationState clip in animation)
                        {
                            animation.Play(clip.name);clip.time=clip.length*.5f;animation.Sample();
                            Bounds posed=EnemyVisual.GeometryBounds(visual.Model,visual.transform);
                            Require(posed.size.magnitude>heights[i]*.3f && posed.size.magnitude<heights[i]*6,
                                kinds[i]+" changes scale unexpectedly in "+clip.name);
                            Require(posed.max.y>heights[i]*.2f && posed.min.y>-heights[i],
                                kinds[i]+" moves below its ground anchor in "+clip.name);
                        }
                    }
                    foreach(var renderer in visual.Model.GetComponentsInChildren<Renderer>())
                        foreach(var material in renderer.sharedMaterials)
                        {
                            Require(material && material.shader && material.shader.isSupported,
                                kinds[i]+" has a missing/unsupported material shader.");
                            bool needsTexture=kinds[i]=="Bear"||kinds[i]=="Zombie"||kinds[i]=="Rider"||
                                (kinds[i]=="Moose"&&!material.name.ToLowerInvariant().Contains("antler"));
                            if(needsTexture)Require(material.mainTexture,kinds[i]+" is missing its color texture.");
                            if(kinds[i]=="Deer")Require(material.shader.name=="Forest Rider/Model Vertex Colors",
                                "Deer must render its source vertex colors.");
                        }
                }
                finally{root.SetActive(false);UnityEngine.Object.Destroy(root);}
            }
            Debug.Log("All nine models passed fitted height/grounding, animation extent and material checks. Inspect the mounted rider and moving actors in the Game view too.");
        }
        static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
        }
    }
}
