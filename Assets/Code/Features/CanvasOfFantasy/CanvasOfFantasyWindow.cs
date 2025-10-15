using System;
using System.Collections.Generic;
using System.Linq;
using Code.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Code.Features.CanvasOfFantasy
{
    public class CanvasOfFantasyWindow : MonoBehaviour
    {
        [SerializeField] private StickerCategory _canvasOfFantasyCategory;
        [SerializeField] private Transform _container;
        [SerializeField] private StickerItem _stickerItem;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private InteractiveStickerItem _interactiveStickerItemPrefab;

        private InventoryModel _inventoryModel;
        private List<InventorySkinsData> _inventorySkinsData;
        private StickerItem _selectedStickerItem;
        private InteractiveStickerItem _selectedInteractiveSticker;
        private Button _backgroundButton;
        private InventorySkinConfigs _inventorySkinConfigs;

        public StickerItem SelectedStickerItem => _selectedStickerItem;

        // ✅ Новое событие
        public event Action<StickerItem> OnStickerSelectedEvent;

        [Inject]
        public void Construct(InventoryModel inventoryModel, InventorySkinConfigs inventorySkinConfigs)
        {
            _inventorySkinConfigs = inventorySkinConfigs;
            _inventoryModel = inventoryModel;
        }

        private void Awake()
        {
            _backgroundButton = _backgroundImage.gameObject.GetComponent<Button>();
            if (_backgroundButton == null)
            {
                _backgroundButton = _backgroundImage.gameObject.AddComponent<Button>();
            }
            _backgroundButton.transition = Selectable.Transition.None;
        }

        private void Start()
        {
            _inventorySkinsData = _inventoryModel.GetInventorySkinsData();
            _backgroundButton.onClick.AddListener(OnBackgroundClicked);
        }

        private void OnDestroy()
        {
            _backgroundButton.onClick.RemoveListener(OnBackgroundClicked);
        }

        public void CategorySelect(StickerCategoryType categoryType)
        {
            List<InventorySkinsData> inventorySkinsData = null;

            switch (categoryType)
            {
                case StickerCategoryType.Character:
                    inventorySkinsData = _inventorySkinsData.FindAll(x => x.Type == InventoryCategoryType.Character);
                    break;
                case StickerCategoryType.Bomb:
                    inventorySkinsData = _inventorySkinsData.FindAll(x => x.Type == InventoryCategoryType.Bomb);
                    break;
                case StickerCategoryType.Flower:
                    inventorySkinsData = _inventorySkinsData.FindAll(x => x.Type == InventoryCategoryType.Flowers);
                    break;
                case StickerCategoryType.Spike:
                    inventorySkinsData = _inventorySkinsData.FindAll(x => x.Type == InventoryCategoryType.Spike);
                    break;
                case StickerCategoryType.Zigzag:
                    inventorySkinsData = _inventorySkinsData.FindAll(x => x.Type == InventoryCategoryType.Zigzag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(categoryType), categoryType, null);
            }

            CreateStickerItem(inventorySkinsData);
        }

        private void CreateStickerItem(List<InventorySkinsData> inventorySkins)
        {
            foreach (Transform child in _container)
            {
                Destroy(child.gameObject);
            }

            DeselectCurrentSticker();
            DeselectCurrentInteractiveSticker();

            foreach (var sticker in _backgroundImage.GetComponentsInChildren<InteractiveStickerItem>())
            {
                sticker.EnableInteractivity();
            }

            foreach (var skinData in inventorySkins)
            {
                if (!skinData.IsLocked)
                {
                    var inventorySkinData = _inventorySkinConfigs.InventorySkinsConfigs
                        .FirstOrDefault(x => x.Type == skinData.Type).InventorySkins
                        .FirstOrDefault(x => x.SkinId == skinData.SkinId);

                    StickerItem sticker = Instantiate(_stickerItem, _container);
                    sticker.Setup(inventorySkinData.Icon, this);
                }
            }
        }

        public void OnStickerSelected(StickerItem sticker)
        {
            // Деактивируем текущий активный стикер
            DeselectCurrentInteractiveSticker();

            if (_selectedStickerItem == sticker)
            {
                _selectedStickerItem.Deselect();
                _selectedStickerItem = null;

                foreach (var interactiveSticker in _backgroundImage.GetComponentsInChildren<InteractiveStickerItem>())
                {
                    interactiveSticker.EnableInteractivity();
                }

                OnStickerSelectedEvent?.Invoke(null); // уведомляем, что стикер снят
            }
            else
            {
                if (_selectedStickerItem != null)
                {
                    _selectedStickerItem.Deselect();
                }

                _selectedStickerItem = sticker;

                foreach (var interactiveSticker in _backgroundImage.GetComponentsInChildren<InteractiveStickerItem>())
                {
                    interactiveSticker.DisableInteractivity();
                }

                OnStickerSelectedEvent?.Invoke(_selectedStickerItem); // уведомляем о новом стикере
            }
        }

        public void DeselectCurrentSticker()
        {
            if (_selectedStickerItem != null)
            {
                _selectedStickerItem.Deselect();
                _selectedStickerItem = null;

                foreach (var interactiveSticker in _backgroundImage.GetComponentsInChildren<InteractiveStickerItem>())
                {
                    interactiveSticker.EnableInteractivity();
                }

                OnStickerSelectedEvent?.Invoke(null);
            }
        }

        public void OnInteractiveStickerSelected(InteractiveStickerItem sticker)
        {
            if (_selectedInteractiveSticker != null && _selectedInteractiveSticker != sticker)
            {
                _selectedInteractiveSticker.Deselect();
            }

            _selectedInteractiveSticker = sticker;
            _selectedInteractiveSticker.Select();
        }

        public void DeselectCurrentInteractiveSticker()
        {
            if (_selectedInteractiveSticker != null)
            {
                _selectedInteractiveSticker.Deselect();
                _selectedInteractiveSticker = null;
            }
        }

        private void OnBackgroundClicked()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                InteractiveStickerItem sticker = result.gameObject.GetComponent<InteractiveStickerItem>();
                if (sticker != null)
                {
                    OnInteractiveStickerSelected(sticker);
                    return;
                }
            }

            DeselectCurrentInteractiveSticker();

            if (_selectedStickerItem == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _backgroundImage.rectTransform,
                Input.mousePosition,
                null,
                out localPoint);

            InteractiveStickerItem interactiveSticker = Instantiate(_interactiveStickerItemPrefab, _backgroundImage.transform);
            interactiveSticker.Setup(_selectedStickerItem.GetIconSprite(), this);
            interactiveSticker.transform.localPosition = localPoint;
            interactiveSticker.Select();

            DeselectCurrentSticker();
        }
    }
}
