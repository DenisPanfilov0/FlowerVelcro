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
        [SerializeField] private List<GameObject> _dialogWindows;
        [SerializeField] private List<HandAnimation> _handAnimations;
        [SerializeField] private ShadowController _shadowController;
        private ProgressData _progressData;
        private AudioManager _audioManager;
        private int _currentImageIndex = 0;

        [Inject]
        public void Construct(ProgressData progressData, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _progressData = progressData;
        }
        
        private void Start()
        {
            // _shadowController.Setup();
            //
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
                _dialogWindows == null || _dialogWindows.Count == 0 || 
                _handAnimations == null || _handAnimations.Count == 0)
            {
                Debug.LogWarning("One or more lists are empty or null.");
                return;
            }

            // Отключение всех диалоговых окон
            foreach (var dialog in _dialogWindows)
            {
                if (dialog != null)
                {
                    dialog.SetActive(false);
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

            if (_currentImageIndex < _dialogWindows.Count && _dialogWindows[_currentImageIndex] != null)
            {
                _dialogWindows[_currentImageIndex].SetActive(true);
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
            // Увеличение индекса
            _currentImageIndex++;

            // Проверка на завершение туториала
            if (_currentImageIndex >= _images.Count || 
                _currentImageIndex >= _dialogWindows.Count || 
                _currentImageIndex >= _handAnimations.Count)
            {
                TutorialEnded();
                return;
            }

            // Обновление состояния туториала
            UpdateTutorialState();
        }
    }
}