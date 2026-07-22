# Target Lock — Unlock on Death

## Goal
When the locked-onto enemy dies, the lock releases immediately instead of
staying pointed at a dead/pooled object — full manual control returns right
away. Toggleable per `TargetLockData` asset.

## Deliverables
- `Scripts/Data/TargetLockData.cs` — `unlockOnTargetDeath` bool (default true).
- `Scripts/Player/TargetLock.cs`:
  - `SetTarget(Lockable)` helper centralizes every place `_target` is assigned
    (acquire, switch, clear) and subscribes/unsubscribes to the target's
    `Health.OnDied` (null-safe — not every `Lockable` has one, e.g. planets).
  - `HandleTargetDied()` — fired by that subscription, calls `SetTarget(null)`
    only if `data.UnlockOnTargetDeath` is true.

## Decisions & defaults
- No auto-retarget to a nearby replacement (tried, user preferred plain
  unlock) — dying drops the lock, same as pressing the Lock button, unless
  toggled off.

## Out of scope
Retargeting to another candidate on death, retargeting on range-exceeded
(already just unlocks, unchanged).

## Acceptance checks
1. Lock an enemy, kill it → lock releases, ship regains full manual pitch/yaw
   control immediately.
2. Killing an enemy you're not locked onto has no effect on the current lock.
3. No console errors across acquire → death → unlock.
