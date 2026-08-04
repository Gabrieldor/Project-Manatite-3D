using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manatite.Core.SaveSystem
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public PlayerSaveData player = new PlayerSaveData();
        public List<PlanetSaveData> planets = new List<PlanetSaveData>();
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int health;
        public int maxHealth;
    }

    [Serializable]
    public sealed class PlanetSaveData
    {
        public int planetId;
        public string typeId;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;
        public float zoneRadius;
        public bool beaten;
    }
}
