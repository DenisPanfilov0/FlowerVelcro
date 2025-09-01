using System;
using Code.Gameplay.Services.GameStateService;
using Code.Progress.Data;

namespace Code.Gameplay.Services.Heart
{
    public class HeartService : IDisposable
    {
        private const int HEART_MAX = 1;
        
        private readonly IGameStateService _gameStateService;
        private readonly ProgressData _progressData;
        public event Action<int> HeartCountChange;
        public event Action HeartDecrease;
        
        private int _heartCount = 1;

        public HeartService(IGameStateService gameStateService, ProgressData progressData)
        {
            _gameStateService = gameStateService;
            _progressData = progressData;
        }

        public int GetCountHeart() => 
            _heartCount;

        public void IncreaseHeart()
        {
            if (_heartCount < HEART_MAX)
            {
                _heartCount++;
                HeartCountChange?.Invoke(_heartCount);
            }
        }

        public void DecreaseHeart()
        {
            if (!_progressData.IsTutorialChecked)
            {
                HeartDecrease?.Invoke();
                return;
            }
            
            if (_heartCount >= HEART_MAX)
            {
                _heartCount--;
                HeartCountChange?.Invoke(_heartCount);
            }

            if (_heartCount <= 0)
            {
                GameLose();
            }
        }

        public void Dispose()
        {
            _heartCount = HEART_MAX;
        }

        private void GameLose()
        {
            _gameStateService.GameLose();
        }
    }
}