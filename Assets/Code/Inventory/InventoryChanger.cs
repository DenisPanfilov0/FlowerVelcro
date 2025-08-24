using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.GlobalScreen.Behaviour;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Inventory
{
    public class InventoryChanger : MonoBehaviour
    {
        [SerializeField] private InventoryItem _inventoryItem;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private Button _chestButton;
        [SerializeField] private OpeningChestPanel _openingChestPanel;
        [SerializeField] private TMP_Text _chestText;
        [SerializeField] private Image _chestImage;
        [SerializeField] private Color _activeColor;
        [SerializeField] private Color _inactiveColor;
        [SerializeField] private Button _closeButton;

        private List<InventoryItem> _activeItems = new List<InventoryItem>();
        private Stack<InventoryItem> _pool = new Stack<InventoryItem>();
        private InventorySkinConfigs _inventorySkinConfigs;
        private InventoryModel _inventoryModel;
        private InventoryCategoryItem _categoryItem;
        private int _switchVersion = 0;
        private Coroutine _currentHideCoroutine;
        private Coroutine _currentShowCoroutine;
        private InventoryCategoryType _currentCategory;
        private AudioManager _audioManager;

        private const float StaggerDelay = 0.025f;

        [Inject]
        public void Construct(InventorySkinConfigs inventorySkinConfigs, InventoryModel inventoryModel, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _inventoryModel = inventoryModel;
            _inventorySkinConfigs = inventorySkinConfigs;
            InitPool();
        }

        private void Start()
        {
            var firstCategory = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault()?.Type ?? InventoryCategoryType.Unknown;
            if (firstCategory != InventoryCategoryType.Unknown)
            {
                var firstCategoryItem = FindObjectsOfType<InventoryCategoryItem>()
                    .FirstOrDefault(item => item.GetComponent<InventoryCategoryItem>().GetCategoryType() == firstCategory);
                if (firstCategoryItem != null)
                {
                    ChangeCategory(firstCategory, firstCategoryItem);
                }
            }
            
            _chestButton.onClick.AddListener(OpeningChestPanelShow);
            _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _chestButton.onClick.RemoveListener(OpeningChestPanelShow);
            _closeButton.onClick.RemoveListener(Hide);
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

        private void OpeningChestPanelShow()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _openingChestPanel.Show();
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
            UpdateChestButtonState(categoryType);
        }

        public void UpdateChestButtonState(InventoryCategoryType categoryType)
        {
            bool hasLockedSkins = _inventorySkinConfigs.InventorySkinsConfigs
                .FirstOrDefault(x => x.Type == categoryType)?.InventorySkins
                .Any(skin => _inventoryModel.IsSkinLocked(categoryType, skin.SkinId)) ?? false;
            _chestButton.interactable = hasLockedSkins;
            _chestText.color = hasLockedSkins ? _activeColor : _inactiveColor;
            _chestImage.color = hasLockedSkins ? _activeColor : _inactiveColor;
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
            int selectedSkinId = _inventoryModel.GetSelectedSkinId(categoryType);
            for (int i = 0; i < sortedConfigs.Count; i++)
            {
                selected.Add(_pool.Pop());
            }
            var showOrder = selected.OrderBy(item => item.transform.GetSiblingIndex()).ToList();

            for (int i = 0; i < showOrder.Count; i++)
            {
                bool isLocked = _inventoryModel.IsSkinLocked(categoryType, sortedConfigs[i].SkinId);
                bool isSelected = sortedConfigs[i].SkinId == selectedSkinId;
                showOrder[i].Setup(_audioManager, sortedConfigs[i].Icon, sortedConfigs[i].SkinId, categoryType, _inventoryModel, this, isLocked, isSelected);
                _activeItems.Add(showOrder[i]);
                if (i < showOrder.Count - 1)
                {
                    yield return new WaitForSeconds(StaggerDelay);
                }
            }
        }

        public void UnlockSkinInList(InventoryCategoryType categoryType, int skinId)
        {
            InventoryItem itemToUnlock = _activeItems.FirstOrDefault(item => item.GetSkinId() == skinId);
            if (itemToUnlock != null)
            {
                itemToUnlock.Unlock();
                ResortItems(categoryType);
                int selectedSkinId = _inventoryModel.GetSelectedSkinId(categoryType);
                InventoryItem selectedItem = _activeItems.FirstOrDefault(item => item.GetSkinId() == selectedSkinId);
                if (selectedItem != null)
                {
                    SelectItem(selectedItem);
                }
            }
        }

        private void ResortItems(InventoryCategoryType categoryType)
        {
            InventorySkinConfig skinConfig = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault(x => x.Type == categoryType);
            if (skinConfig == null)
            {
                return;
            }

            var sortedConfigs = skinConfig.InventorySkins
                .Select((skin, index) => new { Skin = skin, Index = index })
                .OrderBy(x => _inventoryModel.IsSkinLocked(categoryType, x.Skin.SkinId))
                .ThenBy(x => x.Index)
                .ToList();

            var sortedItems = new List<InventoryItem>();
            foreach (var config in sortedConfigs)
            {
                var item = _activeItems.FirstOrDefault(i => i.GetSkinId() == config.Skin.SkinId);
                if (item != null)
                {
                    sortedItems.Add(item);
                }
            }

            _activeItems = sortedItems;
            for (int i = 0; i < _activeItems.Count; i++)
            {
                _activeItems[i].transform.SetSiblingIndex(i);
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
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            gameObject.SetActive(false);
        }

        public void RefreshCategory()
        {
            if (_categoryItem != null)
            {
                ChangeCategory(_currentCategory, _categoryItem);
                _inventoryModel.SaveData();
            }
        }

        public InventoryCategoryType GetCurrentCategory()
        {
            return _currentCategory;
        }
    }
}