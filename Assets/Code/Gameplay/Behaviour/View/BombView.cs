using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class BombView : ItemView
    {
        private bool _isReturningToPool;

        public override void Setup(PlayerStickingService playerStickingService, Sprite slimeIcon, ItemSpawnerTypeId typeId, 
            HeartService heartService, IGameStateService gameStateService)
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