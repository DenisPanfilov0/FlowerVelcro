using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.TimerService;
using Code.Infrastructure.StaticData;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Code.Gameplay.Services.SpawnersServices.BombSpawnerService
{
    public class BombSpawnerService : IBombSpawnerService
    {
        private readonly IStaticDataService _staticDataService;
        private readonly ITimerService _timerService;
        private readonly DiContainer _container;
        private readonly IFallManagerService _fallManagerService;
        private readonly IGameStateService _gameStateService;

        private GameObject _bombPrefab;
        private bool _isSpawningActive;

        public BombSpawnerService(IStaticDataService staticDataService, ITimerService timerService,
            DiContainer container, IFallManagerService fallManagerService,
            IGameStateService gameStateService)
        {
            _staticDataService = staticDataService;
            _timerService = timerService;
            _container = container;
            _fallManagerService = fallManagerService;
            _gameStateService = gameStateService;

            _bombPrefab = GetBombPrefab();

            _gameStateService.OnGameLose += StopSpawn;
        }

        public void StartSpawn(Transform spawnZoneTransform)
        {
            _isSpawningActive = true;
            StartSpawnLoop(spawnZoneTransform);
        }

        private void StartSpawnLoop(Transform spawnZoneTransform)
        {
            if (!_isSpawningActive) return;
            
            _timerService.StartTimer(Random.Range(2.1f, 3.5f), () =>
            {
                if (spawnZoneTransform != null)
                {
                    SpawnBomb(spawnZoneTransform);
                    StartSpawnLoop(spawnZoneTransform);
                }
            });
        }

        private void StopSpawn()
        {
            _isSpawningActive = false;
        }

        private void SpawnBomb(Transform spawnZoneTransform)
        {
            if (!_isSpawningActive) return;

            Vector2 spawnPosition = GetRandomSpawnPosition(spawnZoneTransform);
            GameObject bomb = _container.InstantiatePrefab(_bombPrefab, spawnPosition, Quaternion.identity, spawnZoneTransform);

            _fallManagerService.AddFallingObject(bomb);
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

        private GameObject GetBombPrefab() =>
            _staticDataService.GetItemSpawnerPrefab(ItemSpawnerTypeId.Bomb);
    }
}
