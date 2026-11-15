# Mobile performance plan

Low is the default quality preset and targets 30 FPS; Balanced also targets 30, High targets 60. Change it under CAMP → SETTINGS. The choice saves locally. A selected marker and the HUD's quality/FPS text make the current setting visible.

Implemented costs controls: low-poly shared forest meshes and static batching; at most 12/16/20 ordinary enemies on Low for Normal/Hard/Extreme; low grass range/density; 75% render dimensions on mobile; half-size texture mips; two-weight global skinning (the mounted pair explicitly retains four); no real-time shadows/reflection probes/MSAA/HDR on Low; cached animation action selection and off-screen animation culling; compressed/reduced imported clips; 5Hz HUD updates; shared materials; mono 22.05kHz sounds with streamed ambience/music and rate-limited impacts. The dragon encounter suspends ordinary enemies, so it does not stack a boss on the wildlife budget. Raising quality does not remove these basic caching/batching improvements.

The Android build includes both ARMv7 and ARM64 with OpenGL ES 3 to cover 32-bit Android installations as well as 64-bit phones. Minimum OS remains Android 8. Devices without the required OS/graphics support are outside this build's compatibility. Including both ABIs increases a universal APK's size; Play App Bundles can deliver the appropriate ABI.

## Measurements still required

No phone benchmark or Unity profiler capture has been run here. Do not claim a minimum RAM/GPU requirement or guaranteed 30 FPS from source checks alone.

Test a representative 2GB/3GB RAM budget phone, a mid-range phone and a recent phone. For each record model, Android version, ABI, GPU, preset, render dimensions, load time, APK size, peak memory, typical/low FPS and temperature/battery observations. Test for at least 10 minutes to expose thermal throttling.

1. Cold-launch and revisit the forest: check startup generation, FBX/audio loading, save restore and memory spikes.
2. Ride through the densest forest, then turn the camera through foliage and climb hill crests.
3. Fill the wildlife cap, attack an elite group, combine charge/kick/whirlwind and inspect stutters/GC in Unity Profiler.
4. Play through night, collect gems and run several dragon cycles with fireballs, dive warnings, guard and dodges visible.
5. Change Low → High → Low repeatedly in camp. Confirm the current selection, resolution, grass distance and enemy cap; check that resolution scaling does not compound on repeated clicks/relaunches.
6. Background/resume, rotate the physical phone, test safe areas/multitouch and watch audio resume.

Use 33.3ms/frame as Low's target budget. If it misses: inspect actual CPU/GPU timing first, then reduce grass/draw distance or render scale. If startup is the issue, bake/generate terrain ahead of time or distribute generation over loading frames. If skinning/memory dominates, reduce visible rigs/clip keys before adding more visual effects.

The startup terrain is still generated synchronously, the world uses one native Terrain tile, and models use the built-in Standard shader. Those costs must be profiled before calling the game ready for very limited hardware. There is no automatic device benchmark, adaptive resolution or thermal scaling yet.

Unity 6.4 migration: generated terrain/grass textures opt into mip limits and retain about 267 KiB of readable pixel/mip data so runtime quality changes can re-upload them. This small CPU allocation is intentional; imported model textures retain their normal importer settings. Re-profile the new editor/player version on target phones.

v2 player presentation uses two explicitly bound skinned renderers (8,698 triangles, 8,728 vertices, 67 bones). Runtime model/clip data totals about 0.8 MiB. The knight's small gradient atlas is baked into vertex colors; no extra model textures are loaded. Only bone transforms interpolate each frame; no runtime mesh baking or per-frame array allocation occurs outside the diagnostic menu. Four bone weights are retained at all quality levels to preserve skin shape. Shadows are disabled on these two renderers. Full animation bounds and `updateWhenOffscreen` prevent an off-screen or stale-bound rig from freezing/disappearing. This single mounted pair needs on-device CPU/GPU profiling; source budgets do not establish a frame-rate guarantee.

Animated enemies use shared mesh pairs with GPU interpolation of vertex positions and normals. Only the mesh reference and an instanced blend value change during playback; off-screen looping animation skips renderer writes, and there is no runtime CPU skin baking or per-enemy vertex copy. Source poses now occupy about **15.3 MiB**. The shared pose meshes use approximately **35.4 MiB** of vertex/index payload when all five species are loaded, before driver/Unity overhead; this is higher than the earlier stepped-frame renderer. CPU pose arrays and binary assets are released after upload, and shared GPU frames are released at shutdown. Profile this memory/smoothness tradeoff on budget phones; no device performance claim is implied.

Melee contact checks use allocation-free segment/capsule math during active attack windows, with a terrain/scenery linecast only after an overlap. Large frame steps subdivide the active sweep into at most six short segments per attack. Moose charge uses a swept volume instead of per-frame proximity damage. No extra physics objects, attack colliders or coroutines are spawned for strikes.
