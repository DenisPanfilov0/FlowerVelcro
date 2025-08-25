using System;
using Code.Gameplay.Windows;
using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Infrastructure.WindowsService;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Services.GameStateService
{
    public class IGameStateService : IInitializable, ITickable
    {
        public event Action<float> OnGameSpeedChange; 
        public float GameSpeed { get; set; }
        public bool IsGameStop { get; set; }

        public event Action OnGameLose;
        
        private readonly IWindowService _windowService;
        private readonly IGameStateMachine _gameStateMachine;

        public IGameStateService(IWindowService windowService, IGameStateMachine gameStateMachine)
        {
            _windowService = windowService;
            _gameStateMachine = gameStateMachine;
        }

        public void Initialize()
        {
            GameSpeed = 1;
            IsGameStop = true;
        }

        public void GameStart()
        {
            IsGameStop = false;
        }

        public void GameLose()
        {
            IsGameStop = true;
            GameSpeed = 1f;
            OnGameLose?.Invoke();
            _windowService.Open(WindowId.GameLoseWindow);
        }

        public void RestartLevel()
        {
            GameSpeed = 1f;
            _gameStateMachine.Enter<RestartLevelState>();
        }

        public void Tick()
        {
            if (IsGameStop) return;
            
            GameSpeed += Time.deltaTime / 100f;
        }
    }
}