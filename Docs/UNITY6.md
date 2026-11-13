# Unity 6.4 editor migration

The project now targets **6000.4.11f1**, revision **b0a1d6caadd2**, verified against [Unity's release metadata](https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.4.11f1). Unity Personal works. The previous 2022.3.62f1 project remains in Git history at commit `1ff26d2`; use a separate checkout if you need that version.

## Open an existing checkout

1. Save your work and close the old Unity editor. Commit or otherwise preserve local edits before pulling.
2. Run `git pull --ff-only origin main` from the repository folder.
3. In Unity Hub, choose **6000.4.11f1** for this project. Install it from the [official release page](https://unity.com/releases/editor/whats-new/6000.4.11f1) if it is missing. Use this version's mobile build modules rather than the old editor's SDK/NDK/JDK.
4. Let Unity finish upgrading/importing assets and resolving packages. If Unity offers its API updater, allow it to finish, then review any changed files.
5. Select **Forest Rider → Create or open game scene** and press Play. The scene is created on first import; the forest, actors and HUD are generated in Play Mode. Runtime terrain edits still do not persist to Edit Mode.
6. Run **Window → General → Test Runner → EditMode → Run All**, then follow [TESTING.md](../TESTING.md). In Play Mode, run **Forest Rider → Validate terrain (Play Mode)** at Low and High graphics quality.

## Compatibility changes

- Editor version and revision are pinned to 6000.4.11f1.
- uGUI **2.0.0** and Test Framework **1.6.0** match the core packages documented for Unity 6.4. The editor supplies these core packages; Unity creates/updates its dependency lock file on first resolution.
- Legacy input and text rendering use engine-provided APIs. `com.unity.modules.inputlegacy` and `com.unity.modules.textrendering` are not supported UPM package IDs and must not be added to the manifest. `InputManager.asset` includes Horizontal, Vertical, Submit and Cancel bindings. The setup script selects **Input Manager (Old)** for keyboard, pointer and touch input. If it changes a previous configuration, save and restart the editor when its Console message says so. This avoids depending on a Unity 6 template's input default.
- Runtime object searches use `FindAnyObjectByType`. Build settings use `NamedBuildTarget`. Audio preloading is configured through `AudioImporterSampleSettings`.
- The Built-in renderer remains in use for Standard materials, native terrain and grass. Runtime day/night lighting explicitly uses flat ambient lighting, so it does not rely on automatic ambient-probe baking removed in Unity 6.
- Generated surface/grass textures explicitly opt into mipmap limits and retain a small readable copy (about 267 KiB total including mips). Unity 6 needs that copy to upload the appropriate resolution when the player changes quality. Grass density explicitly uses instance counts.
- The scene menu now opens an already-created Forest scene. Automatic setup creates it additively, preserving any open scene and unsaved edits.

The game keeps its company/product identifiers, mobile application IDs and JSON save format. Existing local saves should remain available; verify this on your device. Low graphics remains the default. Android builds still use ARMv7 + ARM64, IL2CPP, OpenGL ES 3 and minimum API 26 (Android 8).

## Troubleshooting

- **Package not found: `com.unity.modules.inputlegacy` or `com.unity.modules.textrendering`:** quit Unity, pull the latest `main`, and reopen the project. The manifest fix removes both unsupported package IDs. Unity will resolve dependencies again with the corrected manifest; reinstalling the editor is not necessary for this error.
- **Input exception or unresponsive controls:** open **Edit → Project Settings → Player → Other Settings → Configuration → Active Input Handling**, select **Input Manager (Old)**, then restart the editor. Do not select the new Input System alone for this version of the game.
- **Pink materials:** check **Project Settings → Graphics** and every relevant **Quality** level. Render Pipeline Asset should be **None**. Open the repository itself in Hub; do not copy it into a URP/HDRP template or run a render-pipeline conversion.
- **Package/import cache errors after switching editors:** close Unity and rename the project's generated `Library` folder to `Library.backup` outside the repository, then reopen. Unity rebuilds the cache; keep `Assets`, `Packages` and `ProjectSettings`. If the error persists, record the first Console error.
- **An empty scene in Edit Mode:** expected for this runtime-generated prototype. Select the Forest Rider scene menu and press Play to see the world.

This is a source/settings migration. Unity 6000.4.11f1 is not installed in the development workspace, so editor import, Unity API compilation, gameplay and mobile builds still require validation in that editor. [VALIDATION.md](VALIDATION.md) distinguishes completed checks from outstanding tests.

References: [Unity 6.0 upgrade guide](https://docs.unity3d.com/6000.4/Documentation/Manual/UpgradeGuideUnity6.html), [Unity 6.4 upgrade guide](https://docs.unity3d.com/6000.4/Documentation/Manual/UpgradeGuideUnity64.html), [uGUI](https://docs.unity3d.com/6000.4/Documentation/Manual/com.unity.ugui.html), [Test Framework](https://docs.unity3d.com/6000.4/Documentation/Manual/com.unity.test-framework.html), [runtime texture mipmaps](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D-ctor.html).
