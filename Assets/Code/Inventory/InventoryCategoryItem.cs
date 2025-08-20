using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Inventory
{
    public class InventoryCategoryItem : MonoBehaviour
    {
        [SerializeField] private Button _useCategory;
        [SerializeField] private Image _image;
        [SerializeField] private InventoryCategoryType _categoryType;
        [SerializeField] private InventoryChanger _inventoryChanger;
        [SerializeField] private Color _colorActive;

        private void Start()
        {
            _useCategory.onClick.AddListener(ChangeCategory);
        }

        private void OnDestroy()
        {
            _useCategory.onClick.RemoveListener(ChangeCategory);
        }

        private void ChangeCategory()
        {
            _inventoryChanger.ChangeCategory(_categoryType, this);
            ChangeAvailable(true);
        }

        public void ChangeAvailable(bool state)
        {
            if (state)
            {
                _image.color = _colorActive;
                _useCategory.interactable = false;
            }
            else
            {
                _image.color = Color.white;
                _useCategory.interactable = true;
            }
        }
    }

    public enum InventoryCategoryType
    {
        Unknown = 0,
        Flowers = 1,
        Bomb = 2,
        Zigzag = 3,
        Spike = 4,
    }
}