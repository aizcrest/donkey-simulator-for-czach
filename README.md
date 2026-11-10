# Forest Rider

A Unity 2022.3 LTS C# mobile PvE prototype. Ride an armored donkey through a seeded 3D forest, fight bears and deer, and survive zombies at night. All models use original low-poly primitive geometry assembled in code; no third-party assets or paid packages are required.

## Visual concept

![Forest Rider concept artwork](Docs/forest-rider-concept.png)

This generated concept illustrates the intended art direction; it is not a screenshot of the current prototype.

## Run

1. Install Unity **2022.3.62f1** with Android Build Support (SDK, NDK, OpenJDK) and/or iOS Build Support through Unity Hub.
2. Add this folder as a project and open it. Allow the initial import and compilation to finish.
3. The editor generates `Assets/Scenes/Forest.unity` and registers it for builds. If needed use **Forest Rider → Create or open game scene**.
4. Press Play. The game creates its world, rider, enemies, camera, lighting, and UI automatically when the scene loads.

Mobile: drag the left stick to ride; tap ATTACK to swing in front of your donkey; HEAL uses a potion; CAMP pauses combat and opens equipment upgrades, manual save, revive, and resume. Desktop: WASD/arrows, Space, H, Escape, F5 respectively. Movement is relative to the fixed follow camera. Ride over golden loot cubes to collect them.

## Rules

- Start with a level 1 sword, rider armor, donkey armor, and three potions.
- Bears and deer roam the forest. Deer flee until struck; bears chase nearby riders.
- A full day lasts six real minutes. Night spawns zombies, which disappear at dawn.
- Every kill awards coins and has a 65% chance to drop a potion, sword upgrade, rider armor, donkey armor, or extra coins, each equally likely.
- Gear caps at level 10. Camp upgrades cost 40 times the current level. Armor reduces incoming damage, up to 65%; both armor types increase maximum health. Potions restore 50 health.
- Death keeps equipment and progress. CAMP → REVIVE restores health and returns you to the clearing.
- Local JSON saves preserve health, position, day phase, kills, coins, potions and equipment. Manual save, 30-second autosave, loot collection, upgrades, death, application backgrounding and quit write progress. Save files live in `Application.persistentDataPath/forest-rider.json`; a backup and temporary file provide recovery copies. Enemy positions and dropped loot are transient. Uninstalling the app can remove saves.

## Build

**Forest Rider → Build Android APK** generates `Builds/ForestRider.apk`, using IL2CPP, ARM64 and minimum Android API 26. Install the Android module first. Unity may download the target SDK required by your installed tooling. Use Build Settings to configure distribution signing and publish an AAB if desired.

**Forest Rider → Build iOS Xcode project** exports `Builds/iOS`. Finish signing and building on macOS with Xcode and an Apple developer provisioning profile.

Batch example after installing Unity:

```sh
Unity -batchmode -quit -projectPath "$PWD" -executeMethod DonkeyForest.Editor.GameBuild.Android -logFile build.log
```

## Validation and limits

This repository was authored in an environment without the Unity editor or a C# compiler. No APK, device run, or Unity compilation has been verified here. Run the checklist in `TESTING.md` before using the game on a device. The visuals are intentionally prototype quality: primitive models, no rigged animations, music, authored terrain, or audio. The world is bounded to an approximately 172m square; at most 22 enemies are active. No ads, accounts, network play, analytics, or cloud saves.
