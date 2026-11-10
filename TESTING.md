# Unity and device acceptance checks

- Open with Unity 2022.3.62f1. Confirm no Console compilation errors, and generated Forest scene is enabled in Build Settings.
- Play: confirm a rider on a donkey, trees, animals and HUD appear. Ride using WASD, then the touch stick on a landscape device. Check collision with tree trunks and world boundaries.
- Fight a bear with Space/ATTACK. Check attack direction, cooldown, damage, death reward, and golden loot collection. Fight deer; verify they flee before being hit and retaliate afterward.
- Collect gear and potions; confirm stats change, health cannot exceed maximum and potions cannot be used after death.
- Let time advance to night; verify zombies spawn, attack and drop loot. At dawn verify zombies disappear.
- Open CAMP. Verify enemies, world time and player movement pause. Buy each upgrade with sufficient coins; verify insufficient coins and level 10 are handled.
- Save, close the app, reopen. Check equipment, potions, coins, kills, health, location and day phase. Repeat after backgrounding. Confirm recovery from a damaged primary save using the backup.
- Die and reopen the game; verify CAMP → REVIVE returns you to the clearing with full health and retained gear.
- Android: build ARM64 APK with installed SDK/NDK and test simultaneous left-stick drag plus right-hand attack. Test safe areas, pause/resume and 10 minutes of play on target hardware.
- iOS: export Xcode project, sign on macOS, run on a physical device and repeat mobile acceptance checks.

Automated repository checks: `python3 Tools/check_project.py`. These validate source/configuration structure only and do not replace Unity compilation or gameplay checks.

## Terrain upgrade acceptance

- In Play Mode run **Forest Rider → Validate terrain (Play Mode)**. Confirm the validation passes in the Console.
- Inspect the runtime terrain: rolling hills, level camp, winding earth trails, moss and rock blends, grass off the trails. Check for magenta shaders in both Editor and an actual Android/WebGL build.
- Ride uphill, downhill and sideways over hill crests. Confirm the donkey remains grounded without sinking or falling through; check animal movement on the same slopes.
- Kill enemies on a slope and collect loot; verify no hits occur across excessive height differences.
- Save on a hillside, quit and reopen; check that the rider reappears at the correct terrain height. Also load a save created before the terrain upgrade.
- Revive at camp and confirm the donkey appears above the clearing. Check camera clearance behind hill crests and tree trunks.
- Measure frame rate and grass pop-in on a target phone; lower detail density/distance first if needed. Terrain generation adds startup work, so measure initial load time on device too.

## Free enemy models and gem/dragon progression

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
- Run EditMode tests in Unity Test Runner. The three rules tests passed in a standalone .NET 8 NUnit run. Full Unity compilation, the Unity Test Runner run and device gameplay still require the Unity editor.
