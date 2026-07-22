# Enemy Combat — Health & Weapons

## Goal
Enemies can be damaged and destroyed, and (per enemy type) can shoot back at the
player using the same weapon/projectile pipeline the player uses.

## Reuse vs create
- Reuse: `ObjectPooler`, `Projectile` (pooled, `OnHit` event seam), `WeaponData` SO
  (extended with a `damage` field), existing SO/asmdef conventions.
- Create: `Manatite.Combat.Health` (generic — player will reuse it later per
  Backlog #1), `Manatite.Data.HealthData` SO, `Manatite.Weapons.ProjectileLauncher`
  (shared fire+damage logic factored out of `Weapon` so enemies use the *exact*
  same path), `Manatite.Enemies.EnemyWeapon`.

## Deliverables
- `Scripts/Data/HealthData.cs` (SO: `maxHealth`) + `Data/EnemyHealth_Basic.asset`.
- `Scripts/Combat/Health.cs` — current/max, `TakeDamage`, `OnDamaged`, `OnDied`;
  resets to full in `OnEnable` (pool-safe).
- `Scripts/Data/WeaponData.cs` — add `damage` field + `Damage` property.
- `Scripts/Weapons/ProjectileLauncher.cs` — shared `Fire(data, firePoint)`:
  spawns via `ObjectPooler`, subscribes `Projectile.OnHit` to apply `data.Damage`
  to the hit collider's `Health` (`GetComponentInParent`). `Weapon.Fire()` now
  calls this instead of duplicating the spawn logic.
- `Scripts/Weapons/Projectile.cs` — release path now clears `OnHit` (`= null`)
  on every return-to-pool (hit *and* lifetime timeout), so per-shot subscriptions
  never leak across reuse.
- `Scripts/Enemies/EnemyController.cs` — `[RequireComponent(typeof(Health))]`;
  subscribes `Health.OnDied` → releases to pool (same as leash-distance death).
- `Scripts/Enemies/EnemyWeapon.cs` — no input; fires forward on a `WeaponData`-
  driven cooldown whenever enabled; `[SerializeField] bool canFire = true` toggle.
- `Data/Weapon_EnemyBasic.asset` (own fire rate/damage, reuses `Projectile_Basic`
  prefab).
- `Prefabs/Enemies/BasicEnemy.prefab` — add `Health` (→ `EnemyHealth_Basic`),
  `EnemyWeapon` (→ `Weapon_EnemyBasic`), and a child `FirePoint` transform offset
  in front of the capsule (its own `CapsuleCollider` would otherwise self-hit the
  projectile instantly at spawn).

## Decisions & defaults
- "Configure if an enemy shoots" = the `EnemyWeapon` component itself (present +
  `canFire` toggle) — same pattern as `Lockable`/`EnemyController` being just
  components on a prefab. No shooting variants can omit the component or flip
  the toggle off. Say if you wanted this driven from `EnemySpawnData` instead.
- No aiming/range gating: `EnemyController` already rotates the enemy to face
  the player, so `EnemyWeapon` just fires forward on its own timer — simplest
  MVP, refinable later (e.g. as a modifier).
- No friendly-fire check — enemy shots damage whatever `Health` they hit.
- `Health` is generic/reusable, but wiring it to the player/HUD is out of scope
  here (Backlog #1's other half).

## Out of scope
Player health/HUD, death VFX/audio, damage numbers popup, modifiers (system is
just not blocked by this — `Health` events + `Projectile.OnHit` are the seams).

## Acceptance checks
1. Player projectiles reduce an enemy's health and it's released to the pool
   (disappears) at 0.
2. A `BasicEnemy` fires projectiles at the player at `Weapon_EnemyBasic`'s fire
   rate without self-colliding on spawn.
3. Toggling `EnemyWeapon.canFire` off on the prefab stops it from shooting with
   no other code changes.
4. No console errors across a play session with multiple spawns/deaths/reuses.
