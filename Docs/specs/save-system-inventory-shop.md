# Save system: inventory + shop marker persistence

## Goal
A player who saves and reloads keeps every chip they've equipped, picked up, or forged, and doesn't lose an unvisited chip-forging shop marker on a cleared planet.

## Reused
`SaveData`/`RunSaveController`/`SaveFile`/`PendingLoad` (existing save pipeline), `PlanetSpawner.SpawnPlanet`/`SpawnFromSave` (planet restore path), `PlayerInventory` slot arrays, `ChipMerger`/`ChipData.CreateMerged`.

## Deliverables
- `Data/ChipDatabaseData.cs` (`Manatite.Data`, new SO, `Manatite/Chip Database` menu): flat `ChipData[] chips`, `ChipData FindById(string id)` (matches by `.name`). One asset (`Assets/_Project/Data/ChipDatabase.asset`) listing every real chip asset — the runtime-safe equivalent of `PlanetSpawner.FindTypeById`, since `AssetDatabase` isn't available in a build.
- `Core/SaveSystem/SaveData.cs`: add `ChipSaveData` (`isEmpty`, `isMerged`, `chipId`, `displayName`, `description`, `ChipModifierSaveData[] modifiers`, `float[] rolledValues`), `ChipModifierSaveData` (`statTarget`, `valueKind`), `InventorySaveData` (`List<ChipSaveData> equipSlots`, `List<ChipSaveData> inventorySlots`). `SaveData` gains `InventorySaveData inventory`. `PlanetSaveData` gains `bool hasShop`.
- `Player/PlayerInventory.cs`: new `[SerializeField] ChipDatabaseData chipDatabase`; `InventorySaveData CaptureState()` and `void RestoreState(InventorySaveData data)`. Skip `startingInventoryItems` population in `Awake` when `PendingLoad.Data != null`.
- `Environment/PlanetSpawner.cs`: extract a `SpawnShopMarker(Transform planetRoot, float radius)` helper from the existing `RollShop` body; live clear keeps rolling `ShopChance` then calls it, `SpawnPlanet`/`SpawnFromSave` call it directly when `beaten && hasShop` (no re-roll). `SpawnPlanet` gains a trailing `bool hasShop = false` param.
- `Core/SaveSystem/RunSaveController.cs`: `[SerializeField] PlayerInventory playerInventory`; `Save()` calls `playerInventory.CaptureState()` and, per planet, sets `hasShop` via `state.GetComponentInChildren<ShopMarker>(true) != null`; `Start()` calls `playerInventory.RestoreState(...)` alongside the existing player-health restore.
- Scene wiring: assign `RunSaveController.playerInventory` and `PlayerInventory.chipDatabase` in `Outer Space.unity`.

## Decisions & defaults
- Assumed real (non-merged) chips save by asset name (`chipId` = `ChipData.name`) resolved via the new `ChipDatabaseData`, same pattern as `PlanetSaveData.typeId`. If the id isn't found (renamed/deleted asset) the slot loads empty rather than throwing.
- Assumed merged chips (no `.asset`, from `ChipMerger`) save their full definition inline (`displayName`, `description`, `modifiers`, `rolledValues`) via `ChipData.CreateMerged` on load — `isMerged=true` skips the `chipId` lookup entirely.
- Assumed merged-chip icon is dropped on reload (`null`) rather than reconstructed — `JsonUtility` can't portably serialize a `Sprite` reference, and the merge feature already treats its icon as a placeholder. Not a regression: today a merged chip's icon vanishes anyway the instant the run ends.
- Assumed both slot lists always serialize a fixed length (4 / 36) with an explicit `isEmpty` flag per entry, not null elements — `JsonUtility` doesn't round-trip null list elements.
- Assumed old saves (pre-inventory) load fine with an empty inventory and no shop markers — additive JSON fields default gracefully, no version bump/migration needed.
- Assumed shop-marker existence is inferred at save time by scanning each planet instance's children for a live `ShopMarker` (no new field on `PlanetState`), mirroring how `RunSaveController.Save()` already scans the scene rather than tracking a shadow copy of state.
- Assumed a shop marker is never "consumed" (matches current behavior — flying through it just reopens the panel), so restoring it on load is a single unconditional spawn, no visited/used flag needed.

## Out of scope
- Currency/economy (none exists yet — merging is free).
- Migrating/versioning old save files beyond graceful defaulting.
- Player ship position/velocity persistence (unchanged — still respawns facing the nearest planet).

## Acceptance checks
1. Equip a starting chip, pick up a loot chip, save, reload the scene via Load Game: both chips are in the same slots with the same rolled values.
2. Merge two chips in the shop, save, reload: the merged chip survives in its slot with correct combined modifiers/rolled values (icon may be blank).
3. Clear a planet that rolls a shop marker, save before visiting it, reload: the marker exists above the planet again.
4. Clear a planet that does *not* roll a shop marker, save, reload: no marker spawns.
5. Load an old pre-inventory save file: loads without errors, inventory empty, no console errors.
