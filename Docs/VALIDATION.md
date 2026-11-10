# Validation — 2026-11-10

- Repository/package checks, FBX SHA-256 integrity and asset credit checks: passed.
- C# 9 syntax parsing with Roslyn 4.11: 11 files, zero errors. This does not check Unity API references.
- Engine-independent gem/dragon NUnit tests compiled with Roslyn and ran using NUnitLite 3.13.3 on .NET 8.0.15: **3 passed, 0 failed**. They check exact attack order/timings, stun-only vulnerability, timer overshoot, unique gem pickup and partial deposits without duplication. These are the same source tests included under `Assets/Tests/EditMode`.
- FBX round-trip verification using Blender 4.3.2: all seven models passed mesh checks; textures survived for Bear/Moose/Zombie, and animation clips survived for Deer (26), Wolf (24), Fox (24), Zombie (32), Dragon (5). Bear and Moose source models are static.
- The adapted Moose model was rendered in Blender and visually checked for the broad antlers and intact body/materials. This was an asset check, not a gameplay screenshot.

Still unverified: Unity API compilation, Unity import/material appearance, Unity Test Runner execution, actual encounter playthrough, touch controls and target-device performance. Use `TESTING.md` to run the remaining checks with Unity 2022.3.62f1.
