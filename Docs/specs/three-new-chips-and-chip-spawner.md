# Three new chips + chip spawner window

## Goal
Remove the "player starts with 4 chips" seeding, add three new chips (Speed, Health, Reflect),
and give designers a Play-mode editor window to grant chips for testing.

## Deliverables
- `Data/ChipStatTarget.cs`: add `MoveSpeed`, `MaxHealth`, `ReflectChance`.
- `Data/ChipModifier.cs`: clamp ranges for `MaxHealth` (0..30 flat) and `ReflectChance` (0..1 flat).
  `MoveSpeed` reuses the existing generic Multiplier range (-0.75..1).
- `Combat/Health.cs`: add `BaseMax` (unmodified `data.MaxHealth`, ignoring any override).
- `Player/PlayerHealthChipBridge.cs` (new): keeps `Health.Max` synced to equipped `MaxHealth` chips
  (recomputed on `PlayerInventory.OnInventoryChanged` + at `Awake`).
- `Player/ShipController.cs`: scale `StrafeThrust`/`ForwardThrust`/`VerticalThrust`/`MaxSpeed` by
  `inventory.GetModifiedFloat(ChipStatTarget.MoveSpeed, 1f)` each `FixedUpdate`.
- `Enemies/EnemyController.cs`: expose `Health` (public property) so `EnemyWeapon` can pass "who's
  firing" down to `ProjectileLauncher`.
- `Weapons/ProjectileLauncher.cs`: thread an optional `Health attackerHealth` through both `Fire`
  overloads → `ApplyPrimaryHit` → `DamageHealth`; add reflect check before applying damage to a
  player-tagged target.
- `Enemies/EnemyWeapon.cs`: pass `_controller.Health` as the attacker on every shot.
- 3 new `ChipData` assets under `Data/Weapons/`, registered in `ChipDatabase.asset`:
  - `Chip_MoveSpeed`: MoveSpeed, Multiplier, 0.10..0.25
  - `Chip_MaxHealth`: MaxHealth, Flat, 10..20
  - `Chip_Reflect`: MoveSpeed Multiplier -0.10..-0.05, **and** ReflectChance Flat 0.05..0.20
- Scene wiring (`Outer Space.unity`, `Sandbox/PlayerTest.unity`): clear `startingInventoryItems` on
  `PlayerInventory`; add `PlayerHealthChipBridge` to the player ship root.
- `Scripts/Editor/ChipSpawnerWindow.cs` (new): `Window/Manatite/Chip Spawner`, lists every `ChipData`
  under `Data/Weapons`, one button per chip calling `PlayerInventory.TryAddItem` on the
  `Player`-tagged object (Play mode only, mirrors `ShopSpawnerWindow`).

## Decisions & defaults
- MoveSpeed scales thrust *and* max speed uniformly (feels like an engine boost, not just a higher
  cap).
- MaxHealth only changes the cap; current HP clamps down on decrease but is **not** auto-healed on
  increase (permanent stat buff, not a heal item). Say if you want equip-time auto-heal instead.
- Reflect redirects damage **instantly** to the attacker (no visible return projectile/travel
  time) — standard "damage reflect" semantics. Say if you want an actual flying-back projectile.
- Reflect bypasses `ProcBus` entirely (direct `Health.TakeDamage` on the attacker) so it can never
  chain into another chip's proc, per your spec.
- Enemies never read `PlayerInventory` (existing architecture), so these three chips only ever
  affect the player.
- New chip icons left empty — no AI-generated art per project rule; assign later.
- "Start with 4 chips" removal only clears the scene-serialized array; the `startingInventoryItems`
  field itself stays on `PlayerInventory` in case a future starting-kit is wanted.

## Out of scope
- Balance Configurator entries for the new chips' tunable ranges.
- Reflect-specific VFX/SFX (reuses existing HitTaken/HitDealt cues, or plays neither on a successful
  reflect for the player's own hit).
- Auto-equipping from the spawner window — chips land in inventory, same as any other pickup.

## Addendum: consumable Repair chip
Added mid-implementation at the user's request:
- `ChipData.IsConsumable` (new bool flag). `Chip_Repair.asset`: `HealAmount` Flat 5..50, reuses the
  same shared icon as every other chip, `isConsumable: true`.
- `Health.Heal(int)` (new) — clamped like `TakeDamage`, reuses the `OnDamaged` event so listeners
  (the damage screen vignette) stay in sync on a heal too.
- `PlayerInventory.RemoveSlot(bool, int)` (new) — clears a slot outright, no swap partner.
- `InventoryCursor.OnSelectPerformed`: the first Select press on a consumable chip now short-circuits
  to a new `Consume` path (heal + `RemoveSlot`) instead of picking it up for a swap. No new input
  binding needed.
- Dropped into every planet's `LootTableData` at 50% (the other 3 new chips at 25%, matching the
  existing 4). Registered in `ChipDatabase.asset`. Toast/tooltip need zero extra work — both already
  key off any `ChipData` reaching `PlayerInventory.TryAddItem`/`OnItemAdded`.

## Acceptance checks
- Both scenes: player starts with empty equip + inventory chip slots.
- Equipping `Chip_MoveSpeed` raises ship top speed; `Chip_Reflect` lowers it.
- With `Chip_Reflect` equipped, some hits taken deal 0 damage to the player and instead damage the
  firing enemy.
- Equipping/unequipping `Chip_MaxHealth` changes `Health.Max` and clamps current HP correctly.
- `Window > Manatite > Chip Spawner` adds a chip to the player's inventory in Play mode.
