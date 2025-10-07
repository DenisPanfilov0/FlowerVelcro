using System.Collections;
using System.Collections.Generic;
using Code.Progress.Data;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Tutorial
{
    public class EndTutorial : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private List<Image> _images;
        [SerializeField] private List<DialogWindowController> _dialogControllers;
        [SerializeField] private List<HandAnimation> _handAnimations;
        [SerializeField] private List<float> _transitionDelays; // List of delays for each step
        [SerializeField] private ShadowController _shadowController;
        [SerializeField] private Image _shadowImage;
        private ProgressData _progressData;
        private AudioManager _audioManager;
        private int _currentImageIndex = 0;
        private float _initialShadowAlpha;

        [Inject]
        public void Construct(ProgressData progressData, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _progressData = progressData;
        }
        
        private void Start()
        {
            // Store initial shadow alpha
            _initialShadowAlpha = _shadowImage != null ? _shadowImage.color.a : 1f;

            // Инициализация анимаций рук
            foreach (var hand in _handAnimations)
            {
                hand.Initialize();
            }
            
            // Подписка на событие
            _shadowController.OnTransparentAreaReleased += HandleTransparentAreaReleased;
            
            // Начальная настройка
            UpdateTutorialState();
        }

        private void OnDestroy()
        {
            // Отписка от события
            _shadowController.OnTransparentAreaReleased -= HandleTransparentAreaReleased;

            // Остановка всех корутин анимации
            foreach (var hand in _handAnimations)
            {
                hand.SetActive(false);
            }
        }

        private void TutorialEnded()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _progressData.SetTutorialChecked(true);
            Destroy(gameObject);
        }

        private void UpdateTutorialState()
        {
            // Проверка на валидность списков
            if (_images == null || _images.Count == 0 || 
                _dialogControllers == null || _dialogControllers.Count == 0 || 
                _handAnimations == null || _handAnimations.Count == 0 ||
                _transitionDelays == null || _transitionDelays.Count == 0)
            {
                Debug.LogWarning("One or more lists are empty or null.");
                return;
            }

            // Отключение всех диалоговых окон
            foreach (var dialog in _dialogControllers)
            {
                if (dialog != null)
                {
                    dialog.gameObject.SetActive(false);
                }
            }

            // Отключение всех анимаций рук
            foreach (var hand in _handAnimations)
            {
                hand.SetActive(false);
            }

            // Активация текущих элементов
            if (_currentImageIndex < _images.Count && _images[_currentImageIndex] != null)
            {
                _shadowController.RedrawShadowWithImageSafe(_images[_currentImageIndex]);
            }
            else
            {
                Debug.LogWarning($"Image at index {_currentImageIndex} is null or out of range.");
            }

            if (_currentImageIndex < _dialogControllers.Count && _dialogControllers[_currentImageIndex] != null)
            {
                _dialogControllers[_currentImageIndex].gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Dialog window at index {_currentImageIndex} is null or out of range.");
            }

            if (_currentImageIndex < _handAnimations.Count)
            {
                _handAnimations[_currentImageIndex].SetActive(true);
                _handAnimations[_currentImageIndex].Animate(this);
            }
            else
            {
                Debug.LogWarning($"Hand animation at index {_currentImageIndex} is out of range.");
            }
        }

        private void HandleTransparentAreaReleased()
        {
            StartCoroutine(TransitionToNext());
        }

        private IEnumerator TransitionToNext()
        {
            // Stop current hand animation
            if (_currentImageIndex < _handAnimations.Count)
            {
                _handAnimations[_currentImageIndex].StopAnimate(this);
            }

            // Fade out current elements
            Coroutine fadeHand = null;
            if (_currentImageIndex < _handAnimations.Count)
            {
                fadeHand = StartCoroutine(_handAnimations[_currentImageIndex].FadeAlpha(1f, 0f, 0.5f));
            }
            Coroutine fadeDialog = null;
            if (_currentImageIndex < _dialogControllers.Count)
            {
                fadeDialog = StartCoroutine(_dialogControllers[_currentImageIndex].FadeAlpha(1f, 0f, 0.5f));
            }
            Coroutine fadeShadowOut = StartCoroutine(FadeShadow(1f, 0.01f, 0.5f));

            // Wait for all fade out coroutines to complete
            if (fadeHand != null) yield return fadeHand;
            if (fadeDialog != null) yield return fadeDialog;
            yield return fadeShadowOut;

            // Deactivate current hand and dialog
            if (_currentImageIndex < _handAnimations.Count)
            {
                _handAnimations[_currentImageIndex].SetActive(false);
            }
            if (_currentImageIndex < _dialogControllers.Count)
            {
                _dialogControllers[_currentImageIndex].gameObject.SetActive(false);
            }

            // Disable raycasting
            _shadowController.SetRaycastTarget(false);

            // Fill shadow texture to be fully opaque (no hole)
            _shadowController.FillShadowTextureFullyOpaque();

            // Increment index
            _currentImageIndex++;

            // Check if tutorial ended
            if (_currentImageIndex >= _images.Count || 
                _currentImageIndex >= _dialogControllers.Count || 
                _currentImageIndex >= _handAnimations.Count)
            {
                TutorialEnded();
                yield break;
            }

            // Wait for the specified delay
            float delay = _currentImageIndex < _transitionDelays.Count ? _transitionDelays[_currentImageIndex] : 0f;
            yield return new WaitForSeconds(delay);

            // Prepare for redraw with initial alpha
            Color shadowColor = _shadowImage.color;
            _shadowImage.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, _initialShadowAlpha);

            // Redraw shadow with new image
            if (_currentImageIndex < _images.Count && _images[_currentImageIndex] != null)
            {
                _shadowController.RedrawShadowWithImageSafe(_images[_currentImageIndex]);
            }

            // Set shadow alpha to low for fade in
            SetShadowAlpha(0.01f);

            // Activate new dialog and hand with alpha 0
            if (_currentImageIndex < _dialogControllers.Count && _dialogControllers[_currentImageIndex] != null)
            {
                _dialogControllers[_currentImageIndex].gameObject.SetActive(true);
                _dialogControllers[_currentImageIndex].SetAlpha(0f);
            }
            if (_currentImageIndex < _handAnimations.Count)
            {
                _handAnimations[_currentImageIndex].SetActive(true);
                _handAnimations[_currentImageIndex].SetAlpha(0f);
            }

            // Enable raycasting
            _shadowController.SetRaycastTarget(true);

            // Fade in new elements
            Coroutine fadeHandIn = null;
            if (_currentImageIndex < _handAnimations.Count)
            {
                fadeHandIn = StartCoroutine(_handAnimations[_currentImageIndex].FadeAlpha(0f, 1f, 0.5f));
            }
            Coroutine fadeDialogIn = null;
            if (_currentImageIndex < _dialogControllers.Count)
            {
                fadeDialogIn = StartCoroutine(_dialogControllers[_currentImageIndex].FadeAlpha(0f, 1f, 0.5f));
            }
            Coroutine fadeShadowIn = StartCoroutine(FadeShadow(0.01f, _initialShadowAlpha, 0.5f));

            // Wait for all fade in coroutines to complete
            if (fadeHandIn != null) yield return fadeHandIn;
            if (fadeDialogIn != null) yield return fadeDialogIn;
            yield return fadeShadowIn;

            // Start hand animation
            if (_currentImageIndex < _handAnimations.Count)
            {
                _handAnimations[_currentImageIndex].Animate(this);
            }
        }

        private void SetShadowAlpha(float alpha)
        {
            if (_shadowImage == null) return;

            Color color = _shadowImage.color;
            color.a = alpha;
            _shadowImage.color = color;
        }

        private IEnumerator FadeShadow(float from, float to, float duration)
        {
            if (_shadowImage == null) yield break;

            float time = 0f;
            Color color = _shadowImage.color;
            color.a = from;
            _shadowImage.color = color;

            while (time < duration)
            {
                time += Time.deltaTime;
                color.a = Mathf.Lerp(from, to, time / duration);
                _shadowImage.color = color;
                yield return null;
            }

            color.a = to;
            _shadowImage.color = color;
        }
    }
}