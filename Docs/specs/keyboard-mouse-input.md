# Keyboard & Mouse Input

**Goal**: the player can fly and fight with keyboard+mouse alone — mouse aims the nose (cockpit camera is rigidly mounted, so no separate look), left click fires, right click toggles target lock, WASD translates, Q/E rolls, Space/Shift moves up/down, Esc pauses, I opens inventory, Enter selects.

**Context**: `ShipControls.inputactions` already defines every action needed (`Ship` map: Move/Look/Elevate/Roll/Lock/Fire/Pause/Inventory/Cancel; `InventoryNav` map: Navigate/Select) — currently 100% gamepad-bound, no keyboard/mouse bindings exist anywhere, and no control schemes are defined (every binding on an action is always live regardless of device — that's how the existing multi-binding actions, e.g. `InventoryNav/Navigate` via dpad + leftStick, already work). `ShipController`/`Weapon`/`TargetLock`/`PauseMenu`/`InventoryMenu`/`InventoryCursor`/`ShopCursor` all read these actions by name only — no C# changes needed, this is pure data.

## Deliverables
- `Assets/_Project/Input/ShipControls.inputactions` — add keyboard/mouse bindings to existing actions:
  - **Move**: new WASD 2D-Vector composite (W=fwd, S=back, A=strafe-left, D=strafe-right)
  - **Look**: `<Mouse>/delta` with a binding-level `scaleVector2(x=0.02,y=0.02)` processor (non-inverted — mouse up = nose up, matching the GDD's stated gamepad convention)
  - **Elevate**: new 1D-Axis composite (Space=+1/up, Left Shift=−1/down)
  - **Roll**: new 1D-Axis composite (Q=−1/left, E=+1/right — matches the existing rightStick/x polarity)
  - **Fire**: `<Mouse>/leftButton`
  - **Lock**: `<Mouse>/rightButton`
  - **Pause**: `<Keyboard>/escape`
  - **Cancel**: `<Keyboard>/escape`
  - **Inventory**: `<Keyboard>/i`
  - **Select** (`InventoryNav` map, already drives both `InventoryCursor` and `ShopCursor`): `<Keyboard>/enter` + `<Keyboard>/numpadEnter`

## Decisions & defaults
- Assumed right-click = the existing **Lock** action (target-lock toggle, same as gamepad R3). Say if you meant something else.
- Mouse sensitivity is a fixed binding-level processor constant, editable later directly in the Input Actions editor (no code) — not wired to a ScriptableObject/Balance-Configurator slider, since input mapping is a different concern from gameplay tuning.
- Esc is wired to both **Pause** and **Cancel** (back out of Settings/Shop), matching its usual dual role.
- No control scheme / device split added — new bindings just sit alongside the gamepad ones on the same actions, so both input methods keep working interchangeably at all times, same as today.

## Out of scope
New actions, rebind UI, per-device control schemes, a tunable sensitivity setting, `RollModifier` keyboard binding.

## Acceptance checks
- Keyboard/mouse only, Play mode: WASD thrusts fwd/back/strafe, mouse movement pitches/yaws the nose, Q/E rolls, Space/Shift ascend/descend.
- Left click fires; right click toggles target lock (crosshair reacts).
- Esc opens/closes Pause Menu and backs out of Settings/Shop panels; I opens/closes Inventory; Enter selects the hovered inventory/shop slot.
- Gamepad controls still work unchanged (regression check — only additive bindings were made).
