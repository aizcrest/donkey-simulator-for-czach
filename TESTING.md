# Unity and device acceptance checks

- Camera: hold the right mouse button and drag, or swipe an empty area on the right half of a phone screen. Check a full yaw rotation, pitch limits, camera-relative movement, tree/hill obstruction, boss framing, camp pause and focus loss. A touch that begins on joystick/attack/skills must not rotate the view; a separate right-side touch should rotate while riding.
- Mounted visibility: confirm actual donkey/rider bodies render during idle, walking, attacks and guard at all three quality levels. Use **Forest Rider → Report mounted rendering (Play Mode)** if either disappears. Confirm the player has ordinary MeshRenderers and no SkinnedMeshRenderer or CPU pose-baking component. Profile joint animation on a low-end phone.
- Start Play in both the generated Forest scene and a scene with a default Main Camera. Verify one active game camera/listener, no duplicate-listener warning after startup and working orbit controls.
- Open with Unity 6000.4.11f1. Confirm no Console compilation errors, and generated Forest scene is enabled in **File → Build Profiles → Scene List**. Follow `Docs/UNITY6.md` when upgrading an existing checkout.
- Confirm **Active Input Handling = Input Manager (Old)**. If the setup script changed it, save and restart the Editor once before testing. Verify WASD/arrows, mouse UI clicks and simultaneous joystick/button touches. The project has no Input System package dependency.
- Confirm the Built-in Render Pipeline is active (no render pipeline asset assigned in Graphics or Quality settings). Check daylight/night ambient lighting and generated terrain without baking lighting.
- Play: confirm a rider on a donkey, trees, animals and HUD appear. Ride using WASD, then the touch stick on a landscape device. Check collision with tree trunks and world boundaries.
- Fight a bear with Space/ATTACK. Check attack direction, cooldown, damage, death reward, and golden loot collection. Fight deer; verify they flee before being hit and retaliate afterward.
- Collect gear and potions; confirm stats change, health cannot exceed maximum and potions cannot be used after death.
- Let time advance to night; verify zombies spawn, attack and drop loot. At dawn verify zombies disappear.
- Open CAMP. Verify enemies, world time and player movement pause. Buy each upgrade with sufficient coins; verify insufficient coins and level 10 are handled.
- Save, close the app, reopen. Check equipment, potions, coins, kills, health, location and day phase. Repeat after backgrounding. Confirm recovery from a damaged primary save using the backup.
- Die and reopen the game; verify CAMP → REVIVE returns you to the clearing with full health and retained gear.
- Android: build ARMv7 + ARM64 APK (OpenGL ES 3) with installed SDK/NDK and test simultaneous left-stick drag plus right-hand attack. Test safe areas, pause/resume and 10 minutes of play on target hardware.
- iOS: export Xcode project, sign on macOS, run on a physical device and repeat mobile acceptance checks.

Automated repository checks: `python3 Tools/check_project.py`. These validate repository/asset integrity only and do not replace Unity compilation or gameplay checks.

## Terrain upgrade acceptance

- In Play Mode run **Forest Rider → Validate terrain (Play Mode)**. Confirm the validation passes in the Console.
- Inspect the runtime terrain: rolling hills, level camp, winding earth trails, moss and rock blends, grass off the trails. Check for magenta shaders in both Editor and an actual Android/WebGL build.
- Ride uphill, downhill and sideways over hill crests. Confirm the donkey remains grounded without sinking or falling through; check animal movement on the same slopes.
- Kill enemies on a slope and collect loot; verify no hits occur across excessive height differences.
- Save on a hillside, quit and reopen; check that the rider reappears at the correct terrain height. Also load a save created before the terrain upgrade.
- Revive at camp and confirm the donkey appears above the clearing. Check camera clearance behind hill crests and tree trunks.
- Measure frame rate and grass pop-in on a target phone; lower detail density/distance first if needed. Terrain generation adds startup work, so measure initial load time on device too.

## Free enemy models and gem/dragon progression

- Run **Forest Rider → Validate model presentation (Play Mode)** at both Low and High quality. It loads all seven imported enemy models at a translated/rotated origin, checks initial height and grounding, samples their clips for collapsed/buried geometry, and verifies color textures and the deer's vertex-color shader.
- Confirm donkey, rider and sword are visible at the start and after riding/attacking. Check feet on flat ground and slopes. Watch for the rider/mount snapping to a different scale when animation starts. The follow camera should clear to the day/night fog color, with no default gray skybox band.
- Let all FBX imports finish and enter Play Mode. Verify Bear, Deer, Moose, Wolf, Fox and Zombie render at sensible ground height, face their movement direction and have correct materials. Check movement/attack clips where available; Bear/Moose are static source meshes.
- Open CAMP → ASSET CREDITS. Confirm creators, licenses and a link to individual model sources are visible.
- Collect a gem, save, reload: it must stay collected and carried count must match. Deposit a partial set at the central altar, reload, verify altar slots and inventory. Find all ten; test repeated collection or repeated OFFER GEMS cannot duplicate progress.
- Offer the tenth gem: exactly one dragon appears; ordinary enemies and their spawner stop. Check the dragon model/flight clip and wider camera on a phone.
- Time the repeating cycle: 10 seconds of fireballs, 1.5s locked red warning, .8s dive, 5 seconds stunned. Aerial shots show orange 2.1m circles for 1.2s before damage. The target does not move after marking. Leftover shots must disappear before the dive warning.
- Move outside orange/red circles and verify no damage. Remain inside each and verify a single damage event per impact; dodge through an impact to verify immunity. Check dodge cooldown and simultaneous joystick/dodge/attack touches.
- Try hitting the airborne/diving dragon: no damage. During the green stun window, hit it in front of the donkey; verify boss health falls, then becomes immune again exactly at takeoff.
- Open CAMP during each phase/projectile flight. Timers, damage, movement and animations must pause. Resume and verify the remaining timing is preserved.
- Ride more than 32m from camp and return: boss health resets and old attacks vanish. Die, revive, save/reload mid-fight: gem deposits persist and one fresh boss starts; no ghost hazards remain.
- Defeat the dragon: verify +500 coins, +3 potions and victory survives reload. Ordinary wildlife spawning resumes and the altar cannot summon duplicate bosses after victory.
- Run EditMode tests in Unity Test Runner. The rules tests passed in a standalone .NET 8 NUnit run. Full Unity compilation, the Unity Test Runner run and device gameplay still require the Unity editor.

## Mounted skills, presentation and progression

- Run all **nine** EditMode tests. These same nine tests passed under standalone .NET 8; Unity Test Runner still needs to be run.
- Check the replacement segmented knight/donkey, seated pose, visible long ears/legs/helmet, saddle, sword, idle/walk/gallop/kick and attack/guard animations. Confirm pause/death freezes the pose, all quality presets retain both bodies, and camera orbit works around them. In-engine appearance remains unverified here.
- Use Whirlwind surrounded by animals; check radial damage. Stand behind/front of an animal while using Rear Kick and verify rear-only hits, knockback and stun. Charge into several enemies; each should be hit once, and trunks/bounds must still stop movement. Charge cannot activate during dodge; dodge cannot activate during charge.
- Guard an identical enemy/dragon hit with and without Iron Guard and compare damage. Verify guard lasts its displayed duration and is not invulnerability. Skill cooldowns/active durations must freeze in camp and on death active effects must end. Save/reload during cooldown: levels and remaining cooldown survive, active buffs do not.
- Hit the dragon with every damaging skill during flight/dive: no damage. During its five-second stun, skill/sword hits work; stun/knockback must never change the boss cycle.
- Try upgrading with insufficient coins/points and at skill level 5. Spend a point and reload: balances/levels must match. Old saves should load with skills at level 1 and Low graphics, without losing old gear/gems/victory.
- Check XP level-ups, health cap, skill point awards and level 20 cap. Complete/claim each Journal contract, reload and try claiming twice. Old gem/victory progress should qualify for corresponding contracts.
- Make three rapid sword attacks: verify the third is heavier and staggers. Wait to reset the combo. Find elite variants, confirm gold marker/extra health/rewards, dodge the moose's telegraphed rush, and observe wolves flanking.
- Listen to day/night ambience, music, hoof cadence, attacks, skill sounds, gems, healing, menu feedback and boss attacks. Move Master/Music/Effects sliders, mute, pause/resume, background/restart and verify saved volumes and no duplicate listeners or unwanted delayed menu sounds.

## Graphics selection and low-end acceptance

- CAMP → SETTINGS must expose **Low / Balanced / High** with the active selection marked. Low is the first-run and old-save default.
- Select every preset and verify the HUD's quality/FPS label, saved selection, frame-rate target, grass range/density, camera distance, texture mip limit and enemy cap. Mobile resolution should scale from the original render dimensions, not shrink again on repeated clicks. Run terrain validation at Low and High: generated textures must be readable and opt into mip limits so Unity 6 can re-upload them on a quality change.
- Lower High → Low with more than 12 wildlife alive: surplus enemies are removed without kills/XP/loot. The dragon encounter should still have ordinary wildlife suspended.
- Test on both a 32-bit Android installation and ARM64 hardware if available. Measure sustained FPS/memory/heat on a budget phone for at least 10 minutes; a 30 FPS setting alone is not evidence of actual performance. Follow `Docs/PERFORMANCE.md`.
