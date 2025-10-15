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
        public List<InventorySelectedData> SelectedSkins; // Новый поле для selected
    }

    [Serializable]
    public class InventorySkinsData
    {
        public InventoryCategoryType Type;
        public int SkinId;
        public bool IsLocked;
    }

    [Serializable]
    public class InventorySelectedData // Новый класс для selected
    {
        public InventoryCategoryType Type;
        public int SkinId;
    }

    public class InventoryModel : IInitializable, ISaveLoad, IDisposable
    {
        private readonly InventorySkinConfigs _inventorySkinConfigs;
        private readonly SaveLoadService _saveLoadService;
        private List<InventorySkinsData> _inventorySkins = new();
        private List<InventorySelectedData> _selectedSkins = new(); // Отдельный список для selected
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
            _selectedSkins = new List<InventorySelectedData>();
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
                var firstSkin = config.InventorySkins.FirstOrDefault(s => !_inventorySkins.Any(x => x.Type == config.Type && x.SkinId == s.SkinId && x.IsLocked)) 
                    ?? config.InventorySkins.FirstOrDefault();
                if (firstSkin != null)
                {
                    _selectedSkins.Add(new InventorySelectedData { Type = config.Type, SkinId = firstSkin.SkinId });
                    var skinData = _inventorySkins.FirstOrDefault(x => x.Type == config.Type && x.SkinId == firstSkin.SkinId);
                    if (skinData != null)
                    {
                        skinData.IsLocked = false; // Ensure first skin is unlocked
                    }
                }
            }
            SaveData();
        }

        public void SaveData()
        {
            var data = new InventorySaveData
            {
                Skins = _inventorySkins,
                SelectedSkins = _selectedSkins
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        public void LoadData()
        {
            var data = _saveLoadService.LoadData<InventorySaveData>(SAVE_KEY);
            if (data != null)
            {
                _inventorySkins = data.Skins ?? new List<InventorySkinsData>();
                _selectedSkins = data.SelectedSkins ?? new List<InventorySelectedData>();
                // Добавляем недостающие скины из конфига
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
                    // Если нет selected для категории, устанавливаем дефолт
                    if (!_selectedSkins.Any(x => x.Type == config.Type))
                    {
                        var firstSkin = config.InventorySkins.FirstOrDefault();
                        if (firstSkin != null)
                        {
                            _selectedSkins.Add(new InventorySelectedData { Type = config.Type, SkinId = firstSkin.SkinId });
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
            var selectedData = _selectedSkins.FirstOrDefault(x => x.Type == category);
            if (selectedData != null)
            {
                selectedData.SkinId = skinId;
            }
            else
            {
                _selectedSkins.Add(new InventorySelectedData { Type = category, SkinId = skinId });
            }
            SaveData();
        }

        public int GetSelectedSkinId(InventoryCategoryType category)
        {
            return _selectedSkins.FirstOrDefault(x => x.Type == category)?.SkinId ?? 0;
        }

        public Sprite GetSkin(InventoryCategoryType category)
        {
            int selectedId = GetSelectedSkinId(category);
            InventorySkinConfig skinConfig = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault(x => x.Type == category);
            return skinConfig?.InventorySkins.FirstOrDefault(x => x.SkinId == selectedId)?.Icon;
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

        public List<InventorySkinsData> GetInventorySkinsData()
        {
            return _inventorySkins;
        }
    }
}