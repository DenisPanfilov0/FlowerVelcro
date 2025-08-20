using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerStickingService;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class SpikeView : ItemView
    {
        public override void Setup(IPlayerStickingService playerStickingService, Sprite icon, ItemSpawnerTypeId typeId, IHeartService heartService, IGameStateService gameStateService)
        {
            base.Setup(playerStickingService, icon, typeId, heartService, gameStateService);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponent<PlayerView>())
            {
                if (other.transform.position.y > transform.position.y)
                {
                    _heartService?.DecreaseHeart();
                    // StartCoroutine(GetComponent<ItemAppearance>().DisappearAnimation(() => _spawnerService?.ReturnToPool(this, _typeId)));
                    _spawnerService.ReturnToPool(this, _typeId);
                }
            }
        }
    }
}