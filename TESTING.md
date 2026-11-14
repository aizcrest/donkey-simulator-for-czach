# Unity and device acceptance checks

- Death UI: die in each difficulty and load a save with zero health. Confirm the gold/green **You died.** overlay blocks underlying touch controls and **REVIVE** restores health at camp, closes any open menu, resets the stick/dodge/skill effects and resumes play. Repeat death/revive without opening Camp. Run the Unity-only `CombatHudTests` for dead-save HUD construction and gem JSON persistence.
- Extreme gems: collect IDs in a non-sequential order, offer some, collect more and die. Only the carried IDs should disappear from inventory; altar count stays fixed. Check the new positions are on reachable ground away from trunks, reload while dead and after revival, and confirm markers/nearest-gem hints stay at those saved positions. Recover them and verify no duplicate pickup/XP, then offer the tenth gem. Die repeatedly without picking up anything: no additional scattering. Normal/Hard must keep carried gems. Repeat using an older save with partial altar progress and no deposited-ID/coordinate fields.
- Population: compare initial counts and steady-state caps against the README table for every graphics/difficulty combination, including nighttime zombies, in-place difficulty changes, revival and reset. The boss must still suspend wildlife. Profile Extreme/Low on a budget phone; 20 actors is a cap, not an FPS guarantee.
- Dragon: verify warning and impact radii match at 4/4.5/5m for Normal/Hard/Extreme. Stamina starts at 100, loses 10 per fireball and 30 per dive, and freezes in Camp. Remaining stamina carries between cycles. At zero, the next stun must last 7/5.75/4.5s respectively, then refill to 100 at takeoff. Non-exhausted stuns stay 5/3.75/2.5s. Check death, retreat, difficulty change and a reloaded fight reset stamina and remove old hazards; victory hides the bar.
- After import, check **File → Build Profiles → Scene List**: `Forest` first and `TerrainBuildSupport` enabled. Repeat with a custom profile that overrides the global list. The support scene must remain included even though gameplay never loads it. Build a fresh APK with both scenes; confirm visible moss, earth trails and rocky hills immediately on device, at Low/Balanced/High and after pause/resume. Terrain instancing stays off on phones at every preset.
- Cold launch at 16:9, the reported 1280×591 aspect, 20:9 and 4:3. The entire splash title and donkey must fit without stretching; dark margins are expected. Gameplay starts after the splash, so no enemies attack behind it. Inspect the installed launcher icon with round and square masks; check Android legacy/round/adaptive slots in Player Settings. Android's OS-controlled initial launch screen may show the icon before the illustrated splash.
- Run `TerrainBuildSupportTests` in EditMode: the support scene must import with an inactive Terrain/Collider, scene-list inclusion must be idempotent, keep the startup scene first, and re-enable a disabled support entry.
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

- Run the five `TerrainCollisionTests` cases in **Window → General → Test Runner → EditMode**. They check collision immediately after generation at camp and on hills, restore a deliberately ignored layer pair, apply repeated gravity to player/animal-sized controllers without recovery, and test recovery with the collider disabled. These Unity-only cases have not run in the development workspace.
- Rebuild the APK after pulling this fix; installing over the existing app with the same signing key retains the Android save. On a cold launch, stand still for 30 seconds and watch both rider and wildlife: neither should fall through the map. Ride, dodge and charge across hills, stun/knock back enemies and watch a moose charge. Repeat after background/resume and at Low/High quality. A one-time `Recovered an actor below the forest terrain` warning means the safety recovery was needed; capture Android Logcat if it appears on every launch. Check collision itself in addition to visible grounding.
- In Play Mode run **Forest Rider → Validate terrain (Play Mode)**. Confirm the validation passes in the Console.
- Inspect the runtime terrain: rolling hills, level camp, winding earth trails, moss and rock blends, grass off the trails. Check for magenta shaders in both Editor and an actual Android/WebGL build.
- Ride uphill, downhill and sideways over hill crests. Confirm the donkey remains grounded without sinking or falling through; check animal movement on the same slopes.
- Kill enemies on a slope and collect loot; verify no hits occur across excessive height differences.
- Save on a hillside, quit and reopen; check that the rider reappears at the correct terrain height. Also load a save created before the terrain upgrade.
- Revive at camp and confirm the donkey appears above the clearing. Check camera clearance behind hill crests and tree trunks.
- Measure frame rate and grass pop-in on a target phone; lower detail density/distance first if needed. Terrain generation adds startup work, so measure initial load time on device too.

## Free enemy models and gem/dragon progression

- Run **Forest Rider → Validate model presentation (Play Mode)** at both Low and High quality. It loads all seven imported enemy models at a translated/rotated origin, checks initial height and grounding, samples their clips for collapsed/buried geometry, and verifies color textures and the deer's vertex-color shader.
- Confirm donkey, rider and sword are visible at the start and after riding/attacking. Check feet on flat ground and slopes. Watch for the rider/mount snapping to a different scale when animation starts. The follow camera should show the new day/night sky with a matching fog horizon, with no default gray skybox band.
- Let all FBX imports finish and enter Play Mode. Verify Bear, Deer, Moose, Wolf, Fox and Zombie render at sensible ground height, face their movement direction and have correct materials. Check movement/attack clips where available; Bear/Moose are static source meshes.
- Open CAMP → ASSET CREDITS. Confirm creators, licenses and a link to individual model sources are visible.
- Collect a gem, save, reload: it must stay collected and carried count must match. Deposit a partial set at the central altar, reload, verify altar slots and inventory. Find all ten; test repeated collection or repeated OFFER GEMS cannot duplicate progress.
- Offer the tenth gem: exactly one dragon appears; ordinary enemies and their spawner stop. Check the dragon model/flight clip and wider camera on a phone.
- On Normal, time the repeating cycle: 10 seconds of fireballs, 1.5s locked red warning, .8s dive, 5 seconds stunned. Aerial shots show orange 2.1m circles for 1.2s before damage. The target does not move after marking. Leftover shots must disappear before the dive warning.
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

## Animated enemy visibility correction

- In Play Mode run **Forest Rider → Validate model presentation (Play Mode)**. The five animated types must use ordinary MeshRenderers and have nonempty mesh frames; bear/moose keep their static source models.
- Inspect Deer, Wolf, Fox and night Zombies up close, while moving/attacking/stunned, after leaving/re-entering view, and at all quality presets. Confirm their bodies and colors render with their health bars.
- Summon the Dragon and inspect flight, attack/dive and stunned poses. Its existing 10-second aerial phase and 5-second vulnerability window must remain unchanged. Check pause, retreat, death and reload.
- Run `python3 Tools/check_enemy_frames.py` to verify every exported pose, indices, normals and initial grounding. This does not replace Unity shader compilation or rendering tests.

## Textured controls, day/night sky and animated skills

- After import, check Console for shader/API errors, then test both Play Mode and an Android build. Confirm no pink/black sky or skill ribbons; the build setup retains both runtime shaders.
- Check all action icons, round joystick/knob, textured menu buttons, pressed feedback and skill cooldown wipes at narrow/wide landscape sizes and on a device with a notch. Hold movement and use a skill with a second finger; confirm buttons do not rotate the camera.
- Orbit upward to see the sky. In Play Mode, select the Forest Rider object and adjust Data → Time to 0, .22, .25, .49, .74 and .78 to inspect midnight, dawn, noon and dusk. Let the clock cross 1 to 0: clouds and lighting should wrap without a jump. These debug time edits affect the next save.
- Wait for a normal cycle and check sun/moon movement, drifting clouds, night stars, matching fog and readable ground/enemies. Pause in CAMP and confirm the sky freezes; save/reload and confirm the phase returns.
- Use all four skills: cyan spinning arcs, blue shield hoops, gold charge ribbons and orange rear kick arcs must be visible in day/night on Low/Balanced/High. Confirm kick effects face behind the donkey, cooldown wipes finish, and combat damage/timing are unchanged.
- Pause/resume during Guard/Charge; die/revive during an active effect. Ensure no permanent or duplicate effects remain. Switch quality in camp during Guard and check the ribbon detail updates after resuming.
- Profile repeated skill use on a low-end phone: renderer/material counts should remain stable. Check frame time, UI readability and transparent-effect overdraw; no device benchmarks have been performed in this workspace.

## Difficulty selection

- Start a fresh save and load an older save without `difficulty`: both should select Normal. Verify an out-of-range difficulty value falls back to Normal. Back up saves before editing them for a test.
- Open **CAMP → SETTINGS → DIFFICULTY** and select each mode. Check labels, selection highlight and HUD on a landscape phone. Close/reopen the game and verify the selected mode persists. Change graphics quality and verify it does not change difficulty.
- At the same player level, check normal and elite enemies on all modes: HP scales 1× / 1.5× / 2×; attack damage scales 1× / 1.35× / 1.75× before player armor/guard. Include zombie melee and moose charges. Injure an enemy, switch modes repeatedly, and verify its health bar percentage is preserved.
- Fight the dragon in each mode: maximum HP is 650 / 975 / 1300. Fireball warning radius and damage radius must both be 2.1 / 2.7 / 3.3m, with 1.2 / .85 / .5 seconds from launch to impact. Check impacts just inside/outside the circle and dodge immunity. The flight phase is still ten seconds; the stun lasts 5 / 3.75 / 2.5 seconds, with vulnerability ending at takeoff.
- Check dragon fireball and dive damage scale with difficulty; target-locking, phase cleanup, pause and retreat should still work. Check Extreme's readability and dodge window on a low-end device at Low quality.
- While a fireball is in flight or the dragon is stunned, open camp and change modes. The old boss/hazards must disappear, exactly one full-health boss should replace it, and the new fight should stay paused until camp closes. Selecting the already-selected mode must not restart the fight.
- Die/revive, save/reload mid-encounter, and change difficulty after victory: difficulty should persist, gem/equipment progress should remain, and a defeated dragon must not respawn or award duplicate rewards.
- Run all 16 EditMode test cases. Standalone rule tests are automated here; Unity save loading, combat integration, UI and device playtesting still need local validation.

## Import-loop recovery and regression

- Close Unity, pull the latest repository, then reopen with 6000.4.11f1 and let its one-time reimport finish. The model postprocessor version changed; no manual deletion of assets or saves is needed.
- Confirm Bear, Moose, Zombie and Rider and their `.fbm` textures finish importing without the infinite-loop error. Run **Window → General → Test Runner → EditMode → ModelImportTests**. This Unity-only test imports the four affected pairs twice and checks stable dependencies/source bytes.
- Enter Play Mode and run **Forest Rider → Validate model presentation (Play Mode)**. Inspect bear/moose coat textures and separate brown antlers. Spawn several of each: they should share runtime material copies. Check that imported asset materials are not marked dirty after play and that exiting/re-entering Play Mode does not leak copies.
- Build the Android APK and inspect the same models on device. The original PNGs remain under Resources, so the runtime material bindings are included in builds. No Unity import or Android build has been run in the Cloud workspace.

## Scene setup with an Untitled scene

- In a temporary checkout without `Assets/Scenes/Forest.unity`, open an Untitled template scene, then reload scripts. Startup must not throw, show a save dialog, close the scene or discard its objects. Repeat with both an untouched template and an edited Untitled scene.
- Choose **Forest Rider → Create or open game scene**. Save the Untitled scene when prompted; Forest should be created and opened. Repeat in a temporary checkout, cancelling the save dialog: the current scene must remain and no Forest scene should be created.
- Choose **Forest Rider → Build Android APK** with a missing Forest and an Untitled scene. Save it first; the generated Forest scene should be used for the build. Cancelling must not start a build. Repeat for iOS export.
- With saved scenes open, create Forest and verify the existing scenes stay loaded with the same active scene. With Forest already on disk, startup should not create another scene or prompt to save an unrelated Untitled scene.
- Check a failed Forest scene save stops the command and cleans up its temporary scene. Never test folder permission failures against your only copy of unsaved work.

## Reset progress

- Open **CAMP → RESET PROGRESS**. Confirm the warning lists the cleared progression and retained settings. CANCEL or closing camp must preserve progress. The reset button must remain behind other camp panels.
- Confirm the reset from an advanced save: coins/kills become zero, equipment returns to level 1, potions to 3, health to the fresh-save default, journey/skills to their starting values, time to daytime and position to the clearing. Difficulty, graphics and audio settings must stay selected.
- Reset during a dragon fireball, stun window, rider skill, after death, and after dragon victory. No old boss, hazards, loot or buffs should survive. All ten gems must be collectible again and the altar must allow another dragon encounter after new deposits.
- Restart the app after resetting, including immediately after confirmation. The new run must load. Old `.bak`/`.tmp` recovery candidates must be gone or replaced with new-run data. Test repeated resets and check stable terrain/player/material counts.
- Check reset in both the generated Forest scene and Play Mode from an Untitled scene; no scene reload is needed. Test Android file replacement and a storage failure: an unsuccessful reset must show an error and leave the current adventure loaded.
- Run `ProgressResetTests` in EditMode. The four reset cases passed in the standalone runner against a temporary filesystem; Unity JSON integration, runtime cleanup, confirmation UI and Android/iOS storage still need local testing.

## Camp and interrupted HUD setup

- Stop Play Mode, pull the fix, wait for the UI textures to reimport and start Play again. Confirm the HUD includes status text, movement, ATTACK/DODGE/HEAL and all four skills before clicking CAMP. Test CAMP/RESUME and Escape repeatedly, including after death and progress reset.
- Run **EditMode → TouchArtTests** to verify every UI asset loads as a Sprite and the button border is preserved. Sky lookup must remain a texture. This test requires Unity and has not been run in the Cloud workspace.
- In a disposable checkout, remove one UI icon or the button texture, then Play: a warning should identify the missing art and plain labeled controls must still work. Restore the asset afterward. No missing image may abort HUD construction.
- While playing, remove the Camp child from the runtime HUD, then press Escape: the HUD should rebuild once, opening a functioning Camp without duplicate hotbars or CampAdditions components. Test deleting the whole HUD and pressing Escape too.
- Cause an early startup failure in a disposable checkout (for example, temporarily remove mounted model data). Confirm the first exception is logged, no partial HUD accepts clicks, and exiting Play Mode does not write an incomplete/default save over existing progress.
- Repeat normal startup and Camp interaction in an Android build. Editor compilation, these Unity lifecycle checks and device behavior remain unverified here.

## Hidden submenu startup regression

- After updating, stop and restart Play Mode. The status text, joystick, combat controls and CAMP must appear. Startup must finish without a CampAdditions.Refresh null-reference error.
- Open CAMP with Settings/Skills/Journal/Difficulty closed. Open each submenu, return to Camp, and repeat. Hidden button labels should refresh without null references, including difficulty/quality selections, skill upgrade costs and contract states.
- Run **EditMode → CampUiTests → PausedHudSetupAndClosedSubmenuRefreshKeepTheirLabels**. This builds the real HUD on an inactive test host so Awake never loads or writes an adventure, then exercises closed-panel refresh. It requires Unity and has not been executed here.

## Model textures, cubemap mismatch and startup cleanup

- Stop Play Mode, update, then reimport **Assets/Resources/Enemies** once. Select each `.fbm` PNG: Texture Type must be Default and Texture Shape must be 2D. UI sprites and the sky lookup must also use 2D texture shape.
- Run **ModelImportTests** and **ImportedMaterialTests** in the Unity EditMode runner. The model test now checks that the main PNG asset is a Texture2D and the runtime Resources path resolves, in addition to repeated-import stability.
- Play and spawn several moose/bears: coats should render, antlers should keep their own material, and HUD/CAMP should remain available. No missing model texture exception or CUBE-to-2D assignment should occur. Repeat in an Android build.
- In a disposable checkout, temporarily remove a coat texture, then Play: a warning should appear, and the animal should use an existing valid 2D texture or a plain species coat color. Startup and controls must still finish. Restore/reimport the asset afterward.
- Exercise a failed visual setup in a disposable checkout: partially created enemy/visual objects must be inactive and removed, with the original exception preserved and no repeating EnemyVisual.Update null errors. Original asset materials must remain unchanged after Play Mode.
