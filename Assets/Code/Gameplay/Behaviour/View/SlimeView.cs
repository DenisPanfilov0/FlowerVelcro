using Code.Gameplay.Services.PlayerStickingService;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Gameplay.Behaviour.View
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class SlimeView : MonoBehaviour
    {
        [field: SerializeField] public Collider2D _slimeCollider2D { get; private set; }
        [SerializeField] private Rigidbody2D _rb;

        // Множитель скорости игры
        public float N = 1f;

        // Базовая скорость падения
        [SerializeField] private float baseFallSpeed = 5f;

        private IPlayerStickingService _playerStickingService;
        private bool _isFalling = true;
        private bool _isCollected;

        [Inject]
        public void Construct(IPlayerStickingService playerStickingService)
        {
            _playerStickingService = playerStickingService;
        }

        private void Awake()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody2D>();

            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.gravityScale = 0f;
        }

        private void OnEnable()
        {
            // Падаем с постоянной скоростью сразу при спавне
            _rb.velocity = Vector2.down * baseFallSpeed * N;
        }

        private void FixedUpdate()
        {
            if (_isFalling)
                _rb.velocity = Vector2.down * baseFallSpeed * N;
        }

        // Реакция на клик мышью или тап
        private void OnMouseDown()
        {
            if (!_isCollected)
            {
                _playerStickingService.SlimeClicked(this);
            }
        }

        public void CloseFlower()
        {
            GetComponent<SpriteRenderer>().color = Color.gray;
            _isCollected = true;
            // Destroy(gameObject); // временный дестрой в методе, в будущем хочу сделать анимационную замену
        }
    }
}