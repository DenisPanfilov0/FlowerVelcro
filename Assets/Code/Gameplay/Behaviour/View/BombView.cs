using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.HeartService;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour.View
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class BombView : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;

        // Множитель скорости игры (меняется извне)
        public float N = 1f;

        private IHeartService _heartService;
        private IFallManagerService _fallManagerService;
        private bool _isFalling = true;

        // Базовая скорость падения (единиц в секунду)
        [SerializeField] private float baseFallSpeed = 5f;

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

            // Чтобы не пропускать коллизии
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Отключаем гравитацию, падение будет задаваться вручную
            _rb.gravityScale = 0f;
        }

        private void OnEnable()
        {
            // Задаём постоянную скорость падения сразу при появлении
            _rb.velocity = Vector2.down * baseFallSpeed * N;
        }

        private void FixedUpdate()
        {
            if (_isFalling)
            {
                // Поддерживаем постоянную скорость (без ускорения)
                _rb.velocity = Vector2.down * baseFallSpeed * N;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponent<PlayerView>())
            {
                _heartService.DecreaseHeart();
                _fallManagerService.RemoveFallingObject(gameObject);
                Destroy(gameObject);
            }
        }
    }
}
