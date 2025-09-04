using System.Collections;
using Code.Gameplay.Services.GameScore;
using UnityEngine;
using Zenject;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.PlayerSticking;
using Code.Inventory;
using UnityEngine.UI;

namespace Code.Gameplay.Behaviour.View
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private GameObject _ropePrefab;
        [SerializeField] private float _moveSpeed = 1200f;
        [SerializeField] private float _baseFallSpeed = 5f;
        [SerializeField] private float _centerThreshold = 0.1f;
        [SerializeField] private float _stopThreshold = 0.05f;
        [SerializeField] public float N = 1f; // For Inspector visibility, updated by GameSpeed
        [SerializeField] private SpriteRenderer _characterIcon;

        private PlayerStickingService _playerStickingService;
        private IGameStateService _gameStateService;
        private GameScoreService _gameScoreService;
        private AudioManager _audioManager;
        private GameObject _ropeObject;
        private SpriteRenderer _ropeSpriteRenderer;
        private Coroutine _moveCoroutine;
        private Rigidbody2D _rb;
        private Camera _mainCamera;
        private bool _isFalling = true;
        private bool _isGameActive = true;
        private float _screenBottomY;
        private float _lastOrthographicSize;
        private Vector2 _lastScreenResolution;
        private float _playerHeight;
        private InventoryModel _inventoryModel;
        private Flower _targetFlower; // Для сохранения цели притягивания при паузе

        [Inject]
        public void Construct(
            PlayerStickingService playerStickingService,
            IGameStateService gameStateService,
            Camera mainCamera,
            GameScoreService gameScoreService,
            AudioManager audioManager,
            InventoryModel inventoryModel)
        {
            _inventoryModel = inventoryModel;
            _gameScoreService = gameScoreService;
            _playerStickingService = playerStickingService;
            _gameStateService = gameStateService;
            _mainCamera = mainCamera;
            _audioManager = audioManager;
        }

        private void Awake()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody2D>();

            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.gravityScale = 0f;

            Collider2D collider = GetComponent<Collider2D>();
            _playerHeight = collider.bounds.size.y;

            if (_gameStateService != null)
            {
                _gameStateService.OnGameLose += HandleGameLose;
                _gameStateService.OnGamePause += HandleGamePause;
                _gameStateService.OnGameResume += HandleGameResume;
            }
        }

        private void Start()
        {
            _playerStickingService.AddPlayer(this);
            _playerStickingService.PlayerGlued += MoveToTarget;
            _playerStickingService.OnFinishSticking += HandleFinishSticking;
            UpdateScreenBounds();
            _lastOrthographicSize = _mainCamera.orthographicSize;
            _lastScreenResolution = new Vector2(Screen.width, Screen.height);

            _characterIcon.sprite = _inventoryModel.GetSkin(InventoryCategoryType.Character);
        }

        private void OnEnable()
        {
            if (_isGameActive && _gameStateService != null)
            {
                N = _gameStateService.GameSpeed; // Update serialized field for Inspector
                if (_isFalling)
                {
                    _rb.linearVelocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;
                }
            }
        }

        private void OnDestroy()
        {
            Destroy(_ropeObject);
            _playerStickingService.FinishSticking();
            _playerStickingService.PlayerGlued -= MoveToTarget;
            _playerStickingService.OnFinishSticking -= HandleFinishSticking;
            if (_gameStateService != null)
            {
                _gameStateService.OnGameLose -= HandleGameLose;
                _gameStateService.OnGamePause -= HandleGamePause;
                _gameStateService.OnGameResume -= HandleGameResume;
            }
        }

        private void Update()
        {
            if (!_isGameActive || _gameStateService.IsGamePause) return;

            if (_mainCamera.orthographicSize != _lastOrthographicSize ||
                Screen.width != _lastScreenResolution.x ||
                Screen.height != _lastScreenResolution.y)
            {
                UpdateScreenBounds();
                _lastOrthographicSize = _mainCamera.orthographicSize;
                _lastScreenResolution = new Vector2(Screen.width, Screen.height);
            }

            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
        }

        private void FixedUpdate()
        {
            if (!_isGameActive || !_isFalling || _gameStateService.IsGamePause) return;

            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            _rb.linearVelocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;

            float playerBottomY = transform.position.y - _playerHeight / 2f;
            if (playerBottomY <= _screenBottomY + _stopThreshold)
            {
                transform.position = new Vector3(transform.position.x, _screenBottomY + _playerHeight / 2f, transform.position.z);
                _rb.linearVelocity = Vector2.zero;
            }
        }

        private void UpdateScreenBounds()
        {
            float cameraHeight = 2f * _mainCamera.orthographicSize;
            _screenBottomY = _mainCamera.transform.position.y - cameraHeight / 2f;
        }

        private void MoveToTarget(Flower flower)
        {
            if (!_isGameActive || flower == null || _gameStateService.IsGamePause) return;

            _targetFlower = flower; // Сохраняем цель для возобновления после паузы
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _isFalling = false;
            _rb.linearVelocity = Vector2.zero;
            CreateRope();
            _moveCoroutine = StartCoroutine(MoveTowards(flower));
        }

        private void CreateRope()
        {
            if (_ropeObject != null)
            {
                Destroy(_ropeObject);
            }

            if (!_isGameActive || _gameStateService.IsGamePause) return;

            _ropeObject = Instantiate(_ropePrefab, transform.position, Quaternion.identity);
            _ropeSpriteRenderer = _ropeObject.GetComponent<SpriteRenderer>();
            _ropeSpriteRenderer.size = new Vector2(_ropeSpriteRenderer.size.x, 1f);
        }

        private IEnumerator MoveTowards(Flower flower)
        {
            if (flower == null || !_isGameActive) yield break;

            float distance;
            do
            {
                if (_gameStateService.IsGamePause) yield break; // Прерываем корутину при паузе
                distance = Vector3.Distance(transform.position, flower.transform.position);
                if (distance > _centerThreshold)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        flower.transform.position,
                        _moveSpeed * _gameStateService.GameSpeed * Time.deltaTime
                    );
                    UpdateRope(flower);
                    // Rotate player to face flower
                    Vector3 direction = flower.transform.position - transform.position;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle - 90);
                }
                yield return null;
            } while (distance > _centerThreshold && _isGameActive && !_gameStateService.IsGamePause);

            if (_isGameActive && !_gameStateService.IsGamePause)
            {
                Destroy(_ropeObject);
                _ropeObject = null;
                _playerStickingService.FinishSticking();
                flower.CloseFlower();

                if (flower.GetComponent<SlimeView>())
                {
                    _gameScoreService.IncreaseScore(1);
                }
                else
                {
                    _gameScoreService.IncreaseScore(3);
                }

                _audioManager.PlaySoundEffect(AudioClipTypeId.CollectedPollen);
            }
        }

        private void UpdateRope(Flower flower)
        {
            if (_ropeObject == null || !_isGameActive || flower == null || _gameStateService.IsGamePause) return;

            Vector3 flowerPosition = flower.transform.position;
            Vector3 playerPosition = transform.position;
            _ropeObject.transform.position = (playerPosition + flowerPosition) / 2;
            float distance = Vector3.Distance(playerPosition, flowerPosition);
            _ropeSpriteRenderer.size = new Vector2(_ropeSpriteRenderer.size.x, distance);
            Vector3 direction = flowerPosition - playerPosition;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _ropeObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }

        private void HandleFinishSticking()
        {
            if (!_isGameActive || _gameStateService.IsGamePause) return;

            if (_ropeObject != null)
            {
                Destroy(_ropeObject);
                _ropeObject = null;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _isFalling = true;
            _targetFlower = null; // Сбрасываем цель
            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            _rb.linearVelocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;
            // Reset rotation to face upward when falling
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        private void HandleGameLose()
        {
            _rb.gameObject.SetActive(false);
            _isGameActive = false;
            _isFalling = false;
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            if (_ropeObject != null)
            {
                Destroy(_ropeObject);
                _ropeObject = null;
            }
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
            _targetFlower = null; // Сбрасываем цель
            // Reset rotation to face upward on game over
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        private void HandleGamePause()
        {
            _isFalling = false;
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            if (_ropeObject != null)
            {
                Destroy(_ropeObject);
                _ropeObject = null;
            }
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
            // Сохраняем текущую цель притягивания (если есть)
        }

        private void HandleGameResume()
        {
            if (!_isGameActive) return;

            _isFalling = true;
            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            if (_targetFlower != null)
            {
                // Возобновляем притягивание к цветку
                MoveToTarget(_targetFlower);
            }
            else
            {
                // Возобновляем падение
                if (_rb != null)
                {
                    _rb.linearVelocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;
                }
            }
        }
    }
}