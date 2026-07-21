using System.Collections.Generic;
using UnityEngine;

namespace Manatite.Combat
{
    /// <summary>
    /// Marker component: any object carrying this can be acquired by
    /// <see cref="Manatite.Player.TargetLock"/>. Self-registers into a static
    /// list so acquisition never needs a per-frame scene scan.
    /// </summary>
    public sealed class Lockable : MonoBehaviour
    {
        private static readonly List<Lockable> all = new List<Lockable>();

        public static IReadOnlyList<Lockable> All => all;

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);
    }
}
