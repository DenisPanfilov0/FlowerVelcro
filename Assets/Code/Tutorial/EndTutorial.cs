using System.Collections;
using System.Collections.Generic;
using Code.Progress.Data;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Tutorial
{
    [System.Serializable]
    public class TutorialStage
    {
        public List<Image> Images = new();
        public List<DialogWindowController> DialogControllers = new();
        public List<HandAnimation> HandAnimations = new();
        public List<float> TransitionDelays = new();
    }

    public class EndTutorial : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private List<TutorialStage> _stages = new(); // Все стадии
        [SerializeField] private ShadowController _shadowController;
        [SerializeField] private Image _shadowImage;

        private ProgressData _progressData;
        private AudioManager _audioManager;
        private float _initialShadowAlpha;

        private int _currentStageIndex = -1;
        private int _currentStepIndex = 0;

        [Inject]
        public void Construct(ProgressData progressData, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _progressData = progressData;
        }

        private void Start()
        {
            _initialShadowAlpha = _shadowImage != null ? _shadowImage.color.a : 1f;

            // Инициализация всех рук
            foreach (var stage in _stages)
            {
                foreach (var hand in stage.HandAnimations)
                    hand.Initialize();
            }

            _shadowController.OnTransparentAreaReleased += HandleTransparentAreaReleased;
        }

        private void OnDestroy()
        {
            _shadowController.OnTransparentAreaReleased -= HandleTransparentAreaReleased;

            foreach (var stage in _stages)
            {
                foreach (var hand in stage.HandAnimations)
                    hand.SetActive(false);
            }
        }

        /// <summary>
        /// Запускает проигрывание конкретной стадии.
        /// </summary>
        public void PlayStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= _stages.Count)
            {
                Debug.LogWarning($"Stage index {stageIndex} out of range.");
                return;
            }

            _currentStageIndex = stageIndex;
            _currentStepIndex = 0;
            UpdateTutorialState();
        }

        private void TutorialEnded()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _progressData.SetTutorialChecked(true);
            Destroy(gameObject);
        }

        private void HandleTransparentAreaReleased()
        {
            if (_currentStageIndex < 0 || _currentStageIndex >= _stages.Count) return;
            StartCoroutine(TransitionToNext());
        }

        private void UpdateTutorialState()
        {
            var stage = _stages[_currentStageIndex];

            // Выключаем все диалоги и руки этой стадии
            foreach (var dialog in stage.DialogControllers)
                dialog?.gameObject.SetActive(false);

            foreach (var hand in stage.HandAnimations)
                hand.SetActive(false);

            // Активируем текущие элементы
            if (_currentStepIndex < stage.Images.Count && stage.Images[_currentStepIndex] != null)
                _shadowController.RedrawShadowWithImageSafe(stage.Images[_currentStepIndex]);

            if (_currentStepIndex < stage.DialogControllers.Count && stage.DialogControllers[_currentStepIndex] != null)
                stage.DialogControllers[_currentStepIndex].gameObject.SetActive(true);

            if (_currentStepIndex < stage.HandAnimations.Count)
            {
                var hand = stage.HandAnimations[_currentStepIndex];
                hand.SetActive(true);
                hand.Animate(this);
            }
        }

        private IEnumerator TransitionToNext()
        {
            var stage = _stages[_currentStageIndex];

            // Останавливаем текущую анимацию руки
            if (_currentStepIndex < stage.HandAnimations.Count)
                stage.HandAnimations[_currentStepIndex].StopAnimate(this);

            // Фейд аут
            Coroutine fadeHand = null;
            Coroutine fadeDialog = null;
            Coroutine fadeShadowOut = StartCoroutine(FadeShadow(1f, 0.01f, 0.5f));

            if (_currentStepIndex < stage.HandAnimations.Count)
                fadeHand = StartCoroutine(stage.HandAnimations[_currentStepIndex].FadeAlpha(1f, 0f, 0.5f));

            if (_currentStepIndex < stage.DialogControllers.Count)
                fadeDialog = StartCoroutine(stage.DialogControllers[_currentStepIndex].FadeAlpha(1f, 0f, 0.5f));

            if (fadeHand != null) yield return fadeHand;
            if (fadeDialog != null) yield return fadeDialog;
            yield return fadeShadowOut;

            // Деактивируем старые
            if (_currentStepIndex < stage.HandAnimations.Count)
                stage.HandAnimations[_currentStepIndex].SetActive(false);

            if (_currentStepIndex < stage.DialogControllers.Count)
                stage.DialogControllers[_currentStepIndex].gameObject.SetActive(false);

            _shadowController.SetRaycastTarget(false);
            _shadowController.FillShadowTextureFullyOpaque();

            _currentStepIndex++;

            // Проверяем конец стадии
            if (_currentStepIndex >= stage.Images.Count ||
                _currentStepIndex >= stage.DialogControllers.Count ||
                _currentStepIndex >= stage.HandAnimations.Count)
            {
                gameObject.SetActive(false);
                Debug.Log($"Stage {_currentStageIndex} finished.");
                yield break;
            }

            // Задержка перед следующим шагом
            float delay = _currentStepIndex < stage.TransitionDelays.Count ? stage.TransitionDelays[_currentStepIndex] : 0f;
            yield return new WaitForSeconds(delay);

            // Обновляем тень
            Color shadowColor = _shadowImage.color;
            _shadowImage.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, _initialShadowAlpha);

            if (_currentStepIndex < stage.Images.Count && stage.Images[_currentStepIndex] != null)
                _shadowController.RedrawShadowWithImageSafe(stage.Images[_currentStepIndex]);

            SetShadowAlpha(0.01f);

            // Активируем новые элементы
            if (_currentStepIndex < stage.DialogControllers.Count)
            {
                stage.DialogControllers[_currentStepIndex].gameObject.SetActive(true);
                stage.DialogControllers[_currentStepIndex].SetAlpha(0f);
            }

            if (_currentStepIndex < stage.HandAnimations.Count)
            {
                stage.HandAnimations[_currentStepIndex].SetActive(true);
                stage.HandAnimations[_currentStepIndex].SetAlpha(0f);
            }

            _shadowController.SetRaycastTarget(true);

            Coroutine fadeHandIn = null;
            Coroutine fadeDialogIn = null;
            Coroutine fadeShadowIn = StartCoroutine(FadeShadow(0.01f, _initialShadowAlpha, 0.5f));

            if (_currentStepIndex < stage.HandAnimations.Count)
                fadeHandIn = StartCoroutine(stage.HandAnimations[_currentStepIndex].FadeAlpha(0f, 1f, 0.5f));

            if (_currentStepIndex < stage.DialogControllers.Count)
                fadeDialogIn = StartCoroutine(stage.DialogControllers[_currentStepIndex].FadeAlpha(0f, 1f, 0.5f));

            if (fadeHandIn != null) yield return fadeHandIn;
            if (fadeDialogIn != null) yield return fadeDialogIn;
            yield return fadeShadowIn;

            if (_currentStepIndex < stage.HandAnimations.Count)
                stage.HandAnimations[_currentStepIndex].Animate(this);
        }

        private void SetShadowAlpha(float alpha)
        {
            if (_shadowImage == null) return;
            var color = _shadowImage.color;
            color.a = alpha;
            _shadowImage.color = color;
        }

        private IEnumerator FadeShadow(float from, float to, float duration)
        {
            if (_shadowImage == null) yield break;

            float time = 0f;
            var color = _shadowImage.color;
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
