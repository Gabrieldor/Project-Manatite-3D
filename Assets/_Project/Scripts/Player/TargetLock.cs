using Manatite.Combat;
using Manatite.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.Player
{
    /// <summary>
    /// R3 target lock: acquires the <see cref="Lockable"/> nearest screen center
    /// (within a viewport radius) and, while locked, auto-torques the ship's nose
    /// onto it via a PD controller. <see cref="ShipController"/> yields pitch/yaw
    /// input to this component while <see cref="IsLocked"/> is true.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TargetLock : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TargetLockData data;
        [Tooltip("The ShipControls.inputactions asset containing the ship action map.")]
        [SerializeField] private InputActionAsset controls;
        [Tooltip("Camera used to judge screen-center proximity (the cockpit camera).")]
        [SerializeField] private Camera lockCamera;

        [Header("Setup")]
        [SerializeField] private string actionMapName = "Ship";

        private Rigidbody _rb;
        private InputActionMap _shipMap;
        private InputAction _lock;
        private Lockable _target;

        public bool IsLocked => _target != null;
        public Transform CurrentTarget => _target != null ? _target.transform : null;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            if (data == null)
                Debug.LogError($"{nameof(TargetLock)} on '{name}' has no {nameof(TargetLockData)} assigned.", this);
            if (lockCamera == null)
                Debug.LogError($"{nameof(TargetLock)} on '{name}' has no {nameof(Camera)} assigned.", this);

            if (controls != null)
            {
                _shipMap = controls.FindActionMap(actionMapName, throwIfNotFound: true);
                _lock = _shipMap.FindAction("Lock", throwIfNotFound: true);
            }
            else
            {
                Debug.LogError($"{nameof(TargetLock)} on '{name}' has no {nameof(InputActionAsset)} assigned.", this);
            }
        }

        private void OnEnable() => _lock?.Enable();

        private void OnDisable()
        {
            _lock?.Disable();
            _target = null;
        }

        private void Update()
        {
            if (_lock != null && _lock.WasPressedThisFrame())
            {
                if (IsLocked) _target = null;
                else AcquireBest();
            }
        }

        private void AcquireBest()
        {
            if (data == null || lockCamera == null) return;

            Lockable best = null;
            float bestScreenDist = data.LockAcquireRadius;
            var candidates = Lockable.All;

            for (int i = 0; i < candidates.Count; i++)
            {
                Lockable candidate = candidates[i];
                Transform t = candidate.transform;

                Vector3 viewport = lockCamera.WorldToViewportPoint(t.position);
                if (viewport.z <= 0f) continue; // behind camera

                if (Vector3.Distance(transform.position, t.position) > data.MaxLockRange) continue;

                float dx = viewport.x - 0.5f;
                float dy = viewport.y - 0.5f;
                float screenDist = Mathf.Sqrt(dx * dx + dy * dy);
                if (screenDist > bestScreenDist) continue;

                best = candidate;
                bestScreenDist = screenDist;
            }

            _target = best;
        }

        private void FixedUpdate()
        {
            if (!IsLocked || data == null) return;

            Transform t = _target.transform;

            if (Vector3.Distance(transform.position, t.position) > data.MaxLockRange)
            {
                _target = null;
                return;
            }

            // Kinematic rotate-towards (not torque): recomputed from the actual current bearing each
            // step, so it has no steady-state lag under a moving bearing (e.g. while strafing) the way
            // a torque+damping PD controller would.
            Vector3 dirWorld = (t.position - transform.position).normalized;
            Quaternion desiredRotation = Quaternion.LookRotation(dirWorld, transform.up);
            Quaternion newRotation = Quaternion.RotateTowards(
                transform.rotation, desiredRotation, data.AutoAimTurnRate * Time.fixedDeltaTime);
            _rb.MoveRotation(newRotation);

            // Roll stays player-controlled via torque (ShipController); clear only pitch/yaw spin so
            // leftover angular momentum doesn't fight next step's kinematic rotation.
            Vector3 localAngVel = transform.InverseTransformDirection(_rb.angularVelocity);
            localAngVel.x = 0f;
            localAngVel.y = 0f;
            _rb.angularVelocity = transform.TransformDirection(localAngVel);
        }
    }
}
