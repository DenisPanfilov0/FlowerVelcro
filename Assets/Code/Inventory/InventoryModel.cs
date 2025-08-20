using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Code.Inventory
{
    [Serializable]
    public class InventorySkinsData
    {
        public InventoryCategoryType Type;
        public int SkinId;
    }
    
    public class InventoryModel : IInitializable
    {
        private readonly InventorySkinConfigs _inventorySkinConfigs;

        public InventoryModel(InventorySkinConfigs inventorySkinConfigs)
        {
            _inventorySkinConfigs = inventorySkinConfigs;
        }
        
        public List<InventorySkinsData> _inventorySkins = new();

        public void Initialize()
        {
            _inventorySkins = new()
            {
                new InventorySkinsData { Type = InventoryCategoryType.Flowers, SkinId = 1 },
                new InventorySkinsData { Type = InventoryCategoryType.Bomb, SkinId = 1 },
                new InventorySkinsData { Type = InventoryCategoryType.Zigzag, SkinId = 1 },
                new InventorySkinsData { Type = InventoryCategoryType.Spike, SkinId = 1 },
            };
        }

        public void ChangeSkin(InventoryCategoryType category, int skinId)
        {
            InventorySkinsData skinData = _inventorySkins.FirstOrDefault(x => x.Type == category);

            if (skinData != null)
            {
                skinData.SkinId = skinId;
            }
            else
            {
                _inventorySkins.Add(new InventorySkinsData { Type = category, SkinId = skinId });
            }
        }

        public int GetSelectedSkinId(InventoryCategoryType category)
        {
            return _inventorySkins.FirstOrDefault(x => x.Type == category)?.SkinId ?? 0;
        }

        public Sprite GetSkin(InventoryCategoryType category)
        {
            InventorySkinsData skinData = _inventorySkins.FirstOrDefault(x => x.Type == category);
            InventorySkinConfig skinConfig = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault(x => x.Type == category);

            if (skinData != null && skinConfig != null)
            {
                return skinConfig.InventorySkins.FirstOrDefault(x => x.SkinId == skinData.SkinId).Icon;
            }
            else
            {
                return null;
            }
        }
    }
}