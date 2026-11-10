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
