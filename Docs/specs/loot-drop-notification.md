# Loot Drop Notification

When a chip drops into the player's inventory (planet-cleared loot), a toast pops up on the
HUD briefly showing what was picked up.

## Deliverables

- `Assets/_Project/Scripts/Player/PlayerInventory.cs`: new `public event Action<ChipInstance> OnItemAdded`,
  fired from `TryAddItem` right after a slot is filled (carries the rolled instance, not just
  the base `ChipData`, so the toast could show rolled values later if ever needed).
- `Assets/_Project/Scripts/UI/ChipTooltipFormatter.cs` (`Manatite.UI`) — static helper
  extracted from `TooltipView`'s existing flat/percent-line formatting (colored
  `+N StatName` / `+N% StatName` strings per modifier) so both `TooltipView` and the new
  notification can build identical stat text from a `ChipInstance` without duplicating the
  formatting logic. `TooltipView` is refactored to call it; its own behavior is unchanged.
- `Assets/_Project/Scripts/UI/LootNotificationView.cs` (`Manatite.UI`) — subscribes to
  `PlayerInventory.OnItemAdded`, queues drops (`Queue<ChipInstance>`), shows one at a time:
  activate panel, set icon/name/flat-stats/percent-stats/description, hold
  `displayDuration`, deactivate, pop next.
- Scene (`Outer Space.unity`, `HUD` canvas, instance 58412): new inactive child
  `LootNotificationPanel` —
  - Root: sliced `background.png` panel (same sprite as `TooltipPanel`/`Slot_00`), anchored
    top-right corner of the HUD, tall enough for the extra stat rows (~360×150px).
  - Left third: `ItemSlot` child (sliced `background.png`, 0.55 gray tint — matches
    `Slot_00`) containing an `Icon` `Image` (chip sprite, ~0.64 fill, matches
    `InventoryIconView`'s inset convention).
  - Right two-thirds, top to bottom: `NameText` (TMP, bold — the title), then a stat row
    (`FlatStatText` / `PercentStatText` side by side, same two-column layout as
    `TooltipPanel`), then `DescriptionText` below.
  - `LootNotificationView` component added to the `HUD` GameObject, wired to `NavePrincipal`'s
    `PlayerInventory` and the new panel's children.

## Decisions & defaults

- Confirmed: the toast shows the **full stat breakdown** (flat/percent lines, same coloring
  as `TooltipView`) plus description, with the chip name added as a title above them.
- Assumed **top-right corner** placement, ~360×150px, clear of the crosshair and of the
  inventory panels (which only show while the inventory is open, when loot can't be dropping).
- Assumed **2.5s hold, no fade animation** (instant show/hide) — matches the project's existing
  "simplest performant option" precedent (see `PlanetClearedRing`).
- Assumed **sequential queueing**: if a planet drops several chips at once, toasts show one
  after another rather than stacking simultaneously.
- Reuses the existing `background.png` sliced sprite for both the outer panel and the item
  slot — this is already the project's one common backpanel texture (used by both
  `TooltipPanel` and every inventory `Slot_NN`).

## Out of scope

- Fade/slide animation, sound cue, stacking multiple toasts on screen at once.
- Persisting/replaying missed toasts across scene loads.

## Acceptance checks

1. Clearing a planet whose loot table successfully rolls a chip shows a toast with that
   chip's icon, name, and description, then it disappears after ~2.5s.
2. Multiple simultaneous drops show as separate toasts, one after another, not overlapping.
3. No new console errors/warnings.
