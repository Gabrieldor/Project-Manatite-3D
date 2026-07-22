using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Tunable pursuit parameters for a basic enemy. See <see cref="Manatite.Enemies.EnemyController"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyMovement_", menuName = "Manatite/Enemy Movement Data", order = 2)]
    public sealed class EnemyMovementData : ScriptableObject
    {
        [Header("Pursuit")]
        [Tooltip("Linear speed toward the target (m/s).")]
        [SerializeField] private float moveSpeed = 12f;
        [Tooltip("Max turn rate while steering toward the target (deg/s).")]
        [SerializeField] private float turnRate = 90f;
        [Tooltip("Stops closing once within this distance of the target (m).")]
        [SerializeField] private float stopDistance = 15f;
        [Tooltip("Releases back to the pool once farther than this from the target (m). Safety valve until a proper despawn/death system exists.")]
        [SerializeField] private float leashDistance = 3000f;

        public float MoveSpeed => moveSpeed;
        public float TurnRate => turnRate;
        public float StopDistance => stopDistance;
        public float LeashDistance => leashDistance;
    }
}
