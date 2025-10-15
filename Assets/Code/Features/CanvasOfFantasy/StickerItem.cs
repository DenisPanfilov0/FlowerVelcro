using UnityEngine;
using UnityEngine.UI;

namespace Code.Features.CanvasOfFantasy
{
    public class StickerItem : MonoBehaviour
    {
        [SerializeField] private Image _selectedBack;
        [SerializeField] private Image _icon;
        [SerializeField] private Button _selectItem;
        [SerializeField] private Sprite _normalBackgroundSprite;
        [SerializeField] private Sprite _selectedBackgroundSprite;

        private CanvasOfFantasyWindow _canvasOfFantasyWindow;

        public void Setup(Sprite icon, CanvasOfFantasyWindow canvasOfFantasyWindow)
        {
            _icon.sprite = icon;
            _canvasOfFantasyWindow = canvasOfFantasyWindow;
            _selectedBack.sprite = _normalBackgroundSprite;
        }

        private void Start()
        {
            _selectItem.onClick.AddListener(ItemSelected);
        }

        private void OnDestroy()
        {
            _selectItem.onClick.RemoveListener(ItemSelected);
        }

        private void ItemSelected()
        {
            if (_canvasOfFantasyWindow.SelectedStickerItem == this)
            {
                _selectedBack.sprite = _normalBackgroundSprite;
                _canvasOfFantasyWindow.OnStickerSelected(null); // Deselect
            }
            else
            {
                _selectedBack.sprite = _selectedBackgroundSprite;
                _canvasOfFantasyWindow.OnStickerSelected(this);
            }
        }

        public void Deselect()
        {
            _selectedBack.sprite = _normalBackgroundSprite;
        }

        public Sprite GetIconSprite()
        {
            return _icon.sprite;
        }
    }
}