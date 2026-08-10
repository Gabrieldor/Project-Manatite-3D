# Corrosion Chip

## Goal
Equipping the Corrosion chip makes the primary weapon's hits linger: a struck enemy gets a green tint, taking chip-configured damage-per-second for a chip-configured duration, flinching (a brief random rotational "tweak") on every tick.

## Reuse vs. create
- **Reuse**: `ChipData`/`ChipModifier`/`ChipStatTarget`/`ChipInstance` framework, `PlayerInventory.GetModifiedFloat/Int`, `Weapon` → `ProjectileLauncher.Fire/ApplyDamage` pipeline (same trailing-optional-params pattern as splash), `Health.TakeDamage` (already fires `OnDamaged`/`OnDied` normally — death mid-corrosion just works), Balance Configurator's existing generic `ChipModifier[]` drawer (no new configurator code needed), `fireratechip.png` icon placeholder (no new art per project rule).
- **Create**: two new `ChipStatTarget` entries, a `CorrosionEffect` component (the status itself, including a runtime-generated tint overlay material), one `ChipData` asset.

## Deliverables
- `Scripts/Data/ChipStatTarget.cs` — add `CorrosionDuration`, `CorrosionDps`.
- `Scripts/Data/ChipModifier.cs` — `ClampToValueKindRange` gets its own envelope for both (like `SplashRadius` already does): `CorrosionDuration` 0..10 (seconds), `CorrosionDps` 0..5 (int, floored same as other int stats).
- `Scripts/Weapons/Weapon.cs` — `Fire()` resolves `effectiveCorrosionDuration = inventory.GetModifiedFloat(ChipStatTarget.CorrosionDuration, 0f)` and `effectiveCorrosionDps = inventory.GetModifiedInt(ChipStatTarget.CorrosionDps, 0)`, passes both into `ProjectileLauncher.Fire`.
- `Scripts/Weapons/ProjectileLauncher.cs` — `Fire`/`ApplyDamage` gain trailing optional params `float corrosionDuration = 0f, int corrosionDps = 0`. When both > 0, after direct damage: get-or-add a `CorrosionEffect` on the hit `Health`'s GameObject and call `Apply(dps, duration)`. `EnemyWeapon` untouched (defaults stay 0, enemies never corrode).
- `Scripts/Combat/CorrosionEffect.cs` (new) — status component:
  - Tint: the target's own shader (a texture-sampling toon graph shared with the player ship) exposes no tintable color property, so `MaterialPropertyBlock` has nothing to drive. Instead, `Apply` appends a runtime-generated translucent green `Universal Render Pipeline/Unlit` material (proper alpha-blend transparent setup, one shared instance for the whole game) as an extra material slot on every child `Renderer`'s `sharedMaterials`; `EndEffect` removes that trailing slot. Works regardless of the target's own shader.
  - Damage tick: every 1 second while active, `health.TakeDamage(dps)`.
  - Tweak: on each tick, kicks a child transform (target's `Model` child if present, else self) to a random rotation offset (±8° per axis) around its rest pose, then lets it ease back to rest over ~0.15s before the next possible tick — reads as a flinch, not a permanent skew.
  - Re-hit while already active refreshes duration/dps to the new call's values rather than stacking.
  - `OnDisable` (pool release, e.g. on death) fully resets: clears tint, restores rest rotation, zeroes remaining duration — so a reused pooled enemy never spawns pre-corroded.
- `Assets/_Project/Data/Weapons/Chip_Corrosion.asset` — new `ChipData`, 2 modifiers: `CorrosionDuration` Flat (rolls 3..7s), `CorrosionDps` Flat (rolls 1..2); icon = reused `fireratechip.png`. Wired into `NavePrincipal`'s `PlayerInventory.startingInventoryItems` for testing (same as the other two chips).

## Decisions & defaults
- No fire-rate/damage tradeoff on this chip (unlike Splash Damage) — pure bonus proc, matching your description.
- Assumed corrosion only ever lands on enemies (the player's own weapon is the only caller passing non-zero values; `EnemyWeapon` keeps defaulting to 0) — player-on-player/enemy-on-player corrosion is out of scope until enemies get chips.
- Assumed re-hitting an already-corroded target refreshes (doesn't stack duration or dps) — simplest behavior, avoids runaway DPS from rapid-fire weapons.
- Assumed damage ticks are wall-clock (`Time.deltaTime` accumulation), independent of `Health`'s own reset-on-enable — consistent with how `Health.TakeDamage` already works elsewhere.
- Jitter reuses the enemy prefab's existing `Model` child (verified present on `BasicEnemy`) so it never fights `EnemyController`'s own root-rotation control (root is owned by the AI's `Rigidbody.MoveRotation` every `FixedUpdate`).
- Chip starts in inventory (untested loot/pickup system, same as the other two chips).
- **Smoke VFX dropped (2026-08-09 follow-up)**: originally shipped a pooled looping smoke `ParticleSystem`, but the user couldn't see it in a normal Play session even after fixing its material (auto-generated as Opaque, ignoring the configured alpha fade) and its culling mode (`Automatic`, which can leave a freshly-spawned effect unsimulated). Rather than keep chasing it, removed the smoke entirely per user request — `WeaponData.corrosionVfxPrefab` and all its threading through `Weapon`/`ProjectileLauncher`/`CorrosionEffect.Apply` are gone, and the prefab/material assets were deleted. Tint + DoT tick + jitter are unaffected.

## Out of scope
- Loot drops / RNG acquisition.
- Save/load persistence (matches existing chip system scope).
- Stacking multiple corrosion applications, damage falloff, or resistances.
- A bespoke chip icon.
- Corrosion affecting the player (no enemy chips yet).
- Any visual smoke/particle effect (removed, see Decisions above).

## Acceptance checks
- Equipping `Chip_Corrosion` and hitting an enemy: it tints green and loses 1 HP per second (per rolled `CorrosionDps`) for the rolled duration, then reverts to normal color.
- Each damage tick visibly kicks the enemy's rotation briefly before it eases back.
- An enemy that dies mid-corrosion cleans up normally (no leaked green tint on the next pooled spawn of that prefab).
- Unequipping the chip: new hits no longer apply corrosion (already-active corrosion on existing targets is out of scope to cut short).
