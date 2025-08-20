using System;
using Code.Gameplay.Services.GameStateService;

namespace Code.Gameplay.Services.HeartService
{
    public class HeartService : IHeartService
    {
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
            if (_heartCount < 3)
            {
                _heartCount++;
                HeartCountChange?.Invoke(_heartCount);
            }
        }

        public void DecreaseHeart()
        {
            if (_heartCount >= 1)
            {
                _heartCount--;
                HeartCountChange?.Invoke(_heartCount);
            }

            if (_heartCount <= 0)
            {
                GameLose();
            }
        }

        public void Cleanup()
        {
            _heartCount = 1;
        }

        private void GameLose()
        {
            _gameStateService.GameLose();
        }
    }
}