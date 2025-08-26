using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Inventory
{
    public class OpeningChestPanel : MonoBehaviour
    {
        [SerializeField] private Image _chestImage;
        [SerializeField] private Image _activeShadow;
        [SerializeField] private Image _panelShadow;
        [SerializeField] private Button _interactiveShadow;
        [SerializeField] private Button _adButton;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _closePanel;
        [SerializeField] private RewardSkin _rewardSkinPrefab;
        [SerializeField] private Transform _rewardParent;
        [SerializeField] private TMP_Text _chestText;
        [SerializeField] private TMP_Text _categoryText;
        [SerializeField] private TMP_Text _promptText;
        [SerializeField] private GameObject _blocker;
        [SerializeField] private InventoryChanger _inventoryChanger;
        [SerializeField] private Color _activeColor;
        [SerializeField] private Color _inactiveColor;
        [SerializeField] private Image _buyImage;
        [SerializeField] private TMP_Text _buyText;

        private CurrencyModel _currencyModel;
        private InventorySkinConfigs _inventorySkinConfigs;
        private InventoryModel _inventoryModel;
        private RewardSkin _rewardSkinInstance;
        private Coroutine _currentAnimation;
        private bool _isHidingReward;

        private Vector2 _buyButtonInitialPos;
        private Vector2 _adButtonInitialPos;
        private Vector3 _closePanelInitialScale;
        private AudioManager _audioManager;

        [Inject]
        public void Construct(CurrencyModel currencyModel, InventorySkinConfigs inventorySkinConfigs, InventoryModel inventoryModel, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _currencyModel = currencyModel;
            _inventorySkinConfigs = inventorySkinConfigs;
            _inventoryModel = inventoryModel;
        }

        private void Awake()
        {
            _buyButtonInitialPos = _buyButton.GetComponent<RectTransform>().anchoredPosition;
            _adButtonInitialPos = _adButton.GetComponent<RectTransform>().anchoredPosition;
            _closePanelInitialScale = _closePanel.transform.localScale;

            _chestImage.transform.localScale = Vector3.zero;
            _chestImage.transform.rotation = Quaternion.identity;

            _chestText.color = new Color(_chestText.color.r, _chestText.color.g, _chestText.color.b, 0f);
            _categoryText.color = new Color(_categoryText.color.r, _categoryText.color.g, _categoryText.color.b, 0f);
            _promptText.color = new Color(_promptText.color.r, _promptText.color.g, _promptText.color.b, 0f);

            _buyButton.GetComponent<RectTransform>().anchoredPosition = _buyButtonInitialPos + new Vector2(-Screen.width, 0f);
            _buyButton.gameObject.SetActive(false);

            _adButton.GetComponent<RectTransform>().anchoredPosition = _adButtonInitialPos + new Vector2(Screen.width, 0f);
            _adButton.gameObject.SetActive(false);

            _closePanel.transform.localScale = Vector3.zero;
            _closePanel.gameObject.SetActive(true);

            _activeShadow.gameObject.SetActive(false);
            _panelShadow.color = new Color(_panelShadow.color.r, _panelShadow.color.g, _panelShadow.color.b, 0f);
            _blocker.SetActive(true);

            _isHidingReward = false;
        }

        private void Start()
        {
            bool canSpend = _currencyModel.CanSpend(1000);
            _buyButton.interactable = canSpend;
            _buyText.color = canSpend ? _activeColor : _inactiveColor;
            _buyImage.color = canSpend ? _activeColor : _inactiveColor;

            _buyButton.onClick.AddListener(BuyChest);
            _adButton.onClick.AddListener(WatchAd);
            _interactiveShadow.onClick.AddListener(HideReward);
            _closePanel.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(BuyChest);
            _adButton.onClick.RemoveListener(WatchAd);
            _interactiveShadow.onClick.RemoveListener(HideReward);
            _closePanel.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            _currentAnimation = StartCoroutine(ShowAnimation());
        }

        public void Hide()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            _currentAnimation = StartCoroutine(HideAnimation());
        }

        private IEnumerator ShowAnimation()
        {
            _blocker.SetActive(true);

            float panelDuration = 0.3f;
            float elementDuration = 0.5f;

            Coroutine panelCoroutine = StartCoroutine(ScaleCoroutine(transform, Vector3.zero, Vector3.one, panelDuration));
            Coroutine shadowCoroutine = StartCoroutine(FadeImageCoroutine(_panelShadow, 0f, 230f / 255f, panelDuration));

            yield return panelCoroutine;
            yield return shadowCoroutine;

            StartCoroutine(ScaleCoroutine(_chestImage.transform, Vector3.zero, Vector3.one, elementDuration));

            yield return new WaitForSeconds(0.3f);
            StartCoroutine(FadeTextCoroutine(_chestText, 0f, 1f, elementDuration));
            StartCoroutine(FadeTextCoroutine(_categoryText, 0f, 1f, elementDuration));

            yield return new WaitForSeconds(0.2f);
            _buyButton.gameObject.SetActive(true);
            _adButton.gameObject.SetActive(true);
            StartCoroutine(SlideButtonCoroutine(_buyButton.GetComponent<RectTransform>(), 
                _buyButtonInitialPos + new Vector2(-Screen.width, 0f), 
                _buyButtonInitialPos, 
                elementDuration));
            StartCoroutine(SlideButtonCoroutine(_adButton.GetComponent<RectTransform>(), 
                _adButtonInitialPos + new Vector2(Screen.width, 0f), 
                _adButtonInitialPos, 
                elementDuration));

            yield return new WaitForSeconds(0.2f);
            StartCoroutine(ScaleCoroutine(_closePanel.transform, Vector3.zero, _closePanelInitialScale, elementDuration));

            _blocker.SetActive(false);
        }

        private IEnumerator HideAnimation()
        {
            _blocker.SetActive(true);

            float panelDuration = 0.3f;
            float elementDuration = 0.5f;

            Coroutine closeButtonCoroutine = StartCoroutine(ScaleCoroutine(_closePanel.transform, _closePanel.transform.localScale, Vector3.zero, elementDuration));

            yield return new WaitForSeconds(0.3f);
            Coroutine buyButtonCoroutine = StartCoroutine(SlideButtonCoroutine(_buyButton.GetComponent<RectTransform>(), 
                _buyButton.GetComponent<RectTransform>().anchoredPosition, 
                _buyButtonInitialPos + new Vector2(-Screen.width, 0f), 
                elementDuration));
            Coroutine adButtonCoroutine = StartCoroutine(SlideButtonCoroutine(_adButton.GetComponent<RectTransform>(), 
                _adButton.GetComponent<RectTransform>().anchoredPosition, 
                _adButtonInitialPos + new Vector2(Screen.width, 0f), 
                elementDuration));

            yield return new WaitForSeconds(0.2f);
            Coroutine textCoroutine = StartCoroutine(FadeTextCoroutine(_chestText, 1f, 0f, elementDuration));
            Coroutine categoryTextCoroutine = StartCoroutine(FadeTextCoroutine(_categoryText, 1f, 0f, elementDuration));

            yield return new WaitForSeconds(0.2f);
            Coroutine chestCoroutine = StartCoroutine(ScaleCoroutine(_chestImage.transform, _chestImage.transform.localScale, Vector3.zero, elementDuration));

            yield return closeButtonCoroutine;
            yield return buyButtonCoroutine;
            yield return adButtonCoroutine;
            yield return textCoroutine;
            yield return categoryTextCoroutine;
            yield return chestCoroutine;

            _buyButton.gameObject.SetActive(false);
            _adButton.gameObject.SetActive(false);

            Coroutine panelCoroutine = StartCoroutine(ScaleCoroutine(transform, transform.localScale, Vector3.zero, panelDuration));
            Coroutine shadowCoroutine = StartCoroutine(FadeImageCoroutine(_panelShadow, _panelShadow.color.a, 0f, panelDuration));

            yield return panelCoroutine;
            yield return shadowCoroutine;

            gameObject.SetActive(false);
            _blocker.SetActive(false);
        }

        private void BuyChest()
        {
            if (_currencyModel.CanSpend(1000))
            {
                _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
                _currencyModel.SpendCurrency(1000);
                OpenChest();
            }
        }

        private void WatchAd()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            OpenChest();
        }

        private void OpenChest()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            _currentAnimation = StartCoroutine(OpenChestAnimation());
        }

        private IEnumerator OpenChestAnimation()
        {
            InventoryCategoryType category = _inventoryChanger.GetCurrentCategory();
            InventorySkinConfig skinConfig = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault(x => x.Type == category);
            if (skinConfig == null || skinConfig.InventorySkins.Count == 0)
            {
                yield break;
            }

            var lockedSkins = skinConfig.InventorySkins
                .Where(x => _inventoryModel.IsSkinLocked(category, x.SkinId))
                .ToList();
            if (lockedSkins.Count == 0)
            {
                yield break;
            }
            InventorySkinData randomSkin = lockedSkins[UnityEngine.Random.Range(0, lockedSkins.Count)];

            _activeShadow.gameObject.SetActive(true);
            Color startShadowColor = _activeShadow.color;
            startShadowColor.a = 0f;
            _activeShadow.color = startShadowColor;

            Vector3 startChestScale = _chestImage.transform.localScale;
            Vector3 targetChestScale = Vector3.one * 2f;
            Vector3 initialChestPosition = _chestImage.transform.localPosition;
            float elapsed = 0f;
            float shakeDuration = 1.5f;
            float shrinkDuration = 0.5f;
            float shadowDuration = 1f;

            float shakeFrequency = 20f;
            float shakeAmplitude = 30f;
            float verticalShakeAmplitude = 15f;
            float horizontalShakeAmplitude = 15f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;

                if (elapsed < shadowDuration)
                {
                    float shadowT = elapsed / shadowDuration;
                    Color targetShadowColor = startShadowColor;
                    targetShadowColor.a = Mathf.Lerp(0f, 245f / 255f, shadowT);
                    _activeShadow.color = targetShadowColor;
                }

                _chestImage.transform.localScale = Vector3.Lerp(startChestScale, targetChestScale, t);

                float currentAmplitude = shakeAmplitude * t;
                float shakeZ = Mathf.Sin(elapsed * shakeFrequency) * currentAmplitude;
                float shakeX = Mathf.Sin(elapsed * shakeFrequency * 1.5f) * horizontalShakeAmplitude * t;
                float shakeY = Mathf.Cos(elapsed * shakeFrequency * 0.8f) * verticalShakeAmplitude * t;

                _chestImage.transform.rotation = Quaternion.Euler(0f, 0f, shakeZ);
                _chestImage.transform.localPosition = initialChestPosition + new Vector3(shakeX, shakeY, 0f);

                yield return null;
            }

            _audioManager.PlaySoundEffect(AudioClipTypeId.OpenChest);

            _rewardSkinInstance = Instantiate(_rewardSkinPrefab, _rewardParent);
            _rewardSkinInstance.transform.localScale = Vector3.zero;
            _rewardSkinInstance.Setup(randomSkin.Icon);

            _promptText.color = new Color(_promptText.color.r, _promptText.color.g, _promptText.color.b, 0f);

            elapsed = 0f;
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shrinkDuration;
                float eased = EaseOutBack(t);

                _chestImage.transform.localScale = Vector3.Lerp(targetChestScale, Vector3.zero, eased);
                _chestImage.transform.rotation = Quaternion.identity;
                _chestImage.transform.localPosition = initialChestPosition;

                _rewardSkinInstance.transform.localScale = Vector3.one * 2f * eased;

                _promptText.color = new Color(_promptText.color.r, _promptText.color.g, _promptText.color.b, eased);

                yield return null;
            }

            _chestImage.transform.localScale = Vector3.one;
            _chestImage.transform.rotation = Quaternion.identity;
            _chestImage.transform.localPosition = initialChestPosition;

            _rewardSkinInstance.transform.localScale = Vector3.one * 2f;
            _promptText.color = new Color(_promptText.color.r, _promptText.color.g, _promptText.color.b, 1f);

            _inventoryModel.UnlockSkin(category, randomSkin.SkinId);
            _inventoryChanger.UnlockSkinInList(category, randomSkin.SkinId);

            _interactiveShadow.interactable = true;

            bool isLastSkin = !skinConfig.InventorySkins.Any(x => _inventoryModel.IsSkinLocked(category, x.SkinId));
            if (isLastSkin)
            {
                _inventoryChanger.UpdateChestButtonState(category);
            }
        }

        private void HideReward()
        {
            if (_isHidingReward) return;
            _isHidingReward = true;
            _interactiveShadow.interactable = false;

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            _currentAnimation = StartCoroutine(HideChestAnimation());
        }

        private IEnumerator HideChestAnimation()
        {
            float fadeDuration = 0.5f;
            Color startShadowColor = _activeShadow.color;
            Vector3 startRewardScale = _rewardSkinInstance != null ? _rewardSkinInstance.transform.localScale : Vector3.zero;
            Color startPromptColor = _promptText.color;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                float eased = EaseInBack(t);

                Color targetShadowColor = startShadowColor;
                targetShadowColor.a = Mathf.Lerp(startShadowColor.a, 0f, eased);
                _activeShadow.color = targetShadowColor;

                if (_rewardSkinInstance != null)
                {
                    _rewardSkinInstance.transform.localScale = Vector3.Lerp(startRewardScale, Vector3.zero, eased);
                }

                _promptText.color = new Color(startPromptColor.r, startPromptColor.g, startPromptColor.b, Mathf.Lerp(startPromptColor.a, 0f, eased));

                yield return null;
            }

            _activeShadow.color = new Color(startShadowColor.r, startShadowColor.g, startShadowColor.b, 0f);
            _activeShadow.gameObject.SetActive(false);

            if (_rewardSkinInstance != null)
            {
                _rewardSkinInstance.transform.localScale = Vector3.zero;
                Destroy(_rewardSkinInstance.gameObject);
                _rewardSkinInstance = null;
            }

            _promptText.color = new Color(startPromptColor.r, startPromptColor.g, startPromptColor.b, 0f);

            bool canSpend = _currencyModel.CanSpend(1000);
            _buyButton.interactable = canSpend;
            _buyText.color = canSpend ? _activeColor : _inactiveColor;
            _buyImage.color = canSpend ? _activeColor : _inactiveColor;

            _isHidingReward = false;
            _blocker.SetActive(false);

            InventoryCategoryType category = _inventoryChanger.GetCurrentCategory();
            InventorySkinConfig skinConfig = _inventorySkinConfigs.InventorySkinsConfigs.FirstOrDefault(x => x.Type == category);
            if (skinConfig != null)
            {
                bool isLastSkin = !skinConfig.InventorySkins.Any(x => _inventoryModel.IsSkinLocked(category, x.SkinId));
                if (isLastSkin)
                {
                    Hide();
                    _inventoryChanger.UpdateChestButtonState(category);
                }
            }
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        private float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t);
            return c3 * t * t * t - c1 * t * t;
        }

        private IEnumerator ScaleCoroutine(Transform target, Vector3 startScale, Vector3 endScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            target.localScale = endScale;
        }

        private IEnumerator FadeTextCoroutine(TMP_Text text, float startAlpha, float endAlpha, float duration)
        {
            Color color = text.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                text.color = color;
                yield return null;
            }
            color.a = endAlpha;
            text.color = color;
        }

        private IEnumerator FadeImageCoroutine(Image image, float startAlpha, float endAlpha, float duration)
        {
            Color color = image.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                image.color = color;
                yield return null;
            }
            color.a = endAlpha;
            image.color = color;
        }

        private IEnumerator SlideButtonCoroutine(RectTransform button, Vector2 startPos, Vector2 targetPos, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                button.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            button.anchoredPosition = targetPos;
        }
    }
}