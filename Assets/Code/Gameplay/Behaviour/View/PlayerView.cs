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
        [SerializeField] private float _speedMultiplier = 1f;
        [SerializeField] private float _baseFallSpeed = 5f;
        [SerializeField] private float _centerThreshold = 0.1f; // Погрешность для достижения центра слайма
        [SerializeField] private float _stopThreshold = 0.05f; // Погрешность для остановки у нижней границы

        private IPlayerFallingService _playerFallingService;
        private IPlayerStickingService _playerStickingService;
        private IGameStateService _gameStateService;
        private GameObject _ropeObject;
        private SpriteRenderer _ropeSpriteRenderer;
        private Coroutine _moveCoroutine;
        private Rigidbody2D _rb;
        private Camera _mainCamera;
        private bool _isFalling = true;
        private bool _isGameActive = true;
        public float N = 1f; // Множитель скорости игры
        private float _screenBottomY;
        private float _lastOrthographicSize;
        private Vector2 _lastScreenResolution;
        private float _playerHeight;
        private IGameScoreService _gameScoreService;

        [Inject]
        public void Construct(IPlayerStickingService playerStickingService, IPlayerFallingService playerFallingService, 
            IGameStateService gameStateService, Camera mainCamera, IGameScoreService gameScoreService)
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

            // Получаем высоту игрока из коллайдера
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
            // Сохраняем начальные значения для отслеживания изменений
            _lastOrthographicSize = _mainCamera.orthographicSize;
            _lastScreenResolution = new Vector2(Screen.width, Screen.height);
        }

        private void OnEnable()
        {
            if (_isGameActive)
            {
                // Задаём скорость падения при активации
                _rb.velocity = Vector2.down * _baseFallSpeed * N;
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

            // Проверяем, изменилось ли разрешение или размер камеры
            if (_mainCamera.orthographicSize != _lastOrthographicSize ||
                Screen.width != _lastScreenResolution.x ||
                Screen.height != _lastScreenResolution.y)
            {
                UpdateScreenBounds();
                _lastOrthographicSize = _mainCamera.orthographicSize;
                _lastScreenResolution = new Vector2(Screen.width, Screen.height);
            }
        }

        private void FixedUpdate()
        {
            if (!_isGameActive || !_isFalling) return;

            // Поддерживаем постоянную скорость падения
            _rb.velocity = Vector2.down * _baseFallSpeed * N;

            // Ограничиваем падение с учётом высоты игрока и погрешности
            float playerBottomY = transform.position.y - _playerHeight / 2f;
            if (playerBottomY <= _screenBottomY + _stopThreshold)
            {
                transform.position = new Vector3(transform.position.x, _screenBottomY + _playerHeight / 2f, transform.position.z);
                _rb.velocity = Vector2.zero;
            }
        }

        private void UpdateScreenBounds()
        {
            // Вычисляем нижнюю границу экрана в мировых координатах
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

            _isFalling = false; // Отключаем падение
            _rb.velocity = Vector2.zero; // Останавливаем физику

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

            // Не трогаем ширину, оставляем тайлинг по x как в префабе
            _ropeSpriteRenderer.size = new Vector2(_ropeSpriteRenderer.size.x, 1f); // Устанавливаем только начальную длину
        }

        private IEnumerator MoveTowards(SlimeView slime)
        {
            if (slime == null) yield break;

            while (_isGameActive && Vector3.Distance(transform.position, slime.transform.position) > _centerThreshold)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    slime.transform.position,
                    _moveSpeed * _speedMultiplier * Time.deltaTime
                );

                UpdateRope(slime);

                yield return null;
            }

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
            if (_ropeObject == null || !_isGameActive) return;

            Vector3 slimePosition = slime.transform.position;
            Vector3 playerPosition = transform.position;

            // Позиционируем верёвку в центре между игроком и слаймом
            _ropeObject.transform.position = (playerPosition + slimePosition) / 2;

            // Вычисляем расстояние между игроком и слаймом
            float distance = Vector3.Distance(playerPosition, slimePosition);

            // Обновляем только длину верёвки (y), ширина (x) остаётся как в префабе
            _ropeSpriteRenderer.size = new Vector2(_ropeSpriteRenderer.size.x, distance);

            // Поворачиваем верёвку в направлении от игрока к слайму
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

            _isFalling = true; // Возобновляем падение
            _rb.velocity = Vector2.down * _baseFallSpeed * N; // Возвращаем скорость падения
        }

        private void HandleGameLose()
        {
            _isGameActive = false;

            // Прерываем все движения и анимации
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

            _isFalling = false;
            _rb.velocity = Vector2.zero;
        }
    }
}