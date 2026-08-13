# Chip Forging Shop — UI validation pass

## Goal
Clearing a planet can spawn a big yellow "!" marker above it; flying into it opens a
Shop panel (same visual style as other menus) with two hoverable merge slots and a
(currently inert) Merge button, where each slot is filled by picking a chip from the
player's inventory. This pass only validates the UI/interaction shell — merging itself
does nothing yet.

## Reuse vs. new
- Reuse: `PauseMenu`/`InventoryMenu` open/close + Time.timeScale/Ship-map-disable
  pattern, `ChipTooltipFormatter`, `InventoryIconView` (icon rendering, gets a null-guard
  so it can be pointed at a single grid), backpanel/button visual style (`ee7935ad..`
  background, `9f2d91b0..` button sprites already in the scene), `LootTableData`/
  `PlanetData`/`PlanetSpawner` loot-on-clear pipeline.
- New: `ShopMarker` (world-space "!"), `ShopPanel`, `ShopCursor` (merge slots + inventory
  picker grid nav — structurally like `InventoryCursor` but a different shape/flow, so
  it's a new small script rather than a retrofit of the shipped, verified one), 2 new
  additive-only merge slots on `PlayerInventory`.

## Deliverables
- `Data/LootTableData.cs`: add `[Range(0,1)] [SerializeField] float shopChance` + `ShopChance`
  getter, rolled independently on clear exactly like a chip entry's `dropChance`.
- `Player/PlayerInventory.cs`: additive-only — 2 merge slots (`GetMergeSlot`,
  `IsMergeSlotEmpty`, `SwapMergeWithInventory(mergeIndex, inventoryIndex)`, symmetric
  swap so placing/replacing/removing are all the same call). No existing method signature
  changes.
- `Environment/ShopMarker.cs`: trigger collider (Player tag) → `ShopPanel.Open()`;
  billboards to face the camera in `LateUpdate`.
- `Environment/PlanetSpawner.cs`: `shopMarkerPrefab` field; on `OnEncounterCleared`, if
  `type.LootTable.SpawnsShop`, instantiate it as a child of the planet root (not `Visual`,
  so it doesn't inherit the planet's random scale) at `Vector3.up * (radius + offset)`.
- `UI/ShopPanel.cs`: open/close, pause convention identical to `InventoryMenu`; Cancel
  closes the picker first if open, else closes the panel (mirrors `PauseMenu`/
  `SettingsMenu` nesting). `OnMergePressed()` stub, wired to the Merge button, empty body.
- `UI/ShopCursor.cs`: gamepad nav (reuses `InventoryNav` action map) across 2 merge slots;
  Select on a merge slot reveals the inventory grid (hidden by default) and focus moves
  into it; Select on an inventory slot calls `SwapMergeWithInventory` and hides the grid
  again. Fires `OnHoverChipChanged` for whichever grid is focused, for the tooltip.
- `UI/InventoryIconView.cs`: null-guard `BuildIcons` so a second instance can target only
  one grid (used for the shop's picker grid).
- Scene (`Outer Space.unity`), via Unity MCP: `ShopPanel` GameObject under the HUD/menu
  canvas — backdrop, backpanel, top-1/3 row (2 slots + Merge button), bottom-2/3 inventory
  grid (hidden by default), tooltip text block — built from the same sprites/prefab
  pattern as the Pause/Inventory menus. `ShopMarker` prefab (empty root + 3D `!` TextMeshPro
  child, yellow, + `SphereCollider(isTrigger)`).
- Pause menu: temporary "Spawn Shop" debug button that instantiates `ShopMarker` a fixed
  distance in front of the player ship, for testing without clearing a planet.
- `Docs/BACKLOG.md`: one-line "Done" entry once verified.

## Decisions & defaults
- Assumed the marker auto-opens the Shop on trigger-enter (like `EnemySpawnZone`), not a
  separate "press to interact" prompt — no `Interact` input action exists yet and this
  avoids adding one for a UI-validation pass. Say if you want a button-press prompt instead.
- Assumed the marker is a 3D TextMeshPro "!" (yellow, billboarded), not custom geometry —
  no AI-generated assets per project rule, and this is the simplest built-in option.
- Assumed picking a chip for a merge slot always shows the full 36-slot grid (same layout
  as the real inventory), not a filtered/compact list.
- Assumed "spawn the shop" (test button) means spawn the world marker in front of the ship
  (exercises marker → trigger → panel end-to-end), not open the panel directly.
- Merge slots are real `PlayerInventory` state (not panel-local), so a chip placed there is
  actually removed from the inventory grid — ready for a real Merge implementation later.

## Out of scope
- Merge button doing anything (no recipe/output logic).
- Drop-chance roll, VFX/animation/audio on the marker or panel open.
- Removing the temporary Pause Menu test button (left in until a real spawn path exists).

## Acceptance checks
- Clearing a planet whose loot table has `spawnsShop = true` shows a yellow "!" above it;
  flying into it pauses the game and opens the Shop panel.
- Pause Menu's "Spawn Shop" button spawns the marker in front of the ship; flying into it
  opens the same panel without needing a planet clear.
- Hovering either merge slot (gamepad nav) shows its tooltip when occupied, blank when
  empty; selecting an empty or full slot reveals the inventory grid.
- Selecting a chip in the revealed grid moves it into the chosen merge slot and the grid
  hides again; the chip is gone from its old inventory slot.
- Merge button is present and clickable but does nothing.
