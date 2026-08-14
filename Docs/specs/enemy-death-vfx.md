# Enemy Death VFX

## Goal
When an enemy dies, it bursts into a flat blue disk-shaped particle effect (dimmer, cooler-colored cousin of the splash-damage explosion) instead of just vanishing.

## Reused
- `Manatite.Core.ParticleExplosionEffect` (`Assets/_Project/Scripts/Core/ParticleExplosionEffect.cs`) — pooled, self-releasing `IRadiusVfx` particle burst. Only the prefab's ParticleSystem module values/material differ; no new script needed.
- `ObjectPooler.Get` / `.Release` (`Assets/_Project/Scripts/Core/ObjectPooler.cs`).
- `Health.OnDied` → `EnemyController.HandleDeath()` (already wired).
- Material/color reference: `Assets/_Project/Materials/VFX/LightningArc.mat` (`_BaseColor` `0.65, 0.85, 1`).
- Structural reference: `Assets/_Project/Prefabs/VFX/ParticleExplosion.prefab` + `Assets/_Project/Materials/VFX/ExplosionParticle.mat`.

## Deliverables
- `Assets/_Project/Materials/VFX/EnemyDeathDisk.mat` — clone of `ExplosionParticle.mat` (same shader/soft-glow texture, Additive blend), `_BaseColor`/`_Color` set to LightningArc's blue, lower alpha.
- `Assets/_Project/Prefabs/VFX/EnemyDeathDisk.prefab` — clone of `ParticleExplosion.prefab`'s structure: `ParticleSystem` + `ParticleExplosionEffect`. Shape module = **Circle** (flat) instead of Sphere, so the burst is disk-shaped rather than a sphere. No point light child (this is what makes it "dimmer" than the chip explosion, which flashes a bright light). Lower `sizePerRadiusRange`/`particlesPerRadius` defaults than `ParticleExplosion`. Uses the new material, blue color-over-lifetime gradient fading to transparent.
- `Assets/_Project/Scripts/Data/EnemyData.cs` — new `[Header("Death")] deathVfxPrefab`/`deathVfxRadius` fields + `DeathVfxPrefab`/`DeathVfxRadius` properties.
- `Assets/_Project/Scripts/Enemies/EnemyController.cs` — `HandleDeath()` snapshots position/rotation before releasing the enemy to the pool, then spawns `data.DeathVfxPrefab` via `ObjectPooler.Get` and calls `IRadiusVfx.Play(data.DeathVfxRadius)` if present.
- Wire `deathVfxPrefab` → `EnemyDeathDisk.prefab` on `EnemyData_Basic/Medium/Large.asset`.

## Decisions & defaults
- The "radius" driving `Play()` is a dedicated `EnemyData.DeathVfxRadius` field (world units), tuned per enemy type to its actual hull size — **not** `EnemyData.Scale`, which is a small transform multiplier (~3-7) and produced an invisible sub-1-unit burst against a ~60-100 unit ship when first tried (caught via user report, fixed same day).
- Assumed "dimmer" means: no light flash, fewer/smaller particles, lower peak alpha — not a duller color.
- Assumed disk orientation follows the enemy's death transform as-is (no forced billboard/up-alignment) — cheap and looks fine for a symmetric radial burst.
- No new death SFX (out of scope, can be added separately via `AudioManager`/`SfxCue`).

## Out of scope
- VFX Graph conversion, screen shake, death sound cue, player-ship death VFX.

## Acceptance checks
1. Killing an enemy in Play mode spawns a flat blue disk burst at its death position, visibly dimmer/cooler than the splash explosion.
2. Large enemies produce a visibly bigger disk than Basic enemies.
3. No console errors; the VFX instance self-releases back to the pool after playing.
4. All three enemy types (Basic/Medium/Large) show the effect on death.
