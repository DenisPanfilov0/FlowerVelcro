using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Gameplay.Services.HeartService;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class SlimeView : Flower
    {
        // // [field: SerializeField] public Collider2D _slimeCollider2D { get; private set; }
        // [SerializeField] private bool _isCollected;
        // [SerializeField] private ParticleSystem _particlePrefab;
        //
        // public override void Setup(IPlayerStickingService playerStickingService, Sprite slimeIcon, ItemSpawnerTypeId typeId, IHeartService heartService, IGameStateService gameStateService)
        // {
        //     base.Setup(playerStickingService, slimeIcon, typeId, heartService, gameStateService);
        // }
        //
        // public override void Reset()
        // {
        //     base.Reset();
        //     _isCollected = false;
        // }
        //
        // private void OnMouseDown()
        // {
        //     if (!_isCollected)
        //     {
        //         _playerStickingService.SlimeClicked(this);
        //     }
        // }
        //
        // // public void OnClicked()
        // // {
        // //     if (!_isCollected)
        // //     {
        // //         _playerStickingService.SlimeClicked(this);
        // //     }
        // // }

        public override void CloseFlower()
        {
            _icon.color = Color.gray;
            _isCollected = true;
            // StartCoroutine(GetComponent<ItemAppearance>().DisappearAnimation(() => _spawnerService?.ReturnToPool(this, _typeId)));
            _spawnerService?.ReturnToPool(this, _typeId);
            ParticleSystem particle = Instantiate(_particlePrefab, transform.parent);
            particle.transform.position = transform.position;
        }
    }
}