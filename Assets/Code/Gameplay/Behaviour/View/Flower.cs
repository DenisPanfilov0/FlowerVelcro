using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerStickingService;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class Flower : ItemView
    {
        // [field: SerializeField] public Collider2D _slimeCollider2D { get; private set; }
        [SerializeField] protected bool _isCollected;
        [SerializeField] protected ParticleSystem _particlePrefab;

        public override void Setup(IPlayerStickingService playerStickingService, Sprite slimeIcon, ItemSpawnerTypeId typeId, IHeartService heartService, IGameStateService gameStateService)
        {
            base.Setup(playerStickingService, slimeIcon, typeId, heartService, gameStateService);
        }

        public override void Reset()
        {
            base.Reset();
            _isCollected = false;
        }

        private void OnMouseDown()
        {
            if (!_isCollected)
            {
                _playerStickingService.SlimeClicked(this);
            }
        }
        
        public virtual void CloseFlower()
        {
            
        }
    }
}