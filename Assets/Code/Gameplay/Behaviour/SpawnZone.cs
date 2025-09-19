using Code.Gameplay.Services.SpawnersServices;
using UnityEngine;
using YG;
using Zenject;

namespace Code.Gameplay.Behaviour
{
    public class SpawnZone : MonoBehaviour
    {
        private BoxCollider2D _collider;
        private Camera _camera;
        private ItemSpawnerService _itemSpawner;

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private bool _isFullscreen;

        private float _checkTimer; // таймер для проверки
        private const float CheckInterval = 0.1f; // раз в 0.1 секунды

        private const float ReferenceWidth = 1179f; // ширина референсного устройства
        private const float CorrectionFactor = 1.35f; // корректирующий коэффициент

        [Inject]
        public void Construct(ItemSpawnerService itemSpawner, Camera mainCamera)
        {
            _itemSpawner = itemSpawner;
            _itemSpawner.StartSpawn(transform);
            _camera = mainCamera;
        }

        private void Start()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;

            ApplyAdaptiveSettings();

            _isFullscreen = YG2.isFullscreen;
        }

        private void Update()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < CheckInterval)
                return;

            _checkTimer = 0f; // сброс таймера

            if (YG2.isFullscreen != _isFullscreen)
            {
                ApplyAdaptiveSettings();
                _isFullscreen = YG2.isFullscreen;
            }

#if UNITY_EDITOR
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                ApplyAdaptiveSettings();
            }
#endif
        }

        private void ApplyAdaptiveSettings()
        {
            AdjustCameraSize();
            UpdateColliderSize();

            _itemSpawner.SetSpawnZone(transform);

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void AdjustCameraSize()
        {
            float referenceOrthographicSize = _camera.orthographicSize;
            float currentWidth = Screen.width;

            float targetOrthographicSize =
                referenceOrthographicSize * (ReferenceWidth / currentWidth) * CorrectionFactor;

            _camera.orthographicSize = Mathf.Clamp(targetOrthographicSize, 20f, 40f);
        }

        private void UpdateColliderSize()
        {
            float height = 2f * _camera.orthographicSize;
            float width = height * _camera.aspect;
            _collider.size = new Vector2(width, height);
        }
    }
}
