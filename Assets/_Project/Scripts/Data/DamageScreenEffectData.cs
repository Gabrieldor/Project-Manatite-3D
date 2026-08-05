using UnityEngine;

namespace Manatite.Data
{
    /// <summary>
    /// Maps fraction of health lost (0 = full health, 1 = dead) to broken-glass
    /// crack intensity for <see cref="Manatite.Player.DamageScreenEffect"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageScreenEffect_", menuName = "Manatite/Damage Screen Effect Data", order = 5)]
    public sealed class DamageScreenEffectData : ScriptableObject
    {
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public float Evaluate(float healthPercent01) => intensityCurve.Evaluate(1f - Mathf.Clamp01(healthPercent01));
    }
}
