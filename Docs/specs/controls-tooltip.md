# First-Time Controls Tooltip

## Goal
The first time a player ever enters gameplay (`Outer Space`), before they can act, they see a controls guide (gamepad or keyboard/mouse) instead of jumping straight into flight blind. Every later scene entry skips it and plays normally.

## Reuse vs Create
- **Reuse**: `Outer Space`'s existing Pause Menu backpanel/button visual style (`background.png` panel sprite, `button` sprite + hand-painted resting `Image.color`/material — this project's buttons don't rely on `Selectable`'s color-tint transitions at runtime, see Decisions below), `TypeLightSans SDF` font, `PlayerPrefs`-backed first-run-flag pattern (same technique `AudioManager`/`SettingsMenu` use), `PauseMenu`'s freeze mechanism (`Time.timeScale = 0`, `AudioListener.pause`, disabling the `Ship` action map) and its `ShipControls.inputactions` reference.
- **Create**: `ControlsTooltip.cs` (`Manatite.UI`, generic show/hide/tab-switch), `GameplayControlsIntro.cs` (`Manatite.UI`, the gameplay-scene driver: shows-on-start + pauses/resumes), a new `ControlsIntroPanel` hierarchy under `PauseMenuCanvas` in `Outer Space.unity`.

## Deliverables
- `Assets/_Project/Scripts/UI/ControlsTooltip.cs`: `Show(Action onContinue)` opens the panel, selects the Gamepad tab, and selects `gamepadTabButton` in the `EventSystem`. `LateUpdate` swaps the active guide the moment the `EventSystem`'s selected object becomes one of the two tab buttons — **no Submit/click required**, matching how a tab strip should read under gamepad/keyboard navigation. `OnContinue()` sets the `PlayerPrefs` flag (`"Manatite.ControlsTooltipSeen"`), hides the panel, and invokes the stored callback. Static `HasBeenSeen` reads the same flag.
- `Assets/_Project/Scripts/UI/GameplayControlsIntro.cs`: on `Start()`, if `!ControlsTooltip.HasBeenSeen`, freezes gameplay (`Time.timeScale = 0`, `AudioListener.pause = true`, disables the `Ship` action map) and calls `controlsTooltip.Show(OnDismissed)`; `OnDismissed` reverses all three. No-ops entirely once the flag is set.
- `Outer Space.unity`, under `PauseMenuCanvas`:
  - `ControlsIntroPanel` (inactive by default) — full-screen dim backdrop + centered `Panel` (900×620, same `background.png` sprite as the Pause Menu panel), holding a `Content` container inset 36px from the panel's edges (keeps buttons clear of the panel's beveled frame art — see Decisions).
    - `Tabs`: top 10% of `Content`, inset to the center 33% of its width, `Button_Gamepad` (default selected) / `Button_Keyboard`.
    - `GuideArea`: bottom 90% of `Content` — `GamepadGuideText`/`KeyboardGuideText` (TMP, one active at a time) plus `Button_Continue`.
  - `ControlsIntro` — a plain always-active GameObject holding `GameplayControlsIntro`, wired to the same `ShipControls.inputactions` asset/`"Ship"` map `PauseMenu` uses, and to the `ControlsTooltip` on `ControlsIntroPanel`.
  - The 3 buttons use **Explicit** navigation (`Button_Gamepad`/`Button_Keyboard` → Down → `Button_Continue`; `Button_Continue` → Up → `Button_Gamepad`) instead of Unity's default Automatic mode — see Decisions.
- `MainMenu.cs`: reverted to its original, unmodified `OnNewGame()` — New Game loads `Outer Space` directly again; the tooltip no longer lives in the Main Menu.
- `Docs/BACKLOG.md`: Done entries for the feature and this follow-up fix pass.

## Guide content (authored from the real bindings, not generated at runtime)
Read directly from `Assets/_Project/Input/ShipControls.inputactions`' `Ship` action map. Uses a plain hyphen (`-`) rather than an em dash, since the project font's glyph coverage for `—` wasn't confirmed.

**Gamepad tab**: Move-Left Stick, Look-Right Stick, Ascend/Descend-Right Trigger/Left Trigger, Roll-Left Bumper+Right Stick, Fire-A or Right Bumper, Lock Target-Right Stick Click, Pause-Start, Inventory-Select

**Keyboard/Mouse tab**: Move-WASD, Look-Mouse, Ascend/Descend-Space/Left Shift, Roll-Q/E, Fire-Left Click, Lock Target-Right Click, Pause-Esc, Inventory-I

## Decisions & defaults
- **Relocated from Main Menu to gameplay scene** (user correction after the first pass): originally shown on the first New Game click, in `Main Menu.unity`; moved to show as the first thing in `Outer Space.unity` instead, freezing gameplay (same mechanism `PauseMenu` already uses) until dismissed.
- **Button resting color must be copied from `Image.color`, not `Button.colors.normalColor`** (bug found in first pass): this project's buttons don't actually use Unity's `Selectable` ColorTint transition at runtime — their look is a hand-painted `Image.color` (`RGBA(0.278, 0.906, 0.757, 0.88)`, sprite `button`, `Image.Type.Simple`) that Selectable's transition never overrides in practice, matching the existing code comment in `MainMenu.cs` about "hand-painted Image alpha, not Selectable's color-tint transitions." Confirmed identical on `Button_Quit` in both `Main Menu.unity` and `Outer Space.unity`.
- **Explicit navigation, not Automatic** (bug found in first pass): Unity's default Automatic navigation mode scans every active `Selectable` in the scene by screen distance regardless of visual occlusion — in the original Main Menu placement this caused `Down` from a tab to land on a hidden background button before reaching Continue. Wiring exact neighbors avoids this regardless of what else is active nearby.
- **36px inset `Content` container** (bug found in first pass): the tab row and Continue button were originally anchored flush against the panel's own edges, overlapping its beveled corner-cut frame art.
- Assumed the flag is a global `PlayerPrefs` bool, not part of `SaveData` — independent of any save slot, matching how `AudioManager` already keeps its volumes outside the save system.
- Assumed dismissal is only via the explicit `Button_Continue` (no Cancel/Esc shortcut) — nothing else is interactive while gameplay is frozen.
- Assumed the guide only covers the `Ship` action map (flight/combat), not `InventoryNav` (menu navigation).

## Out of scope
- No runtime rebinding UI, no per-action icon glyphs (text only, TypeLightSans, matching every other menu).
- No mid-run "view controls again" entry point (could reuse `ControlsTooltip.Show` from the Pause Menu later — not requested now).

## Acceptance checks
- Fresh `PlayerPrefs` (or a cleared `"Manatite.ControlsTooltipSeen"` key): entering `Outer Space` immediately freezes gameplay and shows the panel with the Gamepad tab active/selected — no click required to reach this state.
- Moving selection onto the Keyboard/Mouse tab (no Submit) swaps the visible guide text.
- Pressing Down from either tab reaches Continue in exactly one press.
- Clicking/submitting Continue hides the panel, resumes gameplay (`timeScale`, audio, `Ship` map all restored), and sets the `PlayerPrefs` flag.
- With the flag already set, entering `Outer Space` plays normally — panel never appears, gameplay never freezes.
- Clicking New Game in the Main Menu goes straight to `Outer Space` as it always did — no interception there.
