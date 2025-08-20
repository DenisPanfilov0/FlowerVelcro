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
        [SerializeField] private GameObject _blocker;
        [SerializeField] private InventoryChanger _inventoryChanger;
        private CurrencyModel _currencyModel;
        private InventorySkinConfigs _inventorySkinConfigs;
        private RewardSkin _rewardSkinInstance;
        private Coroutine _currentAnimation;

        private Vector3 _buyButtonInitialPos;
        private Vector3 _adButtonInitialPos;
        private Vector3 _closePanelInitialScale;

        [Inject]
        public void Construct(CurrencyModel currencyModel, InventorySkinConfigs inventorySkinConfigs)
        {
            _currencyModel = currencyModel;
            _inventorySkinConfigs = inventorySkinConfigs;
        }

        private void Awake()
        {
            // Save initial positions and scales
            _buyButtonInitialPos = _buyButton.transform.localPosition;
            _adButtonInitialPos = _adButton.transform.localPosition;
            _closePanelInitialScale = _closePanel.transform.localScale;

            // Set initial states
            _chestImage.transform.localScale = Vector3.zero;
            _chestImage.transform.rotation = Quaternion.identity;

            _chestText.color = new Color(_chestText.color.r, _chestText.color.g, _chestText.color.b, 0f);
            _categoryText.color = new Color(_categoryText.color.r, _categoryText.color.g, _categoryText.color.b, 0f);

            _buyButton.transform.localPosition = _buyButtonInitialPos + new Vector3(-Screen.width, 0f, 0f);
            _buyButton.gameObject.SetActive(false);

            _adButton.transform.localPosition = _adButtonInitialPos + new Vector3(Screen.width, 0f, 0f);
            _adButton.gameObject.SetActive(false);

            _closePanel.transform.localScale = Vector3.zero;
            _closePanel.gameObject.SetActive(true);

            _activeShadow.gameObject.SetActive(false);
            _panelShadow.color = new Color(_panelShadow.color.r, _panelShadow.color.g, _panelShadow.color.b, 0f);
            _blocker.SetActive(true);

            // Set panel scale last
            transform.localScale = Vector3.zero;
        }

        private void Start()
        {
            _buyButton.interactable = _currencyModel.CanSpend(1000);
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
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            _currentAnimation = StartCoroutine(HideAnimation());
        }

        private IEnumerator ShowAnimation()
        {
            _blocker.SetActive(true);

            float panelDuration = 0.3f; // Panel and shadow
            float elementDuration = 0.5f; // Chest, texts, buttons, close button

            // Scale panel and fade shadow
            Coroutine panelCoroutine = StartCoroutine(ScaleCoroutine(transform, Vector3.zero, Vector3.one, panelDuration));
            Coroutine shadowCoroutine = StartCoroutine(FadeImageCoroutine(_panelShadow, 0f, 230f / 255f, panelDuration));

            yield return panelCoroutine;
            yield return shadowCoroutine;

            // Start chest animation
            StartCoroutine(ScaleCoroutine(_chestImage.transform, Vector3.zero, Vector3.one, elementDuration));

            // Start text animation after 0.3s
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(FadeTextCoroutine(_chestText, 0f, 1f, elementDuration));
            StartCoroutine(FadeTextCoroutine(_categoryText, 0f, 1f, elementDuration));

            // Start buttons animation after 0.2s
            yield return new WaitForSeconds(0.2f);
            _buyButton.gameObject.SetActive(true);
            _adButton.gameObject.SetActive(true);
            StartCoroutine(SlideButtonCoroutine(_buyButton.transform, _buyButton.transform.localPosition, _buyButtonInitialPos, elementDuration));
            StartCoroutine(SlideButtonCoroutine(_adButton.transform, _adButton.transform.localPosition, _adButtonInitialPos, elementDuration));

            // Start close button scale after 0.2s
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(ScaleCoroutine(_closePanel.transform, Vector3.zero, _closePanelInitialScale, elementDuration));

            _blocker.SetActive(false);
        }

        private IEnumerator HideAnimation()
        {
            _blocker.SetActive(true);

            float panelDuration = 0.3f; // Panel and shadow
            float elementDuration = 0.5f; // Chest, texts, buttons, close button

            // Scale close button to 0
            Coroutine closeButtonCoroutine = StartCoroutine(ScaleCoroutine(_closePanel.transform, _closePanel.transform.localScale, Vector3.zero, elementDuration));

            // Start buttons slide out after 0.3s
            yield return new WaitForSeconds(0.3f);
            Vector3 buyTargetPos = _buyButtonInitialPos + new Vector3(-Screen.width, 0f, 0f);
            Coroutine buyButtonCoroutine = StartCoroutine(SlideButtonCoroutine(_buyButton.transform, _buyButton.transform.localPosition, buyTargetPos, elementDuration));

            Vector3 adTargetPos = _adButtonInitialPos + new Vector3(Screen.width, 0f, 0f);
            Coroutine adButtonCoroutine = StartCoroutine(SlideButtonCoroutine(_adButton.transform, _adButton.transform.localPosition, adTargetPos, elementDuration));

            // Start text fade out after 0.2s
            yield return new WaitForSeconds(0.2f);
            Coroutine textCoroutine = StartCoroutine(FadeTextCoroutine(_chestText, 1f, 0f, elementDuration));
            Coroutine categoryTextCoroutine = StartCoroutine(FadeTextCoroutine(_categoryText, 1f, 0f, elementDuration));

            // Start chest scale to 0 after 0.2s
            yield return new WaitForSeconds(0.2f);
            Coroutine chestCoroutine = StartCoroutine(ScaleCoroutine(_chestImage.transform, _chestImage.transform.localScale, Vector3.zero, elementDuration));

            // Wait for all animations to complete
            yield return closeButtonCoroutine;
            yield return buyButtonCoroutine;
            yield return adButtonCoroutine;
            yield return textCoroutine;
            yield return categoryTextCoroutine;
            yield return chestCoroutine;

            // Disable buttons
            _buyButton.gameObject.SetActive(false);
            _adButton.gameObject.SetActive(false);

            // Scale panel and fade shadow
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
                _currencyModel.SpendCurrency(1000);
                OpenChest();
            }
        }

        private void WatchAd()
        {
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
            InventorySkinData randomSkin = skinConfig.InventorySkins[UnityEngine.Random.Range(0, skinConfig.InventorySkins.Count)];

            _activeShadow.gameObject.SetActive(true);
            Color startShadowColor = _activeShadow.color;
            startShadowColor.a = 0f;
            _activeShadow.color = startShadowColor;

            Vector3 startChestScale = _chestImage.transform.localScale;
            Vector3 targetChestScale = Vector3.one * 2f;
            Vector3 initialChestPosition = _chestImage.transform.localPosition; // Save initial position
            float elapsed = 0f;
            float duration = 2f;
            float shadowDuration = 1f;

            float shakeFrequency = 10f;
            float shakeAmplitude = 20f; // Max ±20 degrees
            float verticalShakeAmplitude = 10f;
            float horizontalShakeAmplitude = 10f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (elapsed < shadowDuration)
                {
                    float shadowT = elapsed / shadowDuration;
                    Color targetShadowColor = startShadowColor;
                    targetShadowColor.a = Mathf.Lerp(0f, 245f / 255f, shadowT);
                    _activeShadow.color = targetShadowColor;
                }

                _chestImage.transform.localScale = Vector3.Lerp(startChestScale, targetChestScale, t);

                // Shake rotation starts near 0 and ramps up to ±20 degrees
                float currentAmplitude = shakeAmplitude * t; // Linearly increase amplitude
                float shakeZ = Mathf.Sin(elapsed * shakeFrequency) * currentAmplitude;
                float shakeX = Mathf.Sin(elapsed * shakeFrequency * 1.5f) * horizontalShakeAmplitude * t;
                float shakeY = Mathf.Cos(elapsed * shakeFrequency * 0.8f) * verticalShakeAmplitude * t;

                _chestImage.transform.rotation = Quaternion.Euler(0f, 0f, shakeZ);
                _chestImage.transform.localPosition = initialChestPosition + new Vector3(shakeX, shakeY, 0f);

                yield return null;
            }

            _rewardSkinInstance = Instantiate(_rewardSkinPrefab, _rewardParent);
            _rewardSkinInstance.transform.localScale = Vector3.zero;
            _rewardSkinInstance.Setup(randomSkin.Icon);

            float rewardDuration = 1f;
            elapsed = 0f;
            while (elapsed < rewardDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rewardDuration;
                float eased = EaseOutBack(t);
                _rewardSkinInstance.transform.localScale = Vector3.one * 2f * eased;
                yield return null;
            }
            _rewardSkinInstance.transform.localScale = Vector3.one * 2f;

            _chestImage.transform.localScale = startChestScale;
            _chestImage.transform.rotation = Quaternion.identity;
            _chestImage.transform.localPosition = initialChestPosition; // Return to initial position

            _interactiveShadow.interactable = true;
        }

        private void HideReward()
        {
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
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                Color targetShadowColor = startShadowColor;
                targetShadowColor.a = Mathf.Lerp(startShadowColor.a, 0f, t);
                _activeShadow.color = targetShadowColor;
                yield return null;
            }
            _activeShadow.gameObject.SetActive(false);

            if (_rewardSkinInstance == null) yield break;
            Vector3 startRewardScale = _rewardSkinInstance.transform.localScale;
            float rewardDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < rewardDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rewardDuration;
                float eased = EaseInBack(t);
                _rewardSkinInstance.transform.localScale = Vector3.Lerp(startRewardScale, Vector3.zero, eased);
                yield return null;
            }
            _rewardSkinInstance.transform.localScale = Vector3.zero;
            Destroy(_rewardSkinInstance.gameObject);

            _buyButton.interactable = _currencyModel.CanSpend(1000);
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

        private IEnumerator SlideButtonCoroutine(Transform button, Vector3 startPos, Vector3 targetPos, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                button.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            button.localPosition = targetPos;
        }
    }
}