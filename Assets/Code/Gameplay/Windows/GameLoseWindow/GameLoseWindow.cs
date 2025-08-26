using System.Collections;
using Code.Gameplay.Services.GameScoreService;
using Code.Gameplay.Services.GameStateService;
using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Infrastructure.WindowsService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Gameplay.Windows.GameLoseWindow
{
    public class GameLoseWindow : BaseWindow
    {
        [SerializeField] private Button _restartLevel;
        [SerializeField] private Button _home;
        [SerializeField] private TMP_Text _score;
        [SerializeField] private TMP_Text _recordCounter;
        [SerializeField] private TMP_Text _newRecordMessage;
        private IGameStateService _gameStateService;
        private IGameScoreService _gameScoreService;
        private IGameStateMachine _gameStateMachine;
        private AudioManager _audioManager;
        private Vector3 _initialWindowScale;
        private Vector3 _initialScoreScale;
        private Vector3 _initialRestartButtonScale;
        private Vector3 _initialHomeButtonScale;
        private Vector3 _initialRecordCounterScale;
        private Vector3 _initialRecordMessageScale;

        private const float StaggerDelay = 0.2f;

        [Inject]
        public void Construct(IGameStateService gameStateService, IGameScoreService gameScoreService, IGameStateMachine gameStateMachine, AudioManager audioManager)
        {
            _gameStateMachine = gameStateMachine;
            _gameScoreService = gameScoreService;
            _gameStateService = gameStateService;
            _audioManager = audioManager;
            Id = WindowId.GameLoseWindow;
        }

        private void Awake()
        {
            // Сохраняем изначальные масштабы
            _initialWindowScale = transform.localScale;
            _initialScoreScale = _score.transform.localScale;
            _initialRestartButtonScale = _restartLevel.transform.localScale;
            _initialHomeButtonScale = _home.transform.localScale;
            _initialRecordCounterScale = _recordCounter.transform.localScale;
            _initialRecordMessageScale = _newRecordMessage.transform.localScale;

            // Устанавливаем начальный масштаб 0 для всех элементов
            transform.localScale = Vector3.zero;
            _score.transform.localScale = Vector3.zero;
            _restartLevel.transform.localScale = Vector3.zero;
            _home.transform.localScale = Vector3.zero;
            _recordCounter.transform.localScale = Vector3.zero;
            _newRecordMessage.transform.localScale = Vector3.zero;

            // Деактивируем объекты, которые не должны быть видны сразу
            _newRecordMessage.gameObject.SetActive(false);
            _recordCounter.gameObject.SetActive(false);
        }

        private void Start()
        {
            _restartLevel.onClick.AddListener(RestartLevel);
            _home.onClick.AddListener(EnterMainMenu);

            // Запускаем звук EndFly сразу при открытии окна
            _audioManager.PlaySoundEffect(AudioClipTypeId.EndFly);

            // Запускаем анимацию появления
            StartCoroutine(ShowAnimation());
        }

        private void OnDestroy()
        {
            _restartLevel.onClick.RemoveListener(RestartLevel);
            _home.onClick.RemoveListener(EnterMainMenu);
        }

        private IEnumerator ShowAnimation()
        {
            // Анимация главного объекта окна (0.3 секунды)
            StartCoroutine(AnimateElement(transform, Vector3.zero, _initialWindowScale, 0.3f));

            // Плавное появление остальных элементов с задержкой 0.2 секунды между началом анимаций
            yield return new WaitForSeconds(StaggerDelay);
            _score.gameObject.SetActive(true);
            StartCoroutine(AnimateElement(_score.transform, Vector3.zero, _initialScoreScale, 0.3f));

            yield return new WaitForSeconds(StaggerDelay);
            _recordCounter.gameObject.SetActive(true);
            _recordCounter.text = "0";
            StartCoroutine(AnimateElement(_recordCounter.transform, Vector3.zero, _initialRecordCounterScale, 0.3f));
            StartCoroutine(AnimateScoreCounter());

            yield return new WaitForSeconds(StaggerDelay);
            StartCoroutine(AnimateElement(_restartLevel.transform, Vector3.zero, _initialRestartButtonScale, 0.3f));

            yield return new WaitForSeconds(StaggerDelay);
            StartCoroutine(AnimateElement(_home.transform, Vector3.zero, _initialHomeButtonScale, 0.3f));

            // Проверка нового рекорда
            if (_gameScoreService.CheckTheRecord())
            {
                yield return new WaitForSeconds(StaggerDelay);
                _newRecordMessage.gameObject.SetActive(true);
                _audioManager.PlaySoundEffect(AudioClipTypeId.NewRecord);
                yield return StartCoroutine(AnimateElement(_newRecordMessage.transform, Vector3.zero, _initialRecordMessageScale, 0.3f));
            }
        }

        private IEnumerator AnimateElement(Transform target, Vector3 startScale, Vector3 targetScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = EaseOutBack(t);
                target.localScale = Vector3.Lerp(startScale, targetScale, eased);
                yield return null;
            }
            target.localScale = targetScale;
        }

        private IEnumerator AnimateScoreCounter()
        {
            float duration = 0.6f;
            float elapsed = 0f;
            int targetScore = _gameScoreService.GetScore();
            int startScore = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = EaseOutQuad(t);
                int currentScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, eased));
                _recordCounter.text = currentScore.ToString();
                _score.text = currentScore.ToString();
                yield return null;
            }

            _recordCounter.text = targetScore.ToString();
            _score.text = targetScore.ToString();
        }

        private void RestartLevel()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _gameStateService.RestartLevel();
        }

        private void EnterMainMenu()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _gameStateMachine.Enter<LoadMainMenuState>();
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        private float EaseOutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }
    }
}