# Pause Menu

## Goal
Mid-flight, pressing Start freezes the game and shows a menu (Save Game / Load Game / Settings / Quit) styled like the Main Menu; pressing Start again resumes.

## Reuse vs Create
- **Reuse**: `Ship` action map's existing `Pause` action (bound to `<Gamepad>/start`, unused by any script today — this task is what wires it). `Textures/background.png` (panel) and `Textures/button.png` (buttons) art, `TypeLightSans SDF` font, disabled-placeholder button convention, all carried over from the Main Menu scene.
- **Create**: one script, one Canvas + 4 buttons in `Outer Space.unity`.

## Deliverables
- `Assets/_Project/Scripts/UI/PauseMenu.cs` (`Manatite.UI`) — finds the `Pause` action on the `Ship` map (same `FindActionMap`/`FindAction` pattern as `ShipController`/`Weapon`); on press, toggles pause state:
  - `Time.timeScale` 0 ↔ 1 (freezes all `FixedUpdate` gameplay — ship, enemies, projectiles — for free, no changes needed to those scripts) and `AudioListener.pause`.
  - Shows/hides a child `Panel` GameObject (menu starts hidden; the script itself lives on the always-active Canvas so it keeps receiving the `Pause` action while hidden).
  - Disables every other action on the `Ship` map (`Move`/`Look`/`Elevate`/`Roll`/`Fire`/`Lock`/`RollModifier`) while paused so held/queued input (e.g. Fire) can't slip through the one frame before physics fully freezes, then re-enables them on resume.
  - On open, sets the `EventSystem`'s selected object to the Quit button (the only interactable one) so gamepad-only navigation works immediately — no mouse cursor exists in this gamepad-first game.
  - `OnQuitToMainMenu()`: resets `Time.timeScale`/`AudioListener.pause` to unpaused, then `SceneManager.LoadScene("Main Menu")`.
- `Outer Space.unity` scene addition: `PauseMenuCanvas` (Screen Space – Overlay, `CanvasScaler` Scale-With-Screen-Size 1920×1080 — matches `MainMenuCanvas`, unlike the HUD canvas's Constant-Pixel-Size) as a sibling of `HUD`, holding `PauseMenu.cs` + a child `Panel` (same `background.png` sliced Image + `VerticalLayoutGroup` spacing/padding as `MainMenuCanvas/MenuPanel`) with 4 buttons built the same way as the Main Menu's (`button.png` Image, `TypeLightSans SDF` label):
  - Save Game — `interactable = false` (no save system yet)
  - Load Game — `interactable = false` (no save system yet)
  - Settings — `interactable = false` (no settings screen yet)
  - Quit — `interactable = true`, wired to `PauseMenu.OnQuitToMainMenu`
  - `Panel` starts inactive.

## Decisions & defaults
- Assumed **Quit returns to the Main Menu scene** (not `Application.Quit()`) — a pause-menu "Quit" conventionally means "leave this run," and the app-quit case is already covered by the Main Menu's own Quit button. Say if you want it to exit the app instead.
- Assumed **Start toggles** pause open/closed (no separate Resume button) — matches "the pause trigger" being a single action, and keeps the button list exactly the 4 named.
- Assumed Save Game is disabled for the same reason Load Game already is in the Main Menu: no save system exists. If you actually have one coming next, say so and I'll wire it instead of stubbing it.
- Assumed no visual dimming/blur behind the panel — the semi-transparent panel + frozen scene reads as "paused" on its own, matching the Main Menu's flat-panel style.

## Out of scope
Actual save/load system, Settings screen contents, a pause menu inside the Main Menu scene itself (N/A there), confirm-before-quit dialog, menu-open/close transition or sound.

## Acceptance checks
1. In Play mode on `Outer Space.unity`, pressing gamepad Start freezes the ship/enemies/projectiles mid-motion and shows the panel with Quit already selectable via d-pad/stick.
2. Pressing Start again hides the panel and gameplay resumes exactly where it froze (no input lost, no stray shot fired from the held trigger).
3. Save Game / Load Game / Settings render visibly disabled, same look as their Main Menu counterparts.
4. Quit loads `Main Menu.unity` with `Time.timeScale` back at 1 and no console errors.
