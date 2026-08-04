# Main Menu Scene

## Goal
Player sees a title-screen: their ship floating centered on screen, an infinitely scrolling starfield skybox behind it, and a menu panel in the bottom third to start/continue/configure/quit.

## Reuse vs Create
- **Reuse**: `SKY_OuterSpace.mat` (Skybox/Cubemap shader over `skybox.png`, already imported as a 6-frame cubemap — this is why the background must be an actual Skybox, not a flat scrolling image: the source texture isn't a flat panorama). Reuse NavePrincipal's visual hull (root + `Model` child) from `Outer Space.unity`.
- **Create**: new scene, one prefab, two small scripts, Canvas UI.

## Deliverables
- `Assets/_Project/Prefabs/Player/NavePrincipal_Display.prefab` — trimmed copy of `NavePrincipal` (root transform/scale 6 + `Model` child only; no `ShipController`/`Rigidbody`/`TargetLock`/weapon/`Health` components, no `CameraMount`/`FirePoint`/`AimPoint`). Reusable anywhere a static display ship is needed.
- `Assets/_Project/Scenes/Main Menu.unity` — Camera (Skybox background), Directional Light, `NavePrincipal_Display` instance centered on screen, Canvas (Screen Space – Overlay) + EventSystem (Input System UI module).
- `Assets/_Project/Scripts/UI/MenuSkyboxScroller.cs` (`Manatite.UI`) — on `Start`, instantiates a runtime copy of an assigned base skybox material (never mutates the shared asset) and assigns it to `RenderSettings.skybox`; each frame increments the copy's `_Rotation` float to scroll right-to-left. `[SerializeField]` scroll speed (deg/sec) — presentation-only tunable, not promoted to a ScriptableObject.
- `Assets/_Project/Scripts/UI/MainMenu.cs` (`Manatite.UI`) — wires the 4 buttons: New Game → `SceneManager.LoadScene("Outer Space")`; Quit → `Application.Quit()` (mirrors `PlayerDeath`'s editor/build handling); Load Game / Settings → present but non-interactable (no save or settings system exists yet).
- Bottom-third-center panel: `Image` (built-in "Background" UI sprite — sliced, has the rounded/beveled look already, no new art needed), alpha ~0.6, containing a `Vertical Layout Group` with the 4 TMP buttons.
- Build Settings: add `Main Menu` (index 0) and `Outer Space` (index 1); drop the stale template `SampleScene` entry.

## Decisions & defaults
- Assumed background = real rotating Skybox (not a flat UI image) — forced by how `skybox.png` is imported (cubemap, not panorama).
- Assumed ship is static (no idle rotation/bob) — say if you want motion.
- Assumed Load Game / Settings buttons exist but are disabled (`interactable = false`) since neither system exists yet — say if you'd rather they log a "coming soon" message or be hidden entirely.
- Assumed New Game target is the existing `Outer Space.unity` gameplay scene.
- Assumed Main Menu becomes Build Settings scene index 0 (game boots to the menu).

## Out of scope
Save/Load system, Settings screen contents, menu music/SFX, transition animations, gamepad navigation of the menu (mouse/click only for now).

## Acceptance checks
1. Entering Play on `Main Menu.unity`: ship visible centered, background visibly and continuously scrolls right→left with no seam/pop.
2. Panel sits in the bottom third, horizontally centered, semi-transparent with rounded corners; 4 buttons visible and laid out vertically.
3. Clicking New Game loads `Outer Space` and gameplay starts normally; Quit exits (stops Play Mode in-editor); Load Game/Settings are visibly disabled.
4. No console errors on scene load or during Play.
