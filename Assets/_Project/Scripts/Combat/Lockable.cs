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

        private Renderer _renderer;

        private void Awake() => _renderer = GetComponentInChildren<Renderer>();

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        /// <summary>World-space render bounds, used for line-of-sight sampling. False if no renderer.</summary>
        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (_renderer != null)
            {
                bounds = _renderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }
    }
}
