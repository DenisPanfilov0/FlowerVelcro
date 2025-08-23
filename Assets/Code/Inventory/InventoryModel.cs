using System;
using System.Collections.Generic;
using System.Linq;
using Code;
using UnityEngine;
using Zenject;

namespace Code.Inventory
{
    [Serializable]
    public class InventorySaveData
    {
        public List<InventorySkinsData> Skins;
    }

    [Serializable]
    public class InventorySkinsData
    {
        public InventoryCategoryType Type;
        public int SkinId;
        public bool IsLocked;
    }

    public class InventoryModel : IInitializable, ISaveLoad, IDisposable
    {
        private readonly InventorySkinConfigs _inventorySkinConfigs;
        private readonly SaveLoadService _saveLoadService;
        private List<InventorySkinsData> _inventorySkins = new();
        private const string SAVE_KEY = "InventorySkins";

        public InventoryModel(InventorySkinConfigs inventorySkinConfigs, SaveLoadService saveLoadService)
        {
            _inventorySkinConfigs = inventorySkinConfigs;
            _saveLoadService = saveLoadService;
        }

        public void Initialize()
        {
            LoadData();
        }

        private void InitializeDefaultValues()
        {
            _inventorySkins = new List<InventorySkinsData>();
            foreach (var config in _inventorySkinConfigs.InventorySkinsConfigs)
            {
                foreach (var skin in config.InventorySkins)
                {
                    _inventorySkins.Add(new InventorySkinsData
                    {
                        Type = config.Type,
                        SkinId = skin.SkinId,
                        IsLocked = skin.IsLocked
                    });
                }
                if (_inventorySkins.Any(x => x.Type == config.Type && !x.IsLocked))
                {
                    var firstUnlocked = _inventorySkins.FirstOrDefault(x => x.Type == config.Type && !x.IsLocked);
                    ChangeSkin(config.Type, firstUnlocked.SkinId);
                }
                else
                {
                    var firstSkin = config.InventorySkins.FirstOrDefault();
                    if (firstSkin != null)
                    {
                        ChangeSkin(config.Type, firstSkin.SkinId);
                        var skinData = _inventorySkins.FirstOrDefault(x => x.Type == config.Type && x.SkinId == firstSkin.SkinId);
                        if (skinData != null)
                        {
                            skinData.IsLocked = false; // Ensure first skin is unlocked
                        }
                    }
                }
            }
            SaveData();
        }

        public void SaveData()
        {
            var data = new InventorySaveData
            {
                Skins = _inventorySkins
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        public void LoadData()
        {
            var data = _saveLoadService.LoadData<InventorySaveData>(SAVE_KEY);
            if (data != null && data.Skins != null)
            {
                _inventorySkins = data.Skins;
                // Ensure all skins from config are present in save data
                foreach (var config in _inventorySkinConfigs.InventorySkinsConfigs)
                {
                    foreach (var skin in config.InventorySkins)
                    {
                        if (!_inventorySkins.Any(x => x.Type == config.Type && x.SkinId == skin.SkinId))
                        {
                            _inventorySkins.Add(new InventorySkinsData
                            {
                                Type = config.Type,
                                SkinId = skin.SkinId,
                                IsLocked = skin.IsLocked
                            });
                        }
                    }
                }
            }
            else
            {
                InitializeDefaultValues();
            }
        }

        public void Dispose()
        {
            SaveData();
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
                _inventorySkins.Add(new InventorySkinsData { Type = category, SkinId = skinId, IsLocked = false });
            }
            SaveData();
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
                return skinConfig.InventorySkins.FirstOrDefault(x => x.SkinId == skinData.SkinId)?.Icon;
            }
            return null;
        }

        public bool IsSkinLocked(InventoryCategoryType category, int skinId)
        {
            var skinData = _inventorySkins.FirstOrDefault(x => x.Type == category && x.SkinId == skinId);
            return skinData != null ? skinData.IsLocked : true;
        }

        public void UnlockSkin(InventoryCategoryType category, int skinId)
        {
            var skinData = _inventorySkins.FirstOrDefault(x => x.Type == category && x.SkinId == skinId);
            if (skinData != null)
            {
                skinData.IsLocked = false;
            }
            else
            {
                _inventorySkins.Add(new InventorySkinsData
                {
                    Type = category,
                    SkinId = skinId,
                    IsLocked = false
                });
            }
            SaveData();
        }
    }
}