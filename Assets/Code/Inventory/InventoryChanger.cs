using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Code.Inventory
{
    public class InventoryChanger : MonoBehaviour
    {
        [SerializeField] private InventoryItem _inventoryItem;
        [SerializeField] private Transform _itemsContainer;

        private List<InventoryItem> _activeItems = new List<InventoryItem>();
        private Stack<InventoryItem> _pool = new Stack<InventoryItem>();
        private InventorySkinConfigs _inventorySkinConfigs;
        private InventoryModel _inventoryModel;
        private InventoryCategoryItem _categoryItem;
        private int _switchVersion = 0;
        private Coroutine _currentHideCoroutine;
        private Coroutine _currentShowCoroutine;
        private InventoryCategoryType _currentCategory;


        private const float StaggerDelay = 0.05f;

        [Inject]
        public void Construct(InventorySkinConfigs inventorySkinConfigs, InventoryModel inventoryModel)
        {
            _inventoryModel = inventoryModel;
            _inventorySkinConfigs = inventorySkinConfigs;
            InitPool();
        }

        private void InitPool()
        {
            int maxItems = _inventorySkinConfigs.InventorySkinsConfigs.Max(x => x.InventorySkins.Count);
            for (int i = 0; i < maxItems; i++)
            {
                InventoryItem item = Instantiate(_inventoryItem, _itemsContainer);
                item.gameObject.SetActive(false);
                _pool.Push(item);
            }
        }

        public void ChangeCategory(InventoryCategoryType categoryType, InventoryCategoryItem categoryItem)
        {
            if (_categoryItem != null)
            {
                _categoryItem.ChangeAvailable(false);
            }

            _categoryItem = categoryItem;

            if (_categoryItem != null)
            {
                _categoryItem.ChangeAvailable(true);
            }

            _switchVersion++;
            if (_currentHideCoroutine != null)
            {
                StopCoroutine(_currentHideCoroutine);
                _currentHideCoroutine = null;
            }
            if (_currentShowCoroutine != null)
            {
                StopCoroutine(_currentShowCoroutine);
                _currentShowCoroutine = null;
            }

            if (_activeItems.Count > 0)
            {
                _currentHideCoroutine = StartCoroutine(HideStaggered(categoryType, _switchVersion));
            }
            else
            {
                ShowCategorySkins(categoryType);
            }

            _currentCategory = categoryType;
        }

        private IEnumerator HideStaggered(InventoryCategoryType categoryType, int thisVersion)
        {
            var hideOrder = _activeItems.OrderByDescending(item => item.transform.GetSiblingIndex()).ToList();
            int count = hideOrder.Count;
            int completed = 0;
            Action onHidden = () =>
            {
                completed++;
                if (completed == count && thisVersion == _switchVersion)
                {
                    foreach (var item in _activeItems)
                    {
                        _pool.Push(item);
                    }
                    _activeItems.Clear();
                    ShowCategorySkins(categoryType);
                }
            };

            for (int i = 0; i < hideOrder.Count; i++)
            {
                hideOrder[i].HideAnimated(onHidden);
                if (i < hideOrder.Count - 1)
                {
                    yield return new WaitForSeconds(StaggerDelay);
                }
            }
        }

        private void ShowCategorySkins(InventoryCategoryType categoryType)
        {
            InventorySkinConfig skinConfig = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault(x => x.Type == categoryType);

            if (skinConfig == null)
            {
                return;
            }

            // Sort skins: unlocked (IsLocked == false) first, then locked (IsLocked == true)
            var sortedConfigs = skinConfig.InventorySkins
                .Select(skin => new
                {
                    Skin = skin,
                    IsLocked = _inventoryModel.IsSkinLocked(categoryType, skin.SkinId)
                })
                .OrderBy(x => x.IsLocked)
                .Select(x => x.Skin)
                .ToList();

            int needed = sortedConfigs.Count;
            while (_pool.Count < needed)
            {
                InventoryItem item = Instantiate(_inventoryItem, _itemsContainer);
                item.gameObject.SetActive(false);
                _pool.Push(item);
            }

            if (_currentShowCoroutine != null)
            {
                StopCoroutine(_currentShowCoroutine);
                _currentShowCoroutine = null;
            }
            _currentShowCoroutine = StartCoroutine(ShowStaggered(sortedConfigs, categoryType));
        }

        private IEnumerator ShowStaggered(List<InventorySkinData> sortedConfigs, InventoryCategoryType categoryType)
        {
            List<InventoryItem> selected = new List<InventoryItem>();
            for (int i = 0; i < sortedConfigs.Count; i++)
            {
                selected.Add(_pool.Pop());
            }
            var showOrder = selected.OrderBy(item => item.transform.GetSiblingIndex()).ToList();

            for (int i = 0; i < showOrder.Count; i++)
            {
                bool isLocked = _inventoryModel.IsSkinLocked(categoryType, sortedConfigs[i].SkinId);
                showOrder[i].Setup(sortedConfigs[i].Icon, sortedConfigs[i].SkinId, categoryType, _inventoryModel, this, isLocked);
                _activeItems.Add(showOrder[i]);
                if (i < showOrder.Count - 1)
                {
                    yield return new WaitForSeconds(StaggerDelay);
                }
            }
        }

        public void SelectItem(InventoryItem selectedItem)
        {
            foreach (var item in _activeItems)
            {
                item.SetSelected(item == selectedItem);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void RefreshCategory()
        {
            if (_categoryItem != null)
            {
                ChangeCategory(_currentCategory, _categoryItem);
                _inventoryModel.SaveData(); // Save after refreshing
            }
        }

        public InventoryCategoryType GetCurrentCategory()
        {
            return _currentCategory;
        }
    }
}