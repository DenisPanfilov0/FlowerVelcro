using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Features.CanvasOfFantasy
{
    public class StickerCategory : MonoBehaviour
    {
        [SerializeField] private StickerCategoryType _stickerCategoryType;
        [SerializeField] private Button _categorySelect;
        [SerializeField] private CanvasOfFantasyWindow _canvasOfFantasyWindow;

        private void Start()
        {
            _categorySelect.onClick.AddListener(CategorySelected);
        }

        private void OnDestroy()
        {
            _categorySelect.onClick.RemoveListener(CategorySelected);
        }

        private void CategorySelected()
        {
            _canvasOfFantasyWindow.CategorySelect(_stickerCategoryType);
        }
    }
}