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
        [SerializeField] private TMP_Text _newRecordMessage;
        private IGameStateService _gameStateService;
        private IGameScoreService _gameScoreService;
        private IGameStateMachine _gameStateMachine;

        private Vector3 _initialRecordMessageScale;

        [Inject]
        public void Construct(IGameStateService gameStateService, IGameScoreService gameScoreService, IGameStateMachine gameStateMachine)
        {
            _gameStateMachine = gameStateMachine;
            _gameScoreService = gameScoreService;
            _gameStateService = gameStateService;
            Id = WindowId.GameLoseWindow;
        }

        private void Awake()
        {
            // Сохраняем изначальный масштаб _newRecordMessage
            _initialRecordMessageScale = _newRecordMessage.transform.localScale;
            // Устанавливаем начальный масштаб 0, чтобы подготовить анимацию
            _newRecordMessage.transform.localScale = Vector3.zero;
            _newRecordMessage.gameObject.SetActive(false);
        }

        private void Start()
        {
            _score.text = _gameScoreService.GetScore().ToString();
            
            _restartLevel.onClick.AddListener(RestartLevel);
            _home.onClick.AddListener(EnterMainMenu);
            
            if (_gameScoreService.CheckTheRecord())
            {
                _newRecordMessage.gameObject.SetActive(true);
                StartCoroutine(AnimateRecordMessage());
            }
        }

        private void OnDestroy()
        {
            _restartLevel.onClick.RemoveListener(RestartLevel);
            _home.onClick.RemoveListener(EnterMainMenu);
        }

        private void RestartLevel()
        {
            _gameStateService.RestartLevel();
        }

        private void EnterMainMenu()
        {
            _gameStateMachine.Enter<LoadMainMenuState>();
        }

        private IEnumerator AnimateRecordMessage()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = _initialRecordMessageScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = EaseOutBack(t);
                _newRecordMessage.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
                yield return null;
            }

            _newRecordMessage.transform.localScale = targetScale;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }
    }
}