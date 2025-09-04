using System;
using Code.Gameplay.Services.GameStateService;
using Code.Inventory;
using Code.Leaderboards;
using Code.Progress.Data;

namespace Code.Gameplay.Services.GameScore
{
    public class GameScoreService
    {
        public event Action<int> ScoreChange;
        
        private readonly IGameStateService _gameStateService;
        private readonly ProgressData _progress;
        private readonly CurrencyModel _currencyModel;
        private readonly LeaderBoardModel _leaderBoardModel;
        private int _score = 0;
        // private bool _isGameStop = false;
        private bool _isNewRecord = false;

        public GameScoreService(IGameStateService gameStateService, ProgressData progress, 
            CurrencyModel currencyModel, LeaderBoardModel leaderBoardModel)
        {
            _gameStateService = gameStateService;
            _progress = progress;
            _currencyModel = currencyModel;
            _leaderBoardModel = leaderBoardModel;

            _gameStateService.OnGameLose += () =>
            {
                // _isGameStop = true;
                        
                _currencyModel.AddCurrency(_score);
                _progress.TotalPollenCollected += _score;
                _progress.TotalGamesPlayed++;
                
                if (_progress.MaxScore < _score)
                {
                    // _progress.ProgressData.MaxScore = _score;
                    _progress.ChangeMaxScore(_score);
                    _leaderBoardModel.SetLeaderboard(LeaderBoardType.FVBestRecordAllTime, _score);
                    _isNewRecord = true;
                }
            };
        }

        public void MultiplyReward()
        {
            _currencyModel.AddCurrency(_score * 2);
        }

        public void Dispose()
        {
            // _score = 0;
            ResetScore();
            // _isGameStop = false;
            _isNewRecord = false;
        }

        public void IncreaseScore(int value)
        {
            _score += value;
            
            ScoreChange?.Invoke(_score);
            // ScoreUpdate();
        }

        public void ResetScore()
        {
            _score = 0;
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