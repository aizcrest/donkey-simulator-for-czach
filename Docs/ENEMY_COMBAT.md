# Enemy attack timing and validation

Wildlife and zombies use a committed attack cycle. The attacker stops steering toward the player during wind-up, strike and recovery; only the short forward lunge moves it. The health bar turns orange during wind-up. Bear/moose have original procedural body poses; deer/wolf/fox/zombie use their licensed source attack clips, sampled by the combat clock. Existing idle/movement/hit/dragon flight poses now interpolate on the GPU instead of switching abruptly between static frames.

| Enemy | Wind-up | Active strike | Recovery | Lunge |
| --- | ---: | ---: | ---: | ---: |
| Bear | 0.55 s | 0.22 s | 0.63 s | 0.30 m |
| Moose | 0.65 s | 0.24 s | 0.71 s | 0.35 m |
| Zombie | 0.50 s | 0.22 s | 0.68 s | 0.25 m |
| Wolf | 0.35 s | 0.18 s | 0.57 s | 0.50 m |
| Fox | 0.40 s | 0.18 s | 0.62 s | 0.35 m |
| Deer | 0.50 s | 0.22 s | 0.68 s | 0.30 m |

`EnemyAttackProfile` also defines species-specific forward offsets, strike radii, heights and lateral swipes. These are gameplay contact volumes, not per-bone or per-triangle collision. Visual alignment and reach should be checked in Unity on hills and from all camera angles. Bear/moose still use static meshes with animated body transforms; they do not have new articulated paw/leg rigs.

Active strikes sweep a sphere against the mounted player's upright character-controller capsule, including height. An overlap is rejected when terrain/scenery blocks the attacker-to-player line. Contact consumes the strike even if dodge invincibility rejects damage. A miss does not consume the strike until recovery, allowing a player who enters the still-active swipe to be hit. Stun or death cancels the cycle. Moose charge retains its one-second warning and locked direction but now checks its swept forward contact volume rather than a 2.6 m circle around the animal.

Dodge lasts 0.32 seconds with a 1.6-second cooldown. Protection covers the final movement step, avoiding an update-order gap. All damage sources use a frame-local inbox resolved after Update/input callbacks, so a touch dodge can reject damage queued earlier that frame. A cyan tint shows protection on both donkey and rider. Charges still provide no immunity. The dragon retains its existing timed area attacks; they go through the same dodge gate.

## Automated checks

- Engine-independent NUnit tests cover all six attack profiles, misses, recovery, interruption, skipped active windows, one-hit limits, capsule height/side/behind misses, swept charge contact, dodge boundaries at 30/60/120 FPS, late touch dodge, pause/reset and pending-damage clearing.
- `Tools/check_enemy_frames.py` validates 168 poses. The exporter preserves the previous valid normal if a source triangle briefly degenerates during a strike.
- `Forest Rider → Validate model presentation (Play Mode)` checks interpolated mesh attributes, source fit and the referenced animation shader.
- C# syntax checks and repository metadata checks run outside Unity; they do not compile Unity APIs or the mobile shader.

## Unity / Android checks still required

1. Import with Unity 6000.4.11f1, run the model validation menu, and check the Console and shader compilation.
2. Let each enemy wind up; sidestep, back away, and run behind it. A missed attack must not reduce health. Stand in the strike to confirm exactly one hit.
3. Stun an enemy during wind-up and confirm it cannot finish the canceled attack. Repeat on a slope and across a tree/rock obstacle.
4. Move sideways during the moose warning; its charge should miss. Cross its actual path to confirm one hit, including at a reduced frame rate.
5. Dodge through melee contact, a charge, a fireball impact and the dive landing. Check touch and keyboard input, the cyan tint, the final dodge frame and cooldown. Remaining inside an area after protection ends must not cause a previously resolved hit to recur.
6. Pause during wind-up/dodge; resume, die/revive and reset progress. Confirm no stale hit or permanent immunity remains.
7. Profile Low/Hard/Extreme populations on a budget phone. GPU pose interpolation trades extra shared mesh memory for smoother animation; see PERFORMANCE.md. Unity/device testing was unavailable in the authoring environment.
