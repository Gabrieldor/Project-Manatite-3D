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

        public float LockAcquireRadius => lockAcquireRadius;
        public float MaxLockRange => maxLockRange;
        public float AutoAimTurnRate => autoAimTurnRate;
    }
}
