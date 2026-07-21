# Spec: Target lock (R3)

## Goal
While flying, if a lockable object sits near the center of the screen, pressing R3 locks it: the ship's nose auto-turns to track and hold on that target, freeing the pilot to focus on positioning rather than manual aim.

## Decisions & defaults
- **Camera is untouched** (stays rigidly mounted to the cockpit). Locking instead makes the **ship auto-turn** to face the target — user's explicit choice over camera-decoupling or HUD-only marker.
- Lock candidates = any enabled `Lockable` marker component in the scene, via a static self-registering list (no `FindObjectsOfType` in a hot path). Future enemies/pickups become lockable by adding this component.
- "Within a radius of screen center" = viewport-space distance from (0.5, 0.5) ≤ `LockAcquireRadius` (default 0.12), object in front of the camera, within `MaxLockRange` (default 4000). Among qualifiers, closest-to-center wins.
- **R3 toggles**: not locked → acquire best candidate (no-op if none qualify). Locked → release.
- Auto-unlock if the target is destroyed/disabled or exits `MaxLockRange`. No angle-based break (arcade-friendly).
- While locked, right-stick pitch/yaw is ignored by `ShipController` — auto-aim owns pitch/yaw via a kinematic rotate-towards (`Quaternion.RotateTowards` + `Rigidbody.MoveRotation`, capped at `AutoAimTurnRate` deg/s), not torque+damping. A PD torque controller lags under a moving bearing (e.g. while strafing, since it only reacts to accumulated error); rotate-towards recomputes from the actual current bearing every physics step, so it tracks translation-induced bearing changes with no steady-state offset. Left stick, elevate, and roll stay fully player-controlled.
- No HUD/reticle this pass — no UI system exists yet.

## Deliverables
- `Assets/_Project/Input/ShipControls.inputactions` — add `Lock` (Button) bound to `<Gamepad>/rightStickPress` in the "Ship" map.
- `Scripts/Combat/Lockable.cs` (`Manatite.Combat`) — marker component + static registry.
- `Scripts/Data/TargetLockData.cs` (`Manatite.Data`) SO — `LockAcquireRadius`, `MaxLockRange`, `AutoAimTorque`, `AutoAimDamping`.
- `Data/TargetLock_Default.asset` — instance of the above.
- `Scripts/Player/TargetLock.cs` (`Manatite.Player`) — acquisition/release logic + PD auto-aim torque in `FixedUpdate`; exposes `IsLocked` / `CurrentTarget` for future weapons/HUD use.
- `ShipController.cs` — optional `TargetLock` reference; skips look-input pitch/yaw torque while locked.
- Scene: `TargetLock` added to `NavePrincipal`, wired up; `Lockable` added to existing `Planet_*` test objects in `Outer Space.unity` so it's testable now.

## Out of scope
HUD lock indicator, weapon auto-aim/homing, multi-target cycling, line-of-sight breaking, enemy reactions to being locked.

## Acceptance checks
1. Center a `Lockable` planet on screen, press R3 → ship nose eases onto and holds it; right stick no longer directly pitches/yaws.
2. Press R3 again → releases; right stick regains direct control.
3. Nothing near center → R3 is a no-op, no errors.
4. No console errors; no per-frame allocations/GetComponent in the lock/aim path.
