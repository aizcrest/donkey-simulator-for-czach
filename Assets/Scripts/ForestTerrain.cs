using System.Collections.Generic;
using UnityEngine;

namespace DonkeyForest
{
    /// <summary>Deterministic native Unity Terrain; generated once when a game starts.</summary>
    public sealed class ForestTerrain : MonoBehaviour
    {
        public const float Size = 180f;
        public const int SceneryLayer = 8;
        public const int GroundLayer = 9;
        const int HeightResolution = 257;
        const int SurfaceResolution = 128;
        const float Elevation = 24f;
        public Terrain Surface { get; private set; }
        public TerrainCollider GroundCollider { get; private set; }
        bool reportedRecovery;
        readonly List<Object> generated = new List<Object>();

        public void Generate()
        {
            var data = new TerrainData
            {
                name = "Forest hills (seed 7419)",
                heightmapResolution = HeightResolution,
                size = new Vector3(Size, Elevation, Size),
                alphamapResolution = SurfaceResolution,
                baseMapResolution = 256
            };
            generated.Add(data);
            var heights = new float[HeightResolution, HeightResolution];
            for (int z = 0; z < HeightResolution; z++)
                for (int x = 0; x < HeightResolution; x++)
                {
                    float wx = x * Size / (HeightResolution - 1) - Size / 2;
                    float wz = z * Size / (HeightResolution - 1) - Size / 2;
                    heights[z, x] = HeightAt(wx, wz) / Elevation;
                }
            data.SetHeights(0, 0, heights);
            data.terrainLayers = new[]
            {
                Layer("Moss and grass", new Color(.19f,.29f,.10f), new Color(.36f,.43f,.20f), 0, 5),
                Layer("Earth and leaf litter", new Color(.24f,.16f,.09f), new Color(.43f,.31f,.17f), 1, 4),
                Layer("Weathered stone", new Color(.29f,.30f,.27f), new Color(.52f,.51f,.44f), 2, 6)
            };
            PaintSurface(data);
            AddGrass(data);
            // Explicit component references keep the runtime-only terrain physics path
            // visible to player-build stripping; assign the same data to both components.
            var ground = new GameObject("Unity Terrain — rolling forest hills");
            ground.layer = GroundLayer;
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(-Size / 2, 0, -Size / 2);
            Surface = ground.AddComponent<Terrain>();Surface.terrainData=data;
            GroundCollider=ground.AddComponent<TerrainCollider>();GroundCollider.terrainData=data;
            GroundCollider.enabled=true;
            Physics.IgnoreLayerCollision(0,GroundLayer,false); // Player and wildlife use Default.
            Surface.drawHeightmap=true;
            var terrainMaterial=new Material(Shader.Find("Nature/Terrain/Standard"));
            generated.Add(terrainMaterial);Surface.materialTemplate=terrainMaterial;
            Surface.heightmapPixelError = 8;
            Surface.basemapDistance = 65;
            Surface.detailObjectDistance = 38;
            Surface.detailObjectDensity = .65f;
            // A runtime-only terrain may have no retained instancing variant in a
            // mobile player. Use the standard heightmap path consistently on phones.
            Surface.drawInstanced = !Application.isMobilePlatform&&SystemInfo.supportsInstancing;
            Surface.Flush();
            Physics.SyncTransforms();
        }

        // Broad smooth hills remain rideable; the central clearing stays level for revival.
        public static float HeightAt(float x, float z)
        {
            float hills = Mound(x, z, -38, 27, 10, 25)
                        + Mound(x, z, 46, -32, 12, 28)
                        + Mound(x, z, 39, 48, 8, 23)
                        + Mound(x, z, -46, -49, 7, 24);
            float noise = Mathf.PerlinNoise(x * .025f + 71.3f, z * .025f + 19.7f) * 2.4f
                        + Mathf.PerlinNoise(x * .065f + 41, z * .065f + 63) * .45f;
            float clearing = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(8, 19, new Vector2(x,z).magnitude));
            return 1.5f + (hills + noise - PathWeight(x,z) * .25f) * clearing;
        }
        static float Mound(float x, float z, float cx, float cz, float height, float radius)
        {
            float dx = x - cx, dz = z - cz;
            return height * Mathf.Exp(-(dx * dx + dz * dz) / (radius * radius));
        }
        public static float PathWeight(float x, float z)
        {
            float northTrail = Mathf.Abs(x - Mathf.Sin(z * .045f) * 8);
            float crossTrail = Mathf.Abs(z - Mathf.Sin(x * .04f) * 9);
            float distance = Mathf.Min(northTrail, crossTrail);
            float trail = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.3f, 3.3f, distance));
            float camp = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 9, new Vector2(x,z).magnitude));
            return Mathf.Max(trail, camp);
        }
        public Vector3 Ground(Vector3 position, float clearance = 0)
        {
            position.y = Surface.SampleHeight(position) + Surface.transform.position.y + clearance;
            return position;
        }
        public void KeepAboveGround(CharacterController actor)
        {
            if(!actor||!actor.enabled||!Surface)return;
            Vector3 position=actor.transform.position;
            Vector3 terrainOrigin=Surface.transform.position;
            if(position.x<terrainOrigin.x||position.z<terrainOrigin.z||position.x>terrainOrigin.x+Size||position.z>terrainOrigin.z+Size)return;
            float floor=Ground(position).y;
            float footOffset=actor.center.y-actor.height*.5f;
            // Leave ordinary contacts/skin-width overlap to physics; recover only a
            // controller whose feet have actually penetrated below the heightmap.
            if(position.y+footOffset>=floor-.15f)return;
            position.y=floor-footOffset+.08f;
            actor.enabled=false;actor.transform.position=position;actor.enabled=true;
            if(!reportedRecovery)
            {
                reportedRecovery=true;
                Debug.LogWarning("Recovered an actor below the forest terrain. Check terrain collision on this device. Collider ready: "+(GroundCollider&&GroundCollider.enabled&&GroundCollider.terrainData==Surface.terrainData));
            }
        }
        public float Slope(Vector3 position)
        {
            return Surface.terrainData.GetSteepness(
                Mathf.Clamp01((position.x + Size / 2) / Size),
                Mathf.Clamp01((position.z + Size / 2) / Size));
        }
        void PaintSurface(TerrainData data)
        {
            var paint = new float[SurfaceResolution, SurfaceResolution, 3];
            for (int z = 0; z < SurfaceResolution; z++)
                for (int x = 0; x < SurfaceResolution; x++)
                {
                    float u = (float)x / (SurfaceResolution - 1), v = (float)z / (SurfaceResolution - 1);
                    float wx = u * Size - Size / 2, wz = v * Size - Size / 2;
                    float slope = data.GetSteepness(u, v);
                    float high = Mathf.InverseLerp(9, 15, data.GetInterpolatedHeight(u, v));
                    float stone = Mathf.Clamp01(Mathf.InverseLerp(15, 30, slope) * .85f + high * .4f);
                    float patch = Mathf.PerlinNoise(wx * .09f + 13, wz * .09f + 93);
                    float earth = Mathf.Clamp01(PathWeight(wx,wz) * .95f + Mathf.Max(0, patch - .45f) * .6f);
                    paint[z,x,0] = (1 - stone) * (1 - earth);
                    paint[z,x,1] = (1 - stone) * earth;
                    paint[z,x,2] = stone;
                }
            data.SetAlphamaps(0, 0, paint);
        }
        TerrainLayer Layer(string label, Color low, Color high, int style, float tile)
        {
            const int resolution = 128;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, -1, false, false,
                new MipmapLimitDescriptor(true, null))
            {
                name = label, wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 2
            };
            var pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float u = (float)x / resolution, v = (float)y / resolution;
                    float broad = TileNoise(u, v, 8, style + 1);
                    float grain = TileNoise(u, v, 48, style + 12);
                    float t = Mathf.Clamp01(broad * .65f + grain * .35f);
                    Color c = Color.Lerp(low, high, t);
                    float fleck = Hash(x, y, style + 39);
                    if (style == 1 && fleck > .965f) c = Color.Lerp(c, new Color(.61f,.40f,.15f), .6f);
                    if (style == 2 && grain < .25f) c *= .73f;
                    pixels[y * resolution + x] = c;
                }
            // Unity 6 needs explicit opt-in and a CPU copy to re-upload on quality changes.
            // Three 128px maps plus the grass tuft use about 267 KiB of readable RGBA mips.
            texture.SetPixels(pixels);texture.Apply(true, false);generated.Add(texture);
            var layer = new TerrainLayer
            {
                name = label, diffuseTexture = texture,
                tileSize = Vector2.one * tile, metallic = 0, smoothness = .03f
            };
            generated.Add(layer);return layer;
        }
        // Periodic value noise avoids seams when the generated ground textures repeat.
        static float TileNoise(float u, float v, int cells, int seed)
        {
            float x = u * cells, y = v * cells;
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float tx = Mathf.SmoothStep(0,1,x-ix), ty = Mathf.SmoothStep(0,1,y-iy);
            return Mathf.Lerp(
                Mathf.Lerp(Hash(ix%cells,iy%cells,seed),Hash((ix+1)%cells,iy%cells,seed),tx),
                Mathf.Lerp(Hash(ix%cells,(iy+1)%cells,seed),Hash((ix+1)%cells,(iy+1)%cells,seed),tx),ty);
        }
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144269);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }
        void AddGrass(TerrainData data)
        {
            var texture = new Texture2D(32, 64, TextureFormat.RGBA32, -1, false, false,
                new MipmapLimitDescriptor(true, null))
            {
                name = "Original grass tuft", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[32 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 32; x++)
                {
                    float v = y / 63f, u = x / 31f;
                    float alpha = 0;
                    for (int blade = 0; blade < 5; blade++)
                    {
                        float tip = .55f + blade * .09f;
                        float t = v / tip;
                        float center = .18f + blade * .15f + Mathf.Sin(t * 1.7f + blade) * .09f * t;
                        if (t <= 1 && Mathf.Abs(u-center) < .036f * (1-t)) alpha = 1;
                    }
                    var color = Color.Lerp(new Color(.21f,.29f,.08f),new Color(.65f,.72f,.31f),v);
                    color.a = alpha;pixels[y * 32 + x] = color;
                }
            texture.SetPixels(pixels);texture.Apply(true, false);generated.Add(texture);
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            data.SetDetailResolution(128,16);
            data.detailPrototypes = new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = texture, renderMode = DetailRenderMode.GrassBillboard,
                    minWidth = .45f, maxWidth = .85f, minHeight = .35f, maxHeight = .7f,
                    healthyColor = new Color(.65f,.75f,.38f), dryColor = new Color(.73f,.65f,.37f),
                    noiseSpread = .2f
                }
            };
            var density = new int[128,128];
            for (int z = 0; z < 128; z++)
                for (int x = 0; x < 128; x++)
                {
                    float u = (x + .5f) / 128, v = (z + .5f) / 128;
                    float wx = u * Size - Size/2, wz = v * Size - Size/2;
                    if (PathWeight(wx,wz) > .2f || data.GetSteepness(u,v) > 24) continue;
                    float patch = Mathf.PerlinNoise(wx * .15f + 52, wz * .15f + 81);
                    density[z,x] = patch > .55f ? 2 : patch > .35f ? 1 : 0;
                }
            data.SetDetailLayer(0,0,0,density);
            data.wavingGrassStrength = .15f;data.wavingGrassSpeed = .25f;
            data.wavingGrassAmount = .15f;
        }
        void OnDestroy()
        {
            foreach (var resource in generated)
            {
                if(!resource)continue;
                if(Application.isPlaying)Destroy(resource);else DestroyImmediate(resource);
            }
        }
    }
}
