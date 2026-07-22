# Weapon System — Primary Fire

## Goal
Player holds RB to fire forward-traveling projectiles from the ship's nose; a projectile despawns the moment it hits something. Built with a clean extension point so a future modifier system can react to spawn/hit events without touching this code again.

## Reuse vs create
- Reuse: `ObjectPooler` (Get/Release), `ShipControls.inputactions` (`Ship` action map), existing SO/asmdef conventions (`Manatite.Data`, `Manatite.Runtime.asmdef`).
- Create: `Manatite.Weapons` namespace/folder, one new input action, one new prefab, one new SO asset, one FirePoint transform on the ship.

## Deliverables
- `Assets/_Project/Scripts/Data/WeaponData.cs` — SO: `fireMode` (Auto / Single), `fireRate` (shots/sec), `projectileSpeed`, `projectileLifetime`, `projectilePrefab`.
- `Assets/_Project/Scripts/Weapons/Weapon.cs` — reads `Fire` action, cooldown-gated spawn via `ObjectPooler.Get`, positions/orients at a `firePoint` Transform.
- `Assets/_Project/Scripts/Weapons/Projectile.cs` — Rigidbody-driven forward travel; lifetime timeout; `OnCollisionEnter` → despawn. Exposes `public event Action<Projectile, Collision> OnHit`, raised right before release-to-pool.
- `Assets/_Project/Data/Weapon_Default.asset` — default tunables instance.
- `Assets/_Project/Prefabs/Weapons/Projectile_Basic.prefab` — placeholder cube, non-kinematic Rigidbody (no gravity, Continuous Dynamic CCD), BoxCollider (non-trigger).
- `ShipControls.inputactions` — add `Fire` Button action to the `Ship` map, bound to `<Gamepad>/rightShoulder`.
- Scene: add a `FirePoint` child transform at the ship's nose; add `Weapon` component to `NavePrincipal` wired to `Weapon_Default`, `FirePoint`, and the existing controls asset.

## Decisions & defaults
- **Modifier hook** (the one open question you flagged): instead of a modifier injecting code/strings into the bullet, `Projectile` exposes a plain C# event (`OnHit`, and later `OnSpawned` if needed) — matching this project's existing "decouple via C# events" rule. A future modifier system subscribes to these events (or a `Weapon`-level hook that configures projectiles at spawn time) rather than mutating bullet internals. No modifier logic is built now — this is just the seam. Flag if you had a different mechanism in mind.
- Fire mode is a `WeaponData` enum toggle: **Auto** (hold RB, gated by `fireRate`) or **Single** (one shot per RB press, still rate-limited by `fireRate` as a minimum interval) — switchable per-asset so both are testable without code changes.
- Assumed non-trigger collision (`OnCollisionEnter`) with Continuous Dynamic CCD, since a thin fast cube can tunnel through thin colliders otherwise. Ship currently has no physical collider, so no self-hit risk.
- "Trigger the effect" = despawn only for now — no damage/health call, since the Health system doesn't exist yet (Backlog #2). The `OnHit` event is where damage will hook in later.
- Single fixed FirePoint (nose-center), no spread/multiple muzzles, no VFX/SFX/muzzle flash yet.

## Out of scope
Damage/health integration, ammo/heat, actual modifier implementations, muzzle VFX/audio, multiple fire points.

## Acceptance checks
1. Holding RB in Play mode spawns cubes from the ship's nose that fly straight forward at `projectileSpeed`, rate-limited by `fireRate`.
2. A projectile disappears (pooled, not destroyed) on hitting any collider, or after `projectileLifetime` if it hits nothing.
3. No console errors; `Fire` reads correctly from the gamepad RB button via the existing `ShipControls` asset.
