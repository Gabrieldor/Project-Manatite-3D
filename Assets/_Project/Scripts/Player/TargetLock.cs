using Manatite.Combat;
using Manatite.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manatite.Player
{
    /// <summary>
    /// R3 target lock: acquires the <see cref="Lockable"/> nearest screen center
    /// (within a viewport radius) and, while locked, kinematically rotates the ship's
    /// nose onto it every physics step. <see cref="ShipController"/> yields pitch/yaw
    /// input to this component while <see cref="IsLocked"/> is true; while locked, the
    /// right stick's otherwise-unused deflection instead flicks the lock to a nearby
    /// candidate in that screen-space direction.
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
        private InputAction _look;
        private InputAction _rollModifier;
        private Lockable _target;
        private bool _switchReady = true;

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
                _look = _shipMap.FindAction("Look", throwIfNotFound: true);
                _rollModifier = _shipMap.FindAction("RollModifier", throwIfNotFound: true);
            }
            else
            {
                Debug.LogError($"{nameof(TargetLock)} on '{name}' has no {nameof(InputActionAsset)} assigned.", this);
            }
        }

        private void OnEnable()
        {
            _lock?.Enable();
            _look?.Enable();
            _rollModifier?.Enable();
        }

        private void OnDisable()
        {
            _lock?.Disable();
            _look?.Disable();
            _rollModifier?.Disable();
            SetTarget(null);
            _switchReady = true;
        }

        private void Update()
        {
            if (_lock != null && _lock.WasPressedThisFrame())
            {
                if (IsLocked) SetTarget(null);
                else AcquireBest();
            }

            if (IsLocked && _look != null) UpdateSwitching();
        }

        /// <summary>
        /// Single point where <see cref="_target"/> changes, so the death subscription
        /// always tracks whichever <see cref="Lockable"/> is currently locked.
        /// </summary>
        private void SetTarget(Lockable target)
        {
            if (_target == target) return;

            if (_target != null && _target.TryGetComponent(out Health oldHealth))
                oldHealth.OnDied -= HandleTargetDied;

            _target = target;

            if (_target != null && _target.TryGetComponent(out Health newHealth))
                newHealth.OnDied += HandleTargetDied;
        }

        /// <summary>Releases the lock when the current target dies, if enabled in <see cref="TargetLockData"/>.</summary>
        private void HandleTargetDied()
        {
            if (data != null && data.UnlockOnTargetDeath) SetTarget(null);
        }

        // While locked, ShipController zeroes manual pitch/yaw from the right stick, so its raw
        // deflection is otherwise unused here — reused as a target-switch flick instead of adding
        // a new input binding.
        private void UpdateSwitching()
        {
            bool rolling = _rollModifier != null && _rollModifier.IsPressed();
            Vector2 stick = rolling ? Vector2.zero : _look.ReadValue<Vector2>();
            float magnitude = stick.magnitude;

            if (_switchReady && data != null && magnitude >= data.SwitchArmThreshold)
            {
                _switchReady = false;
                TrySwitchTarget(stick / magnitude);
            }
            else if (data == null || magnitude <= data.SwitchReleaseThreshold)
            {
                _switchReady = true;
            }
        }

        private void TrySwitchTarget(Vector2 stickDir)
        {
            if (data == null || lockCamera == null || _target == null) return;

            Vector3 originViewport = lockCamera.WorldToViewportPoint(_target.transform.position);
            float cosHalfAngle = Mathf.Cos(data.SwitchConeHalfAngle * Mathf.Deg2Rad);
            var candidates = Lockable.All;

            Lockable best = null;
            float bestDist = data.SwitchSearchRadius;

            for (int i = 0; i < candidates.Count; i++)
            {
                Lockable candidate = candidates[i];
                if (candidate == _target) continue;

                Transform t = candidate.transform;
                Vector3 viewport = lockCamera.WorldToViewportPoint(t.position);
                if (viewport.z <= 0f) continue; // behind camera

                if (Vector3.Distance(transform.position, t.position) > data.MaxLockRange) continue;

                Vector2 delta = new Vector2(viewport.x - originViewport.x, viewport.y - originViewport.y);
                float dist = delta.magnitude;
                if (dist > bestDist || dist < 0.0001f) continue;

                if (Vector2.Dot(delta / dist, stickDir) < cosHalfAngle) continue;

                if (!IsVisible(candidate)) continue;

                best = candidate;
                bestDist = dist;
            }

            if (best != null) SetTarget(best);
        }

        /// <summary>
        /// True if any part of the candidate's render bounds has a clear line of sight from the
        /// camera — so switching skips a target fully hidden behind another object. Samples the
        /// bounds' 8 corners + center rather than just the pivot, since a planet-sized object can
        /// have its center occluded while an edge is still visible.
        /// </summary>
        private bool IsVisible(Lockable candidate)
        {
            if (!candidate.TryGetWorldBounds(out Bounds bounds)) return true; // no renderer to sample

            Vector3 origin = lockCamera.transform.position;
            Transform candidateRoot = candidate.transform;

            for (int i = 0; i < 9; i++)
            {
                Vector3 point = i < 8
                    ? new Vector3(
                        (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (i & 4) == 0 ? bounds.min.z : bounds.max.z)
                    : bounds.center;

                Vector3 toPoint = point - origin;
                float distance = toPoint.magnitude;
                if (distance < 0.001f) return true;

                bool blocked = Physics.Raycast(
                    origin, toPoint / distance, out RaycastHit hit,
                    distance - 0.05f, ~0, QueryTriggerInteraction.Ignore);

                if (!blocked || hit.transform == candidateRoot || hit.transform.IsChildOf(candidateRoot))
                    return true; // clear path, or only the candidate's own geometry is in the way
            }

            return false;
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

                if (!IsVisible(candidate)) continue;

                best = candidate;
                bestScreenDist = screenDist;
            }

            SetTarget(best);
        }

        private void FixedUpdate()
        {
            if (!IsLocked || data == null) return;

            Transform t = _target.transform;

            if (Vector3.Distance(transform.position, t.position) > data.MaxLockRange)
            {
                SetTarget(null);
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
