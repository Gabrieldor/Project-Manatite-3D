using Manatite.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.Player
{
    /// <summary>
    /// Gamepad-driven 6DOF ship flight. Left stick translates (forward/back + strafe),
    /// right stick aims the nose (pitch/yaw, non-inverted), triggers change altitude.
    /// Holding LB switches the right stick to roll instead of aiming. Physics is
    /// Newtonian by default (see <see cref="ShipMovementData"/> damping). All
    /// forces/torques are ship-local and applied in FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShipController : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("All movement tunables (thrust, torque, damping, max speeds) live on this asset.")]
        [SerializeField] private ShipMovementData data;
        [Tooltip("The ShipControls.inputactions asset containing the ship action map.")]
        [SerializeField] private InputActionAsset controls;

        [Header("Setup")]
        [Tooltip("Name of the action map (inside the controls asset) to drive this ship.")]
        [SerializeField] private string actionMapName = "Ship";
        [Tooltip("Ships ignore gravity in space. Leave off unless you want a pull.")]
        [SerializeField] private bool useGravity = false;
        [Tooltip("Rigidbody interpolation — smooths the rigidly-mounted cockpit camera.")]
        [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;
        [Tooltip("Optional. While its target is locked, this ship yields pitch/yaw torque to the auto-aim controller.")]
        [SerializeField] private TargetLock targetLock;

        private Rigidbody _rb;
        private InputActionMap _shipMap;
        private InputAction _move;   // Vector2: x=strafe, y=forward
        private InputAction _look;   // Vector2: x=yaw, y=pitch
        private InputAction _elevate; // Axis: +up / -down
        private InputAction _roll;    // Axis: +right / -left
        private InputAction _rollModifier; // Button: while held, right stick rolls instead of looking

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = useGravity;
            _rb.interpolation = interpolation;

            if (data != null)
            {
                // Built-in damping is uniform; we damp per-axis manually in FixedUpdate.
                _rb.linearDamping = 0f;
                _rb.angularDamping = 0f;
                _rb.maxAngularVelocity = data.MaxAngularSpeed > 0f
                    ? data.MaxAngularSpeed * Mathf.Deg2Rad
                    : 1000f;
            }
            else
            {
                Debug.LogError($"{nameof(ShipController)} on '{name}' has no {nameof(ShipMovementData)} assigned.", this);
            }

            if (controls != null)
            {
                _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
                _move = _shipMap.FindAction("Move", throwIfNotFound: true);
                _look = _shipMap.FindAction("Look", throwIfNotFound: true);
                _elevate = _shipMap.FindAction("Elevate", throwIfNotFound: true);
                _roll = _shipMap.FindAction("Roll", throwIfNotFound: true);
                _rollModifier = _shipMap.FindAction("RollModifier", throwIfNotFound: true);
            }
            else
            {
                Debug.LogError($"{nameof(ShipController)} on '{name}' has no {nameof(InputActionAsset)} assigned.", this);
            }
        }

        private void OnEnable() => _shipMap?.Enable();
        private void OnDisable() => _shipMap?.Disable();

        private void FixedUpdate()
        {
            if (data == null || _shipMap == null) return;

            Vector2 move = _move.ReadValue<Vector2>();
            bool rolling = _rollModifier.IsPressed();
            Vector2 look = rolling ? Vector2.zero : _look.ReadValue<Vector2>();
            float elevate = _elevate.ReadValue<float>();
            float roll = _roll.ReadValue<float>();

            // --- Translation (ship-local) ---
            Vector3 thrust = new Vector3(
                move.x * data.StrafeThrust,   // strafe left/right
                elevate * data.VerticalThrust, // RT up / LT down
                move.y * data.ForwardThrust);  // forward/back
            _rb.AddRelativeForce(thrust, ForceMode.Acceleration);

            if (data.MaxSpeed > 0f)
            {
                Vector3 v = _rb.linearVelocity;
                if (v.sqrMagnitude > data.MaxSpeed * data.MaxSpeed)
                    _rb.linearVelocity = v.normalized * data.MaxSpeed;
            }

            // --- Rotation (ship-local torque) ---
            // Unity: +torque about local X pitches nose DOWN, so negate look.y for
            // "stick up = nose up". +Y yaws nose right; -Z rolls right.
            // While LB (RollModifier) is held, the right stick rolls instead of aiming.
            // While a target is locked, TargetLock owns pitch/yaw instead of manual look input.
            bool autoAimActive = targetLock != null && targetLock.IsLocked;
            Vector3 torque = new Vector3(
                autoAimActive ? 0f : -look.y * data.PitchTorque,
                autoAimActive ? 0f : look.x * data.YawTorque,
                -roll * data.RollTorque);
            _rb.AddRelativeTorque(torque * Mathf.Deg2Rad, ForceMode.Acceleration);

            // --- Per-axis damping (ship-local, read live so it's tunable in play) ---
            float dt = Time.fixedDeltaTime;
            Vector3 localVel = transform.InverseTransformDirection(_rb.linearVelocity);
            localVel.x *= Mathf.Exp(-data.StrafeDamping * dt);
            localVel.y *= Mathf.Exp(-data.VerticalDamping * dt);
            localVel.z *= Mathf.Exp(-data.ForwardDamping * dt);
            _rb.linearVelocity = transform.TransformDirection(localVel);

            Vector3 localAng = transform.InverseTransformDirection(_rb.angularVelocity);
            localAng.x *= Mathf.Exp(-data.PitchDamping * dt);
            localAng.y *= Mathf.Exp(-data.YawDamping * dt);
            localAng.z *= Mathf.Exp(-data.RollDamping * dt);
            _rb.angularVelocity = transform.TransformDirection(localAng);
        }
    }
}
