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
