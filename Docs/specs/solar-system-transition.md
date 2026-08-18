# Solar system transition

## Goal
When the player clears every planet in the current field and a fresh field spawns around them (`PlanetSpawner.RegenerateField`), the screen fades to black, shows "Arriving at a new Solar System", then fades back in on the new field — hiding the pop-in/destroy swap instead of showing it directly.

## Reuse vs create
- **Reuse**: `PlanetSpawner.RegenerateField()` (destroy+respawn logic, unchanged), `TypeLightSans SDF` font, Screen Space Overlay Canvas pattern already used by `PauseMenuCanvas`.
- **Create**: `Manatite.UI.SolarSystemTransitionUI` (new MonoBehaviour), a new Canvas in the `Outer Space` scene.

## Deliverables
- `Assets/_Project/Scripts/UI/SolarSystemTransitionUI.cs` — drives a `CanvasGroup` alpha via coroutine: fade in (black + message appear) → hold → invoke a callback (the actual field swap happens here, fully hidden) → fade out. Public entry point: `Play(System.Action onBlackout)`.
- `Assets/_Project/Scripts/Environment/PlanetSpawner.cs` — add `[SerializeField] private SolarSystemTransitionUI transitionUI;`; `RegenerateField()` now calls `transitionUI.Play(DoRegenerate)` (falls back to calling `DoRegenerate()` directly if unassigned) instead of swapping the field immediately.
- Scene `Outer Space.unity` — new `SolarSystemTransitionCanvas` (Canvas + CanvasScaler + CanvasGroup + `SolarSystemTransitionUI`), full-stretch black `Image` child, centered `TextMeshProUGUI` child reading "Arriving at a new Solar System" (TypeLightSans SDF). Wired into `PlanetSpawner.transitionUI`.

## Decisions & defaults
- Assumed **fixed timing**: 0.6s fade-to-black, 1.2s hold, 0.6s fade-out — no skip input (say if you want it skippable).
- Assumed **gameplay keeps running** under the blackout (ship controls/physics not paused) — planets respawn around the player's current position regardless, so nothing depends on the player holding still. Simpler than threading a pause through `ShipController`/`Weapon` input maps; say if you want input frozen too.
- Canvas `sortingOrder = 50` (every existing canvas sits at 0) so it always renders above HUD/Pause/Inventory/Shop.
- Single fixed message string, no per-biome variation, no audio sting.

## Out of scope
- No skip/hurry-up input.
- No distinct messaging per planet-type roster or difficulty tier.
- No audio cue.
- No pausing ship input during the transition.

## Acceptance checks
1. Clearing the last planet in a field fades to black, shows the message, then fades back in — the new planet batch only appears once the screen was fully black.
2. Works on a second consecutive regeneration in the same run (not one-shot).
3. No console errors; `PlanetSpawner.transitionUI` is wired (non-null) in the scene.
