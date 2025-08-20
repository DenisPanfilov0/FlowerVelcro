using System.Collections;
using Code.Gameplay.Services.GameScoreService;
using UnityEngine;
using Zenject;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.PlayerFallingService;
using Code.Gameplay.Services.PlayerStickingService;

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

        private IPlayerFallingService _playerFallingService;
        private IPlayerStickingService _playerStickingService;
        private IGameStateService _gameStateService;
        private IGameScoreService _gameScoreService;
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

        [Inject]
        public void Construct(
            IPlayerStickingService playerStickingService,
            IPlayerFallingService playerFallingService,
            IGameStateService gameStateService,
            Camera mainCamera,
            IGameScoreService gameScoreService)
        {
            _gameScoreService = gameScoreService;
            _playerFallingService = playerFallingService;
            _playerStickingService = playerStickingService;
            _gameStateService = gameStateService;
            _mainCamera = mainCamera;
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
        }

        private void Start()
        {
            _playerStickingService.AddPlayer(this);
            _playerFallingService.AddPlayer(this);
            _playerStickingService.PlayerGlued += MoveToTarget;
            _playerStickingService.OnFinishSticking += HandleFinishSticking;
            _gameStateService.OnGameLose += HandleGameLose;
            UpdateScreenBounds();
            _lastOrthographicSize = _mainCamera.orthographicSize;
            _lastScreenResolution = new Vector2(Screen.width, Screen.height);
        }

        private void OnEnable()
        {
            if (_isGameActive)
            {
                N = _gameStateService.GameSpeed; // Update serialized field for Inspector
                _rb.velocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;
            }
        }

        private void OnDestroy()
        {
            _playerStickingService.PlayerGlued -= MoveToTarget;
            _playerStickingService.OnFinishSticking -= HandleFinishSticking;
            _gameStateService.OnGameLose -= HandleGameLose;
        }

        private void Update()
        {
            if (!_isGameActive) return;

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
            if (!_isGameActive || !_isFalling) return;

            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            _rb.velocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;

            float playerBottomY = transform.position.y - _playerHeight / 2f;
            if (playerBottomY <= _screenBottomY + _stopThreshold)
            {
                transform.position = new Vector3(transform.position.x, _screenBottomY + _playerHeight / 2f, transform.position.z);
                _rb.velocity = Vector2.zero;
            }
        }

        private void UpdateScreenBounds()
        {
            float cameraHeight = 2f * _mainCamera.orthographicSize;
            _screenBottomY = _mainCamera.transform.position.y - cameraHeight / 2f;
        }

        private void MoveToTarget(SlimeView slime)
        {
            if (!_isGameActive || slime == null) return;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _isFalling = false;
            _rb.velocity = Vector2.zero;
            CreateRope();
            _moveCoroutine = StartCoroutine(MoveTowards(slime));
        }

        private void CreateRope()
        {
            if (_ropeObject != null)
            {
                Destroy(_ropeObject);
            }

            if (!_isGameActive) return;

            _ropeObject = Instantiate(_ropePrefab, transform.position, Quaternion.identity);
            _ropeSpriteRenderer = _ropeObject.GetComponent<SpriteRenderer>();
            _ropeSpriteRenderer.size = new Vector2(_ropeSpriteRenderer.size.x, 1f);
        }

        private IEnumerator MoveTowards(SlimeView slime)
        {
            if (slime == null || !_isGameActive) yield break;

            float distance;
            do
            {
                distance = Vector3.Distance(transform.position, slime.transform.position);
                if (distance > _centerThreshold)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        slime.transform.position,
                        _moveSpeed * _gameStateService.GameSpeed * Time.deltaTime
                    );
                    UpdateRope(slime);
                }
                yield return null;
            } while (distance > _centerThreshold && _isGameActive);

            if (_isGameActive)
            {
                Destroy(_ropeObject);
                _playerStickingService.FinishSticking();
                slime.CloseFlower();
                _gameScoreService.IncreaseScore();
            }
        }

        private void UpdateRope(SlimeView slime)
        {
            if (_ropeObject == null || !_isGameActive || slime == null) return;

            Vector3 slimePosition = slime.transform.position;
            Vector3 playerPosition = transform.position;
            _ropeObject.transform.position = (playerPosition + slimePosition) / 2;
            float distance = Vector3.Distance(playerPosition, slimePosition);
            _ropeSpriteRenderer.size = new Vector2(_ropeSpriteRenderer.size.x, distance);
            Vector3 direction = slimePosition - playerPosition;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _ropeObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }

        private void HandleFinishSticking()
        {
            if (!_isGameActive) return;

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
            N = _gameStateService.GameSpeed; // Update serialized field for Inspector
            _rb.velocity = Vector2.down * _baseFallSpeed * _gameStateService.GameSpeed;
        }

        private void HandleGameLose()
        {
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
                _rb.velocity = Vector2.zero;
            }
        }
    }
}