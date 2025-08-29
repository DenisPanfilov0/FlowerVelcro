using System;
using Code.Gameplay.Services.GameStateService;

namespace Code.Gameplay.Services.Heart
{
    public class HeartService : IDisposable
    {
        private const int HEART_MAX = 1;
        
        private readonly IGameStateService _gameStateService;
        public event Action<int> HeartCountChange; 
        
        private int _heartCount = 1;

        public HeartService(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService;
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