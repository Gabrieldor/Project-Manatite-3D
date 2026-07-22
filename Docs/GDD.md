# Manatite — Game Design Document

> Living document. Claude updates this when design decisions are made during development; sections marked *TBD* are open — filling them in is the fastest way to improve every future feature request.

## Vision
- **Genre**: 3D space shooter. *(TBD: on-rails / arena free-flight / horizontal corridor?)*
- **One-liner**: *TBD — what's the fantasy? e.g. "lone fighter pilot carving through alien waves."*
- **Platform target**: PC only. (A `Mobile_RPAsset` exists in `Assets/Settings` but is not an active target — leftover from template scaffolding.)
- **Session shape**: *TBD — endless waves / levels / boss runs?*
- **Playable field**: bounding box centered at `(275, 0, 137.5)`, size `9350 × 6600 × 10175` (X: -4400..4950, Y: -3300..3300, Z: -4950..5225), origin `(0,0,0)` at field center. Derived from the farthest "Planet_*" test obstacle on each axis in `Outer Space.unity` +10% margin. Marked in-scene by the `PlayfieldBounds` trigger volume. Camera `farClipPlane` set to 17000 to cover worst-case corner-to-corner viewing distance inside it.

## Core loop
*TBD: e.g. fight wave → collect drops → upgrade → harder wave.*

## Player ship
- Movement model: **6DoF free-flight, Newtonian** (thrust-based; no auto-stop). Gamepad:
  left stick = translate (forward/back + strafe), right stick = pitch/yaw (non-inverted, up=nose up),
  LT/RT = descend/ascend, LB/RB = roll, R3 = target lock. First-person **cockpit camera** rigidly mounted. Tunables in `ShipMovement_Default.asset`.
- Target lock: R3 acquires the `Lockable` object nearest screen center (within a viewport radius); while locked, the ship auto-turns its nose onto the target (camera stays rigidly mounted — no independent camera movement). While locked, flicking the right stick switches the lock to the nearest other `Lockable` in that screen-space direction relative to the current target (within a search radius/cone; no qualifier = no-op). R3 again releases. Tunables in `TargetLock_Default.asset`.
- Weapons: **Primary fire** — RB spawns pooled forward-traveling projectiles (placeholder cube), despawn on hit or after a lifetime timeout. Fire mode (Auto/Single) and all tunables live in `WeaponData` assets (default: `Weapon_Default.asset`). Modifier system planned but not built: `Projectile` exposes an `OnHit` C# event as the future attachment point instead of injecting code into the bullet directly. Secondary weapon/pickups/heat/ammo: *TBD*.
- Health/shield: *TBD*

## Enemies
- **Basic Enemy**: capsule placeholder model, straight-line pursuit of the player, no attack yet. Tunables in `EnemyMovement_Basic.asset`.
- **Spawn rule**: proximity-triggered. Each `Planet_*` has a trigger zone (1.5× the planet's radius); entering it spawns a configurable roster (which enemies, how many, all-at-once or timed waves) around the ship's current position. Config lives in `EnemySpawnData` assets (default: `EnemySpawn_Default.asset`, 5× Basic Enemy, all at once). Zones re-arm 30s after their encounter finishes spawning.
- *TBD: other archetypes (shooter, tank, boss); enemy attacks/health/death.*

## Progression & scoring
*TBD*

## Art & audio direction
*TBD: stylized? realistic? palette?*

## Decisions log
| Date | Decision | Why |
|------|----------|-----|
| 2026-07-18 | ScriptableObject-driven tuning, pooled projectiles/enemies, new Input System only | Project architecture baseline (see CLAUDE.md) |
| 2026-07-20 | Player ship = 6DoF Newtonian free-flight, gamepad-first, first-person cockpit view | User control spec (see `specs/ship-6dof-fps.md`) |
| 2026-07-21 | Playable field fixed at 9350×6600×10175 box (see Vision) | Derived from farthest surviving mobility-test "planet" per axis +10% margin |
| 2026-07-21 | Platform target is PC only, not PC+Mobile | User correction; `Mobile_RPAsset` is unused template leftover |
| 2026-07-21 | Target lock (R3): ship auto-turns to face target, camera stays rigidly mounted | User's explicit choice over decoupled camera or HUD-only marker (see `specs/target-lock.md`) |
| 2026-07-21 | Enemy spawning: per-planet sphere-trigger zones, not global distance polling; data-driven roster/timing via `EnemySpawnData` | Matches hot-path/no-Update-polling rule; keeps spawn config swappable per planet later (see `specs/enemy-spawning.md`) |
| 2026-07-21 | Primary weapon: modifier hook is a plain C# event (`Projectile.OnHit`), not code/string injection into the bullet | Matches project's existing decouple-via-events rule; any future modifier scheme (event subscription, spawn-time strategy list) can attach to it (see `specs/weapon-system.md`) |
| 2026-07-21 | Target-switch flick reuses the existing `Look` action (right stick) with edge-triggered hysteresis, instead of a new input binding/cooldown timer | While locked, that stick's deflection is already unused for pitch/yaw; simplest reuse, no `ShipControls.inputactions` changes (see `specs/target-lock-switching.md`) |
| 2026-07-21 | Crosshair reacts to lock via a center-reticle color swap plus a marker that tracks the locked target's screen position, not new shape/art | Built-in UI Image only, no new texture/art pipeline (see `specs/crosshair.md`) |
