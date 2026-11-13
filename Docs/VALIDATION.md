# Validation

## Untitled scene startup/build correction — 2026-11-14

The reported stack trace confirmed that the startup delay callback tried to create Forest additively while an Untitled scene was open. Scene setup now checks all open scene paths before creating a missing Forest scene and quietly defers creation during startup. Explicit Open/Android/iOS commands save Untitled scenes first, honor cancellation, and stop when scene preparation fails. Temporary generated scenes are closed in a finally block, and the prior active scene is restored.

Repository checks and C# 9 syntax parsing passed (31 files). A temporary standalone harness compiled the actual GameBuild source against simulated Editor APIs; nine scenarios passed: deferred startup, saving an Untitled scene before building/opening, cancellation, an already-existing Forest scene, save failure cleanup and active-scene preservation. This is control-flow verification, **not Unity API compilation or a Unity Editor test**. Unity scene dialogs, actual scene creation and Android builds remain unverified here.

## Model import-loop correction — 2026-11-14

The user reported repeated changes to the Bear/Moose/Zombie/Rider FBXs and their matching `.fbm` PNGs. The custom model postprocessor synchronously loaded those textures and registered explicit artifact dependencies while importing the models. Removed those cross-asset lookups/dependencies from the callback (including the shader lookup), and bumped its version to force a fresh import. Explicit material binding now runs on shared, owned material copies when imported models spawn; it never modifies imported source materials. Baked enemies continue using their existing vertex colors.

Repository/asset/metadata checks and C# 9 syntax parsing passed (31 files). Model and texture source files were not changed. Added a Unity-only EditMode integration test that imports all four affected pairs twice, verifies stable dependency hashes and verifies unchanged PNG bytes. **That integration test and Unity import recovery have not run here because no Unity editor is installed.** The cause is inferred from the reported dependency pairs and importer code; local Unity confirmation is still required.

## Difficulty modes — 2026-11-13

Added saved Normal/Hard/Extreme selection, shared combat profiles, health/damage scaling for wildlife and the dragon, matched fireball warning/damage radii and travel times, and per-mode stun durations. Normal retains the original values. Existing enemies preserve health percentage on a mode change; an active boss restarts with old hazards removed.

Repository/metadata checks and C# 9 syntax parsing passed (29 files). All 16 standalone NUnit rule cases passed, including original Normal timing, mode ordering, repeated per-mode dragon cycles, Extreme overshoot, invalid mode fallback and injured/dead enemy health rescaling. Reviewed runtime call sites for damage, phase timing, telegraphs, save normalization and settings wiring. No Unity editor is installed: Unity API compilation, save migration through JsonUtility, settings layout, runtime combat integration and device balance remain unverified. Follow [TESTING.md](../TESTING.md).

## Touch artwork, sky and skill effects — 2026-11-13

Added ten original UI textures plus a packed cloud/star lookup, runtime sprites and cooldown overlays, a saved-clock skybox and reused skill ribbons. Inspected the generated texture contact sheet. Repository/metadata checks and C# 9 syntax parsing passed (27 files, zero syntax errors). Image dimensions/alpha channels and deterministic texture regeneration were checked. Existing nine standalone gameplay-rule tests passed.

The shaders are retained by the build setup and all effects scale with the existing quality setting. No Unity editor is installed here: shader/API compilation, Unity texture import, Play Mode appearance, touch interaction, Android builds and phone performance remain unverified. See the new acceptance steps in [TESTING.md](../TESTING.md).

## Animated enemy visibility correction — 2026-11-13

The user confirmed that only Bear and Moose were visible, and the Dragon was invisible too. Those two visible models are static; all five remaining types used the imported skinning path. Replaced that runtime path with precomputed ordinary-mesh frames derived from the same licensed assets. Bear/Moose rendering and all combat rules remain unchanged.

Validated 138 exported poses across Deer, Wolf, Fox, Zombie and Dragon: nonempty indexed geometry, finite coordinates, unit normals, valid clip timing, normalized initial grounding and a dragon flight clip. Rendered the exported binary geometry/colors in Blender for inspection; [asset preview](baked-enemy-models.png) is not Unity gameplay. Repository and C# syntax checks passed. Actual Unity shader compilation/rendering, animation smoothness and device memory/performance remain unverified here.

## Replacement mounted models — 2026-11-13

The user confirmed the imported player models and CPU-baked workaround still did not display. Replaced the active player presentation entirely with original segmented donkey/rider geometry, shared opaque materials, and transform-based animation. Removed the mounted CPU skinning component; enemy models and orbit controls remain unchanged.

The replacement layout was generated and rendered in Blender: ground clearance about 0.02m, total height about 2.77m, 4,796 triangles before grouping. The [asset preview](replacement-mounted-models.png) uses the same model layout as the game and was visually inspected; it is **not a Unity gameplay screenshot**. Repository/model-data checks and C# syntax parsing passed. Unity compilation, animation appearance and physical-phone performance still require testing locally.

## Mounted renderer workaround and orbit camera — 2026-11-13

The user's follow-up screenshot confirms that the previous sizing correction did **not** resolve the missing rider/donkey. The exact Unity-side cause remains unconfirmed. The reported duplicate AudioListener warning also indicates competing scene setup; startup now disables other display-zero scene cameras and active listeners while the game runs.

The rider/donkey now use a player-only CPU pose-baking compatibility path with ordinary MeshRenderers, bypassing imported skinned-renderer culling. Legacy animations continue evaluating; mesh poses refresh at 15/24/30 Hz on Low/Balanced/High. Wildlife retains its existing renderer. This adds CPU work that must be profiled on target phones. **Forest Rider → Report mounted rendering (Play Mode)** reports actual mesh counts, bounds and camera-frustum checks if visibility remains wrong.

Added mouse right-drag and empty-right-side touch orbit with bounded pitch, retained terrain/tree camera obstruction and boss distance. Gestures beginning over UI are excluded, and joystick/attack touches can run independently. Repository checks and C# syntax checks passed. **Unity rendering, camera gestures, listener behavior and device performance have not been tested here.**

## Model presentation correction — 2026-11-13

The user's first gameplay screenshot showed visible saddle/barding but no recognizable rider or donkey, pale wildlife and a gray horizon. Blender inspection confirmed the model geometry is present, the deer has vertex colors that Standard does not render, and the bear/moose/zombie/rider include separate color textures.

Model placement now samples the starting animation and measures posed mesh geometry in local space, rather than using potentially stale/animation-expanded renderer bounds. Scale/ground offset are applied on an unanimated parent. The importer explicitly binds bundled color textures, uses a vertex-color shader for Deer and increments its version to trigger reimport. The camera explicitly clears to its fog-colored background.

Repository checks and C# syntax parsing passed. Added **Forest Rider → Validate model presentation (Play Mode)** for actual imported geometry, clip bounds and materials. **Unity import, shader compilation, that validator, and the visual result have not been run in this workspace**; the original source FBX files are unchanged. The fixes do not replace the prototype forest props or UI with finished artwork.

## Terrain compilation correction — 2026-11-13

The user's Unity compiler reported CS0117 for `DetailScatterMode.InstanceCount`. The documented Unity 6.4 member is [`DetailScatterMode.InstanceCountMode`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/DetailScatterMode.html). Corrected both the runtime terrain generator and the editor terrain validator. Checked all uses of this enum against the documented members; repository checks and C# syntax parsing passed. Full Unity compilation and Play Mode remain unverified here.

## Package-resolution correction — 2026-11-13

The user's Unity Package Manager error confirmed that `com.unity.modules.inputlegacy` and `com.unity.modules.textrendering` cannot be resolved. These are engine assembly concepts, not supported UPM package IDs. Removed both from the manifest and corrected the repository check that previously required the invalid input entry. The remaining module IDs were checked against [Unity 6.4's built-in package list](https://docs.unity3d.com/6000.4/Documentation/Manual/pack-build.html). Repository/asset checks passed; a fresh Unity package-resolution/import run still needs to be performed locally. The earlier static checks did not detect this package-resolution problem.

## Unity 6000.4.11f1 migration — 2026-11-13

- Confirmed the requested editor release and revision `b0a1d6caadd2` using Unity's release API; checked the Unity 6.4 core-package and migration documentation.
- Updated repository/package/metadata and FBX/audio integrity checks: **passed**.
- Re-ran C# 9 syntax parsing over all **20 C# files: zero syntax errors**. This does not compile against Unity APIs.
- Recompiled and ran the engine-independent tests using NUnitLite 3.13.3 on .NET 8.0.15: **9 passed, 0 failed**. This is not a run of Unity's newly selected Test Framework 1.6.0.
- Reviewed the input bindings, current build/audio APIs, explicit ambient lighting, and Unity 6 texture mipmap behavior. The Play Mode terrain validator now checks texture readability/mipmap opt-in, grass scatter mode and ambient mode as well as terrain data; it still needs to be run in Unity. The original input package assumption was incorrect; see the correction above.

**Not run:** Unity 6000.4.11f1 import/API compilation, package resolution, Editor Test Runner, Play Mode, Android/iOS builds or device profiling. No Unity editor is installed in this workspace. Use [UNITY6.md](UNITY6.md) and [TESTING.md](../TESTING.md) for those checks.

## Previous asset and gameplay-rule validation — 2026-11-12

Passed checks:

- Repository/package and Unity metadata checks, hashes/licensing for all nine FBX models, and integrity/mono/fade checks for all 19 original WAV files.
- C# 9 syntax parsing with Roslyn 4.11: **20 files, zero syntax errors**. This does not resolve Unity API references.
- Engine-independent NUnit tests compiled with Roslyn and run using NUnitLite 3.13.3 on .NET 8.0.15: **9 passed, 0 failed**. Tests cover exact dragon phase order/timing and stun vulnerability, frame overshoot, unique gems/partial deposits, cooldown spam rejection and pause, reload without active-buff restoration, upgrade currency/point costs and caps, damaged/old skill defaults, XP/multiple level-ups/cap, and one-time contract rewards. These same source tests are in `Assets/Tests/EditMode`.
- Blender 4.3.2 FBX round-trip checks: all nine models have usable meshes. Source animation clips survived for Deer (26), Wolf (24), Fox (24), Zombie (32), Dragon (5), Donkey (26), Rider (4). Bear and Moose remain static. Unity's import script selects and compresses only the used clips; that import step needs Unity validation.
- Adapted rider and donkey assembly was rendered in Blender to inspect the mounted pose and torso/hip alignment. [Mounted model preview](mounted-models-preview.png) is an **asset assembly render, not gameplay**. The in-game saddle, barding, sword and scene are not included in this preview.

Performance measures are implemented (default Low/30 FPS target, native quality selection, shared low-poly props/static batching, fewer wildlife/grass, reduced rendering/texture/skinning costs, cached/cullable animation, streamed/compressed audio, throttled HUD). **No phone FPS, memory, heat, load-time or battery benchmark has been performed.** The settings are not a device-performance guarantee.

Still unverified: full Unity API compilation; Unity asset/audio import and visual appearance; Unity Test Runner execution; actual combat/skills/dragon playthrough; mobile UI/safe areas; graphics switching and save migration on device; ARMv7/ARM64/iOS builds and hardware behavior. Follow `TESTING.md` and `Docs/PERFORMANCE.md` in Unity 6000.4.11f1 before release.
