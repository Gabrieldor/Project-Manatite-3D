# Enemy rocket exhaust VFX

## Goal
Enemies visibly thrust — a rocket-flame particle effect emits continuously from the back of each enemy model while it's alive/active, centered on the model and sitting right at the back edge of its hull.

## Reuse vs new
- Reuse: URP Particles/Unlit shader + soft-particle texture (guid `4a39a6b26fa907b49821a17a5e3a6245`) already used by `ExplosionParticle.mat`; same additive-transparent blend convention as the project's other VFX materials.
- New: `Assets/_Project/Materials/VFX/RocketExhaust.mat`; a `RocketExhaust` particle child added to each of `BasicEnemy.prefab`/`MediumEnemy.prefab`/`LargeEnemy.prefab`.

## Decisions & defaults
- All 3 enemy prefabs share identical local-space hull geometry (`BoxCollider` size `20x33.34x26.67`, center `z=3.34`) — only `EnemyData.Scale` differs, applied to the root transform at runtime. So one set of local coordinates works for all three: back edge ≈ local `z = -10`, centered at `x=0, y=0`.
- Emitter is a child of the enemy root (not `Model`), rotated 180° on Y so its local forward (+Z, the cone's default emit axis) points toward the parent's -Z (backward, opposite `transform.forward` which `EnemyController` uses for movement).
- Continuous looping emission (not a pooled one-shot burst like `HitMarkerEffect`/`ParticleExplosion`) — it's a permanent child of an already-pooled enemy, so it just plays/pauses with the enemy's own active state via the existing `ObjectPooler` Get/Release, no new pooling code needed.
- Look: hot cone with a white-hot core fading through orange to transparent red, matching the warm palette already used by `ExplosionParticle`/`LightningArc`. Additive blending, no lighting/shadows/collision. **Sized up per user feedback (initial pass read too small/tight to the hull)**: `startSize` 1.6→6.4 (4x wider), `startLifetime` 0.28s→2.8s at the same `startSpeed` (10) → travel distance 2.8→28 units (10x longer, trails well off the back of the ship into space); shape cone `radius` 1.2→3, `angle` 12°→16° to widen the plume's base to match; `maxParticles` 100→250 (headroom for ~168 particles alive at once at rate 60/s × 2.8s lifetime, same as before so density/spacing along the trail is unchanged).
- Not scaled by `EnemyData.Scale` explicitly — it inherits the root's `localScale` automatically since it's a plain child.

## Out of scope
- No per-enemy-type flame color/size variation (all 3 share the same effect for now).
- No light flicker/glow (`ExplosionParticle`'s companion `ExplosionLight` pattern) — pure particle only, can be added later if it reads too flat.
- No audio changes.

## Acceptance checks
- Each of the 3 enemy prefabs has a `RocketExhaust` ParticleSystem child positioned at local `(0, 0, -10)`, rotated 180° on Y, looping + playOnAwake.
- `RocketExhaust.mat` exists, additive/transparent, assigned to all 3.
- Flame trail spans ~28 local units behind the hull (before the enemy's own root-scale multiplier) and ~6.4-unit-wide particles, clearly extending past the model's silhouette rather than hugging it.
- Compiles clean, no console errors after prefab edits (`/verify`).
