# Crosshair

**Goal**: player always sees a center reticle to aim with, and it visibly reacts (color change + a
marker that tracks the target on screen) once `TargetLock` acquires a lock.

**Reuses**: `Manatite.Player.TargetLock` (`IsLocked`, `CurrentTarget`), cockpit camera already
referenced by `TargetLock`. **Creates**: new `Scripts/UI/` folder (first UI script in the project),
first `Canvas`/`EventSystem` in the scene.

## Deliverables
- `Assets/_Project/Scripts/UI/Crosshair.cs` (`Manatite.UI`) — drives both elements from `TargetLock` state.
- Scene (`Outer Space.unity`): `Canvas` "HUD" (Screen Space - Overlay) + `EventSystem`, with two children:
  - `CenterReticle` (`Image`) — always visible, anchored dead-center.
  - `LockIndicator` (`Image`, `RectTransform`) — hidden until locked, repositioned every frame to the
    locked target's screen position.

## Decisions & defaults
- Assumed built-in Unity UI sprites (`Knob`/`Background`) for both shapes — no new texture/art asset
  generated or imported (project rule: no AI-generated media). Say if you have real crosshair art to
  drop in instead; swapping the `Image.sprite` later is a one-field change.
- Assumed reactive feedback = `CenterReticle` color swap (white → accent, e.g. red) on lock, not a
  shape/sprite swap.
- Assumed Screen Space - Overlay canvas since no diegetic/in-world HUD exists yet.
- Assumed `Crosshair` takes its own `[SerializeField] Camera` (same cockpit camera already wired into
  `TargetLock`) rather than exposing `TargetLock`'s private one.
- Single lock indicator only (matches the current single-target lock system — no multi-target list).

## Out of scope
Custom crosshair artwork, weapon lead/convergence indicator, multiple simultaneous target markers,
juice (pulse/scale tween, sound on lock).

## Acceptance checks
1. Entering play mode shows a static center reticle at all times, screen-position-independent of ship motion.
2. Pressing Lock on a valid target: reticle changes color and `LockIndicator` appears over the target.
3. `LockIndicator` visually tracks the target as ship/target move, every frame, while locked.
4. Releasing the lock (button press or target death) hides `LockIndicator` and reticle reverts to the unlocked color.
