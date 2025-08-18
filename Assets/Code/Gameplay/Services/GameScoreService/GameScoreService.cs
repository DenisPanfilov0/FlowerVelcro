using System;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.TimerService;
using Code.Progress.Provider;

namespace Code.Gameplay.Services.GameScoreService
{
    public class GameScoreService : IGameScoreService
    {
        public event Action<int> ScoreChange;
        
        private readonly ITimerService _timerService;
        private readonly IGameStateService _gameStateService;
        private readonly IProgressProvider _progress;
        private int _score = 0;
        private bool _isGameStop = false;

        public GameScoreService(ITimerService timerService, IGameStateService gameStateService, IProgressProvider progress)
        {
            _gameStateService = gameStateService;
            _progress = progress;
            _timerService = timerService;

            _gameStateService.OnGameLose += () =>
            {
                _isGameStop = true;
                
                if (_progress.ProgressData.MaxScore < _score)
                {
                    _progress.ProgressData.MaxScore = _score;
                }
            };
        }

        // public void ScoreUpdate()
        // {
        //     if (!_isGameStop)
        //     {
        //         _timerService.StartTimer(0.5f, IncreaseScore);
        //     }
        // }

        public void Cleanup()
        {
            _score = 0;
            _isGameStop = false;
        }

        public void IncreaseScore()
        {
            _score++;
            
            ScoreChange?.Invoke(_score);
            // ScoreUpdate();
        }
    }
}