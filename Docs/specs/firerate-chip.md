# Firerate Chip (first Chip/Modifier item)

## Goal
Player has a fire-rate-boosting chip sitting in their inventory from the start of a run; moving it into an Equip slot via the inventory UI makes the primary weapon fire faster. This is the first instance of a general "chip" item system.

## Reuse vs. create
- **Reuse**: `WeaponData`/`Weapon` (fire timing), `InventoryMenu`/`InventoryCursor` (grid UI, cursor nav), Balance Configurator (reflection-driven numeric tuning), existing SO conventions (`Manatite.Data`, `[CreateAssetMenu]`, private field + PascalCase property).
- **Create**: `ChipData` SO class, one chip asset, a runtime inventory/equip data model (doesn't exist yet — current inventory UI is purely visual, no backing slot array), equip/unequip interaction, icon rendering per slot, a fire-rate readout in `Weapon`.

## General chip framework (not just fire rate)
Chips are a data-driven modifier system, of which the fire-rate chip is the first instance:
- `ChipValueKind { Flat, Multiplier }` — each chip is **one kind or the other**, not both. Flat chips roll a value in **[-5, +5]**; multiplier chips roll a value in **[-75%, +100%]** (stored as -0.75..1.00). `ChipData.OnValidate` clamps `minValue`/`maxValue` to whichever envelope matches its `ValueKind`, so the Balance Configurator's sliders can never exceed the legal range.
- `ChipStatTarget` enum names which stat a chip modifies (`FireRate` is the only entry today; a future damage/health/speed chip adds one enum entry + one read-site, no new abstraction layer).
- Every chip **rolls its own random value once, at instantiation** (`Random.Range(minValue, maxValue)`), not at equip time — so two copies of the same `ChipData` are different in practice. The rolled value lives on a per-instance `ChipInstance` (plain class: `ChipData Data`, `float RolledValue`), never mutated onto the shared SO asset (matches the project rule against mutating live SO assets).
- Combining multiple equipped chips on the same target: `result = (baseValue + sum of Flat rolls) * (1 + sum of Multiplier rolls)`.
- **Int stats (damage, health, etc.) always round down**: `Mathf.FloorToInt(result)`. Float stats (fire rate) use the raw float, no rounding.

## Deliverables
- `Assets/_Project/Scripts/Data/ChipData.cs` (`Manatite.Data`) — SO: `displayName`, `icon` (Sprite), `valueKind` (`ChipValueKind`), `statTarget` (`ChipStatTarget`), `minValue`/`maxValue` (float, clamped per kind in `OnValidate`).
- `Assets/_Project/Scripts/Data/ChipValueKind.cs`, `ChipStatTarget.cs` — the two enums above.
- `Assets/_Project/Scripts/Core/ChipInstance.cs` (`Manatite.Core`) — plain class holding `ChipData Data` + `float RolledValue`; rolled once in its constructor.
- `Assets/_Project/Data/Weapons/Chip_FireRate.asset` — `ChipData` instance: `valueKind = Flat`, `statTarget = FireRate`, `minValue = -5`, `maxValue = 5`, icon = imported `fireratechip.png` (retyped to Sprite (2D and UI)).
- `Assets/_Project/Scripts/Player/PlayerInventory.cs` (`Manatite.Player`) — MonoBehaviour on the player ship: 6 equip + 36 inventory slots (`ChipInstance` refs, nullable), `[SerializeField] ChipData[] startingInventoryItems` (each rolled into a `ChipInstance` and placed into inventory slots 0..n at `Awake`), `TrySwap(fromPanel/Index, toPanel/Index)`, `event Action OnInventoryChanged`, `float GetModifiedFloat(ChipStatTarget, float baseValue)`, `int GetModifiedInt(ChipStatTarget, int baseValue)` (floors the float result).
- `InventoryCursor.cs` — add pick-up/place: `A` on an occupied slot with nothing held picks it up (dims that slot's icon); `A` again on a target slot swaps it in (swaps back if target occupied); calls `PlayerInventory.TrySwap`.
- New small `InventorySlotIcon` view — one per slot, instantiated at runtime as a child `Image` under each of the 42 existing slot transforms (avoids hand-editing 42 scene objects), shown/hidden and re-sprited on `OnInventoryChanged`.
- `Weapon.cs` — cache a `PlayerInventory` reference in `Awake`; `Fire()` uses `inventory.GetModifiedFloat(ChipStatTarget.FireRate, data.FireRate)` instead of `data.FireRate` alone.
- Balance Configurator — add `"Chips"` tab, `Type[] ChipDataTypes = { typeof(ChipData) }`; free via existing reflection-driven numeric-field drawing (`minValue`/`maxValue` show as sliders), no new configurator code beyond the tab wire-up.

## Decisions & defaults
- Confirmed: one `ValueKind` per chip (not both flat+% on the same chip).
- Assumed the chip starts in **inventory slot 0** (first slot), not pre-equipped — matches "we'll initialize directly into the inventory."
- Assumed multiple equipped chips targeting the same stat stack (flats sum, multipliers sum) per the formula above.
- Assumed no restriction on which of the 6 equip slots a chip can go in (say if chips should later have "slot types").
- Assumed icons are runtime-instantiated child `Image`s per slot rather than hand-placed in the scene, to avoid 42 manual edits — purely an implementation detail, same visual result.
- Assumed equip/unequip does **not** persist to the save file yet (memory: no chip/inventory state exists in `SaveData` today; schema stays additive-safe for later). Equipped-chip state, and each chip's rolled value, reset on scene reload for now.
- Assumed **not** removed from the game if unequipped — just moves back to an inventory slot (nothing is consumed/destroyed).

## Out of scope
- Pickup-from-world, drop, or delete of chips.
- Save/load persistence of inventory/equip state (including rolled values).
- Any `ChipStatTarget` other than `FireRate` (enum is ready to extend, no other read-sites wired yet).
- Equip-slot type restrictions/categories.

## Acceptance checks
- Starting a run shows the firerate chip icon in inventory slot 0 with some random value baked in (visible via inspector/debug, e.g. `-3` or `+4`); equip slots are empty.
- Moving the chip into any equip slot and closing the inventory: primary weapon's effective fire interval measurably shortens/lengthens by that rolled amount (verify via `Weapon`'s cooldown or shot cadence in Play mode).
- Moving the chip back out of the equip slot returns fire rate to baseline.
- `Chip_FireRate.asset`'s `minValue`/`maxValue` are visible and editable (clamped to -5..+5 since it's a Flat chip) under a "Chips" tab in the Balance Configurator.
- A damage or health value modified by a chip (once one exists) always rounds down, never up — e.g. a +50% multiplier chip on 3 damage yields 4 (floor of 4.5), not 5.
