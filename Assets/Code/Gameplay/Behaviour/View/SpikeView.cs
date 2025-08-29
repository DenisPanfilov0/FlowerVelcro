using Code.Configs.ItemSpawnerConfig;
// using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class SpikeView : ItemView
    {
        public override void Setup(PlayerStickingService playerStickingService, Sprite icon, ItemSpawnerTypeId typeId, 
            HeartService heartService, IGameStateService gameStateService)
        {
            base.Setup(playerStickingService, icon, typeId, heartService, gameStateService);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponent<PlayerView>())
            {
                Rigidbody2D playerRb = other.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null && playerRb.linearVelocity.y < 0 && other.transform.position.y > transform.position.y)
                {
                    _heartService?.DecreaseHeart();
                    _spawnerService.ReturnToPool(this, _typeId);
                }
            }
        }
    }
}