using System;
using System.Collections.Generic;
using Code.Configs.ItemSpawnerConfig;
using Code.Inventory;
using UnityEngine;

namespace Code
{
    [CreateAssetMenu(fileName = "ItemSpawnerConfig", menuName = "Configs/ItemSpawnerConfig", order = 1)]
    public class ItemSpawnerConfig : ScriptableObject
    {
        [Serializable]
        public class ItemSpawnConfig
        {
            public ItemSpawnerTypeId TypeId;
            public InventoryCategoryType CategoryType;
            public GameObject Prefab;
        }

        [Serializable]
        public class BlockItem
        {
            public ItemSpawnerTypeId TypeId;
            public int Quantity;
        }

        [Serializable]
        public class BlockConfig
        {
            public List<BlockItem> Items;
        }

        [Serializable]
        public class StageConfig
        {
            public List<BlockConfig> Blocks;
            public Vector2Int ScoreRange;
        }

        public List<ItemSpawnConfig> SpawnConfigs;
        public Vector2 SpawnDelayRange = new Vector2(0.5f, 1.5f);
        public List<StageConfig> Stages;
    }
}