using System;
using Manatite.Core;
using UnityEngine;

namespace Manatite.Weapons
{
    /// <summary>
    /// Straight-line pooled projectile. Travels forward at a fixed speed until it hits
    /// something or its lifetime runs out, then returns itself to the <see cref="ObjectPooler"/>.
    /// <see cref="OnHit"/> is the seam a future modifier system hooks into (e.g. to apply damage
    /// or spawn extra effects) without this class needing to know about modifiers.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        public event Action<Projectile, Collision> OnHit;

        private Rigidbody _rb;
        private Collider _collider;
        private Collider _ignoredOwnerCollider;
        private float _lifeRemaining;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Uncapped frame rate + fixed 50Hz physics means a non-interpolated Rigidbody only
            // moves once every few render frames while the (interpolated) ship-mounted camera
            // moves every frame — that mismatch reads as ghosting/stutter on a small fast object.
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Pooled projectiles get fired by a different shooter each time, so the previous
        // owner's ignore has to be cleared or that shooter would become permanently immune.
        public void IgnoreOwnerCollider(Collider ownerCollider)
        {
            if (_ignoredOwnerCollider != null)
                Physics.IgnoreCollision(_collider, _ignoredOwnerCollider, false);

            _ignoredOwnerCollider = ownerCollider;

            if (_ignoredOwnerCollider != null)
                Physics.IgnoreCollision(_collider, _ignoredOwnerCollider, true);
        }

        public void Launch(float speed, float lifetime)
        {
            _rb.linearVelocity = transform.forward * speed;
            _rb.angularVelocity = Vector3.zero;
            _lifeRemaining = lifetime;
        }

        private void FixedUpdate()
        {
            _lifeRemaining -= Time.fixedDeltaTime;
            if (_lifeRemaining <= 0f)
                Release();
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnHit?.Invoke(this, collision);
            Release();
        }

        private void Release()
        {
            // Cleared here (not just after firing) so a per-shot subscriber
            // (see ProjectileLauncher) never leaks onto the next pooled reuse.
            OnHit = null;
            ObjectPooler.Release(gameObject);
        }
    }
}
