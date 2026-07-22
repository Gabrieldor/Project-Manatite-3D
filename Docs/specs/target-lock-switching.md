# Spec: Target-switching while locked (right-stick flick)

## Goal
While locked onto a target, flicking the right stick toward another lockable object nearby on screen switches the lock to it, letting the pilot re-target without breaking lock and re-acquiring from scratch.

## Decisions & defaults
- **Reuse the existing `Look` action** (right stick) instead of adding a new input action/binding. While locked, `ShipController` already zeroes manual pitch/yaw from this stick, so its raw deflection is otherwise unused — perfect to repurpose. No `ShipControls.inputactions` changes.
- **Edge-triggered "flick" detection** (hysteresis, no new timer): a switch attempt fires the instant stick magnitude crosses `SwitchArmThreshold` (default 0.6); it then requires the stick to fall back to `SwitchReleaseThreshold` (default 0.3) or below before it can fire again. Prevents holding the stick over from repeatedly cycling targets and needs no new action/cooldown.
- **Ignored while rolling**: if `RollModifier` (LB) is held, stick deflection is treated as zero for switching (mirrors `ShipController`'s existing `rolling` gate), so a roll adjustment never misfires a switch.
- **Direction/distance = viewport space relative to the current target**, not screen center: for each other enabled `Lockable`, compute its viewport-space offset from the *currently locked target's* viewport position (not (0.5,0.5) — that's only used for initial acquisition). Candidate must be: in front of the camera, within `MaxLockRange`, within `SwitchSearchRadius` (viewport-space distance, default 0.4 — wider than `LockAcquireRadius` since off-center candidates are expected here), and within `SwitchConeHalfAngle` (default 45°) of the stick's direction. Among qualifiers, nearest wins. No qualifiers → no-op, no error.
- Switching only re-points `_target`; it does not touch rotation logic (`FixedUpdate` already re-aims at whatever `_target` currently is every step).
- **Occlusion check**: a candidate only qualifies if some part of it has a clear line of sight from the camera — sampled via its renderer's world bounds (8 corners + center), each tested with a `Physics.Raycast` (ignoring triggers) that stops just short of the sample point. Blocked only if something *other than the candidate itself* is hit first; any one clear sample point is enough. No renderer on the candidate → skip the check (assume visible). Applied to both the switch-target search and the original R3 acquire-best pass, so a fully hidden object can't be locked onto either way.

## Deliverables
- `Scripts/Data/TargetLockData.cs` — add `SwitchArmThreshold`, `SwitchReleaseThreshold`, `SwitchSearchRadius`, `SwitchConeHalfAngle`.
- `Scripts/Player/TargetLock.cs` — resolve `Look` and `RollModifier` actions in `Awake`/enable alongside `Lock`; `Update` runs flick-hysteresis + `TrySwitchTarget(stickDir)` while locked; `IsVisible(Lockable)` occlusion check shared by acquire + switch.
- `Scripts/Combat/Lockable.cs` — caches its `Renderer` and exposes `TryGetWorldBounds` for the occlusion sampling.
- `Data/TargetLock_Default.asset` — picks up new fields via C# defaults (no manual edit needed).

## Out of scope
HUD/reticle feedback on switch, cycling order/memory (e.g. cycling back through previously-skipped targets), left-stick or D-pad alternative binding, audio/visual "switch" feedback.

## Acceptance checks
1. Lock planet A; another `Lockable` sits to the right of it on screen within range — flick right stick right → lock switches to it.
2. Flick toward empty space or a too-far candidate → no change, no console errors.
3. Hold LB (roll) and move the stick → no switch fires; roll still works.
4. Holding the stick fully deflected doesn't rapid-fire switches; must release toward center first.
5. Planet B sits fully behind planet A (no visible part from camera) → flicking toward B does not switch to it; if B is partially visible (an edge peeking out), it does.
