using Manatite.Combat;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// One damage event fed into <see cref="ProcBus"/>: where it landed, who it hit, and who
    /// dealt it. Raised for the projectile's own impact and for every further target an effect
    /// damages, so every <see cref="IProcEffect"/> gets a look at every hit regardless of what
    /// originally caused it.
    /// </summary>
    public readonly struct HitContext
    {
        public readonly Vector3 Point;
        public readonly Health Target;
        public readonly string OwnerTag;

        public HitContext(Vector3 point, Health target, string ownerTag)
        {
            Point = point;
            Target = target;
            OwnerTag = ownerTag;
        }
    }
}
