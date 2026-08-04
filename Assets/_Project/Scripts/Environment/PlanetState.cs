using UnityEngine;

namespace Manatite.Environment
{
    /// <summary>Save/load identity + state for one spawned planet instance, attached by
    /// PlanetSpawner. PlanetId is a stable per-run spawn index (not persisted RNG seed).</summary>
    public sealed class PlanetState : MonoBehaviour
    {
        public int PlanetId { get; private set; }
        public string TypeId { get; private set; }
        public float Scale { get; private set; }
        public float ZoneRadius { get; private set; }
        public bool Beaten { get; set; }

        public void Initialize(int planetId, string typeId, float scale, float zoneRadius)
        {
            PlanetId = planetId;
            TypeId = typeId;
            Scale = scale;
            ZoneRadius = zoneRadius;
        }
    }
}
