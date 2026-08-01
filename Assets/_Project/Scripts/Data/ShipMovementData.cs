using System;
using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Tunable 6DOF flight parameters for a ship. All movement numbers live here
    /// (data-driven) so they can be balanced without touching code. Thrust/torque
    /// are applied as accelerations (mass-independent). Damping defaults to 0 for a
    /// Newtonian feel — the ship keeps drifting/spinning until countered.
    /// </summary>
    [CreateAssetMenu(fileName = "ShipMovement_", menuName = "Manatite/Ship Movement Data", order = 0)]
    public sealed class ShipMovementData : ScriptableObject
    {
        /// <summary>Raised whenever a field is edited (e.g. by the Balance Configurator), so
        /// live instances can re-apply values that are otherwise only read once at spawn.</summary>
        public event Action Changed;
        private void OnValidate() => Changed?.Invoke();


        [Header("Size")]
        [Tooltip("Uniform scale applied to the ship root at spawn. Hull collider and fire " +
                 "point are defined in root-local space, so they scale with this automatically.")]
        [SerializeField] private float scale = 1f;

        [Header("Translation (accel, m/s^2)")]
        [SerializeField] private float forwardThrust = 20f;
        [SerializeField] private float strafeThrust = 15f;
        [SerializeField] private float verticalThrust = 15f;
        [Tooltip("0 = unlimited. Otherwise caps linear speed (m/s).")]
        [SerializeField] private float maxSpeed = 40f;

        [Header("Rotation (accel, deg/s^2 equivalent torque)")]
        [SerializeField] private float pitchTorque = 120f;
        [SerializeField] private float yawTorque = 120f;
        [SerializeField] private float rollTorque = 160f;
        [Tooltip("0 = unlimited. Otherwise caps angular speed (deg/s).")]
        [SerializeField] private float maxAngularSpeed = 120f;

        // Per-degree damping, applied manually in ship-local space (Rigidbody's built-in
        // damping is a single scalar and can't be split per axis). 0 = no damping / drift;
        // higher = velocity on that axis bleeds off faster (per-second exponential rate).
        [Header("Translation damping (0 = drift)")]
        [Range(0f, 10f)] [Tooltip("Damping along ship forward/back (Z).")]
        [SerializeField] private float forwardDamping = 0f;
        [Range(0f, 10f)] [Tooltip("Damping along ship strafe left/right (X).")]
        [SerializeField] private float strafeDamping = 0f;
        [Range(0f, 10f)] [Tooltip("Damping along ship up/down (Y).")]
        [SerializeField] private float verticalDamping = 0f;

        [Header("Rotation damping (0 = drift)")]
        [Range(0f, 10f)] [Tooltip("Damping on pitch (nose up/down).")]
        [SerializeField] private float pitchDamping = 0f;
        [Range(0f, 10f)] [Tooltip("Damping on yaw (nose left/right).")]
        [SerializeField] private float yawDamping = 0f;
        [Range(0f, 10f)] [Tooltip("Damping on roll.")]
        [SerializeField] private float rollDamping = 0f;

        public float Scale => scale;

        public float ForwardThrust => forwardThrust;
        public float StrafeThrust => strafeThrust;
        public float VerticalThrust => verticalThrust;
        public float MaxSpeed => maxSpeed;

        public float PitchTorque => pitchTorque;
        public float YawTorque => yawTorque;
        public float RollTorque => rollTorque;
        public float MaxAngularSpeed => maxAngularSpeed;

        public float ForwardDamping => forwardDamping;
        public float StrafeDamping => strafeDamping;
        public float VerticalDamping => verticalDamping;
        public float PitchDamping => pitchDamping;
        public float YawDamping => yawDamping;
        public float RollDamping => rollDamping;
    }
}
