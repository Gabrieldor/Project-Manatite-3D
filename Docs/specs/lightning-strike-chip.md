# Lightning Strike Chip

## Goal
Equipping the Lightning Strike chip makes every hit crackle outward: the struck enemy's shot arcs to another nearby enemy in a flash of light, then bounces again, chaining through several enemies (a rolled 3-7 by default) before fading, each jump dealing its own small burst of damage.

## Reuse vs. create
- **Reuse**: `ChipData`/`ChipModifier[]`/`ChipValueKind`/`ChipInstance` framework (multi-modifier chip, already established by Splash Damage/Corrosion — no architecture change needed), `PlayerInventory` aggregation, the `Weapon` → `ProjectileLauncher` → `Projectile` fire/hit pipeline, `GameLayers` tag-based friendly-fire skip, `DamageHealth`'s shared hit-sound/tag-skip logic, `ObjectPooler`, the pooled-self-releasing-VFX pattern (`HitMarkerEffect`'s `ParticleSystemStopAction.Callback`), Balance Configurator's Chips tab.
- **Create**: three new `ChipStatTarget` entries + clamp envelopes, chain-lightning target-search + damage loop in `ProjectileLauncher`, a pooled `LightningArcEffect` component (particle system + light) + prefab + material, one new `ChipData` asset.

## Deliverables
- `Scripts/Data/ChipStatTarget.cs` — add `LightningRange`, `LightningChainCount`, `LightningDamage`.
- `Scripts/Data/ChipModifier.cs` — new clamp envelopes: `LightningRange` 0..1000 (world units), `LightningChainCount` 0..15, `LightningDamage` 0..15.
- `Assets/_Project/Data/Weapons/Chip_LightningStrike.asset` — new `ChipData`, 3 modifiers: `LightningRange`×Flat(500..500), `LightningChainCount`×Flat(3..7), `LightningDamage`×Flat(1..4); icon reuses `fireratechip.png` (say if you want a bespoke icon).
- `Scripts/Data/WeaponData.cs` — add `lightningVfxPrefab` (GameObject) field/property.
- `Scripts/Weapons/Weapon.cs` — `Fire()` resolves `effectiveLightningRange/ChainCount/Damage` via `GetModifiedFloat/Int(ChipStatTarget.Lightning…, 0)`, passes them (+ `data.LightningVfxPrefab`) into `ProjectileLauncher.Fire`.
- `Scripts/Weapons/ProjectileLauncher.cs` — `Fire`/`ApplyDamage` gain trailing optional params (`float lightningRange = 0f, int lightningChainCount = 0, int lightningDamage = 0, GameObject lightningVfxPrefab = null`). When `chainCount > 0 && range > 0`: starting from the hit point/primary target, iterate up to `chainCount` times — each step does an `OverlapSphereNonAlloc` (new static buffer, mirrors `SplashColliderBuffer`) from the current point and ranks candidates by a two-tier comparison: **unstruck-this-shot beats already-struck, regardless of distance; within the same tier, nearest (by `sqrMagnitude`) wins.** The node the bolt is currently at is always excluded (prevents a 0-distance bounce straight back). Spawns a `LightningArcEffect` between the current point and the chosen target, deals `lightningDamage` via the existing `DamageHealth` helper (re-striking an already-hit target deals damage again — no once-per-shot cap on damage, only on selection priority), then continues the chain from there. Stops early (no error) only if literally no other enemy exists within range of the current point — the 3-7 roll is a cap, not a guarantee. `EnemyWeapon.cs` untouched (params default to 0, enemies never chain-lightning).
- `Scripts/Core/LightningArcEffect.cs` (new) — pooled: `Play(Vector3 from, Vector3 to)` positions/orients the object between the two points and scales a stretched particle emission (Shape = thin box/edge along local forward) to span the distance, plus flashes a child `Light` (point, no shadows, short range ~20u) from peak intensity to 0 over the burst. Self-releases to `ObjectPooler` via `ParticleSystemStopAction.Callback`, same as `HitMarkerEffect`.
- `Assets/_Project/Materials/VFX/LightningArc.mat` — additive/unlit particle material (GPU-instanced), authored via Unity MCP.
- `Assets/_Project/Prefabs/VFX/LightningArc.prefab` — `ParticleSystem` + child `Light` + `LightningArcEffect`, authored via Unity MCP.
- `Scripts/Editor/BalanceConfiguratorWindow.cs` — verify the Chips tab renders the new modifiers through the existing array-aware drawer (no change expected).

## Decisions & defaults
- Assumed the "min 3 / max 7 targets" and "1-4 damage" ranges are the chip's **per-instance roll range** (like Corrosion's duration/DPS), not a hard floor — reuses the existing `ChipModifier` min/max-roll mechanic instead of a new mechanism. *(Say if you want every copy of this chip to always chain exactly a fixed count rather than rolling.)*
- Chain target count means **additional** enemies hit after the directly-hit one (a 3-roll chip hits 4 enemies total: 1 direct + 3 chained), each only once per shot.
- Bounce picks the **nearest** valid target in range each hop, but always prefers an unstruck one over an already-struck one even if farther away (only re-strikes when nothing unstruck is left in range) — reads clearly on screen and lets the chain keep going in sparse fights instead of fizzling early.
- No line-of-sight/occlusion checks for v1 (matches Splash Damage's precedent) — a bolt can arc through terrain/other ships.
- Lightning damage is a standalone per-jump amount (like Corrosion DPS), not added to the weapon's direct damage stat; multiple equipped Lightning chips stack additively under the existing sum formula (no special-case exclusivity, matches Splash Damage precedent).
- Player-only for v1 — `EnemyWeapon` keeps defaulting to 0, same precedent as splash/corrosion.
- No new chip icon — reuses `fireratechip.png` as a placeholder.

## Out of scope
- Loot drops / RNG acquisition of the new chip (starts in inventory like the others).
- Save/load persistence.
- Line-of-sight/occlusion checks, branching/forking chains (tree instead of a line), or chain falloff by hop.
- A bespoke chip icon.
- Enemies using this chip.

## Acceptance checks
- Firing into a cluster of 5+ enemies: the directly-hit enemy takes normal weapon damage; a visible bolt (particles + light flash) arcs to the nearest other enemy, then chains again, hitting between 3 and 7 additional enemies total (per the roll), each taking the rolled lightning damage exactly once.
- Firing where fewer than 3 other enemies are in range: the chain keeps bouncing between the available enemies (re-striking as needed) rather than stopping early, and only halts once truly no other enemy is in range of the current point — no errors, no 0-distance bounce back to the node it just came from.
- Unequipping the chip: lightning stops triggering entirely (range/chainCount resolve to 0).
- Repeated fires leave no leaked always-visible arc/light instances — pooled count stays stable.
