# Spec: Ship 6DOF flight + cockpit FPS camera

## Goal
Player pilots the ship in first-person (cockpit) view with full 6DOF free-flight on a gamepad.

## Control mapping (as requested by user)
- **Left stick** — translate: up=forward, down=back, left/right=strafe (ship-local X/Z)
- **Right stick** — rotate nose: up=pitch nose **up** (non-inverted), left/right=yaw
- **RT** — ascend (ship-local +up); **LT** — descend (ship-local -up)
- **LB/RB** — roll left/right *(assumed; the 6th DOF — user didn't specify)*
- **Camera** — cockpit, rigidly attached to ship (rotates with the ship)

## Deliverables
- `Assets/_Project/Scripts/Manatite.Runtime.asmdef` (references `Unity.InputSystem`)
- `Assets/_Project/Input/ShipControls.inputactions` — "Ship" map:
  Move (Value/Vector2, `<Gamepad>/leftStick`), Look (Value/Vector2, `<Gamepad>/rightStick`),
  Elevate (Value/Axis, 1D: +`rightTrigger` / -`leftTrigger`), Roll (Value/Axis, 1D: +`rightShoulder` / -`leftShoulder`)
- `Scripts/Data/ShipMovementData.cs` (SO) + `Data/ShipMovement_Default.asset` — all speeds + damping tunables
- `Scripts/Player/ShipController.cs` (`Manatite.Player`) — reads actions, applies 6DOF via Rigidbody in FixedUpdate
- Scene wiring: Rigidbody on `NavePrincipal` (no gravity, Interpolate), `CameraMount` child at cockpit, Main Camera parented to it

## Decisions & defaults
- Rigidbody physics, gravity off; **damped arcade feel** (release stick → eases to stop). Damping exposed in SO; 0 = Newtonian drift.
- Rotation is rate-based & crisp (direct `MoveRotation`); translation is acceleration-limited toward target local velocity.
- Non-inverted pitch (stick up = nose up) per request.
- Gamepad-first; no KB/mouse scheme this pass.

## Out of scope
- Weapons, speed HUD, boost/afterburner, KB+mouse controls, collision response tuning.

## Acceptance checks
1. Play mode + gamepad: left stick moves ship along its own axes; right stick aims the nose (up=up); RT/LT change altitude; LB/RB roll.
2. View is from the cockpit and rotates rigidly with the ship.
3. No console errors; all movement in FixedUpdate; no per-frame GetComponent/allocations.
