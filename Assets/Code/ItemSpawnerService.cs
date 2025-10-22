using System;
using System.Collections.Generic;
using System.Linq;
using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Behaviour.View;
using Code.Gameplay.Services.GameScore;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using Code.Inventory;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Code.Gameplay.Services.SpawnersServices
{
    public class ItemSpawnerService : IInitializable, IDisposable, ITickable
    {
        private readonly IGameStateService _gameStateService;
        private readonly GameScoreService _gameScoreService;
        private readonly InventoryModel _inventoryModel;
        private readonly PlayerStickingService _playerStickingService;
        private readonly HeartService _heartService;
        private readonly ItemSpawnerConfig _config;
        private readonly List<ItemSpawnerConfig.StageConfig> _stages;
        private readonly Dictionary<ItemSpawnerTypeId, ItemView> _prefabs = new();
        private readonly Dictionary<ItemSpawnerTypeId, Sprite> _icons = new();
        private readonly Dictionary<ItemSpawnerTypeId, Queue<ItemView>> _objectPools = new();
        private readonly Dictionary<ItemSpawnerTypeId, Vector2> _objectSizes = new();
        private readonly List<(Vector2 Position, ItemSpawnerTypeId TypeId)> _recentSpawnPositions = new();
        private bool _isSpawningActive;
        private bool _isPaused; // Добавляем флаг паузы
        private Transform _spawnZoneTransform;
        private int _currentStageIndex;
        private int _targetScore;
        private List<int> _currentSpawnSequence;
        private int _currentSequenceIndex;
        private float _currentItemDelay;
        private ItemSpawnerTypeId? _lastSpawnedType;
        private Dictionary<int, ItemSpawnerTypeId> _typeIndexToTypeId;
        private float _magicFlowerSpawnChange = 0.13f;
        private const float MinSpawnDistance = 0.5f;
        private const int InitialPoolSize = 4;

        [Inject]
        public ItemSpawnerService(
            IGameStateService gameStateService,
            GameScoreService gameScoreService,
            InventoryModel inventoryModel,
            PlayerStickingService playerStickingService,
            HeartService heartService,
            ItemSpawnerConfig config)
        {
            _gameStateService = gameStateService;
            _gameScoreService = gameScoreService;
            _inventoryModel = inventoryModel;
            _playerStickingService = playerStickingService;
            _heartService = heartService;
            _config = config;
            _stages = config.Stages.Select(s => new ItemSpawnerConfig.StageConfig
            {
                Blocks = s.Blocks.Select(b => new ItemSpawnerConfig.BlockConfig { Items = new List<ItemSpawnerConfig.BlockItem>(b.Items) }).ToList(),
                ScoreRange = s.ScoreRange
            }).ToList();
        }

        public void Initialize()
        {
            _gameStateService.GameStart();
            foreach (var config in _config.SpawnConfigs)
            {
                if (config.Prefab == null) continue;
                ItemView view = config.Prefab.GetComponent<ItemView>();
                if (view != null)
                {
                    _prefabs[config.TypeId] = view;
                    _icons[config.TypeId] = _inventoryModel.GetSkin(config.CategoryType);
                    _objectPools[config.TypeId] = new Queue<ItemView>();
                    Vector2 size = Vector2.one;
                    var collider = config.Prefab.GetComponent<BoxCollider2D>();
                    if (collider != null)
                    {
                        size = Vector2.Scale(collider.size, config.Prefab.transform.localScale);
                        size = Vector2.Min(size, new Vector2(2f, 2f));
                        Debug.Log($"[ItemSpawnerService] Size for {config.TypeId}: {size} (Collider: {collider.size}, Scale: {config.Prefab.transform.localScale})");
                    }
                    _objectSizes[config.TypeId] = size;
                    for (int i = 0; i < InitialPoolSize; i++)
                    {
                        ItemView item = UnityEngine.Object.Instantiate(_prefabs[config.TypeId], Vector3.zero, Quaternion.identity, _spawnZoneTransform);
                        item.gameObject.SetActive(false);
                        item.SetupPool(this, config.TypeId);
                        item.Setup(_playerStickingService, _icons[config.TypeId], _heartService, _gameStateService, false);
                        _objectPools[config.TypeId].Enqueue(item);
                    }
                }
            }
            _gameStateService.OnGameLose += StopSpawn;
            _gameStateService.OnGamePause += HandleGamePause;
            _gameStateService.OnGameResume += HandleGameResume;
            _gameScoreService.ScoreChange += OnScoreChange;
            _currentStageIndex = 0;
            _targetScore = Random.Range(_stages[0].ScoreRange.x, _stages[0].ScoreRange.y + 1);
            _currentSpawnSequence = null;
            _currentSequenceIndex = 0;
            _currentItemDelay = 0f;
            _lastSpawnedType = null;
            _typeIndexToTypeId = new Dictionary<int, ItemSpawnerTypeId>();
            _isPaused = false;
        }

        public void Dispose()
        {
            _gameStateService.OnGameLose -= StopSpawn;
            _gameStateService.OnGamePause -= HandleGamePause;
            _gameStateService.OnGameResume -= HandleGameResume;
            _gameScoreService.ScoreChange -= OnScoreChange;
            foreach (var pool in _objectPools.Values)
            {
                while (pool.Count > 0)
                {
                    ItemView item = pool.Dequeue();
                    if (item != null && item.gameObject != null)
                    {
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                }
            }
            _objectPools.Clear();
        }

        public void StartSpawn(Transform spawnZoneTransform)
        {
            if (spawnZoneTransform == null) return;
            _isSpawningActive = true;
            _isPaused = false;
            _spawnZoneTransform = spawnZoneTransform;
            _currentSpawnSequence = null;
            _currentSequenceIndex = 0;
            _currentItemDelay = 0f;
            _recentSpawnPositions.Clear();
            _lastSpawnedType = null;
        }

        public void SetSpawnZone(Transform spawnZoneTransform)
        {
            _spawnZoneTransform = spawnZoneTransform;
        }

        public void Tick()
        {
            if (!_isSpawningActive || _spawnZoneTransform == null || _isPaused) return;

            if (_currentSpawnSequence == null || _currentSequenceIndex >= _currentSpawnSequence.Count)
            {
                if (_currentItemDelay > 0f)
                {
                    _currentItemDelay -= Time.deltaTime * _gameStateService.GameSpeed;
                    return;
                }
                SelectNewBlock();
            }

            if (_currentSequenceIndex < _currentSpawnSequence.Count)
            {
                if (_currentItemDelay > 0f)
                {
                    _currentItemDelay -= Time.deltaTime * _gameStateService.GameSpeed;
                    return;
                }

                int typeIndex = _currentSpawnSequence[_currentSequenceIndex];
                if (!_typeIndexToTypeId.TryGetValue(typeIndex, out ItemSpawnerTypeId typeId))
                {
                    typeId = ItemSpawnerTypeId.Slime;
                }

                Vector2 spawnPosition = GetSpawnPosition(typeId);
                if (spawnPosition != Vector2.zero)
                {
                    SpawnItem(typeId, spawnPosition);
                    _recentSpawnPositions.Add((spawnPosition, typeId));
                    if (_recentSpawnPositions.Count > 10)
                        _recentSpawnPositions.RemoveAt(0);
                    _lastSpawnedType = typeId;
                    _currentSequenceIndex++;
                    _currentItemDelay = Random.Range(_config.SpawnDelayRange.x, _config.SpawnDelayRange.y) / _gameStateService.GameSpeed;
                }
                else
                {
                    Debug.LogWarning($"[ItemSpawnerService] Pausing spawn for {typeId}: no valid position found");
                }
            }
        }

        private void OnScoreChange(int totalScore)
        {
            if (_currentStageIndex >= _stages.Count - 1) return;
            if (totalScore >= _targetScore)
            {
                _currentStageIndex++;
                _currentSpawnSequence = null;
                _currentSequenceIndex = 0;
                _currentItemDelay = 0f;
                _lastSpawnedType = null;
                if (_currentStageIndex < _stages.Count)
                {
                    _targetScore = Random.Range(_stages[_currentStageIndex].ScoreRange.x, _stages[_currentStageIndex].ScoreRange.y + 1);
                }
            }
        }

        private void SelectNewBlock()
        {
            var stage = _stages[_currentStageIndex];
            if (stage.Blocks.Count == 0) return;

            var block = stage.Blocks[Random.Range(0, stage.Blocks.Count)];
            _currentSpawnSequence = GenerateSpawnSequence(block);
            _currentSequenceIndex = 0;
            _currentItemDelay = Random.Range(_config.SpawnDelayRange.x, _config.SpawnDelayRange.y) / _gameStateService.GameSpeed;
        }

        private List<int> GenerateSpawnSequence(ItemSpawnerConfig.BlockConfig block)
        {
            var sequence = new List<int>();
            _typeIndexToTypeId = new Dictionary<int, ItemSpawnerTypeId>();
            int index = 1;

            foreach (var item in block.Items)
            {
                int typeIndex = item.TypeId == ItemSpawnerTypeId.Slime ? 1 : ++index;
                _typeIndexToTypeId[typeIndex] = item.TypeId;
                for (int i = 0; i < item.Quantity; i++)
                {
                    sequence.Add(typeIndex);
                }
            }

            for (int i = 0; i < sequence.Count - 1; )
            {
                if (sequence[i] != 1 && sequence[i + 1] != 1)
                {
                    int slimeIndex = sequence.FindIndex(i + 2, x => x == 1);
                    if (slimeIndex != -1)
                    {
                        sequence.RemoveAt(slimeIndex);
                        sequence.Insert(i + 1, 1);
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            int shuffleAttempts = sequence.Count * 3;
            for (int attempt = 0; attempt < shuffleAttempts; attempt++)
            {
                int i = Random.Range(0, sequence.Count);
                int j = Random.Range(0, sequence.Count);
                if (i == j) continue;

                (sequence[i], sequence[j]) = (sequence[j], sequence[i]);

                bool valid = true;
                if (i > 0 && sequence[i - 1] != 1 && sequence[i] != 1) valid = false;
                if (i < sequence.Count - 1 && sequence[i + 1] != 1 && sequence[i] != 1) valid = false;
                if (j > 0 && sequence[j - 1] != 1 && sequence[j] != 1) valid = false;
                if (j < sequence.Count - 1 && sequence[j + 1] != 1 && sequence[j] != 1) valid = false;

                if (!valid)
                {
                    (sequence[i], sequence[j]) = (sequence[j], sequence[i]);
                }
            }

            if (_lastSpawnedType.HasValue && _lastSpawnedType != ItemSpawnerTypeId.Slime)
            {
                if (sequence.Count > 0 && sequence[0] != 1)
                {
                    bool relocated = false;
                    for (int pos = 1; pos < sequence.Count; pos++)
                    {
                        if (sequence[pos] == 1)
                        {
                            bool validForFirst = true;
                            if (pos - 1 > 0 && sequence[pos - 2] != 1 && sequence[pos - 1] != 1) validForFirst = false;
                            bool validForPos = true;
                            if (pos > 0 && _lastSpawnedType != ItemSpawnerTypeId.Slime && sequence[0] != 1) validForPos = false;

                            int temp = sequence[0];
                            sequence[0] = sequence[pos];
                            sequence[pos] = temp;

                            bool sequenceValid = true;
                            for (int k = 0; k < sequence.Count - 1; k++)
                            {
                                if (sequence[k] != 1 && sequence[k + 1] != 1)
                                {
                                    sequenceValid = false;
                                    break;
                                }
                            }

                            if (sequenceValid)
                            {
                                relocated = true;
                                break;
                            }
                            else
                            {
                                (sequence[0], sequence[pos]) = (sequence[pos], sequence[0]);
                            }
                        }
                    }

                    if (!relocated)
                    {
                        sequence.Insert(0, 1);
                    }
                }
            }

            return sequence;
        }

        private void StopSpawn()
        {
            _isSpawningActive = false;
            _isPaused = false;
        }

        private void HandleGamePause()
        {
            _isPaused = true;
            Debug.Log("[ItemSpawnerService] Spawning paused.");
        }

        private void HandleGameResume()
        {
            if (_isSpawningActive)
            {
                _isPaused = false;
                Debug.Log("[ItemSpawnerService] Spawning resumed.");
            }
        }

        private void SpawnItem(ItemSpawnerTypeId typeId, Vector2 spawnPosition)
        {
            if (!_isSpawningActive || _isPaused || !_prefabs.ContainsKey(typeId) || _spawnZoneTransform == null)
                return;

            // --- СТАРАЯ ЛОГИКА ДЛЯ SLIME и MAGIC FLOWER ---
            // (оставляем на месте, чтобы не сломать механику)
            if (typeId == ItemSpawnerTypeId.Slime && Random.value < _magicFlowerSpawnChange && _prefabs.ContainsKey(ItemSpawnerTypeId.MagicFlower))
            {
                typeId = ItemSpawnerTypeId.MagicFlower;
            }
            
            // --- ДОБАВЛЕННАЯ ЛОГИКА ---
            // Если тип — MagicFlower, то с шансом 25% заменить его на StarFlower
            if (typeId == ItemSpawnerTypeId.MagicFlower && _prefabs.ContainsKey(ItemSpawnerTypeId.StarFlower))
            {
                float chance = Random.value; // 0..1
                if (chance < 0.4f) // 40% шанс
                {
                    typeId = ItemSpawnerTypeId.StarFlower;
                    Debug.Log("[ItemSpawnerService] 🎇 Заспавнен StarFlower вместо MagicFlower (шанс 25%)");
                }
            }

            ItemView item = null;
            if (_objectPools[typeId].Count > 0)
            {
                item = _objectPools[typeId].Dequeue();
                if (item == null || item.gameObject == null)
                {
                    item = UnityEngine.Object.Instantiate(_prefabs[typeId], spawnPosition, Quaternion.identity, _spawnZoneTransform);
                    item.SetupPool(this, typeId);
                    item.Setup(_playerStickingService, _icons[typeId], _heartService, _gameStateService, false);
                }
            }
            else
            {
                item = UnityEngine.Object.Instantiate(_prefabs[typeId], spawnPosition, Quaternion.identity, _spawnZoneTransform);
                item.SetupPool(this, typeId);
                item.Setup(_playerStickingService, _icons[typeId], _heartService, _gameStateService, false);
            }

            if (item != null)
            {
                item.transform.position = spawnPosition;
                item.transform.rotation = Quaternion.identity;
                item.gameObject.SetActive(true);
                item.Reset();
            }
        }

        public void ReturnToPool(ItemView item, ItemSpawnerTypeId typeId)
        {
            if (item == null || item.gameObject == null || !_objectPools.ContainsKey(typeId)) return;

            item.gameObject.SetActive(false);
            _objectPools[typeId].Enqueue(item);
            _recentSpawnPositions.RemoveAll(p => Vector2.Distance(p.Position, item.transform.position) < 0.01f);
        }

        private Vector2 GetSpawnPosition(ItemSpawnerTypeId typeId)
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

            for (int attempts = 0; attempts < 50; attempts++)
            {
                bool preferSideSpawn = typeId != ItemSpawnerTypeId.Slime && Random.value < 0.7f;

                if (preferSideSpawn)
                {
                    bool leftSide = Random.value > 0.5f;
                    float x = leftSide ? Random.Range(minX, minX + spawnZoneSize.x / 2) : Random.Range(maxX - spawnZoneSize.x / 2, maxX);
                    spawnPosition = new Vector2(x, Random.Range(minY, maxY));
                }
                else
                {
                    spawnPosition = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                }

                if (IsPositionValid(spawnPosition, typeId))
                {
                    positionFound = true;
                    Debug.Log($"[ItemSpawnerService] Valid position found for {typeId}: {spawnPosition}");
                    break;
                }
                else
                {
                    Debug.Log($"[ItemSpawnerService] Position {spawnPosition} rejected for {typeId}: invalid due to overlap");
                }
            }

            if (!positionFound)
            {
                Debug.LogWarning($"[ItemSpawnerService] Pausing spawn for {typeId}: no valid position after 50 attempts");
            }

            return positionFound ? spawnPosition : Vector2.zero;
        }

        private bool IsPositionValid(Vector2 position, ItemSpawnerTypeId typeId)
        {
            if (!_objectSizes.ContainsKey(typeId)) return false;

            Vector2 objectSize = _objectSizes[typeId];
            foreach (var (recentPos, otherTypeId) in _recentSpawnPositions)
            {
                Vector2 otherSize = _objectSizes.ContainsKey(otherTypeId) ? _objectSizes[otherTypeId] : Vector2.one;
                float distance = Vector2.Distance(position, recentPos);
                float minDistance = (objectSize.x + otherSize.x) / 2 * MinSpawnDistance;

                if (distance < minDistance)
                {
                    Debug.Log($"[ItemSpawnerService] Position {position} invalid for {typeId}: too close to {otherTypeId} at {recentPos} (distance: {distance}, minDistance: {minDistance})");
                    return false;
                }
            }
            return true;
        }
    }
}