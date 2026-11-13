# Forest Rider

Unity **6000.4.11f1 (Unity 6.4)** C# mobile PvE game: ride an armored donkey through a hilly forest, fight wildlife and nighttime zombies, train rider/mount skills, and gather ten gems to summon a dragon. Free-use knight, donkey and enemy models are included; [THIRD_PARTY.md](THIRD_PARTY.md) records licenses and adaptations. No paid asset packages are needed. Unity Personal is sufficient.

## Open and play

1. Install [Unity 6000.4.11f1](https://unity.com/releases/editor/whats-new/6000.4.11f1) through Unity Hub. Add Android Build Support (SDK, NDK, OpenJDK) and/or iOS Build Support if building for a phone.
2. Add this repository folder as a project and select **6000.4.11f1** in Hub. Let package, FBX, animation and audio imports finish.
3. Open **Forest Rider → Create or open game scene**, then press Play. The world is generated at runtime.

For an existing checkout, close Unity, preserve any local edits, and run `git pull --ff-only origin main` before opening it in the new editor. The first Unity 6 import can take longer. See [Docs/UNITY6.md](Docs/UNITY6.md) for migration details and troubleshooting. The project uses the **Built-in Render Pipeline** and **Input Manager (Old)**; the setup script selects the input backend and logs a restart message only if it had to change it.

| Action | Mobile | Keyboard |
| --- | --- | --- |
| Ride | Left joystick | WASD / arrows |
| Rotate camera | Drag an empty area on the right half | Hold right mouse button and drag |
| Sword attack / three-hit combo | ATTACK | Space |
| Dodge | DODGE | Shift |
| Potion | HEAL | H |
| Rider / donkey skills | Four numbered skill buttons | 1–4 |
| Deposit gems at central altar | OFFER GEMS | E |
| Equipment, training, journal, settings, save, revive | CAMP | Escape |
| Manual save | CAMP → SAVE | F5 |

Movement follows the camera. The third consecutive sword attack deals 40% extra damage and briefly staggers ordinary enemies; wait more than 1.3 seconds between attacks to reset the combo. Dodge lasts .32 seconds with damage immunity and a 1.6-second cooldown. Charge and dodge cannot run at the same time.

## Rider and donkey skills

All four skills start at level 1. Upgrade them in **CAMP → SKILLS** using **one skill point + 60 × current skill level coins**. Maximum skill level is 5. Button labels show level, active state and remaining cooldown.

| Key | Skill | Level 1 effect | Level 1 cooldown |
| --- | --- | --- | --- |
| 1 | Rider: Whirlwind | 4.25m spin attack; 1.35× weapon damage and brief stagger | 8s |
| 2 | Rider: Iron Guard | 50% less damage for 4s, applied with armor reduction | 16s |
| 3 | Donkey: Charge | Forward rush at 12.5m/s for .8s; each enemy takes 1.5× weapon damage once and is stunned for 1s | 12s |
| 4 | Donkey: Rear Kick | Rear cone, 3.2m reach; 1.2× damage, 2m knockback, 1.5s stun | 7s |

Higher skill levels improve power, reach or duration and reduce cooldowns. Charge has no damage immunity; dodge remains the evasion tool. Skills cannot damage the airborne dragon or bypass its stun-only vulnerability. Ordinary enemy stun/knockback effects do not alter the dragon's phase clock.

Levels and cooldowns save locally. Cooldowns freeze in camp and while offline. Active buffs, rushes and combo state end on death/reload, so reloading cannot restore an active buff for free.

## Combat and progression

- Start with a level 1 sword, rider armor, donkey barding, three potions and one skill point.
- Earn XP from kills, gems and contracts. Player level caps at **20**; each level awards a skill point and adds 3 maximum health. Weapon damage gains a small level bonus.
- Wildlife includes bears, deer, moose, wolves and foxes. Deer/foxes flee until attacked; wolves flank; moose telegraph a one-second charge before rushing. Zombie spawns begin at night and disappear at dawn. A full day takes six real minutes.
- **12%** of spawns are elites, marked with a golden diamond: 60% more health, 30% more damage and double kill XP/coins. Normal enemies also scale gently with player level.
- A kill has a 65% loot chance (90% for elites). Ride over golden drops to receive potions, gear upgrades or coins. Gear caps at level 10; camp gear upgrades cost 40 × current level coins. Armor reduces incoming damage up to 65%; rider/donkey armor also increase maximum health. Potions restore 50 health.
- **CAMP → JOURNAL** tracks four one-time contracts: defeat 5 animals, defeat 3 zombies, find 4 gems, defeat the dragon. Claim rewards explicitly. Standard contracts award 80 coins, one skill point, one potion and 60 XP. Dragon Slayer awards 200 coins, two points, one potion and 200 XP. Claims persist and cannot be repeated.
- Death preserves gear, XP, skills and gems. **CAMP → REVIVE** returns you to the central clearing.

Older saves retain their existing progress. XP/hunt counters begin tracking with this update; already-found gems and dragon victory still count toward their new contracts.

## Ten gems and the dragon

Find ten unique glowing turquoise gems. The HUD shows the nearest remaining gem's distance and cardinal direction. Each gem has a saved ID, preventing duplication after reload. Deposit any carried gems within 4.5m of the central altar; you may deposit in batches. The tenth deposit summons the dragon and clears/suspends ordinary enemies.

On **Normal**, the dragon has 650 health and repeats this cycle (Hard/Extreme values are listed under Difficulty below):

1. **Aerial fireballs — 10 seconds.** Orange 2.1m-radius circles lock onto the rider when each shot launches. After 1.2s the impact deals 22 base damage. Move out or dodge; shots do not keep tracking.
2. **Dive warning — 1.5 seconds.** A red 3m-radius marker locks in place.
3. **Dive — .8 seconds.** The dragon dives to the marker and deals 36 base damage once on impact.
4. **Stunned — exactly 5 seconds.** A green ring appears and sword/skill hits can damage the grounded dragon. It becomes immune again at takeoff.

Iron Guard and armor reduce dragon damage; dodge grants brief immunity. Leaving the 32m altar arena resets boss health and attacks. Death/revival or reloading starts an unlocked fight at full health, preserving your deposited gems. Boss health/phase are transient.

Victory saves permanently and awards 500 coins, three potions and 250 XP, in addition to the separately claimable Dragon Slayer contract. Ordinary wildlife resumes. The boss is a one-time progression encounter per save.

## Models, animations and audio

The active player uses original segmented low-poly donkey and armored rider models. They are ordinary meshes with shared materials, grouped by joint/material. Transform animation supplies idle breathing, walking/galloping legs, a rear kick, rider sway, guard and sword swings. The model data is `Assets/Resources/MountedModels.json`, regenerated with `Tools/create_mounted_models.py`. There are no imported player skeletons, animation clips or per-frame mesh baking. [Replacement model preview](Docs/replacement-mounted-models.png) shows the actual asset layout rendered in Blender; it is not a gameplay screenshot.

The earlier Quaternius donkey and Dawid2K knight FBXs remain as reference assets and retain their license credits, but gameplay no longer loads them. Bear and moose use their existing static source meshes. Their textures are assigned to shared runtime material copies after import; the model postprocessor does not synchronously load texture/shader assets. Deer, wolf, fox, zombie and dragon retain their licensed designs, rendered through precomputed ordinary-mesh frames instead of imported skinning. Idle, movement, attack and hit poses are included; the dragon also has flight poses. Frames are shared across enemies of the same species, and no runtime skin baking is used. `Tools/bake_enemy_frames.py` regenerates these from the included source FBXs in Blender; `python3 Tools/check_enemy_frames.py` validates every pose.

The project includes **19 original synthesized audio files**: day/night ambience, a quiet music loop, hooves, sword/combat/skill sounds, dragon attacks, gems, loot, healing and UI feedback. There are no third-party sample recordings. **CAMP → SETTINGS** has saved Master, Music and Effects sliders; camp pauses world audio while menu feedback remains audible.

## Touch controls, sky and skill effects

Touch buttons use original leather/brass textures, a round joystick, action icons, press feedback and skill cooldown wipes. Sprite backgrounds are nine-sliced; labels and hit areas retain their existing layout. Regenerate the artwork with `python3 Tools/generate_touch_art.py` (Pillow and NumPy).

The saved six-minute day/night cycle now drives a skybox with blue daylight, warm dawn/dusk, a moving sun and moon, drifting clouds and stars. Ambient light, directional light and fog blend with the sky. Camp pauses the clock; loading restores the saved sky phase. The sky uses one small lookup texture and no realtime reflection updates.

Whirlwind draws spinning sword arcs, Iron Guard forms rotating shield hoops, Charge leaves speed ribbons and Rear Kick sends expanding arcs behind the donkey. Renderers/materials are reused and effects pause in camp and clear on death/revival. Low uses two 16-segment ribbons per active skill; Balanced/High use three with 24/32 segments. The unlit effect shader works without HDR, bloom or added lights. Actual appearance and cost still need Unity/device verification.

## Difficulty

Choose **CAMP → SETTINGS → DIFFICULTY → Normal / Hard / Extreme**. Normal preserves the original balance and remains the default for new and older saves. The selection saves immediately and appears in the HUD, independently of graphics quality.

| Combat setting | Normal | Hard | Extreme |
| --- | --- | --- | --- |
| Monster and dragon HP | 1× | 1.5× | 2× |
| Monster and dragon damage | 1× | 1.35× | 1.75× |
| Dragon fireball blast radius | 2.1m | 2.7m | 3.3m |
| Fireball travel / dodge-warning time | 1.2s | 0.85s | 0.5s |
| Dragon stun window | 5s | 3.75s | 2.5s |

Extreme fireballs travel 2.4× as fast as Normal over the same distance. Warning circles and impact effects match each mode's damage radius. The aerial phase remains 10 seconds, followed by the original dive warning and dive, then the selected stun duration. Higher difficulty multiplies the existing enemy level/elite scaling; rewards and progression costs remain the same.

Existing wildlife keeps its health percentage when you change modes. Changing difficulty during an active dragon encounter clears its hazards and restarts the fight at full health under the new rules. Gems, equipment, progression and dragon victory remain saved; changing difficulty does not summon a defeated dragon again.

## Graphics quality and mobile performance

**CAMP → SETTINGS → Low / Balanced / High** changes quality immediately and saves it. **Low is the default**, including older saves without this setting. The HUD shows the selected preset and a smoothed FPS reading.

| Setting | Low | Balanced | High |
| --- | --- | --- | --- |
| Frame-rate target | 30 FPS | 30 FPS | 60 FPS |
| Mobile render resolution | 75% | 90% | 100% |
| Maximum wildlife | 12 | 18 | 22 |
| Camera far distance | 85m | 120m | 160m |
| Grass distance / density | 16m / 16% | 27m / 40% | 38m / 65% |
| Texture mip limit | Half-resolution mip | Full | Full |
| Anti-aliasing | Off | 2× | 2× |
| Skinning weights | 2 | 4 | 4 |

Real-time shadows and reflection probes are disabled; Low also disables HDR. Shared 8-sided pine/trunk meshes and 20-face rocks replace dense sphere props; the static forest is batched. Material/audio caches are reused, combat impact sounds are rate limited, animation lookups are cached, and HUD text refreshes at 5Hz. Lowering the enemy cap removes surplus wildlife without rewards. Rendering scale is changed only on mobile; Editor windows keep their size.

These are performance settings and targets, **not measured device guarantees**. Read [Docs/PERFORMANCE.md](Docs/PERFORMANCE.md) for a low-end device test plan and remaining limits.

## Terrain and saves

Native Unity Terrain spans 180 × 180m with a 257 × 257 heightmap, broad hills, a level camp, winding earth trails, three blended surface layers and native billboard grass. The playable area is about 172m square. Ground textures/grass are generated locally. Terrain is created at startup; edits made to it in Play Mode are not persisted.

Character controllers handle slope movement and collision. Spawns, loot, revival and restored positions sample terrain height; the camera checks terrain/trunk obstruction. Existing flat-world saves adapt to elevation on load.

Saves live at `Application.persistentDataPath/forest-rider.json`, with backup/temporary recovery files. They preserve health, X/Z, time, equipment, inventory counters, gems, victory, XP, skill levels/cooldowns, contract claims, audio, quality and difficulty. Autosave runs every 30s and after progress events/backgrounding/quit; camp also offers manual save. Enemy positions and uncollected drops are transient. Uninstalling the app can remove local saves.

## Start again

Use **CAMP → RESET PROGRESS → RESET & START AGAIN** to begin a new adventure. The confirmation explains what will be cleared; **CANCEL** leaves your adventure unchanged.

Reset restores the original starting gear, health, three potions, level/skill defaults and daytime at the central clearing. It clears coins, kills, XP, contracts, skill cooldowns, gems, altar deposits and dragon victory. Existing enemies, loot and dragon attacks are removed, all ten gems return, and new wildlife spawns. Difficulty, graphics and audio preferences are kept. The new adventure is saved immediately; old recovery files are removed so they cannot restore the previous run. The reset cannot be undone through the game.

## Builds and checks

- **Forest Rider → Build Android APK**: IL2CPP, **ARMv7 + ARM64**, OpenGL ES 3, minimum Android API 26 (Android 8). Output: `Builds/ForestRider.apk`. Install Android build modules first. If prompted, save the open Untitled scene before the game scene is generated; Cancel stops the build. Configure signing/AAB distribution in Unity as needed.
- **Forest Rider → Build iOS Xcode project**: output `Builds/iOS`; finish signing/building on macOS with Xcode.
- `python3 Tools/check_project.py`: repository, model integrity, licensing and audio checks.
- `blender -b --python Tools/verify_models.py`: FBX mesh/texture/clip round-trip checks.
- Unity **Window → General → Test Runner → EditMode → Run All**: 20 difficulty/gem/dragon/skill/progression/save-reset test cases plus one Unity-only repeated-model-import regression.
- In Play Mode: **Forest Rider → Validate terrain (Play Mode)**.

All 20 standalone C# NUnit test cases passed outside Unity under .NET 8, and C# 9 syntax checks passed. See [Docs/VALIDATION.md](Docs/VALIDATION.md). Unity API compilation, actual Unity imports, device playthroughs and hardware performance remain unverified. Use [TESTING.md](TESTING.md) before release.

## Remaining work

See [Docs/ROADMAP.md](Docs/ROADMAP.md). Priority gaps are Unity/device validation and balancing, more polished transitions/tutorials, and animations for the static bear/moose. Inventory/crafting, additional environments and bosses are optional future design expansions.

The generated image in `Docs/forest-rider-concept.png` is concept artwork, not a gameplay screenshot.
