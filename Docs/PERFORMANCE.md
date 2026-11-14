# Mobile performance plan

Low is the default quality preset and targets 30 FPS; Balanced also targets 30, High targets 60. Change it under CAMP → SETTINGS. The choice saves locally. A selected marker and the HUD's quality/FPS text make the current setting visible.

Implemented costs controls: low-poly shared forest meshes and static batching; at most 12/16/20 ordinary enemies on Low for Normal/Hard/Extreme; low grass range/density; 75% render dimensions on mobile; half-size texture mips; two-weight skinning; no real-time shadows/reflection probes/MSAA/HDR on Low; cached animation action selection and off-screen animation culling; compressed/reduced imported clips; 5Hz HUD updates; shared materials; mono 22.05kHz sounds with streamed ambience/music and rate-limited impacts. The dragon encounter suspends ordinary enemies, so it does not stack a boss on the wildlife budget. Raising quality does not remove these basic caching/batching improvements.

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

Replacement player presentation: ordinary low-poly meshes grouped by joint/material replace the failed FBX player assets and CPU pose-baking workaround. Only joint transforms animate each frame; geometry is combined once at startup and reused across all graphics presets. Device profiling is still required.

Animated enemies use cached ordinary-mesh poses shared per species. Pose switches replace the shared mesh reference and skip off-screen assignments; runtime bone evaluation/CPU baking is removed for these five species. The pose files add about 12.6 MiB of source data. Mesh CPU copies and loaded binary assets are released after upload, while shared GPU frames remain until game shutdown. Measure first-spawn loading, GPU memory, sampled animation smoothness and sustained frame rate on actual low-end phones.
