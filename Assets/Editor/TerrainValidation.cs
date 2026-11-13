using System;
using UnityEditor;
using UnityEngine;

namespace DonkeyForest.Editor
{
    public static class TerrainValidation
    {
        [MenuItem("Forest Rider/Validate terrain (Play Mode)")]
        public static void Validate()
        {
            var game = ForestGame.Instance;
            Require(EditorApplication.isPlaying && game && game.Landscape,
                "Enter Play Mode and wait for the forest to generate before validating.");
            Terrain surface = game.Landscape.Surface;
            TerrainData data = surface.terrainData;
            Require(surface.GetComponent<TerrainCollider>().terrainData == data, "Terrain collider data mismatch.");
            Require(data.heightmapResolution == 257 && data.size == new Vector3(180,24,180), "Unexpected terrain dimensions.");
            Require(data.terrainLayers.Length == 3, "Expected moss, earth and stone terrain layers.");
            Require(data.detailPrototypes.Length > 0, "Missing grass prototype.");
            Require(data.detailScatterMode == DetailScatterMode.InstanceCountMode, "Grass map must use instance counts.");
            foreach(var layer in data.terrainLayers) ValidateTexture(layer.diffuseTexture);
            ValidateTexture(data.detailPrototypes[0].prototypeTexture);
            Require(RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat,
                "Runtime day/night lighting needs flat ambient lighting.");
            float min = float.MaxValue, max = float.MinValue;
            foreach (float value in data.GetHeights(0,0,257,257))
            {
                Require(!float.IsNaN(value) && value >= 0 && value <= 1, "Invalid heightmap sample.");
                min = Mathf.Min(min,value);max = Mathf.Max(max,value);
            }
            Require((max-min)*data.size.y > 7, "Expected substantial hills rather than a flat floor.");
            var paint = data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
            for(int z=0;z<data.alphamapHeight;z++)
                for(int x=0;x<data.alphamapWidth;x++)
                {
                    float sum=0;
                    for(int layer=0;layer<3;layer++)
                    {
                        Require(paint[z,x,layer]>=0 && paint[z,x,layer]<=1, "Invalid terrain layer weight.");
                        sum+=paint[z,x,layer];
                    }
                    Require(Mathf.Abs(sum-1)<.001f, "Terrain layer weights do not add up to one.");
                }
            for(int z=-75;z<=75;z+=15)
                for(int x=-75;x<=75;x+=15)
                {
                    Vector3 ground=game.Ground(new Vector3(x,0,z));
                    bool hit=Physics.Raycast(new Vector3(x,40,z),Vector3.down,out var result,50,1<<ForestTerrain.GroundLayer);
                    Require(hit && Mathf.Abs(ground.y-result.point.y)<.15f, "Sampled terrain and physics surface disagree.");
                }
            Vector3 center=game.Ground(Vector3.zero);
            Require(Mathf.Abs(center.y-1.5f)<.01f, "Starting clearing must stay level.");
            Require(Mathf.Abs(game.Ground(new Vector3(4,0,0)).y-center.y)<.01f, "Starting clearing is uneven.");
            Require(ForestTerrain.PathWeight(0,0)>.9f, "Camp should have an earth surface.");
            int count=0;
            foreach(int blades in data.GetDetailLayer(0,0,128,128,0)) count+=blades;
            Require(count>0, "Grass density map is empty.");
            Debug.Log($"Terrain validation passed: {max*24-min*24:F1}m relief, normalized layers, {count} detail instances before density scaling, collider samples and level camp. Still test hill movement and camera on a device.");
        }
        static void Require(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException(message);
        }
        static void ValidateTexture(Texture2D texture)
        {
            Require(texture && texture.isReadable && !texture.ignoreMipmapLimit,
                "Generated textures must opt into mip limits and remain readable for quality switching in Unity 6.");
        }
    }
}
