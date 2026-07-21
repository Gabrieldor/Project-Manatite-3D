# Manatite — Game Design Document

> Living document. Claude updates this when design decisions are made during development; sections marked *TBD* are open — filling them in is the fastest way to improve every future feature request.

## Vision
- **Genre**: 3D space shooter. *(TBD: on-rails / arena free-flight / horizontal corridor?)*
- **One-liner**: *TBD — what's the fantasy? e.g. "lone fighter pilot carving through alien waves."*
- **Platform target**: PC only. (A `Mobile_RPAsset` exists in `Assets/Settings` but is not an active target — leftover from template scaffolding.)
- **Session shape**: *TBD — endless waves / levels / boss runs?*
- **Playable field**: bounding box centered at `(275, 0, 137.5)`, size `9350 × 6600 × 10175` (X: -4400..4950, Y: -3300..3300, Z: -4950..5225), origin `(0,0,0)` at field center. Derived from the farthest "Planet_*" test obstacle on each axis in `Outer Space.unity` +10% margin. Marked in-scene by the `PlayfieldBounds` trigger volume. Camera `farClipPlane` set to 17000 to cover worst-case corner-to-corner viewing distance inside it.

## Core loop
*TBD: e.g. fight wave → collect drops → upgrade → harder wave.*

## Player ship
- Movement model: **6DoF free-flight, Newtonian** (thrust-based; no auto-stop). Gamepad:
  left stick = translate (forward/back + strafe), right stick = pitch/yaw (non-inverted, up=nose up),
  LT/RT = descend/ascend, LB/RB = roll, R3 = target lock. First-person **cockpit camera** rigidly mounted. Tunables in `ShipMovement_Default.asset`.
- Target lock: R3 acquires the `Lockable` object nearest screen center (within a viewport radius); while locked, the ship auto-turns its nose onto the target (camera stays rigidly mounted — no independent camera movement). R3 again releases. Tunables in `TargetLock_Default.asset`.
- Weapons: *TBD (primary + secondary? pickups? heat/ammo?)*
- Health/shield: *TBD*

## Enemies
*TBD: archetypes (fodder, shooter, tank, boss), spawn/wave rules.*

## Progression & scoring
*TBD*

## Art & audio direction
*TBD: stylized? realistic? palette?*

## Decisions log
| Date | Decision | Why |
|------|----------|-----|
| 2026-07-18 | ScriptableObject-driven tuning, pooled projectiles/enemies, new Input System only | Project architecture baseline (see CLAUDE.md) |
| 2026-07-20 | Player ship = 6DoF Newtonian free-flight, gamepad-first, first-person cockpit view | User control spec (see `specs/ship-6dof-fps.md`) |
| 2026-07-21 | Playable field fixed at 9350×6600×10175 box (see Vision) | Derived from farthest surviving mobility-test "planet" per axis +10% margin |
| 2026-07-21 | Platform target is PC only, not PC+Mobile | User correction; `Mobile_RPAsset` is unused template leftover |
| 2026-07-21 | Target lock (R3): ship auto-turns to face target, camera stays rigidly mounted | User's explicit choice over decoupled camera or HUD-only marker (see `specs/target-lock.md`) |
