using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerStickingService;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class BombView : ItemView
    {
        private bool _isReturningToPool;

        public override void Setup(IPlayerStickingService playerStickingService, Sprite slimeIcon, ItemSpawnerTypeId typeId, IHeartService heartService, IGameStateService gameStateService)
        {
            base.Setup(playerStickingService, slimeIcon, typeId, heartService, gameStateService);
            _isReturningToPool = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isReturningToPool || gameObject == null) return;
            if (other.gameObject.GetComponent<PlayerView>())
            {
                _heartService?.DecreaseHeart();
                _isReturningToPool = true;
                // StartCoroutine(GetComponent<ItemAppearance>().DisappearAnimation(() => 
                // {
                //     if (_spawnerService != null && gameObject != null)
                //     {
                //         _spawnerService.ReturnToPool(this, _typeId);
                //     }
                //     _isReturningToPool = false;
                // }));
                
                _spawnerService.ReturnToPool(this, _typeId);
            }
        }
    }
}