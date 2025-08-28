using System;
using Code.Gameplay.Behaviour.View;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.PlayerFallingService;
using UnityEngine;

namespace Code.Gameplay.Services.PlayerStickingService
{
    public class PlayerStickingService : IPlayerStickingService
    {
        private readonly IPlayerFallingService _playerFallingService;
        public event Action<Flower> PlayerGlued;
        public event Action OnFinishSticking;

        private PlayerView _playerView;
        private Flower _currentStickSlime;
        private bool _isGlued = false;
        private readonly IGameStateService _gameStateService;

        // Минимальная вертикальная дистанция в world units, чтобы можно было прилипнуть
        private const float MinDistanceY = 1.5f;

        public PlayerStickingService(IPlayerFallingService playerFallingService, IGameStateService gameStateService)
        {
            _playerFallingService = playerFallingService;
            _gameStateService = gameStateService;

            _gameStateService.OnGameLose += FinishSticking;
        }

        public void AddPlayer(PlayerView player)
        {
            _playerView = player;
        }

        public void Cleanup()
        {
            _playerView = null;
            _isGlued = false;
        }

        public void SlimeClicked(Flower flower)
        {
            if (!_isGlued && CanStickToSlime(flower))
            {
                _currentStickSlime = flower;
                StickPlayerToSlime(flower);
            }
            else if (_isGlued && CanStickToSlime(flower) && flower != _currentStickSlime)
            {
                _currentStickSlime = flower;
                StickPlayerToSlime(flower);
            }
            else if (flower == _currentStickSlime)
            {
                FinishSticking();
            }
        }

        public void FinishSticking()
        {
            _isGlued = false;
            // _playerFallingService.PlayerFall();
            OnFinishSticking?.Invoke();
        }

        private bool CanStickToSlime(Flower slimeView)
        {
            if (_playerView == null) return false;

            Vector3 playerPos = _playerView.transform.position;
            Vector3 slimePos = slimeView.transform.position;

            // Проверяем, что слайм выше игрока на определённую дистанцию
            return slimePos.y > playerPos.y && (slimePos.y - playerPos.y) >= MinDistanceY;
        }

        private void StickPlayerToSlime(Flower slimeView)
        {
            _isGlued = true;
            _playerFallingService.StopFalling();
            PlayerGlued?.Invoke(slimeView);
        }
    }
}
