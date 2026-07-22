# Backlog

> Claude: pick from **Next up** when asked to "continue"; append to **Done** (one line, with date) when a feature lands.

## Next up
1. Player health/HUD (Health component already exists and is reused as-is)
2. HUD: health + score

## Ideas (unscoped)
- Screen shake / hit feedback juice pass
- Upgrade system between waves
- Boss encounters
- Wave-clear-gated waves (once enemy death exists)

## Done
- 2026-07-21 — Crosshair: always-visible center reticle (uGUI, `HUD` Canvas) reacts to `TargetLock` — color swap on lock plus a `LockIndicator` that tracks the locked target's screen position each frame; `Crosshair.cs` (see `specs/crosshair.md`)
- 2026-07-21 — Target lock releases immediately when the current target dies (`Health.OnDied`), same as pressing the Lock button; toggleable via `TargetLockData.unlockOnTargetDeath` (see `specs/target-lock-death-retarget.md`)
- 2026-07-21 — Enemy combat: `Health` (generic, pool-safe) + damage on projectile hit, enemy death releases to pool; enemies shoot back via `EnemyWeapon` reusing the player's `ProjectileLauncher`/`Projectile` pipeline, per-enemy `canFire` toggle; `HealthData`/`Weapon_EnemyBasic`/`EnemyHealth_Basic` (see `specs/enemy-combat.md`)
- 2026-07-21 — Target-switch flick: while locked, deflecting the right stick past a threshold switches the lock to the nearest other `Lockable` in that screen-space direction (search radius + direction cone relative to the current target); reuses the existing `Look` action, no new binding (see `specs/target-lock-switching.md`)
- 2026-07-21 — Primary weapon: RB fires pooled forward-traveling projectiles (Auto/Single fire mode toggle), despawn on hit or lifetime timeout, `OnHit` event seam for a future modifier system; `WeaponData`/`Weapon`/`Projectile`, `Projectile_Basic` prefab (see `specs/weapon-system.md`)
- 2026-07-21 — Enemy spawning: per-planet trigger zones spawn a configurable, pooled enemy roster (all-at-once or timed waves) that pursues the ship; `GameLayers`/`ObjectPooler`/`EnemyMovementData`/`EnemySpawnData`/`EnemyController`/`EnemySpawnZone`, `BasicEnemy` prefab (see `specs/enemy-spawning.md`)
- 2026-07-21 — Target lock: R3 acquires nearest-to-screen-center `Lockable`, ship auto-turns onto it (PD auto-aim); `Lockable`/`TargetLock`/`TargetLockData` (see `specs/target-lock.md`)
- 2026-07-20 — Player ship: 6DOF Newtonian flight + gamepad input + cockpit FPS camera; `Manatite.Runtime.asmdef` foundation (see `specs/ship-6dof-fps.md`)
- 2026-07-18 — Agentic workspace setup (CLAUDE.md, skills, agent, docs)
