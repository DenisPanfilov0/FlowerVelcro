using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class Flower : ItemView
    {
        // [field: SerializeField] public Collider2D _slimeCollider2D { get; private set; }
        [SerializeField] protected bool _isCollected;
        [SerializeField] protected ParticleSystem _particlePrefab;

        public override void Setup(PlayerStickingService playerStickingService, Sprite slimeIcon, 
            HeartService heartService, IGameStateService gameStateService, bool isTutorial)
        {
            base.Setup(playerStickingService, slimeIcon, heartService, gameStateService, isTutorial);
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