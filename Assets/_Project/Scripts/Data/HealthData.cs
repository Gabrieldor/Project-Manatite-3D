using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Tunables for a <see cref="Manatite.Combat.Health"/> instance.
    /// </summary>
    [CreateAssetMenu(fileName = "Health_", menuName = "Manatite/Health Data", order = 4)]
    public sealed class HealthData : ScriptableObject
    {
        [SerializeField] private int maxHealth = 100;

        public int MaxHealth => maxHealth;
    }
}
