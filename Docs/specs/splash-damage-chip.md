# Splash Damage Chip

## Goal
Equipping the Splash Damage chip trades fire rate for burst power: the primary weapon fires half as often but hits harder, and each hit blasts a radius around the impact point, damaging every enemy caught in it — with a fading explosion sphere marking the blast.

## Reuse vs. create
- **Reuse**: `ChipData`/`ChipValueKind`/`ChipStatTarget`/`ChipInstance` framework, `PlayerInventory` aggregation (`GetModifiedFloat`/`Int`), the `Weapon` → `ProjectileLauncher` → `Projectile` fire/hit pipeline, `ObjectPooler`, `GameLayers` tag-based friendly-fire skip, Balance Configurator's reflection-driven "Chips" tab, existing `fireratechip.png` icon.
- **Create**: multi-modifier `ChipData` shape (a chip can now carry more than one stat effect at once), two new `ChipStatTarget` entries (`Damage`, `SplashRadius`), splash-on-hit logic in `ProjectileLauncher`, a pooled fading-sphere VFX component + prefab + material, one new `ChipData` asset.

## Architecture change: a chip can now bundle multiple modifiers
Today one `ChipData` = exactly one `(statTarget, valueKind, min/max)` triple. Splash Damage needs three simultaneous effects (fire rate, damage, splash radius) from a single inventory item, so `ChipData` changes from flat fields to a `ChipModifier[] modifiers` array (each entry: `statTarget`, `valueKind`, `minValue`, `maxValue`, clamped the same way as before). `ChipInstance` rolls one random value per modifier entry instead of a single value. `PlayerInventory.GetModifiedFloat/Int` iterate every equipped chip's modifiers array (was: one field per chip) — same combination formula, just more entries to sum. `Chip_FireRate.asset` migrates to a single-entry `modifiers` array (no behavior change). The Balance Configurator's Chips tab keeps working via its existing reflection drawer.

## Deliverables
- `Scripts/Data/ChipStatTarget.cs` — add `Damage`, `SplashRadius`.
- `Scripts/Data/ChipModifier.cs` (new) — `[Serializable]` struct: `statTarget`, `valueKind`, `minValue`, `maxValue`.
- `Scripts/Data/ChipData.cs` — replace single statTarget/valueKind/min/max fields with `ChipModifier[] modifiers`; `OnValidate` clamps each entry by its own `valueKind`.
- `Scripts/Core/ChipInstance.cs` — rolls one `float` per modifier into a parallel `RolledValues` array.
- `Scripts/Player/PlayerInventory.cs` — `GetModifiedFloat`/`GetModifiedInt` sum across all modifiers (of all equipped chips) matching `target`.
- `Assets/_Project/Data/Weapons/Chip_FireRate.asset` — migrated to the new `modifiers` shape (1 entry, same values as today).
- `Assets/_Project/Data/Weapons/Chip_SplashDamage.asset` — new `ChipData`: 3 modifiers — FireRate×Multiplier(-0.5), Damage×Flat(+3), SplashRadius×Flat(50); icon = reused `fireratechip.png`.
- `Scripts/Data/WeaponData.cs` — add `splashVfxPrefab` (GameObject) field/property.
- `Scripts/Weapons/Weapon.cs` — `Fire()` resolves `effectiveDamage = inventory.GetModifiedInt(ChipStatTarget.Damage, data.Damage)` and `effectiveSplashRadius = inventory.GetModifiedFloat(ChipStatTarget.SplashRadius, 0f)`, passes both (+ `data.SplashVfxPrefab`) into `ProjectileLauncher.Fire`.
- `Scripts/Weapons/ProjectileLauncher.cs` — `Fire`/`ApplyDamage` gain trailing optional params `float splashRadius = 0f, GameObject splashVfxPrefab = null`. When `splashRadius > 0`: after direct damage, `Physics.OverlapSphere` at the contact point applies the same damage to every other `Health` in range (own-tag skip reused; the already-hit primary target excluded), then spawns `splashVfxPrefab` via `ObjectPooler`, sized to `splashRadius`. `EnemyWeapon.cs` is untouched — new params default to none, so enemies never splash.
- `Scripts/Core/FadingSphereEffect.cs` (new) — pooled, self-timed: `SetRadius(float)` scales the sphere, fades a `MaterialPropertyBlock` alpha to 0 over its lifetime, then releases itself via `ObjectPooler`. Modeled on `Projectile`'s self-timed release.
- `Assets/_Project/Materials/VFX/SplashExplosion.mat` — URP Unlit, Transparent surface (no custom shader).
- `Assets/_Project/Prefabs/VFX/SplashExplosion.prefab` — primitive sphere mesh (collider removed), material above, `FadingSphereEffect`.
- `Scripts/Editor/BalanceConfiguratorWindow.cs` — verify the Chips tab still renders/edits the new `modifiers` array through its existing reflection drawer; add minimal list support only if it doesn't already handle arrays.

## Decisions & defaults
- Assumed splash damage per secondary enemy = the same modified direct-hit damage (base + chip's flat bonus), no falloff by distance; the primary directly-hit target isn't double-damaged by its own splash. *(Say if splash should be a separately tunable amount.)*
- **Corrected after first pass**: the initial 4-unit default (picked to fit inside the existing ±5 Flat-chip clamp) was ~5-8x too small — measured actual scale is `BasicEnemy` 20×33×27, `MediumEnemy` 40×67×53, `LargeEnemy` 100×167×133, player ship ~20×23×30 (renderer bounds), and enemies spawn spread 60-120 units from the player (`EnemySpawnData.MinSpawnRadius/MaxSpawnRadius`). `SplashRadius` is a world-space distance, not a small stat delta like damage, so `ChipModifier.ClampToValueKindRange` now special-cases it with its own 0..150 envelope instead of sharing the Flat ±5 range. Default raised to **50 units** — bigger than a Basic enemy's own footprint, about half a Medium's, comfortably inside the spawn-spread scale so it can catch clustered enemies without trivializing an encounter.
- Assumed the VFX sphere's radius equals the splash radius (marks the blast boundary) — scale = `radius × 2` on a unit-diameter primitive sphere.
- Assumed explosion VFX lifetime/fade ≈ 0.4s, non-interactive (no collider, doesn't affect physics).
- **Follow-up (same day)**: swapped the placeholder URP-Unlit material for the user's own toon/bloom shader graph (`Shaders/BloomShaderToonColor.shadergraph` — rim light, diffuse gradient, bloom). That graph had no alpha output wired (opaque-only Target), so it was duplicated to `BloomShaderToonColorTransparent.shadergraph` with the Target flipped to Transparent and a new dedicated `_Alpha` scalar property added and wired to `SurfaceDescription.Alpha` (the graph's existing `_Color` property carries an unused alpha channel already, but a separate scalar keeps this decoupled from RGB tint). `SplashExplosion.mat` now references the transparent copy. `FadingSphereEffect` was reworked from fade-only to **grow (0→full size) then fade (alpha 1→0)** — `growDuration`/`fadeDuration` fields — and drives both `_BaseColor.a` and `_Alpha` via `MaterialPropertyBlock` every frame so it works against either shader variant unmodified (unknown property names are silently ignored).
- No splash falloff, chaining, DoT, or line-of-sight/occlusion checks for v1.
- New chip starts in a `startingInventoryItems` slot for testing, same as the fire-rate chip — no loot system exists yet.
- Multiple equipped Splash Damage chips would stack normally under the existing sum formula (no special-case exclusivity).

## Out of scope
- Loot drops / RNG acquisition of the new chip.
- Save/load persistence (matches the existing chip system's current scope).
- Splash falloff by distance, chaining, or a distinct splash-damage value separate from direct damage.
- Reworking the Balance Configurator UI beyond keeping the Chips tab functional.
- A bespoke chip icon (reuses `fireratechip.png` per your instruction).

## Acceptance checks
- Equipping `Chip_SplashDamage`: the weapon's effective fire interval roughly doubles, and its damage-per-shot goes up by 3 (verify via Play Mode cooldown/console).
- Firing into a small enemy cluster: the directly-hit enemy takes (base+3) damage once; every other enemy within ~50 units of the impact also takes (base+3); enemies farther away take none.
- A fading sphere appears at the impact point sized to the splash radius and disappears within ~0.4s; no leaked/always-visible instances after repeated fires.
- Unequipping the chip: fire rate/damage return to baseline and splash stops (radius resolves to 0).
