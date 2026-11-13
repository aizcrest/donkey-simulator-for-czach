# Validation

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
