# Validation — 2026-11-12

Passed checks:

- Repository/package and Unity metadata checks, hashes/licensing for all nine FBX models, and integrity/mono/fade checks for all 19 original WAV files.
- C# 9 syntax parsing with Roslyn 4.11: **20 files, zero syntax errors**. This does not resolve Unity API references.
- Engine-independent NUnit tests compiled with Roslyn and run using NUnitLite 3.13.3 on .NET 8.0.15: **9 passed, 0 failed**. Tests cover exact dragon phase order/timing and stun vulnerability, frame overshoot, unique gems/partial deposits, cooldown spam rejection and pause, reload without active-buff restoration, upgrade currency/point costs and caps, damaged/old skill defaults, XP/multiple level-ups/cap, and one-time contract rewards. These same source tests are in `Assets/Tests/EditMode`.
- Blender 4.3.2 FBX round-trip checks: all nine models have usable meshes. Source animation clips survived for Deer (26), Wolf (24), Fox (24), Zombie (32), Dragon (5), Donkey (26), Rider (4). Bear and Moose remain static. Unity's import script selects and compresses only the used clips; that import step needs Unity validation.
- Adapted rider and donkey assembly was rendered in Blender to inspect the mounted pose and torso/hip alignment. [Mounted model preview](mounted-models-preview.png) is an **asset assembly render, not gameplay**. The in-game saddle, barding, sword and scene are not included in this preview.

Performance measures are implemented (default Low/30 FPS target, native quality selection, shared low-poly props/static batching, fewer wildlife/grass, reduced rendering/texture/skinning costs, cached/cullable animation, streamed/compressed audio, throttled HUD). **No phone FPS, memory, heat, load-time or battery benchmark has been performed.** The settings are not a device-performance guarantee.

Still unverified: full Unity API compilation; Unity asset/audio import and visual appearance; Unity Test Runner execution; actual combat/skills/dragon playthrough; mobile UI/safe areas; graphics switching and save migration on device; ARMv7/ARM64/iOS builds and hardware behavior. Follow `TESTING.md` and `Docs/PERFORMANCE.md` in Unity 2022.3.62f1 before release.
