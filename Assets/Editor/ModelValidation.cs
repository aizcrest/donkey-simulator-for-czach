using System;
using UnityEditor;
using UnityEngine;

namespace DonkeyForest.Editor
{
    public static class ModelValidation
    {
        [MenuItem("Forest Rider/Report enemy rendering (Play Mode)")]
        public static void ReportEnemies()
        {
            var game=ForestGame.Instance;
            Require(EditorApplication.isPlaying&&game,"Enter Play Mode before reporting enemies.");
            var planes=GeometryUtility.CalculateFrustumPlanes(game.View);
            var report=new System.Text.StringBuilder("Live enemy renderers:");
            foreach(var visual in UnityEngine.Object.FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None))
                foreach(var renderer in visual.Model.GetComponentsInChildren<Renderer>(true))
                {
                    var filter=renderer.GetComponent<MeshFilter>();
                    report.Append($"\n{visual.name}: {renderer.GetType().Name}, enabled={renderer.enabled}, active={renderer.gameObject.activeInHierarchy}, vertices={(filter&&filter.sharedMesh?filter.sharedMesh.vertexCount:0)}, inCamera={GeometryUtility.TestPlanesAABB(planes,renderer.bounds)}, bounds={renderer.bounds}");
                }
            Debug.Log(report.ToString());
        }
        [MenuItem("Forest Rider/Report mounted rendering (Play Mode)")]
        public static void ReportMounted()
        {
            var game=ForestGame.Instance;
            Require(EditorApplication.isPlaying&&game,"Enter Play Mode before reporting mounted rendering.");
            var presentation=game.rider.GetComponent<PlayerMountVisual>();
            Require(presentation,"Mounted mesh presentation component is missing.");
            Debug.Log(presentation.Diagnostics(game.View));
        }
        [MenuItem("Forest Rider/Validate model presentation (Play Mode)")]
        public static void Validate()
        {
            Require(EditorApplication.isPlaying, "Enter Play Mode before validating imported models.");
            string[] kinds={"Bear","Deer","Moose","Wolf","Fox","Zombie","Dragon"};
            float[] heights={1.65f,2,3,1.1f,.85f,1.9f,3.5f};
            for(int i=0;i<kinds.Length;i++)
            {
                var root=new GameObject("Temporary model validation");
                try
                {
                    // A translated, rotated spawn catches accidental use of world-space bounds.
                    root.transform.SetPositionAndRotation(new Vector3(23,40,-31),Quaternion.Euler(0,137,0));
                    var visual=EnemyVisual.Create(kinds[i],root.transform,heights[i]);
                    var frames=visual.Model.GetComponent<BakedEnemyAnimation>();if(frames)frames.ValidateFrames();
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
                            bool needsTexture=kinds[i]=="Bear"||
                                (kinds[i]=="Moose"&&!material.name.ToLowerInvariant().Contains("antler"));
                            if(needsTexture)Require(material.mainTexture,kinds[i]+" is missing its color texture.");
                            if(BakedEnemyAnimation.Supports(kinds[i]))Require(material.shader.name=="Forest Rider/Model Vertex Colors",
                                kinds[i]+" must render its baked colors.");
                        }
                }
                finally{root.SetActive(false);UnityEngine.Object.Destroy(root);}
            }
            Debug.Log("All seven enemy models passed fitted height/grounding, animation extent and material checks. Inspect the mounted rider and moving actors in the Game view too.");
        }
        static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
        }
    }
}
