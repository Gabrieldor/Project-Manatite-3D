using System;
using System.Collections.Generic;
using Manatite.Data;
using UnityEngine;

namespace Manatite.Core.SaveSystem
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public PlayerSaveData player = new PlayerSaveData();
        public List<PlanetSaveData> planets = new List<PlanetSaveData>();
        public InventorySaveData inventory = new InventorySaveData();
        public int difficultyTier;
        public int basicEnemyBonus;
        public int mediumEnemyBonus;
        public int largeEnemyBonus;
        public int waveSizeBonus;
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
        public bool hasShop;
    }

    [Serializable]
    public sealed class InventorySaveData
    {
        public List<ChipSaveData> equipSlots = new List<ChipSaveData>();
        public List<ChipSaveData> inventorySlots = new List<ChipSaveData>();
    }

    /// <summary>One inventory/equip slot. Real chips resolve <see cref="chipId"/> against a
    /// <see cref="Manatite.Data.ChipDatabaseData"/> on load; merged chips (no backing .asset)
    /// carry their full definition inline instead.</summary>
    [Serializable]
    public sealed class ChipSaveData
    {
        public bool isEmpty = true;
        public bool isMerged;
        public string chipId;
        public string displayName;
        public string description;
        public string iconSourceId;
        public ChipModifierSaveData[] modifiers = Array.Empty<ChipModifierSaveData>();
        public float[] rolledValues = Array.Empty<float>();
    }

    [Serializable]
    public struct ChipModifierSaveData
    {
        public ChipStatTarget statTarget;
        public ChipValueKind valueKind;
    }
}
