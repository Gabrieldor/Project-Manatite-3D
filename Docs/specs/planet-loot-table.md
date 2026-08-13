# Planet Loot Table

Clearing a planet's encounter has a chance to drop chips straight into the player's inventory.

## Deliverables

- `Assets/_Project/Scripts/Data/LootTableData.cs` (`Manatite.Data`) — ScriptableObject holding
  a `LootEntry[]` (`ChipData item`, `float dropChance` 0–1), `CreateAssetMenu` under
  `Manatite/Loot Table Data`.
- `PlanetData.cs`: new optional `[SerializeField] private LootTableData lootTable;` +
  `public LootTableData LootTable => lootTable;`.
- `PlayerInventory.cs`: new `public bool TryAddItem(ChipData data)` — finds the first empty
  inventory slot (equip slots untouched), wraps `data` in a `ChipInstance`, fires
  `OnInventoryChanged`, returns false (no-op) if the inventory is full.
- `PlanetSpawner.cs`: caches a `PlayerInventory` reference once (`FindGameObjectWithTag` +
  `GetComponentInChildren`, mirroring the existing player lookup in `FacePlayerAtNearestPlanet`).
  In the existing `zone.OnEncounterCleared` handler, after marking the planet beaten, rolls
  `type.LootTable` (if assigned) and calls `TryAddItem` per successful roll.

## Decisions & defaults

- Assumed **independent per-entry rolls** (each entry rolls its own `dropChance` separately,
  so 0, 1, or several items can drop from one planet) rather than a single weighted pick —
  matches the prompt's "list of items and its chance to drop" phrasing and mirrors the
  existing `EnemySpawnData` entry-array style.
- Assumed loot rolls once, right when the encounter clears (same moment the ring/beaten flag
  fires), not on subsequent visits.
- Assumed `LootTableData` is optional per `PlanetData` (null/empty = no drops), so existing
  planet assets keep working unchanged.
- Assumed a full inventory silently drops the item (no ground-pickup entity, no queue) — no
  loot-drop-in-world system exists yet, and the prompt only asked for "send into inventory."
- Chips only — `ChipData` is the only item type in the project right now.

## Out of scope

- Weighted single-pick rolls, stacking/currency items, ground-space pickup objects, loot
  notification UI/toast.
- Save/load of pending loot (a planet already marked `beaten` on load does not re-roll).

## Acceptance checks

1. A `PlanetData` with a `LootTableData` assigned: clearing its encounter adds a chip to the
   first empty inventory slot for each entry that rolled successfully.
2. A `PlanetData` with no `LootTableData` assigned clears with no errors and no drops.
3. Clearing an encounter with a full inventory produces no exceptions/console errors.
4. No new console errors/warnings after `/verify`.
