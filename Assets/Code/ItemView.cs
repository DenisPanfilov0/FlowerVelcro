using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.SpawnersServices;
using Code.Gameplay.Services.GameStateService;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour.View
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class ItemView : MonoBehaviour
    {
        [SerializeField] protected Rigidbody2D _rb;
        [SerializeField] protected SpriteRenderer _icon;
        [SerializeField] protected float baseFallSpeed = 5f;
        [SerializeField] public float N = 1f; // For Inspector visibility, updated by GameSpeed
        protected bool _isFalling = true;
        [SerializeField] protected bool _isGameActive = true;
        protected IPlayerStickingService _playerStickingService;
        protected IHeartService _heartService;
        protected ItemSpawnerService _spawnerService;
        protected ItemSpawnerTypeId _typeId;
        protected IGameStateService _gameStateService;
        [SerializeField] private float _screenBottom;

        [Inject]
        public void Construct(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService;
        }

        public virtual void Setup(IPlayerStickingService playerStickingService, Sprite icon, ItemSpawnerTypeId typeId, IHeartService heartService, IGameStateService gameStateService)
        {
            if (gameObject == null) return;
            _playerStickingService = playerStickingService;
            _heartService = heartService;
            _gameStateService = gameStateService ?? _gameStateService; // Ensure gameStateService is set
            _icon.sprite = icon;
            // if (GetComponent<ItemAppearance>() == null)
            // {
            //     gameObject.AddComponent<ItemAppearance>();
            // }
        }

        public void SetupPool(ItemSpawnerService spawnerService, ItemSpawnerTypeId typeId)
        {
            if (gameObject == null) return;
            _spawnerService = spawnerService;
            _typeId = typeId;
            CalculateScreenBottom();
        }

        public virtual void Reset()
        {
            if (gameObject == null || !_isGameActive || _gameStateService == null) return;
            _isFalling = true;
            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            _rb.velocity = Vector2.down * baseFallSpeed * _gameStateService.GameSpeed;
            _icon.color = Color.white;
        }

        protected virtual void Awake()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody2D>();

            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.gravityScale = 0f;
            if (_gameStateService != null)
            {
                _gameStateService.OnGameLose += HandleGameLose;
            }
        }

        protected virtual void OnEnable()
        {
            if (_rb != null && _isGameActive && _gameStateService != null)
            {
                N = _gameStateService.GameSpeed; // Update serialized field for Inspector
                _rb.velocity = Vector2.down * baseFallSpeed * _gameStateService.GameSpeed;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnGameLose -= HandleGameLose;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!_isFalling || !_isGameActive || gameObject == null || transform == null || _gameStateService == null) return;

            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            _rb.velocity = new Vector2(_rb.velocity.x, -baseFallSpeed * _gameStateService.GameSpeed);
            if (transform.position.y < _screenBottom)
            {
                _spawnerService?.ReturnToPool(this, _typeId);
            }
        }

        private void CalculateScreenBottom()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float height = 2f * mainCamera.orthographicSize;
                _screenBottom = mainCamera.transform.position.y - height / 2 - 1f;
            }
        }

        private void HandleGameLose()
        {
            _isGameActive = false;
            _isFalling = false;
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
            }
        }
    }
}