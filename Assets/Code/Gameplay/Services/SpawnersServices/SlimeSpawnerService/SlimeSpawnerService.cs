using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Behaviour.View;
using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Gameplay.Services.TimerService;
using Code.Infrastructure.StaticData;
using Code.Inventory;
using Code.Progress.Provider;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Code.Gameplay.Services.SpawnersServices.SlimeSpawnerService
{
    public class SlimeSpawnerService : ISlimeSpawnerService
    {
        private readonly IStaticDataService _staticDataService;
        private readonly IProgressProvider _progress;
        private readonly ITimerService _timerService;
        private readonly DiContainer _container;
        private readonly IFallManagerService _fallManagerService;
        private readonly IGameStateService _gameStateService;
        private readonly InventoryModel _inventoryModel;
        private readonly IPlayerStickingService _playerStickingService;

        private SlimeView _slimePrefab;
        private Sprite _slimeIcon;
        private bool _isSpawningActive;

        public SlimeSpawnerService(IStaticDataService staticDataService, ITimerService timerService, IProgressProvider progress,
            DiContainer container, IFallManagerService fallManagerService, IGameStateService gameStateService, 
            InventoryModel inventoryModel, IPlayerStickingService playerStickingService)
        {
            _staticDataService = staticDataService;
            _timerService = timerService;
            _progress = progress;
            _container = container;
            _fallManagerService = fallManagerService;
            _gameStateService = gameStateService;
            _inventoryModel = inventoryModel;
            _playerStickingService = playerStickingService;

            _slimePrefab = GetSlimePrefab();
            _slimeIcon = _inventoryModel.GetSkin(InventoryCategoryType.Flowers);
            
            _gameStateService.OnGameLose += StopSpawn;
        }

        public void StartSpawn(Transform spawnZoneTransform)
        {
            _isSpawningActive = true;
            StartSpawnLoop(spawnZoneTransform);
        }

        private void StopSpawn()
        {
            _isSpawningActive = false;
        }

        private void StartSpawnLoop(Transform spawnZoneTransform)
        {
            if (!_isSpawningActive) return;

            _timerService.StartTimer(Random.Range(1f, 1.7f), () =>
            {
                if (spawnZoneTransform != null)
                {
                    SpawnSlime(spawnZoneTransform);
                    StartSpawnLoop(spawnZoneTransform);
                }
            });

        }

        private void SpawnSlime(Transform spawnZoneTransform)
        {
            if (!_isSpawningActive) return;

            Vector2 spawnPosition = GetRandomSpawnPosition(spawnZoneTransform);
            SlimeView slime = Object.Instantiate(_slimePrefab, spawnPosition, Quaternion.identity, spawnZoneTransform);
            slime.Setup(_playerStickingService, _slimeIcon);

            // _fallManagerService.AddFallingObject(slime);
        }
        
        private Vector2 GetRandomSpawnPosition(Transform spawnZoneTransform)
        {
            Vector3 spawnZoneSize = spawnZoneTransform.GetComponent<BoxCollider2D>().size; // Предполагается, что у SpawnZone есть BoxCollider2D
            // Vector3 spawnZoneSize = new Vector3(spawnZoneTransform.position.x, spawnZoneTransform.position.y, spawnZoneTransform.position.z);
            Vector3 spawnZonePosition = spawnZoneTransform.position;

            float minX = spawnZonePosition.x - spawnZoneSize.x / 2;
            float maxX = spawnZonePosition.x + spawnZoneSize.x / 2;
            float minY = spawnZonePosition.y + spawnZoneSize.y / 2; // Спавн сверху зоны
            float maxY = spawnZonePosition.y + spawnZoneSize.y / 2;

            return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        }

        private SlimeView GetSlimePrefab()
        {
            GameObject prefab = _staticDataService.GetItemSpawnerPrefab(ItemSpawnerTypeId.Slime);
            SlimeView slimeView = prefab.GetComponent<SlimeView>();
            return slimeView != null ? slimeView : null;
        }
    }
}