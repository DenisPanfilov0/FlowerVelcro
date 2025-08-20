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
        private IGameStateService _gameStateService;
        private IGameScoreService _gameScoreService;
        private IGameStateMachine _gameStateMachine;

        [Inject]
        public void Construct(IGameStateService gameStateService, IGameScoreService gameScoreService, IGameStateMachine gameStateMachine)
        {
            _gameStateMachine = gameStateMachine;
            _gameScoreService = gameScoreService;
            _gameStateService = gameStateService;
            Id = WindowId.GameLoseWindow;
        }

        private void Start()
        {
            _score.text = _gameScoreService.GetScore().ToString();
            
            _restartLevel.onClick.AddListener(RestartLevel);
            _home.onClick.AddListener(EnterMainMenu);
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
    }
}