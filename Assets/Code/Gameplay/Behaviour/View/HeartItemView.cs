using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.HeartService;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour.View
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class HeartItemView : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;

        // Множитель скорости игры
        public float N = 1f;

        // Базовая скорость падения
        [SerializeField] private float baseFallSpeed = 5f;

        private IHeartService _heartService;
        private IFallManagerService _fallManagerService;
        private bool _isFalling = true;

        [Inject]
        public void Construct(IHeartService heartService, IFallManagerService fallManagerService)
        {
            _fallManagerService = fallManagerService;
            _heartService = heartService;
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
            // Начинаем движение с постоянной скоростью
            _rb.velocity = Vector2.down * baseFallSpeed * N;
        }

        private void FixedUpdate()
        {
            if (_isFalling)
                _rb.velocity = Vector2.down * baseFallSpeed * N;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _heartService.IncreaseHeart();
                _fallManagerService.RemoveFallingObject(gameObject);
                Destroy(gameObject);
            }
        }
    }
}