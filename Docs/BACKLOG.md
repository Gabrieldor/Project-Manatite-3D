# Backlog

> Claude: pick from **Next up** when asked to "continue"; append to **Done** (one line, with date) when a feature lands.

## Next up
1. HUD: health + score
2. Real player death/respawn flow (currently just quits the app via `PlayerDeath`)

## Ideas (unscoped)
- Screen shake / hit feedback juice pass
- Upgrade system between waves
- Boss encounters
- Wave-clear-gated waves (once enemy death exists)

## Done
- 2026-07-31 — Balance Configurator gains an "Enemies" tab, splitting `EnemyData` and every `EnemySpawnData` roster (Default/Lava/Ice/Acid) out of "General" so all enemy tuning lives on its own page; "General" now holds only player/ship-facing data (ship movement, weapon, health, target lock)
- 2026-07-31 — Balance Configurator gains a "Planets" tab (`GUILayout.Toolbar`, "General"/"Planets") exposing `PlanetSpawnData` (count/separation/placement-attempts) and per-type `PlanetData` (min/max scale, min/max encounter-zone-radius multiplier) as sliders alongside the existing 6 Data types
- 2026-07-31 — `PlanetSpawner` orients the player ship (and its rigidly-mounted cockpit camera) toward the nearest spawned planet once at scene start, so the player isn't facing empty space on entry
- 2026-07-31 — Procedural planet spawning: the 3 hand-placed lava/ice/acid planets are replaced by `PlanetSpawner`, which scatters 3 independently-random-typed planets across the playable field at scene start (random position, scale, and encounter-trigger radius each within data-driven min/max ranges), each wired to its own per-type `EnemySpawnData` roster; `PlanetData`/`PlanetSpawnData`/`PlanetSpawner`, `Planet` prefab (see `specs/procedural-planets.md`)
- 2026-07-31 — Enemy/player `Scale` tunable: `EnemyData`/`ShipMovementData` gained a `Scale` field (default matches today's baked root scale: Basic 1, Medium 2, Large 5, ship 6), applied to the root transform in `Awake` — collider and fire point are already defined in root-local space so they scale automatically; shows up as a slider in the Balance Configurator for free. Planets deferred (no data-driven planet system yet).
- 2026-07-31 — Balance Configurator editor window (`Window > Manatite > Balance Configurator`): every numeric field on the 6 Data ScriptableObjects gets a 0.1x-10x slider around a "standard" baseline (today's tuned values, captured lazily into `BalanceBaseline.asset`); `Manatite.Editor.asmdef`/`BalanceBaseline`/`BalanceConfiguratorWindow` (see `specs/balance-configurator.md`)
- 2026-07-29 — **Real fix for enemies "shooting toward another direction":** added an `AimPoint` child on the player ship at its actual geometric center (world Y≈15.75) and pointed `EnemySpawnZone`'s enemy targeting at it instead of the ship's raw pivot (Y=0). The ship's hull was never centered on its own transform origin, so every enemy shot flew level, ~9-19 units under the entire hull — a live-captured shot proved the yaw aim was already exactly correct, it was purely vertical. Verified live: an enemy that previously landed 0 hits over a full engagement dealt 8 damage once retargeted. (Reverted an earlier wrong `moveSpeed` change made while chasing this — that wasn't the cause.)
- 2026-07-29 — Fixed intermittent enemy misfires even from settled/non-turning enemies: `EnemyController`'s aim calc used a fixed world-up reference in `Quaternion.LookRotation`, which is numerically unstable when the enemy ends up nearly directly above/below the player (common with random-sphere spawn placement in full 6DoF); now falls back to `Vector3.forward` near that singularity
- 2026-07-29 — Player takes damage from enemy projectiles (now that it has `Health`); `PlayerDeath` quits/stops Play Mode on `Health.OnDied` (placeholder until a real game-over flow exists); fixed enemies missing most shots by scaling the undersized player ship 6x (root, not just the mesh) to be comparable in size to the Basic enemy, plus re-tuned `EnemyData.stopDistance` per enemy size so they hold clear of the player's hull instead of overlapping it (see GDD decisions log)
- 2026-07-29 — Damage/health are always `int` (never `float`); all projectiles (player + enemy) default to 1 damage; player has 10 HP via the existing `Health` component + new `Health_Player.asset`
- 2026-07-21 — Crosshair: always-visible center reticle (uGUI, `HUD` Canvas) reacts to `TargetLock` — color swap on lock plus a `LockIndicator` that tracks the locked target's screen position each frame; `Crosshair.cs` (see `specs/crosshair.md`)
- 2026-07-21 — Target lock releases immediately when the current target dies (`Health.OnDied`), same as pressing the Lock button; toggleable via `TargetLockData.unlockOnTargetDeath` (see `specs/target-lock-death-retarget.md`)
- 2026-07-21 — Enemy combat: `Health` (generic, pool-safe) + damage on projectile hit, enemy death releases to pool; enemies shoot back via `EnemyWeapon` reusing the player's `ProjectileLauncher`/`Projectile` pipeline, per-enemy `canFire` toggle; `HealthData`/`Weapon_EnemyBasic`/`EnemyHealth_Basic` (see `specs/enemy-combat.md`)
- 2026-07-21 — Target-switch flick: while locked, deflecting the right stick past a threshold switches the lock to the nearest other `Lockable` in that screen-space direction (search radius + direction cone relative to the current target); reuses the existing `Look` action, no new binding (see `specs/target-lock-switching.md`)
- 2026-07-21 — Primary weapon: RB fires pooled forward-traveling projectiles (Auto/Single fire mode toggle), despawn on hit or lifetime timeout, `OnHit` event seam for a future modifier system; `WeaponData`/`Weapon`/`Projectile`, `Projectile_Basic` prefab (see `specs/weapon-system.md`)
- 2026-07-21 — Enemy spawning: per-planet trigger zones spawn a configurable, pooled enemy roster (all-at-once or timed waves) that pursues the ship; `GameLayers`/`ObjectPooler`/`EnemyMovementData`/`EnemySpawnData`/`EnemyController`/`EnemySpawnZone`, `BasicEnemy` prefab (see `specs/enemy-spawning.md`)
- 2026-07-21 — Target lock: R3 acquires nearest-to-screen-center `Lockable`, ship auto-turns onto it (PD auto-aim); `Lockable`/`TargetLock`/`TargetLockData` (see `specs/target-lock.md`)
- 2026-07-20 — Player ship: 6DOF Newtonian flight + gamepad input + cockpit FPS camera; `Manatite.Runtime.asmdef` foundation (see `specs/ship-6dof-fps.md`)
- 2026-07-18 — Agentic workspace setup (CLAUDE.md, skills, agent, docs)
