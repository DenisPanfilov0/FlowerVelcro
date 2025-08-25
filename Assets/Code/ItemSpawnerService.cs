using System;
using System.Collections.Generic;
using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Behaviour.View;
using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Infrastructure.StaticData;
using Code.Inventory;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Code.Gameplay.Services.SpawnersServices
{
    [Serializable]
    public class ItemSpawnConfig
    {
        public ItemSpawnerTypeId TypeId;
        public InventoryCategoryType CategoryType;
        public Vector2 Size;
    }

    [Serializable]
    public class SpawnPatternItem
    {
        public ItemSpawnerTypeId TypeId;
        public bool UseSamePosition;
        public float Delay;
        public bool PreferSideSpawn;
        public bool IsSafeZoneAvoided;
    }

    [Serializable]
    public class SpawnPattern
    {
        public List<SpawnPatternItem> Items;
    }

    public class ItemSpawnerService : IInitializable, IDisposable, ITickable
    {
        private readonly IStaticDataService _staticDataService;
        private readonly IFallManagerService _fallManagerService;
        private readonly IGameStateService _gameStateService;
        private readonly InventoryModel _inventoryModel;
        private readonly IPlayerStickingService _playerStickingService;
        private readonly IHeartService _heartService;
        private readonly List<ItemSpawnConfig> _spawnConfigs;
        private readonly Dictionary<ItemSpawnerTypeId, ItemView> _prefabs = new();
        private readonly Dictionary<ItemSpawnerTypeId, Sprite> _icons = new();
        private readonly Dictionary<ItemSpawnerTypeId, Queue<ItemView>> _objectPools = new();
        private readonly List<SpawnPattern>[] _patternsByStage = new List<SpawnPattern>[3];
        private readonly List<Vector2> _recentSpawnPositions = new();
        private bool _isSpawningActive;
        private Transform _spawnZoneTransform;
        private float _gameTime;
        private SpawnPattern _currentPattern;
        private int _currentPatternItemIndex;
        private float _currentItemDelay;
        private float _patternDelay;
        private Vector2? _lastPosition;

        private const float BaseMinPatternDelay = 0.4f; // Ускорили спавн
        private const float BaseMaxPatternDelay = 1.2f; // Ускорили спавн
        private const float BaseSpikeStageTime = 20f; // Шипы раньше (было 15f)
        private const float BaseZigzagStageTime = 50f; // Зигзаги раньше (было 30f)
        private const int InitialPoolSize = 10;
        private const float SafeZoneRadius = 2f;
        private const float MinSpawnDistance = 1.5f;

        public ItemSpawnerService(
            IStaticDataService staticDataService,
            IFallManagerService fallManagerService,
            IGameStateService gameStateService,
            InventoryModel inventoryModel,
            IPlayerStickingService playerStickingService,
            IHeartService heartService)
        {
            _staticDataService = staticDataService;
            _fallManagerService = fallManagerService;
            _gameStateService = gameStateService;
            _inventoryModel = inventoryModel;
            _playerStickingService = playerStickingService;
            _heartService = heartService;

            _spawnConfigs = new List<ItemSpawnConfig>
            {
                new ItemSpawnConfig { TypeId = ItemSpawnerTypeId.Slime, CategoryType = InventoryCategoryType.Flowers, Size = new Vector2(1f, 1f) },
                new ItemSpawnConfig { TypeId = ItemSpawnerTypeId.Bomb, CategoryType = InventoryCategoryType.Bomb, Size = new Vector2(1.2f, 1.2f) },
                new ItemSpawnConfig { TypeId = ItemSpawnerTypeId.Zigzag, CategoryType = InventoryCategoryType.Zigzag, Size = new Vector2(1.5f, 1f) },
                new ItemSpawnConfig { TypeId = ItemSpawnerTypeId.Spike, CategoryType = InventoryCategoryType.Spike, Size = new Vector2(1f, 1.5f) },
            };

            InitializePatterns();
        }

        private void InitializePatterns()
        {
            bool IsValidPattern(List<SpawnPatternItem> items)
            {
                int slimeCount = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].TypeId == ItemSpawnerTypeId.Slime)
                    {
                        slimeCount++;
                    }
                    else if (i > 0 && items[i - 1].TypeId != ItemSpawnerTypeId.Slime)
                    {
                        Debug.LogError("Invalid pattern: Enemy after non-Slime.");
                        return false;
                    }
                    else if (slimeCount < 2 && i > 0 && items[i].TypeId != ItemSpawnerTypeId.Slime)
                    {
                        Debug.LogError("Invalid pattern: Fewer than 2 Slimes before enemy.");
                        return false;
                    }
                    else if (items[i].TypeId != ItemSpawnerTypeId.Slime)
                    {
                        slimeCount = 0;
                    }
                }
                return true;
            }

            _patternsByStage[0] = new List<SpawnPattern>
            {
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, PreferSideSpawn = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Bomb, Delay = 0.8f, IsSafeZoneAvoided = true } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, IsSafeZoneAvoided = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Bomb, Delay = 0.8f, PreferSideSpawn = true } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, PreferSideSpawn = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Bomb, Delay = 0.8f } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, UseSamePosition = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f } 
                } },
            };

            _patternsByStage[1] = new List<SpawnPattern>
            {
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, PreferSideSpawn = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Spike, Delay = 0.8f, IsSafeZoneAvoided = true } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, IsSafeZoneAvoided = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Bomb, Delay = 0.8f } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, UseSamePosition = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Spike, Delay = 0.8f, PreferSideSpawn = true } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, PreferSideSpawn = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Bomb, Delay = 0.8f, IsSafeZoneAvoided = true } 
                } },
            };

            _patternsByStage[2] = new List<SpawnPattern>
            {
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, PreferSideSpawn = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Zigzag, Delay = 0.8f, IsSafeZoneAvoided = true } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, IsSafeZoneAvoided = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Bomb, Delay = 0.8f } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, UseSamePosition = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Zigzag, Delay = 0.8f, PreferSideSpawn = true } 
                } },
                new SpawnPattern { Items = new List<SpawnPatternItem> { 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f, PreferSideSpawn = true }, 
                    new() { TypeId = ItemSpawnerTypeId.Slime, Delay = 0.5f }, 
                    new() { TypeId = ItemSpawnerTypeId.Spike, Delay = 0.8f, IsSafeZoneAvoided = true } 
                } },
            };

            foreach (var stagePatterns in _patternsByStage)
            {
                foreach (var pattern in stagePatterns)
                {
                    if (!IsValidPattern(pattern.Items))
                    {
                        Debug.LogError($"Invalid pattern detected in stage {Array.IndexOf(_patternsByStage, stagePatterns)}");
                    }
                }
            }
        }

        public void Initialize()
        {
            _gameStateService.GameStart();
            
            foreach (var config in _spawnConfigs)
            {
                GameObject prefab = _staticDataService.GetItemSpawnerPrefab(config.TypeId);
                ItemView view = prefab?.GetComponent<ItemView>();
                if (view != null)
                {
                    _prefabs[config.TypeId] = view;
                    _icons[config.TypeId] = _inventoryModel.GetSkin(config.CategoryType);
                    _objectPools[config.TypeId] = new Queue<ItemView>();
                    for (int i = 0; i < InitialPoolSize; i++)
                    {
                        ItemView item = Object.Instantiate(_prefabs[config.TypeId], Vector3.zero, Quaternion.identity, _spawnZoneTransform);
                        if (item != null)
                        {
                            item.gameObject.SetActive(false);
                            item.SetupPool(this, config.TypeId);
                            item.Setup(_playerStickingService, _icons[config.TypeId], config.TypeId, _heartService, _gameStateService);
                            _objectPools[config.TypeId].Enqueue(item);
                        }
                    }
                }
            }
            _gameStateService.OnGameLose += StopSpawn;
        }

        public void Dispose()
        {
            _gameStateService.OnGameLose -= StopSpawn;
            foreach (var pool in _objectPools.Values)
            {
                while (pool.Count > 0)
                {
                    ItemView item = pool.Dequeue();
                    if (item != null && item.gameObject != null)
                    {
                        Object.Destroy(item.gameObject);
                    }
                }
            }
            _objectPools.Clear();
        }

        public void StartSpawn(Transform spawnZoneTransform)
        {
            if (spawnZoneTransform == null) return;
            _isSpawningActive = true;
            _spawnZoneTransform = spawnZoneTransform;
            _gameTime = 0f;
            _currentPattern = null;
            _currentPatternItemIndex = 0;
            _currentItemDelay = 0f;
            _patternDelay = 0f;
            _lastPosition = null;
            _recentSpawnPositions.Clear();
        }

        public void Tick()
        {
            if (!_isSpawningActive || _spawnZoneTransform == null) return;

            _gameTime += Time.deltaTime * _gameStateService.GameSpeed;

            if (_currentPattern == null)
            {
                if (_patternDelay > 0f)
                {
                    _patternDelay -= Time.deltaTime * _gameStateService.GameSpeed;
                    return;
                }

                int stage = GetCurrentStage();
                List<SpawnPattern> patterns = _patternsByStage[stage];
                if (patterns == null || patterns.Count == 0) return;
                _currentPattern = patterns[Random.Range(0, patterns.Count)];
                Debug.Log($"Selected pattern for stage {stage}: {string.Join(", ", _currentPattern.Items.ConvertAll(item => item.TypeId.ToString()))}");
                _currentPatternItemIndex = 0;
                _currentItemDelay = _currentPattern.Items[0].Delay / _gameStateService.GameSpeed;
            }

            if (_currentPatternItemIndex < _currentPattern.Items.Count)
            {
                if (_currentItemDelay > 0f)
                {
                    _currentItemDelay -= Time.deltaTime * _gameStateService.GameSpeed;
                    return;
                }

                SpawnPatternItem item = _currentPattern.Items[_currentPatternItemIndex];
                Vector2 spawnPosition = GetSpawnPosition(item);
                if (spawnPosition != Vector2.zero)
                {
                    SpawnItem(item.TypeId, spawnPosition);
                    _lastPosition = spawnPosition;
                    _recentSpawnPositions.Add(spawnPosition);
                    if (_recentSpawnPositions.Count > 10)
                        _recentSpawnPositions.RemoveAt(0);
                }

                _currentPatternItemIndex++;
                if (_currentPatternItemIndex < _currentPattern.Items.Count)
                {
                    _currentItemDelay = _currentPattern.Items[_currentPatternItemIndex].Delay / _gameStateService.GameSpeed;
                }
                else
                {
                    _currentPattern = null;
                    _patternDelay = Random.Range(BaseMinPatternDelay, BaseMaxPatternDelay) / _gameStateService.GameSpeed;
                }
            }
        }

        private int GetCurrentStage()
        {
            float scaledTime = _gameTime / _gameStateService.GameSpeed;
            if (scaledTime >= BaseZigzagStageTime)
                return 2;
            if (scaledTime >= BaseSpikeStageTime)
                return 1;
            return 0;
        }

        private void StopSpawn()
        {
            _isSpawningActive = false;
        }

        private void SpawnItem(ItemSpawnerTypeId typeId, Vector2 spawnPosition)
        {
            if (!_isSpawningActive || !_prefabs.ContainsKey(typeId) || _spawnZoneTransform == null) return;

            ItemView item = null;
            if (_objectPools[typeId].Count > 0)
            {
                item = _objectPools[typeId].Dequeue();
                if (item == null || item.gameObject == null)
                {
                    item = Object.Instantiate(_prefabs[typeId], spawnPosition, Quaternion.identity, _spawnZoneTransform);
                    item.SetupPool(this, typeId);
                    item.Setup(_playerStickingService, _icons[typeId], typeId, _heartService, _gameStateService);
                }
            }
            else
            {
                item = Object.Instantiate(_prefabs[typeId], spawnPosition, Quaternion.identity, _spawnZoneTransform);
                item.SetupPool(this, typeId);
                item.Setup(_playerStickingService, _icons[typeId], typeId, _heartService, _gameStateService);
            }

            if (item != null)
            {
                item.transform.position = spawnPosition;
                item.transform.rotation = Quaternion.identity;
                item.gameObject.SetActive(true);
                item.Reset();
                _fallManagerService.AddFallingObject(item.gameObject);
            }
        }

        public void ReturnToPool(ItemView item, ItemSpawnerTypeId typeId)
        {
            if (item == null || item.gameObject == null || !_objectPools.ContainsKey(typeId)) return;

            item.gameObject.SetActive(false);
            _objectPools[typeId].Enqueue(item);
        }

        private Vector2 GetSpawnPosition(SpawnPatternItem item)
        {
            if (_spawnZoneTransform == null) return Vector2.zero;

            Vector3 spawnZoneSize = _spawnZoneTransform.GetComponent<BoxCollider2D>().size;
            Vector3 spawnZonePosition = _spawnZoneTransform.position;

            float minX = spawnZonePosition.x - spawnZoneSize.x / 2;
            float maxX = spawnZonePosition.x + spawnZoneSize.x / 2;
            float minY = spawnZonePosition.y + spawnZoneSize.y / 2;
            float maxY = spawnZonePosition.y + spawnZoneSize.y / 2;

            Vector2 spawnPosition = Vector2.zero;
            bool positionFound = false;

            for (int attempts = 0; attempts < 10; attempts++)
            {
                if (item.UseSamePosition && _lastPosition.HasValue)
                {
                    spawnPosition = new Vector2(_lastPosition.Value.x, Random.Range(minY, maxY));
                }
                else if (item.PreferSideSpawn)
                {
                    bool leftSide = Random.value > 0.5f;
                    float x = leftSide ? Random.Range(minX, minX + spawnZoneSize.x / 4) : Random.Range(maxX - spawnZoneSize.x / 4, maxX);
                    spawnPosition = new Vector2(x, Random.Range(minY, maxY));
                }
                else
                {
                    spawnPosition = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                }

                if (item.IsSafeZoneAvoided)
                {
                    Vector2 screenCenter = new Vector2(spawnZonePosition.x, spawnZonePosition.y);
                    if (Vector2.Distance(spawnPosition, screenCenter) < SafeZoneRadius)
                        continue;
                }

                if (IsPositionValid(spawnPosition, item.TypeId))
                {
                    positionFound = true;
                    break;
                }
            }

            return positionFound ? spawnPosition : Vector2.zero;
        }

        private bool IsPositionValid(Vector2 position, ItemSpawnerTypeId typeId)
        {
            ItemSpawnConfig config = _spawnConfigs.Find(c => c.TypeId == typeId);
            if (config == null) return false;

            Vector2 objectSize = config.Size;
            foreach (Vector2 recentPos in _recentSpawnPositions)
            {
                ItemSpawnConfig otherConfig = _spawnConfigs.Find(c => _recentSpawnPositions.Contains(recentPos));
                if (otherConfig == null) continue;

                Vector2 otherSize = otherConfig.Size;
                float distance = Vector2.Distance(position, recentPos);
                float minDistance = (objectSize.x + otherSize.x) / 2 * MinSpawnDistance;

                if (distance < minDistance)
                    return false;
            }
            return true;
        }
    }
}