using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using DG.Tweening;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class BombView : ItemView
    {
        private bool _isReturningToPool;

        public override void Setup(PlayerStickingService playerStickingService, Sprite slimeIcon, 
            HeartService heartService, IGameStateService gameStateService, bool isTutorial)
        {
            base.Setup(playerStickingService, slimeIcon, heartService, gameStateService, isTutorial);
            _isReturningToPool = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isReturningToPool || gameObject == null) return;
            if (other.gameObject.GetComponent<PlayerView>())
            {
                _heartService?.DecreaseHeart();
                _isReturningToPool = true;

                if (!_isTutorial)
                {
                    _spawnerService.ReturnToPool(this, _typeId);
                }
                else
                {
                    // Анимация уменьшения масштаба с эффектом баунс
                    transform.DOScale(Vector3.zero, 0.5f)
                        .SetEase(Ease.OutBounce);
                }
            }
        }
    }
}