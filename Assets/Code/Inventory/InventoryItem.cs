using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Inventory
{
    public class InventoryItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _backImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Button _useButton;
        [SerializeField] private Color _selectedColor;
        private int _skinId;
        private InventoryCategoryType _categoryType;
        private InventoryModel _inventoryModel;
        private InventoryChanger _changer;
        private bool _isLocked;
        private float _currentAnimationProgress;

        private const float AnimDuration = 0.2f;
        private const float StaggerDelay = 0.025f;
        private const float BounceOvershoot = 1.2f;
        private const float BounceReturn = 0.9f;
        private Coroutine _currentAnimation;
        private AudioManager _audioManager;

        public void Setup(AudioManager audioManager, Sprite icon, int skinId, InventoryCategoryType categoryType, InventoryModel inventoryModel, InventoryChanger changer, 
            bool isLocked, bool isSelected = false)
        {
            _audioManager = audioManager;
            _inventoryModel = inventoryModel;
            _categoryType = categoryType;
            _changer = changer;
            _icon.sprite = icon;
            _skinId = skinId;
            _isLocked = isLocked;

            UpdateVisuals();
            SetSelected(isSelected);

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            gameObject.SetActive(true);
            transform.localScale = Vector3.zero;
            _currentAnimationProgress = 0f;
            _currentAnimation = StartCoroutine(AppearAnimationSmoothOvershoot());
        }

        public void Unlock()
        {
            _isLocked = false;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _icon.color = _isLocked ? Color.black : Color.white;
            _useButton.interactable = !_isLocked;
        }

        public void HideAnimated(Action onComplete)
        {
            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            _currentAnimation = StartCoroutine(DisappearAnimation(onComplete));
        }

        public void ReverseAppearAnimated(Action onComplete)
        {
            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            _currentAnimation = StartCoroutine(ReverseAppearAnimation(onComplete));
        }

        public IEnumerator AppearAnimationSmoothOvershoot()
        {
            float t = _currentAnimationProgress;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimDuration;
                _currentAnimationProgress = t;
                float eased = EaseOutBack(t);
                transform.localScale = Vector3.one * eased;
                yield return null;
            }
            transform.localScale = Vector3.one;
            _currentAnimationProgress = 1f;
            _currentAnimation = null;
        }

        private IEnumerator ReverseAppearAnimation(Action onComplete)
        {
            float t = _currentAnimationProgress;
            while (t > 0f)
            {
                t -= Time.deltaTime / AnimDuration;
                _currentAnimationProgress = t;
                float eased = EaseOutBack(t);
                transform.localScale = Vector3.one * eased;
                yield return null;
            }
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            _currentAnimationProgress = 0f;
            onComplete?.Invoke();
            _currentAnimation = null;
        }

        private IEnumerator AppearAnimationElastic()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimDuration;
                float eased = EaseOutElastic(t);
                transform.localScale = Vector3.one * eased;
                yield return null;
            }
            transform.localScale = Vector3.one;
            _currentAnimation = null;
        }

        private IEnumerator AppearAnimationNoBounce()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimDuration;
                float eased = EaseOutQuad(t);
                transform.localScale = Vector3.one * eased;
                yield return null;
            }
            transform.localScale = Vector3.one;
            _currentAnimation = null;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        private float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            t = Mathf.Clamp01(t);
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private float EaseOutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        private IEnumerator DisappearAnimation(Action onComplete)
        {
            Vector3 startScale = transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimDuration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            _currentAnimationProgress = 0f;
            onComplete?.Invoke();
            _currentAnimation = null;
        }

        public void SetSelected(bool isSelected)
        {
            _frameImage.gameObject.SetActive(isSelected);
            _useButton.interactable = !isSelected && !_isLocked;
        }

        private void Start()
        {
            _useButton.onClick.AddListener(ItemClick);
        }

        private void OnDestroy()
        {
            _useButton.onClick.RemoveListener(ItemClick);
        }

        private void ItemClick()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            if (!_useButton.interactable) return;
            _changer.SelectItem(this);
            _inventoryModel.ChangeSkin(_categoryType, _skinId);
        }

        public int GetSkinId()
        {
            return _skinId;
        }
    }
}