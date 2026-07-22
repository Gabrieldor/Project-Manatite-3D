using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Tunables for the R3 target-lock/auto-aim system. See
    /// <see cref="Manatite.Player.TargetLock"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "TargetLock_", menuName = "Manatite/Target Lock Data", order = 1)]
    public sealed class TargetLockData : ScriptableObject
    {
        [Header("Acquisition")]
        [Tooltip("Viewport-space radius from screen center (0-1) a Lockable must fall within to be acquired.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float lockAcquireRadius = 0.12f;
        [Tooltip("Max distance (m) a target can be acquired or stay locked from.")]
        [SerializeField] private float maxLockRange = 4000f;

        [Header("Auto-aim")]
        [Tooltip("Max degrees/second the nose rotates toward the target. Kinematic rotate-towards, " +
                 "not torque — tracks bearing changes (e.g. from strafing) immediately instead of lagging behind them.")]
        [SerializeField] private float autoAimTurnRate = 150f;
        [Tooltip("If the locked target dies, release the lock automatically (same as pressing the Lock button).")]
        [SerializeField] private bool unlockOnTargetDeath = true;

        [Header("Target switching")]
        [Tooltip("Right-stick deflection magnitude (0-1) that triggers a switch attempt. Edge-triggered — " +
                 "must fall back to the release threshold before it can fire again.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float switchArmThreshold = 0.6f;
        [Tooltip("Stick must return to this magnitude or below before another switch can be triggered.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float switchReleaseThreshold = 0.3f;
        [Tooltip("Viewport-space radius from the current target within which a candidate can be switched to.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float switchSearchRadius = 0.4f;
        [Tooltip("Half-angle (degrees) of the direction cone a candidate must fall within, relative to stick direction.")]
        [Range(5f, 90f)]
        [SerializeField] private float switchConeHalfAngle = 45f;

        public float LockAcquireRadius => lockAcquireRadius;
        public float MaxLockRange => maxLockRange;
        public float AutoAimTurnRate => autoAimTurnRate;
        public bool UnlockOnTargetDeath => unlockOnTargetDeath;
        public float SwitchArmThreshold => switchArmThreshold;
        public float SwitchReleaseThreshold => switchReleaseThreshold;
        public float SwitchSearchRadius => switchSearchRadius;
        public float SwitchConeHalfAngle => switchConeHalfAngle;
    }
}
