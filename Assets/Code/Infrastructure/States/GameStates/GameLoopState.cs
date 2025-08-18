using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameScoreService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerFallingService;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Gameplay.Services.SpawnersServices.BombSpawnerService;
using Code.Gameplay.Services.SpawnersServices.HeartSpawnerService;
using Code.Gameplay.Services.SpawnersServices.SlimeSpawnerService;
using Code.Infrastructure.States.StateInfrastructure;

namespace Code.Infrastructure.States.GameStates
{
    public class GameLoopState : IState
    {
        private IFallManagerService _fallManagerService;
        private readonly IPlayerStickingService _playerStickingService;
        private readonly IGameScoreService _gameScoreService;
        private readonly IHeartService _heartService;
        private readonly IPlayerFallingService _playerFallingService;

        public GameLoopState(IFallManagerService fallManagerService, IPlayerStickingService playerStickingService,
            IGameScoreService gameScoreService, IHeartService heartService, IPlayerFallingService playerFallingService)
        {
            _fallManagerService = fallManagerService;
            _playerStickingService = playerStickingService;
            _gameScoreService = gameScoreService;
            _heartService = heartService;
            _playerFallingService = playerFallingService;
        }
        
        public void Enter()
        {
            // _gameScoreService.ScoreUpdate();
        }

        public void Exit()
        {
            _fallManagerService.Cleanup();
            _playerStickingService.Cleanup();
            _gameScoreService.Cleanup();
            _heartService.Cleanup();
            _playerFallingService.Cleanup();
        }
    }
}