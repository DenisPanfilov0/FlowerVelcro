using System;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.TimerService;
using Code.Inventory;
using Code.Progress.Data;

namespace Code.Gameplay.Services.GameScoreService
{
    public class GameScoreService : IGameScoreService
    {
        public event Action<int> ScoreChange;
        
        private readonly ITimerService _timerService;
        private readonly IGameStateService _gameStateService;
        private readonly ProgressData _progress;
        private readonly CurrencyModel _currencyModel;
        private int _score = 0;
        private bool _isGameStop = false;
        private bool _isNewRecord = false;

        public GameScoreService(ITimerService timerService, IGameStateService gameStateService, ProgressData progress, CurrencyModel currencyModel)
        {
            _gameStateService = gameStateService;
            _progress = progress;
            _currencyModel = currencyModel;
            _timerService = timerService;

            _gameStateService.OnGameLose += () =>
            {
                _isGameStop = true;
                
                _currencyModel.AddCurrency(_score);
                
                if (_progress.MaxScore < _score)
                {
                    // _progress.ProgressData.MaxScore = _score;
                    _progress.ChangeMaxScore(_score);
                    _isNewRecord = true;
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
            _isNewRecord = false;
        }

        public void IncreaseScore()
        {
            _score++;
            
            ScoreChange?.Invoke(_score);
            // ScoreUpdate();
        }

        public int GetScore()
        {
            return _score;
        }

        public bool CheckTheRecord()
        {
            return _isNewRecord;
        }
    }
}